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
    public class GradController : ControllerBase
    {
        private readonly IGraphClient _client;

        public GradController(IGraphClient client)
        {
            _client = client;
        }

        [HttpPost]
        [Route("DodajGrad/{naziv}/{troskovi}/{idDrzave}")]
        public async Task<ActionResult> DodajGrad(string naziv,int troskovi,string idDrzave)
        {
            Grad grad = new Grad();
            grad.Naziv=naziv;
            grad.Troskovi=troskovi;
            grad.Id=Guid.NewGuid().ToString();

            await _client.Cypher.Create("(g:Grad $g)")
                            .WithParam("g",grad)
                            .ExecuteWithoutResultsAsync();

            /*var lista = await _client.Cypher
                        .Match("(g:Grad)")
                        .Where("g.Naziv='"+naziv+"'")
                        .Return(grad => grad.As<Grad>())
                        .ResultsAsync;
            var gradId=lista.FirstOrDefault();*/

            await _client.Cypher.Match("(gr:Grad),(dr:Drzava)")
                            .Where((Grad gr ,Drzava dr)=> gr.Id==grad.Id && dr.Id==idDrzave)
                            .Create("(gr)-[r:seNalazi]->(dr)")
                            .ExecuteWithoutResultsAsync();
            return Ok("Ok");
        }

       /* [HttpPost]
        [Route("DodajGradDrzavi/{idGrada}/{idDrzave}")]
         public async Task<ActionResult> DodajGradDrzavi(int idGrada, int idDrzave)
         {
            _client.Cypher.Match("(gr:Grad),(dr:Drzava)")
                            .Where((Grad gr ,Drzava dr)=> gr.Id==idGrada&& dr.Id==idDrzave)
                            .Create("(gr)-[r:seNalazi]->(dr)")
                            .ExecuteWithoutResultsAsync();
         }*/
        [HttpGet]
        [Route("PreuzmiGrad/{naziv}")]
        public async Task<ActionResult> PreuzmiGrad(string naziv)
        {
            var grad = await _client.Cypher
                        .Match("(g:Grad)")
                        .Where((Grad g)=>g.Naziv==naziv)
                        .Return(g => g.As<Grad>())
                        .ResultsAsync;

            var zaVracanje= grad.FirstOrDefault();
            if(zaVracanje!=null)
            {
                return Ok(zaVracanje);
            }
            else
            {
                return BadRequest("Nema grada");
            }
            
        }
        [HttpGet]
        [Route("PreuzmiSveGradoveDrzave/{idDrzave}")]
        public async Task<ActionResult> PreuzmiSveGradoveDrzave(string idDrzave)
        {
            var gradovi = await _client.Cypher.Match("(gr:Grad)-[r:seNalazi]->(dr:Drzava)")
                                .Where((Drzava dr)=> dr.Id==idDrzave)
                                .Return(gr => gr.As<Grad>())
                                .ResultsAsync;
        
            return Ok(gradovi);
        }
    }
}
