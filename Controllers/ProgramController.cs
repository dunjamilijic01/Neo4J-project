using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Models;
using Neo4jClient;
using Neo4jClient.Cypher;
using Newtonsoft.Json;
using System.Text.Json;

namespace NeoProba.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProgramController : ControllerBase
    {
        private readonly IGraphClient _client;

        public ProgramController(IGraphClient client)
        {
            _client = client;
        }
        [HttpPost]
        [Route("DodajProgram/{naziv}/{trajanje}/{brojMesta}/{nivoStudija}/{opis}/{jezik}/{idUniverziteta}/{oblasti}")]
        public async Task<ActionResult> DodajProgram(string naziv,int trajanje,int brojMesta,string nivoStudija,string opis,string jezik,string idUniverziteta,string oblasti)
        {
            Program p = new Program();
            p.Id = Guid.NewGuid().ToString();
            p.Naziv = naziv;
            p.Trajanje = trajanje;
            p.BrojMesta = brojMesta;
            p.NivoStudija = nivoStudija;
            p.Opis = opis;
            p.Jezik = jezik;
            
            var uni= await _client.Cypher.Match("(u:Univerzitet)")
                                        .Where((Univerzitet u)=>u.Id==idUniverziteta)
                                        .Return(u=>u.As<Univerzitet>())
                                        .ResultsAsync;
            p.Univerzitet=uni.FirstOrDefault().Naziv;

             await _client.Cypher.Create("(p:Program $p)")
                                .WithParam("p",p)
                                .ExecuteWithoutResultsAsync();
            
            string[] pojedinacniID = oblasti.Split("#");
            foreach(string s in pojedinacniID)
            {
                await _client.Cypher.Match("(pr:Program),(o:Oblast)")
                                    .Where((Program pr, Oblast o)=> o.Id==s && pr.Id==p.Id)
                                    .Create("(pr)-[r:Obuhvata]->(o)")
                                    .Create("(pr)<-[r1:PripadaProgramu]-(o)")
                                    .ExecuteWithoutResultsAsync();
            }
            await _client.Cypher.Match("(u:Univerzitet),(pr:Program)")
                                .Where((Univerzitet u,Program pr)=> u.Id==idUniverziteta && pr.Id==p.Id)
                                .Create("(u)-[r:Sadrzi]->(pr)")
                                .ExecuteWithoutResultsAsync();

            return Ok();
        }
        [HttpGet]
        [Route("VratiProgram/{idPrograma}")]
        public async Task<ActionResult> VratiProgram(string idPrograma)
        {
            var program = await _client.Cypher.Match("(p:Program)")
                                        .Where((Program p)=> p.Id==idPrograma)
                                        .Return(p => p.As<Program>())
                                        .ResultsAsync;
            if(program!=null)
            {
                return Ok(program.FirstOrDefault());
            }
            else
            {
                return BadRequest("Ne postoji program");
            }
        }
        [HttpGet]
        [Route("VratiSveJezike")]
        public async Task<ActionResult> VratiSveJezike()
        {
            var jezici = await _client.Cypher.Match("(p:Program)")
                                        .ReturnDistinct(p => p.As<Program>().Jezik)
                                        .ResultsAsync;

            return Ok(jezici);
        }
        [HttpGet]
        [Route("PefectMatch/{idUniverziteta}/{idPrograma}/{jezik}/{idSertifikata}/{troskoviZivota}/{troskoviSkolarine}")]
        public async Task<ActionResult> PefectMatch(string idUniverziteta,string idPrograma,string jezik,string idSertifikata,string troskoviZivota,string troskoviSkolarine)
        {
            int tz = int.Parse(troskoviZivota);
            int ts = int.Parse(troskoviSkolarine);

            var mojeOblasti = await _client.Cypher.Match("(o:Oblast)-[r:pripadaProgramu]->(p:Program)")
                                                    .Where((Program p) => p.Id==idPrograma)
                                                    .Return(o => o.As<Oblast>().Id)
                                                    .ResultsAsync;
            var masterProgrami = await _client.Cypher.Match("(p:Program)")
                                                    .Where((Program p) => p.NivoStudija=="Master")
                                                    .Return(p => p.As<Program>())
                                                    .ResultsAsync;
            foreach (var mp in masterProgrami)
            {
                float jezikUdeo,tzUdeo,tsUdeo,oblastiUdeo=0;
                var sertifikati = await _client.Cypher.Match("(s:Sertifikat)<-[r1:Podrzava]-(u:Univerzitet)-[r2:Sadrzi]->(p:Program)")
                                                        .Where((Program p) => p.Id==mp.Id)
                                                        .Return(s => s.As<Sertifikat>().Id)
                                                        .ResultsAsync;
                var grad = await _client.Cypher.Match("(p:Program)<-[r1:Sadrzi]-(u:Univerzitet)-[r2:Pripada]->(g:Grad)")
                                                .Where((Program p) => p.Id==mp.Id)
                                                .Return(g => g.As<Grad>())
                                                .ResultsAsync;
                var univerzitet = await _client.Cypher.Match("(u:Univerzitet)-[r1:Sadrzi]->(p:Program)")
                                                        .Where((Program p) => p.Id==mp.Id)
                                                        .Return(u=> u.As<Univerzitet>())
                                                        .ResultsAsync;
                var oblasti = await _client.Cypher.Match("(p:Program)-[r1:Obuhvata]->(o:Oblast)")
                                                    .Where((Program p) => p.Id==mp.Id)
                                                    .Return(o => o.As<Oblast>().Id)
                                                    .ResultsAsync;

                if(mp.Jezik==jezik)
                {
                    if(idSertifikata=="Maternji" || sertifikati.Contains(idSertifikata))
                    {
                        jezikUdeo=100;
                    }
                    else
                    {
                        jezikUdeo =0;
                    }
                }
                else
                {
                    jezikUdeo = 0;
                }

                //troskovi zivota
                if(grad.FirstOrDefault().Troskovi<=tz)
                {
                    tzUdeo = 100;
                }
                else
                {
                    int raz = grad.FirstOrDefault().Troskovi-tz;
                    int br = raz%50;
                    tzUdeo = 100- br*1;
                }
                //troskovi skolarine
                if(univerzitet.FirstOrDefault().Skolarina>=ts)
                {
                    tsUdeo = 100;
                }
                else
                {
                    int raz = univerzitet.FirstOrDefault().Skolarina-tz;
                    int br = raz%50;
                    tsUdeo = 100- br*1;
                }
                //oblasti udeo
                int brOblasti = oblasti.Count();
                foreach (var o in oblasti)
                {
                    if(mojeOblasti.Contains(o))
                    {
                        oblastiUdeo+=100/brOblasti;
                    }
                }
                //
                double ukupno = jezikUdeo*0.5+oblastiUdeo*0.3+(tzUdeo+tsUdeo)*0.2;
                mp.Procenat=ukupno;
            }
            return Ok(masterProgrami.OrderByDescending(x => x.Procenat));
        }
        [HttpGet]
        [Route("VratiProgrameUniverziteta/{idUniverziteta}")]
        public async Task<ActionResult> VratiProgrameUniverziteta(string idUniverziteta)
        {
            var programi = await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)")
                                                    .Where((Univerzitet u)=> u.Id==idUniverziteta)
                                                    .Return(p => p.As<Program>())
                                                    .ResultsAsync;
            return Ok(programi);
        }
        [HttpGet]
        [Route("VratiSvePrograme/{drzavaId}/{gradId}/{uniId}/{nivo}/{listaOblasti}")]
        public async Task<ActionResult> VratiSvePrograme(string drzavaId,string gradId,string uniId,string nivo,string listaOblasti)
        {
            if (listaOblasti=="nema")
            {
                if(String.IsNullOrWhiteSpace(nivo))
                {
                    if(String.IsNullOrWhiteSpace(drzavaId))
                    {
                        return BadRequest("Morate uneti neki parametar za pretragu!");
                    }
                    else
                    {
                         if(String.IsNullOrWhiteSpace(gradId))// preetraga samo po drzavi
                        {
                            var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                        .Where((Drzava d)=>d.Id==drzavaId)
                                                        .Return((p)=>p.As<Program>())
                                                        .ResultsAsync;
                                
                            
                            if(res.Count()!=0)
                            {
                                 return Ok(res.Select(r=>
                                    new{
                                        id=r.Id,
                                        naziv=r.Naziv,
                                        trajanje=r.Trajanje,
                                        brojMesta=r.BrojMesta,
                                        nivoStudija=r.NivoStudija,
                                        opis=r.Opis,
                                        jezik=r.Jezik,
                                        univerzitet=r.Univerzitet
                                        }));
                            }
                            else
                            {
                                return BadRequest("Nema rezultata pretrage");
                            }
                           
                        }
                        else
                        {
                            if(String.IsNullOrWhiteSpace(uniId))// grad i drzava
                                {
                                    var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                            .Where((Drzava d,Grad g)=>d.Id==drzavaId && g.Id==gradId)
                                                            .Return((p)=>p.As<Program>())
                                                                .ResultsAsync;
                                    if(res.Count()!=0)
                                    {
                                        return Ok(res.Select(r=>
                                            new{
                                                id=r.Id,
                                                naziv=r.Naziv,
                                                trajanje=r.Trajanje,
                                                brojMesta=r.BrojMesta,
                                                nivoStudija=r.NivoStudija,
                                                opis=r.Opis,
                                                jezik=r.Jezik,
                                                univerzitet=r.Univerzitet
                                            }));
                                    }
                                    else
                                    {
                                        return BadRequest("Nema rezultata pretrage");
                                    }
                                }
                            else
                            {
                                var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                    .Where((Drzava d,Grad g,Univerzitet u)=>d.Id==drzavaId && g.Id==gradId && u.Id==uniId)
                                                    .Return((p)=>p.As<Program>())
                                                    .ResultsAsync;
                                if(res.Count()!=0)
                                {
                                    return Ok(res.Select(r=>
                                        new{
                                                id=r.Id,
                                                naziv=r.Naziv,
                                                trajanje=r.Trajanje,
                                                brojMesta=r.BrojMesta,
                                                nivoStudija=r.NivoStudija,
                                                opis=r.Opis,
                                                jezik=r.Jezik,
                                                univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                        }

                    }
                }
                else
                {
                    if(String.IsNullOrWhiteSpace(drzavaId))
                    {
                        var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                    .Where((Program p)=>p.NivoStudija==nivo)
                                                    .Return((p)=>p.As<Program>())
                                                    .ResultsAsync;
                                if(res.Count()!=0)
                                {
                                    return Ok(res.Select(r=>
                                        new{
                                               id=r.Id,
                                                naziv=r.Naziv,
                                                trajanje=r.Trajanje,
                                                brojMesta=r.BrojMesta,
                                                nivoStudija=r.NivoStudija,
                                                opis=r.Opis,
                                                jezik=r.Jezik,
                                                univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                    }
                    else
                    {
                        if(String.IsNullOrWhiteSpace(gradId))
                        {
                            var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                    .Where((Drzava d,Program p)=>d.Id==drzavaId && p.NivoStudija==nivo)
                                                    .Return((p)=>p.As<Program>())
                                                    .ResultsAsync;
                                if(res.Count()!=0)
                                {
                                    return Ok(res.Select(r=>
                                        new{
                                               id=r.Id,
                                                naziv=r.Naziv,
                                                trajanje=r.Trajanje,
                                                brojMesta=r.BrojMesta,
                                                nivoStudija=r.NivoStudija,
                                                opis=r.Opis,
                                                jezik=r.Jezik,
                                                univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                        }
                        else
                        {
                            if(String.IsNullOrWhiteSpace(uniId))
                            {
                                var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                    .Where((Drzava d,Grad g,Program p)=>d.Id==drzavaId && g.Id==gradId && p.NivoStudija==nivo)
                                                    .Return((p)=>p.As<Program>())
                                                    .ResultsAsync;
                                if(res.Count()!=0)
                                {
                                    return Ok(res.Select(r=>
                                        new{
                                                id=r.Id,
                                                naziv=r.Naziv,
                                                trajanje=r.Trajanje,
                                                brojMesta=r.BrojMesta,
                                                nivoStudija=r.NivoStudija,
                                                opis=r.Opis,
                                                jezik=r.Jezik,
                                                univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                            else
                            {
                                var res= await _client.Cypher.Match("(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                    .Where((Drzava d,Grad g,Univerzitet u,Program p)=>d.Id==drzavaId && g.Id==gradId && u.Id==uniId && p.NivoStudija==nivo)
                                                    .Return((p)=>p.As<Program>())
                                                    .ResultsAsync;
                                if(res.Count()!=0)
                                {
                                    return Ok(res.Select(r=>
                                        new{
                                                id=r.Id,
                                                naziv=r.Naziv,
                                                trajanje=r.Trajanje,
                                                brojMesta=r.BrojMesta,
                                                nivoStudija=r.NivoStudija,
                                                opis=r.Opis,
                                                jezik=r.Jezik,
                                                univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if(String.IsNullOrWhiteSpace(nivo))
                {
                    if(String.IsNullOrWhiteSpace(drzavaId))
                    {
                        string[] pojedinacniID = listaOblasti.Split("#");
                        List<string> programi= new List<string>();
                        List<Program> deserializedProg= new List<Program>();

                        foreach(string s in pojedinacniID)
                        {
                            var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                        .Where((Oblast o)=>o.Id==s )
                                                        .Return(p=>p.As<Program>())
                                                        .ResultsAsync;
                            foreach (var r in res)
                                if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                    {
                                        programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                        deserializedProg.Add(r);
                                    }
                        }
                        
                        if(deserializedProg.Count()!=0)
                        {
                            return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet                                
                                        }));
                        }
                    
                        else
                        {
                            return BadRequest("Nema rezultata pretrage");
                        }
                        
                    }
                    else
                    {
                        if(String.IsNullOrWhiteSpace(gradId))
                        {
                            string[] pojedinacniID = listaOblasti.Split("#");
                            List<string> programi= new List<string>();
                            List<Program> deserializedProg= new List<Program>();

                            foreach(string s in pojedinacniID)
                            {
                                var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                            .Where((Oblast o,Drzava d)=>o.Id==s && d.Id==drzavaId)
                                                            .Return(p=>p.As<Program>())
                                                            .ResultsAsync;
                                foreach (var r in res)
                                    if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                        {
                                            programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                            deserializedProg.Add(r);
                                        }
                            }
                    
                            if(deserializedProg.Count()!=0)
                            {
                                 return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                            }
                            else
                            {
                                return BadRequest("Nema rezultata pretrage");
                            }
                        }
                        else
                        {
                            if(String.IsNullOrWhiteSpace(uniId))
                            {
                                string[] pojedinacniID = listaOblasti.Split("#");
                                List<string> programi= new List<string>();
                                List<Program> deserializedProg= new List<Program>();

                                foreach(string s in pojedinacniID)
                                {
                                    var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                                .Where((Oblast o,Drzava d,Grad g)=>o.Id==s&&d.Id==drzavaId&&g.Id==gradId)
                                                                .Return(p=>p.As<Program>())
                                                                .ResultsAsync;
                                    foreach (var r in res)
                                        if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                            {
                                                programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                                deserializedProg.Add(r);
                                            }
                                }
                                
                                if(deserializedProg.Count()!=0)
                                {
                                     return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                            else
                            {
                                string[] pojedinacniID = listaOblasti.Split("#");
                                List<string> programi= new List<string>();
                                List<Program> deserializedProg= new List<Program>();

                                foreach(string s in pojedinacniID)
                                {
                                    var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                                .Where((Oblast o,Drzava d,Grad g,Univerzitet u)=>o.Id==s&&d.Id==drzavaId&&g.Id==gradId&&u.Id==uniId)
                                                                .Return(p=>p.As<Program>())
                                                                .ResultsAsync;
                                    foreach (var r in res)
                                        if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                            {
                                                programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                                deserializedProg.Add(r);
                                            }
                                }
                                
                                if(deserializedProg.Count()!=0)
                                {
                                     return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if(String.IsNullOrWhiteSpace(drzavaId))
                    {
                        string[] pojedinacniID = listaOblasti.Split("#");
                        List<string> programi= new List<string>();
                        List<Program> deserializedProg= new List<Program>();

                        foreach(string s in pojedinacniID)
                        {
                            var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                        .Where((Oblast o,Program p)=>o.Id==s&&p.NivoStudija==nivo)
                                                        .Return(p=>p.As<Program>())
                                                        .ResultsAsync;
                            foreach (var r in res)
                                if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                    {
                                        programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                        deserializedProg.Add(r);
                                    }
                        }
                    
                       if(deserializedProg.Count()!=0)
                        {
                             return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                        }
                        else
                        {
                            return BadRequest("Nema rezultata pretrage");
                        }
                    }
                    else
                    {
                        if(String.IsNullOrWhiteSpace(gradId))
                        {
                            string[] pojedinacniID = listaOblasti.Split("#");
                            List<string> programi= new List<string>();
                            List<Program> deserializedProg= new List<Program>();

                            foreach(string s in pojedinacniID)
                            {
                                var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                            .Where((Oblast o,Program p,Drzava d)=>d.Id==drzavaId&& o.Id==s&&p.NivoStudija==nivo)
                                                            .Return(p=>p.As<Program>())
                                                            .ResultsAsync;
                                foreach (var r in res)
                                    if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                        {
                                            programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                            deserializedProg.Add(r);
                                        }
                            }
                        
                            if(deserializedProg.Count()!=0)
                            {
                                 return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                            }
                            else
                            {
                                return BadRequest("Nema rezultata pretrage");
                            }
                        }
                        else
                        {
                            if(String.IsNullOrWhiteSpace(uniId))
                            {
                                string[] pojedinacniID = listaOblasti.Split("#");
                                List<string> programi= new List<string>();
                                List<Program> deserializedProg= new List<Program>();

                                foreach(string s in pojedinacniID)
                                {
                                    var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                                .Where((Oblast o,Program p,Drzava d,Grad g)=>d.Id==drzavaId&& o.Id==s&&p.NivoStudija==nivo&&g.Id==gradId)
                                                                .Return(p=>p.As<Program>())
                                                                .ResultsAsync;
                                    foreach (var r in res)
                                        if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                            {
                                                programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                                deserializedProg.Add(r);
                                            }
                                }
                            
                                if(deserializedProg.Count()!=0)
                                {
                                     return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                            else
                            {
                                string[] pojedinacniID = listaOblasti.Split("#");
                                List<string> programi= new List<string>();
                                List<Program> deserializedProg= new List<Program>();

                                foreach(string s in pojedinacniID)
                                {
                                    var res= await _client.Cypher.Match("(o:Oblast)-[r5:PripadaProgramu]->(p:Program)<-[r:Sadrzi]-(u:Univerzitet)-[r1:Pripada]->(g:Grad)-[r2:seNalazi]->(d:Drzava)")
                                                                .Where((Oblast o,Program p,Drzava d,Grad g,Univerzitet u)=>d.Id==drzavaId&& o.Id==s&&p.NivoStudija==nivo&& g.Id==gradId&&u.Id==uniId)
                                                                .Return(p=>p.As<Program>())
                                                                .ResultsAsync;
                                    foreach (var r in res)
                                        if(!programi.Contains(System.Text.Json.JsonSerializer.Serialize<Program>(r)))
                                            {
                                                programi.Add(System.Text.Json.JsonSerializer.Serialize<Program>(r));
                                                deserializedProg.Add(r);
                                            }
                                }
                            
                                if(deserializedProg.Count()!=0)
                                {
                                     return Ok(deserializedProg.Select(r=>
                                        new{
                                            id=r.Id,
                                            naziv=r.Naziv,
                                            trajanje=r.Trajanje,
                                            brojMesta=r.BrojMesta,
                                            nivoStudija=r.NivoStudija,
                                            opis=r.Opis,
                                            jezik=r.Jezik,
                                            univerzitet=r.Univerzitet
                                        }));
                                }
                                else
                                {
                                    return BadRequest("Nema rezultata pretrage");
                                }
                            }
                        }
                    }
                }
            }
        
        }
    }
}