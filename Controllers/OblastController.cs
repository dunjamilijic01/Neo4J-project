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
    public class OblastController : ControllerBase
    {
        private readonly IGraphClient _client;

        public OblastController(IGraphClient client)
        {
            _client = client;
        }
        [HttpPost]
        [Route("DodajOblast/{naziv}")]
        public async Task<ActionResult> DodajOblast(string naziv)
        {
            Oblast o = new Oblast();
            o.Id = Guid.NewGuid().ToString();
            o.Naziv = naziv;

            var oblast = await _client.Cypher.Match("(o:Oblast)")
                                            .Where((Oblast o)=>o.Naziv==naziv)
                                            .Return(o => o.As<Oblast>())
                                            .ResultsAsync;
            if(oblast.FirstOrDefault()==null)
            {
                await _client.Cypher.Create("(o:Oblast $o)")
                            .WithParam("o",o)
                            .ExecuteWithoutResultsAsync();
            
                return Ok("Dodata Oblast");
            }
            else
            {
                return BadRequest("Oblast sa tim imenom vec postoji");
            }
            

        }
        [HttpGet]
        [Route("PreuzmiSveOblasti")]
        public async Task<ActionResult> PreuzmiSveOblasti()
        {
            var oblasti = await _client.Cypher.Match("(o:Oblast)")
                                            .Return(o => o.As<Oblast>())
                                            .ResultsAsync;
            return Ok(oblasti);
        }
        [HttpGet]
        [Route("PreuzmiOblast/{idOblasti}")]
        public async Task<ActionResult> PreuzmiOblast(string idOblasti)
        {
            var oblast= await _client.Cypher.Match("(o:Oblast)")
                                .Where((Oblast o)=>o.Id==idOblasti)
                                .Return(o => o.As<Oblast>())
                                            .ResultsAsync;
            return Ok(oblast.FirstOrDefault());
        }
    }
}