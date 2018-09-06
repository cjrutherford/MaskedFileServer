using System;
using System.Collections.Generic;
using System.IO;

namespace MaskedFileServer
{
    public static class Wotcha
    {
        public static List<FileRecord> FileList;

        public static void Initialize(string _filePath = "//Files//"){
            FileSystemWatcher fsw = new FileSystemWatcher(_filePath, "*.*");
            fsw.Created += new FileSystemEventHandler(onCreate);
            fsw.Deleted += new FileSystemEventHandler(onDelete);
        }

        private static void onDelete(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void onCreate(object sender, FileSystemEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
