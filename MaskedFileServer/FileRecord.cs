using System;
namespace MaskedFileServer
{
    public class FileRecord
    {
        public String Id {get;set;}
        public String Path {get;set;}
        public bool DeleteOnExpiry{get;set;}
        public DateTime ExpirationDate{get;set;}

        public FileRecord(string _path, int _term = 90, bool _deletionPolicy = false)
        {
            Id = Guid.NewGuid().ToString();
            Path = _path;
            DateTime today = DateTime.Now;
            ExpirationDate = today.AddDays(_term);
            DeleteOnExpiry = _deletionPolicy;
            Console.WriteLine($"Created File Record for {_path}.");
        }
    }
}
