using System;
using System.Collections.Generic;
using System.IO;

namespace MaskedFileServer
{
    public  class Wotcha 
    {
        public List<FileRecord> FileList;
        public bool DeleteOnExpiry { get; set; }
        public int Term { get; set; }


        public Wotcha(string _filePath = "//Files//", bool _deletionPolicy = false, int _defaultTerm = 90){
            FileList = new List<FileRecord>();
            FileSystemWatcher fsw = new FileSystemWatcher(_filePath, "*.*");
            DirectoryInfo di = new DirectoryInfo(_filePath);
            var files = di.GetFiles();
            foreach(var file in files)
            {
                FileRecord f = new FileRecord(file.FullName, _defaultTerm, _deletionPolicy);
                FileList.Add(f);
            }
            fsw.Created += new FileSystemEventHandler(OnCreate);
            fsw.Deleted += new FileSystemEventHandler(OnDelete);
            DeleteOnExpiry = _deletionPolicy;
            Term =  _defaultTerm;

        }

        private  void OnDelete(object sender, FileSystemEventArgs e)
        {
            FileRecord rec = FileList.Find(x => x.Path == e.FullPath);
            Console.WriteLine($"Removing {rec.Path} from the File List");
            FileList.Remove(rec);
        }

        private  void OnCreate(object sender, FileSystemEventArgs e)
        {
            FileRecord rec = new FileRecord(e.FullPath, Term, DeleteOnExpiry);
            FileList.Add(rec);
            Console.WriteLine($"New File Found, Adding {e.FullPath} to the list with an ID of {rec.Id}");
        }
    }
}
