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
    public class StudentskiDomController : ControllerBase
    {
        private readonly IGraphClient _client;

        public StudentskiDomController(IGraphClient client)
        {
            _client = client;
        }
        [HttpPost]
        [Route("DodajDom/{naziv}/{cena}/{ocena}/{lokacija}/{idGrada}")]
        public async Task<ActionResult> DodajGrad(string naziv,int cena,float ocena,string lokacija,string idGrada)
        {
            StudentskiDom sd = new StudentskiDom();
            
            sd.Id = Guid.NewGuid().ToString();
            sd.Naziv = naziv;
            sd.Cena = cena;
            sd.Ocena = ocena;
            sd.Lokacija = lokacija;
            
            await _client.Cypher.Create("(sd:StudentskiDom $sd)")
                            .WithParam("sd",sd)
                            .ExecuteWithoutResultsAsync();


            await _client.Cypher.Match("(gr:Grad),(s:StudentskiDom)")
                            .Where((Grad gr ,StudentskiDom s)=> gr.Id==idGrada && s.Id==sd.Id)
                            .Create("(s)-[r:stacioniranU]->(gr)")
                            .ExecuteWithoutResultsAsync();
            return Ok("Ok");
        }

        [HttpGet]
        [Route("PreuzmiDom/{idDoma}")]
        public async Task<ActionResult> PreuzmiDom(string idDoma)
        {
            var d= await _client.Cypher
                                .Match("(d:StudentskiDom)")
                                .Where((StudentskiDom d)=>d.Id==idDoma)
                                .Return(d=>d.As<StudentskiDom>()).ResultsAsync;
            if(d.FirstOrDefault()!=null)
            {
                return Ok(d.FirstOrDefault());
            }
            else
            {
                return BadRequest("Ne postoji dom");
            }
        }
        [HttpGet]
        [Route("PreuzmiSveDomove/{idDrzave}/{idGrada}/{maxCena}/{minOcena}")]
        public async Task<ActionResult> PreuzmiSveDomove(string idDrzave,string idGrada,string maxCena,string minOcena)
        {
            if(String.IsNullOrWhiteSpace(idDrzave))
            {
                return BadRequest("Morate uneti drzavu");
            }
            else
            {
                if(String.IsNullOrWhiteSpace(idGrada))
                {
                    if(maxCena=="nema")
                    {
                        if(minOcena=="nema")
                        {
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d)=>d.Id==idDrzave)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                        else
                        {
                            float oc=float.Parse(minOcena);
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,StudentskiDom s)=>d.Id==idDrzave && s.Ocena>=oc)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                    }
                    else
                    {
                        if(minOcena=="nema")
                        {
                            int ce=int.Parse(maxCena);
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,StudentskiDom s)=>d.Id==idDrzave && s.Cena<=ce)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                        else
                        {
                            int ce= int.Parse(maxCena);
                            float oc=float.Parse(minOcena);
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,StudentskiDom s)=>d.Id==idDrzave && s.Ocena>=oc&& s.Cena<=ce)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                    }
                }
                else
                {
                    if(maxCena=="nema")
                    {
                        if(minOcena=="nema")
                        {
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,Grad g)=>d.Id==idDrzave && g.Id==idGrada)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                        else
                        {
                            float oc=float.Parse(minOcena);
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,StudentskiDom s,Grad g)=>d.Id==idDrzave && s.Ocena>=oc && g.Id==idGrada)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                    }
                    else
                    {
                        if(minOcena=="nema")
                        {
                            int ce=int.Parse(maxCena);
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,StudentskiDom s,Grad g)=>d.Id==idDrzave && s.Cena<=ce &&g.Id==idGrada)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                        else
                        {
                            float oc=float.Parse(minOcena);
                            int ce=int.Parse(maxCena);
                            var res=await _client.Cypher.Match("(d:Drzava)<-[r:seNalazi]-(g:Grad)<-[r2:stacioniranU]-(s:StudentskiDom)")
                                                        .Where((Drzava d,StudentskiDom s,Grad g)=>d.Id==idDrzave && s.Ocena>=oc && g.Id==idGrada && s.Cena<=ce)
                                                        .Return(s=>s.As<StudentskiDom>())
                                                        .ResultsAsync;
                            if(res.Count()!=0)
                            {
                                return Ok(res);
                            }  
                            else
                            {
                                return BadRequest("nema rezultata pretrage");
                            }                 
                        }
                    }
                }
            }
        }

    }
}