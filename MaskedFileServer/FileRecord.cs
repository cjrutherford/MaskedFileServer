using System;
namespace MaskedFileServer
{
    public class FileRecord
    {
        public String Id {get;set;}
        public String Path {get;set;}
        public bool DeleteOnExpiry{get;set;}
        public DateTime ExpirationDate{get;set;}
        public DateTime CreationTime { get; set; }

        public FileRecord(string _path, DateTime creationTime, int _term = 90, bool _deletionPolicy = false, string _id = "")
        {
            if(_id != "")
            {
                Id = _id;
            }
            else
            {
                Id = Guid.NewGuid().ToString();
            }
            Path = _path;
            CreationTime = creationTime;
            ExpirationDate = creationTime.AddDays(_term);
            DeleteOnExpiry = _deletionPolicy;
            Console.WriteLine($"Created File Record for {_path}.");
        }
    }
}
