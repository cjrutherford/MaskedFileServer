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
                    AddFileToSql(f, ConnString);
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

        private void onRename(object sender, RenamedEventArgs e)
        {
            FileRecord rec = FileList.Find(x => x.Path == e.OldFullPath);
            using (SqlConnection conn = new SqlConnection(ConnString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = $"UPDATE FILE SET Path=@path1 where InternalId=@Id";
                    cmd.Parameters.Add("@path1", SqlDbType.Text);
                    cmd.Parameters.Add("@Id", SqlDbType.Text);
                    cmd.Parameters["@path1"].Value = e.FullPath;
                    cmd.Parameters["@Id"].Value = rec.Id;
                    cmd.Connection = conn;
                    cmd.Prepare();
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
                    cmd.CommandText = $"INSERT INTO FILES(InternalId, Path, DeleteOnExpiry, ExpirationDate, CreationTime) values(@Id, @Path, @dox, @ed, @ct)";
                    //{(string) f.Id}, {f.Path}, {f.DeleteOnExpiry}, {f.ExpirationDate}, {f.CreationTime}
                    cmd.Parameters.Add("@Id", SqlDbType.Text);
                    cmd.Parameters.Add("@Path", SqlDbType.Text);
                    cmd.Parameters.Add("@dox", SqlDbType.Int);
                    cmd.Parameters.Add("@ed", SqlDbType.Text);
                    cmd.Parameters.Add("@ct", SqlDbType.Text);
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
                    cmd.CommandText = $"DELETE FROM FILES WHERE Path=@Path";
                    cmd.Parameters.Add("@Path", SqlDbType.Text);
                    cmd.Parameters["@Path"].Value = rec.Path;
                    cmd.Connection = conn;
                    cmd.Prepare();
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
            FileRecord rec = new FileRecord(e.FullPath, DateTime.Now, Term, DeleteOnExpiry);
            AddFileToSql(rec, ConnString);
            FileList.Add(rec);
            Console.WriteLine($"New File Found, Adding {e.FullPath} to the list with an ID of {rec.Id}");
        }
    }
}
