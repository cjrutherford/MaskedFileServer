using System;
namespace MaskedFileServer
{
    public class FileRecord
    {
        public String Id {get;set;}
        public String Path {get;set;}
        public bool DeleteOnExpiry{get;set;}
        public DateTime ExpirationDate{get;set;}

        public FileRecord(string _path, DateTime creationTime, int _term = 90, bool _deletionPolicy = false)
        {
            Id = Guid.NewGuid().ToString();
            Path = _path;
            ExpirationDate = creationTime.AddDays(_term);
            DeleteOnExpiry = _deletionPolicy;
            Console.WriteLine($"Created File Record for {_path}.");
        }
    }
}
