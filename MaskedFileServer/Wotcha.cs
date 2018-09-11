using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Timers;



namespace MaskedFileServer
{
    public  class Wotcha 
    {
        public List<FileRecord> FileList;
        public bool DeleteOnExpiry { get; set; }
        public int Term { get; set; }
        private Timer CleanupTimer; //cleans up files
        private Timer DataTimer { get; set; }
        public String ConnString { get; set; }



        public Wotcha(string _ConnString, string _filePath = @"./Share", bool _deletionPolicy = false, int _defaultTerm = 90){
            FileList = new List<FileRecord>();
            ConnString = _ConnString;
            FileSystemWatcher fsw = new FileSystemWatcher(_filePath, "*.*");
            DirectoryInfo di = new DirectoryInfo(_filePath);
            using(SqlConnection conn = new SqlConnection(ConnString))
            {
                conn.Open();
                string sql = "SELECT * FROM Files..FileRecord";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            String Id = read["InternalId"].ToString();
                            String pth = read["Path"].ToString();
                            int dbId = (int)read["Id"];
                            DateTime ct = DateTime.Parse(read["CreationTime"].ToString());
                            bool dox = (bool)read["DeleteOnExpiry"];
                            FileList.Add(new FileRecord(pth, ct, dbId,  90, dox, Id));
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
                    FileRecord f = new FileRecord(file.FullName, file.CreationTime, 0, _defaultTerm, _deletionPolicy);
                    AddFileToSql(f, ConnString);
                    int dbId = getFileIdFromSql(f.Path);
                    f.dbId = dbId;
                    FileList.Add(f);
                }

            }
            fsw.Created += new FileSystemEventHandler(OnCreate);
            fsw.Deleted += new FileSystemEventHandler(OnDelete);
            fsw.Renamed += new RenamedEventHandler(onRename);
            fsw.EnableRaisingEvents = true;
            DeleteOnExpiry = _deletionPolicy;
            Term =  _defaultTerm;
            CleanupTimer = new Timer(86400000);
            CleanupTimer.Elapsed += CleanupExpiredFiles;
            CleanupTimer.AutoReset = true;
            CleanupTimer.Enabled = true;

        }

        private int getFileIdFromSql(string path)
        {
            using (SqlConnection conn = new SqlConnection(ConnString))
            {
                conn.Open();
                string sql = "SELECT * FROM Files..FileRecord where Path=@path";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@path", SqlDbType.NVarChar);
                    cmd.Parameters["@path"].Value = path;
                    using (SqlDataReader read = cmd.ExecuteReader())
                    {
                        while (read.Read())
                        {
                            return (int)read["Id"];
                        }
                    }
                }
            }
            return 0;
        }

        private void onRename(object sender, RenamedEventArgs e)
        {

            //TODO:// Fix issue where Find call does not return a file.
            FileRecord rec = FileList.Find(x => x.Path.ToString() == e.OldFullPath.ToString());
            using (SqlConnection conn = new SqlConnection(ConnString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = $"UPDATE Files..FileRecord SET Path=@path1 where Id=@Id";
                    cmd.Parameters.Add("@path1", SqlDbType.NVarChar);
                    cmd.Parameters.Add("@Id", SqlDbType.Int);
                    cmd.Parameters["@path1"].Value = e.FullPath;
                    cmd.Parameters["@Id"].Value = rec.dbId;
                    cmd.Connection = conn;
                    //cmd.Prepare();
                    try
                    {
                        var result = cmd.ExecuteReader();

                    }
                    catch (SqlException er)
                    {
                        Console.WriteLine(er.ToString());
                    }
                }
            }
        }

        private static void AddFileToSql(FileRecord f, String ConnString)
        {
            using (SqlConnection conn = new SqlConnection(ConnString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = $"INSERT INTO Files..FileRecord(InternalId, Path, DeleteOnExpiry, ExpirationDate, CreationTime) values(@Id, @Path, @dox, @ed, @ct)";
                    //{(string) f.Id}, {f.Path}, {f.DeleteOnExpiry}, {f.ExpirationDate}, {f.CreationTime}
                    cmd.Parameters.Add("@Id", SqlDbType.NVarChar);
                    cmd.Parameters.Add("@Path", SqlDbType.NVarChar);
                    cmd.Parameters.Add("@dox", SqlDbType.Int);
                    cmd.Parameters.Add("@ed", SqlDbType.DateTime);
                    cmd.Parameters.Add("@ct", SqlDbType.DateTime);
                    cmd.Parameters["@Id"].Value = f.Id;
                    cmd.Parameters["@Path"].Value = f.Path;
                    cmd.Parameters["@dox"].Value = f.DeleteOnExpiry == true ? 1 : 0;
                    cmd.Parameters["@ed"].Value = f.ExpirationDate;
                    cmd.Parameters["@ct"].Value = f.CreationTime;
                    cmd.Connection = conn;
                    //cmd.Prepare();
                    try
                    {
                        var result = cmd.ExecuteNonQuery();
                        Console.WriteLine(result.ToString());
                    }
                    catch (SqlException e)
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
            RemoveFromSql(rec, ConnString);
        }

        private static void RemoveFromSql(FileRecord rec, String ConnString)
        {
            using (SqlConnection conn = new SqlConnection(ConnString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = $"DELETE FROM Files..FileRecord WHERE Path=@Path";
                    cmd.Parameters.Add("@Path", SqlDbType.NVarChar);
                    cmd.Parameters["@Path"].Value = rec.Path;
                    cmd.Connection = conn;
                    //cmd.Prepare();
                    try
                    {
                        var result = cmd.ExecuteNonQuery();
                        Console.WriteLine(result.ToString());
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                conn.Close();
            }
        }

        private  void OnCreate(object sender, FileSystemEventArgs e)
        {
            FileRecord rec = new FileRecord(e.FullPath, DateTime.Now, 0, Term, DeleteOnExpiry);
            AddFileToSql(rec, ConnString);
            int dbId = getFileIdFromSql(rec.Path);
            rec.dbId = dbId;
            FileList.Add(rec);
            Console.WriteLine($"New File Found, Adding {e.FullPath} to the list with an ID of {rec.Id}");
        }
    }
}
