using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using SQLitePCL;


namespace MaskedFileServer
{
    public  class Wotcha 
    {
        public List<FileRecord> FileList;
        public bool DeleteOnExpiry { get; set; }
        public int Term { get; set; }
        private Timer CleanupTimer; //cleans up files
        private Timer DataTimer { get; set; }



        public Wotcha(string _filePath = @"./Share", bool _deletionPolicy = false, int _defaultTerm = 90){
            FileList = new List<FileRecord>();
            FileSystemWatcher fsw = new FileSystemWatcher(_filePath, "*.*");
            DirectoryInfo di = new DirectoryInfo(_filePath);
            using(SqliteConnection conn = new SqliteConnection(@"Data Source=./localStorage.sqlite"))
            {
                conn.Open();
                string sql = "SELECT * FROM FILES";
                using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                {
                    using (SqliteDataReader read = cmd.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            String Id = read["Id"].ToString();
                            String pth = read["Path"].ToString();
                            DateTime ct = DateTime.Parse(read["CreationTime"].ToString());
                            bool dox = (Int64)read["DeleteOnExpiry"] == 1 ? true : false;
                            FileList.Add(new FileRecord(pth, ct, 90, dox, Id));
                        }
                    }
                }
            }
            FileInfo[] files = di.GetFiles();
            foreach(FileInfo file in files)
            {
                if(FileList.Exists(x => x.Path == file.FullName))
                {
                    Console.WriteLine($"File {file.FullName} already exists in the list. Moving on.");
                }
                else
                {
                    FileRecord f = new FileRecord(file.FullName, file.CreationTime, _defaultTerm, _deletionPolicy);
                    FileList.Add(f);
                    AddFileToSqlite(f);
                }

            }
            fsw.Created += new FileSystemEventHandler(OnCreate);
            fsw.Deleted += new FileSystemEventHandler(OnDelete);
            fsw.EnableRaisingEvents = true;
            DeleteOnExpiry = _deletionPolicy;
            Term =  _defaultTerm;
            CleanupTimer = new Timer(86400000);
            CleanupTimer.Elapsed += CleanupExpiredFiles;
            CleanupTimer.AutoReset = true;
            CleanupTimer.Enabled = true;

        }

        private static void AddFileToSqlite(FileRecord f)
        {
            using (SqliteConnection conn = new SqliteConnection(@"Data Source=./localStorage.sqlite"))
            {
                conn.Open();
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    cmd.CommandText = $"INSERT INTO FILES(Id, Path, DeleteOnExpiry, ExpirationDate, CreationTime) values(@Id, @Path, @dox, @ed, @ct)";
                    //{(string) f.Id}, {f.Path}, {f.DeleteOnExpiry}, {f.ExpirationDate}, {f.CreationTime}
                    cmd.Parameters.Add("@Id", SqliteType.Text);
                    cmd.Parameters.Add("@Path", SqliteType.Text);
                    cmd.Parameters.Add("@dox", SqliteType.Integer);
                    cmd.Parameters.Add("@ed", SqliteType.Text);
                    cmd.Parameters.Add("@ct", SqliteType.Text);
                    cmd.Parameters["@Id"].Value = f.Id;
                    cmd.Parameters["@Path"].Value = f.Path;
                    cmd.Parameters["@dox"].Value = f.DeleteOnExpiry == true ? 1 : 0;
                    cmd.Parameters["@ed"].Value = f.ExpirationDate;
                    cmd.Parameters["@ct"].Value = f.CreationTime;
                    cmd.Connection = conn;
                    cmd.Prepare();
                    try
                    {
                        var result = cmd.ExecuteNonQuery();
                        Console.WriteLine(result.ToString());
                    }
                    catch (SqliteException e)
                    {
                        Console.WriteLine(e.ToString());
                    }
                }
                conn.Close();
            }
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
            RemoveFromSqlite(rec);
        }

        private static void RemoveFromSqlite(FileRecord rec)
        {
            using (SqliteConnection conn = new SqliteConnection(@"Data Source=./localStorage.sqlite"))
            {
                conn.Open();
                using (SqliteCommand cmd = new SqliteCommand())
                {
                    cmd.CommandText = $"DELETE FROM FILES WHERE Path=@Path";
                    cmd.Parameters.Add("@Path", SqliteType.Text);
                    cmd.Parameters["@Path"].Value = rec.Path;
                    cmd.Connection = conn;
                    cmd.Prepare();
                    try
                    {
                        var result = cmd.ExecuteNonQuery();
                        Console.WriteLine(result.ToString());
                    }
                    catch (SqliteException ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                conn.Close();
            }
        }

        private  void OnCreate(object sender, FileSystemEventArgs e)
        {
            FileRecord rec = new FileRecord(e.FullPath, DateTime.Now, Term, DeleteOnExpiry);
            AddFileToSqlite(rec);
            FileList.Add(rec);
            Console.WriteLine($"New File Found, Adding {e.FullPath} to the list with an ID of {rec.Id}");
        }
    }
}
