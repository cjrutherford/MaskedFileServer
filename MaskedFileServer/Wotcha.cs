using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;

namespace MaskedFileServer
{
    public  class Wotcha 
    {
        public List<FileRecord> FileList;
        public bool DeleteOnExpiry { get; set; }
        public int Term { get; set; }
        private Timer timmy;


        public Wotcha(string _filePath = "//Files//", bool _deletionPolicy = false, int _defaultTerm = 90){
            FileList = new List<FileRecord>();
            FileSystemWatcher fsw = new FileSystemWatcher(_filePath, "*.*");
            DirectoryInfo di = new DirectoryInfo(_filePath);
            var files = di.GetFiles();
            foreach(var file in files)
            {
                FileRecord f = new FileRecord(file.FullName, file.CreationTime, _defaultTerm, _deletionPolicy );
                FileList.Add(f);
            }
            fsw.Created += new FileSystemEventHandler(OnCreate);
            fsw.Deleted += new FileSystemEventHandler(OnDelete);
            fsw.EnableRaisingEvents = true;
            DeleteOnExpiry = _deletionPolicy;
            Term =  _defaultTerm;
            timmy = new Timer(86400000);
            timmy.Elapsed += CleanupExpiredFiles;
            timmy.AutoReset = true;
            timmy.Enabled = true;

        }

        private void CleanupExpiredFiles(object sender, ElapsedEventArgs e)
        {
            FileList.ForEach(f =>
            {
                if(f.ExpirationDate <= DateTime.Now)
                {
                    Console.WriteLine($"File: {f.Path} is expired. Deleting from Disk.");
                    File.Delete(f.Path);
                }

            });
        }

        private  void OnDelete(object sender, FileSystemEventArgs e)
        {
            FileRecord rec = FileList.Find(x => x.Path == e.FullPath);
            Console.WriteLine($"Removing {rec.Path} from the File List");
            FileList.Remove(rec);
        }

        private  void OnCreate(object sender, FileSystemEventArgs e)
        {
            FileRecord rec = new FileRecord(e.FullPath, DateTime.Now, Term, DeleteOnExpiry);
            FileList.Add(rec);
            Console.WriteLine($"New File Found, Adding {e.FullPath} to the list with an ID of {rec.Id}");
        }
    }
}
