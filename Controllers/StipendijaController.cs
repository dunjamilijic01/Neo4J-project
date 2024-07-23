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

namespace NeoProba.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StipendijaController : ControllerBase
    {
        private readonly IGraphClient _client;

        public StipendijaController(IGraphClient client)
        {
            _client = client;
        }
        [HttpPost]
        [Route("DodajStipendiju/{naziv}/{opis}/{uslovi}/{iznos}/{kolicina}/{idUniverziteta}")]
        public async Task<ActionResult> DodajStipendiju(string naziv,string opis,string uslovi,int iznos,int kolicina,string idUniverziteta)
        {
            Stipendija stip = new Stipendija();
            stip.Id = Guid.NewGuid().ToString();
            stip.Naziv = naziv;
            stip.Opis = opis;
            stip.Uslovi = uslovi;
            stip.Iznos = iznos;
            stip.Kolicina = kolicina;

            await _client.Cypher.Create("(s:Stipendija $s)")
                                .WithParam("s",stip)
                                .ExecuteWithoutResultsAsync();
            
            await _client.Cypher.Match("(s:Stipendija),(u:Univerzitet)")
                                .Where((Stipendija s,Univerzitet u) => u.Id==idUniverziteta && s.Id==stip.Id)
                                .Create("(u)-[r:Daje]->(s)")
                                .ExecuteWithoutResultsAsync();
            return Ok("Dodata stipendija");
        }
        [HttpGet]
        [Route("VratiSveStipendije/{idDrzave}/{idGrada}/{idUniverziteta}/{minIznos}")]
        public async Task<ActionResult> VratiSveStipendije(string idDrzave,string idGrada,string idUniverziteta,string minIznos)
        {
            
                if(String.IsNullOrWhiteSpace(idDrzave))
                {
                    return BadRequest("Morate uneti drzavu");
                }
                else
                {
                    if(minIznos=="nema")
                    {
                        if(String.IsNullOrWhiteSpace(idGrada))
                    {
                        var stipendije = await _client.Cypher.Match("(d:Drzava)<-[r1:seNalazi]-(g:Grad)<-[r2:Pripada]-(u:Univerzitet)-[r3:Daje]->(s:Stipendija)")
                                                    .Where((Drzava d) => d.Id==idDrzave)
                                                    .Return(s=> s.As<Stipendija>())
                                                    .ResultsAsync;
                        if(stipendije.Count()!=0)
                        {
                            return Ok(stipendije);
                        }
                        else
                        {
                            return BadRequest("Nema rezultata pretrage");
                        }
                    }
                    else
                    {
                        if(String.IsNullOrWhiteSpace(idUniverziteta))
                        {
                            var stipendije = await _client.Cypher.Match("(d:Drzava)<-[r1:seNalazi]-(g:Grad)<-[r2:Pripada]-(u:Univerzitet)-[r3:Daje]->(s:Stipendija)")
                                                    .Where((Drzava d,Grad g) => d.Id==idDrzave && g.Id==idGrada)
                                                    .Return(s=> s.As<Stipendija>())
                                                    .ResultsAsync;
                            if(stipendije.Count()!=0)
                            {
                                return Ok(stipendije);
                            }
                            else
                            {
                                return BadRequest("Nema rezultata pretrage");
                            }
                        }
                        else
                        {
                            var stipendije = await _client.Cypher.Match("(d:Drzava)<-[r1:seNalazi]-(g:Grad)<-[r2:Pripada]-(u:Univerzitet)-[r3:Daje]->(s:Stipendija)")
                                                    .Where((Drzava d,Grad g,Univerzitet u) => d.Id==idDrzave && g.Id==idGrada && u.Id==idUniverziteta)
                                                    .Return(s=> s.As<Stipendija>())
                                                    .ResultsAsync;
                            if(stipendije.Count()!=0)
                            {
                                return Ok(stipendije);
                            }
                            else
                            {
                                return BadRequest("Nema rezultata pretrage");
                            }
                        
                        }
                    }
                    }
                    else
                    {
                         int iznos = Int32.Parse(minIznos);
                         if(String.IsNullOrWhiteSpace(idGrada))
                    {
                        var stipendije = await _client.Cypher.Match("(d:Drzava)<-[r1:seNalazi]-(g:Grad)<-[r2:Pripada]-(u:Univerzitet)-[r3:Daje]->(s:Stipendija)")
                                                    .Where((Stipendija s,Drzava d) => s.Iznos>=iznos && d.Id==idDrzave)
                                                    .Return(s=> s.As<Stipendija>())
                                                    .ResultsAsync;
                        if(stipendije.Count()!=0)
                        {
                            return Ok(stipendije);
                        }
                        else
                        {
                            return BadRequest("Nema rezultata pretrage");
                        }
                    }
                    else
                    {
                        if(String.IsNullOrWhiteSpace(idUniverziteta))
                        {
                            var stipendije = await _client.Cypher.Match("(d:Drzava)<-[r1:seNalazi]-(g:Grad)<-[r2:Pripada]-(u:Univerzitet)-[r3:Daje]->(s:Stipendija)")
                                                    .Where((Stipendija s,Drzava d,Grad g) => s.Iznos>=iznos && d.Id==idDrzave && g.Id==idGrada)
                                                    .Return(s=> s.As<Stipendija>())
                                                    .ResultsAsync;
                            if(stipendije.Count()!=0)
                            {
                                return Ok(stipendije);
                            }
                            else
                            {
                                return BadRequest("Nema rezultata pretrage");
                            }
                        }
                        else
                        {
                            var stipendije = await _client.Cypher.Match("(d:Drzava)<-[r1:seNalazi]-(g:Grad)<-[r2:Pripada]-(u:Univerzitet)-[r3:Daje]->(s:Stipendija)")
                                                    .Where((Stipendija s,Drzava d,Grad g,Univerzitet u) => s.Iznos>=iznos && d.Id==idDrzave && g.Id==idGrada && u.Id==idUniverziteta)
                                                    .Return(s=> s.As<Stipendija>())
                                                    .ResultsAsync;
                            if(stipendije.Count()!=0)
                            {
                                return Ok(stipendije);
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