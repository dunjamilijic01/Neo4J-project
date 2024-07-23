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
    public class SertifikatController : ControllerBase
    {
        private readonly IGraphClient _client;

        public SertifikatController(IGraphClient client)
        {
            _client = client;
        }

        [HttpPost]
        [Route("DodajSertifikat/{naziv}/{uniId}")]
        public async Task<ActionResult> DodajSertifikat(string naziv,string uniId)
        {
            Sertifikat sert = new Sertifikat();
            sert.Id = Guid.NewGuid().ToString();
            sert.Naziv = naziv;

            await _client.Cypher.Create("(s:Sertifikat $s)")
                                .WithParam("s",sert)
                                .ExecuteWithoutResultsAsync();

             await _client.Cypher.Match("(s:Sertifikat),(u:Univerzitet)")
                                .Where((Univerzitet u,Sertifikat s)=> u.Id==uniId && s.Id==sert.Id)
                                .Create("(u)-[r:Podrzava]->(s)")
                                .ExecuteWithoutResultsAsync();

            return Ok("Dodat sertifikat");
        }
        [HttpPost]
        [Route("DodajSertifikatUniverzitetu/{idSertifikata}/{idUniverziteta}")]
        public async Task<ActionResult> DodajSertifikatUniverzitetu(string idSertifikata,string idUniverziteta)
        {
            await _client.Cypher.Match("(s:Sertifikat),(u:Univerzitet)")
                                .Where((Univerzitet u,Sertifikat s)=> u.Id==idUniverziteta && s.Id==idSertifikata)
                                .Create("(u)-[r:Podrzava]->(s)")
                                .ExecuteWithoutResultsAsync();
            return Ok("Dodat sertifikat univerzitetu");
        }
        [HttpGet]
        [Route("VratiSveSertifikate")]
        public async Task<ActionResult> VratiSveSertifikate()
        {
            var sertifikati = await _client.Cypher.Match("(s:Sertifikat)")
                                                    .ReturnDistinct(s => s.As<Sertifikat>())
                                                    .ResultsAsync;
            return Ok(sertifikati);
        }
    }
}