using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MaskedFileServer.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : ControllerBase
    {
        public Wotcha wotcha { get; set; }
        public ValuesController(Wotcha w)
        {
            wotcha = w;
        }
        // GET api/values
        [HttpGet]
        public IEnumerable<FileRecord> Get()
        {
            //String[] stringList = new string[wotcha.FileList.Count];
            //for(int i = 0; i < wotcha.FileList.Count; i++)
            //{
            //    stringList[i] = $"Path: {wotcha.FileList[i].Path} /n ID: {wotcha.FileList[i].Id}";
            //}
            //return stringList;
            return wotcha.FileList;
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
