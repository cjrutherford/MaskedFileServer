using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MaskedFileServer.Controllers
{
    public class ValuesController : ControllerBase
    {
#pragma warning disable IDE1006 // Naming Styles
        public Wotcha wotcha { get; set; }
#pragma warning restore IDE1006 // Naming Styles
        public ValuesController(Wotcha w)
        {
            wotcha = w;
        }
        // GET api/values
        [HttpGet]
        [Route("list")]
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
        [Route("file/{id}")]
        public async Task<FileResult> Get(string id)
        {
            FileRecord fr = wotcha.FileList.Find(x => x.Id == id);
            string[] filePieces = fr.Path.Split('\\');
            string fileName = filePieces[filePieces.Length - 1];
            MemoryStream memory = new MemoryStream();
            using(FileStream fs = new FileStream(fr.Path, FileMode.Open))
            {
                await fs.CopyToAsync(memory);
            }
            memory.Position = 0;
            Console.WriteLine($"Sending File {fileName} to user Via HTTP.");
            return File(memory, "application/pdf", fileName);
        }
    }
}
