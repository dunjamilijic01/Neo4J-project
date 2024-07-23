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
    public class AdminController : ControllerBase
    {
        private readonly IGraphClient _client;

        public AdminController(IGraphClient client)
        {
            _client = client;
        }
        [HttpGet]
        [Route("Login/{username}/{password}")]
        public async Task<ActionResult> Login(string username, string password)
        {
            if(username!="admin@gmail.com")
            {
                return BadRequest("Nevalidan username za admina");
            }
            if(password!="admin123")
            {
                return BadRequest("Nevalidan password za admina");
            }
            return Ok("Ulogovan admin");
        }
    }
}