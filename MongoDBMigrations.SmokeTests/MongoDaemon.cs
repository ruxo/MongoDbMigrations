using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MongoDBMigrations.SmokeTests
{
    public class MongoDaemon : IDisposable
    {
#if DEBUG
        public const string ConnectionString = "mongodb://localhost:27017";
        public const string DatabaseName = "test";
        public const string Host = "localhost";
        public const string Port = "27017";
#else
        public const string ConnectionString = "mongodb://localhost:27017";
        public const string DatabaseName = "MongoDBMigrationTests";
        public const string Host = "localhost";
        public const string Port = "27017";
#endif

        private readonly string _assemblyFolder;
        private readonly string _dbFolder;
        private readonly string _mongoFolder;
        protected Process process;

        public MongoDaemon()
        {
            _assemblyFolder = Path.GetDirectoryName(new Uri(typeof(MongoDaemon).Assembly.CodeBase).LocalPath);
            _mongoFolder = Path.Combine(_assemblyFolder, "mongo");
            _dbFolder = Path.Combine(_mongoFolder, DatabaseName);

            //Re-create db folder if it exists
            if(Directory.Exists(_dbFolder))
            {
                Directory.Delete(_dbFolder, true);
                Directory.CreateDirectory(_dbFolder);
            }

            process = new Process();
            process.StartInfo.FileName = Path.Combine(_mongoFolder, "mongod.exe");
            process.StartInfo.Arguments = $"--dbpath {_dbFolder}  --storageEngine ephemeralForTest --replSet 'rs0'";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            process.Start();

            Query("rs.initiate()");
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                //TODO: dispose all managed resources
            }
            if(process != null && !process.HasExited)
            {
                process.Kill();
            }
        }

        public virtual string Query(string query)
        {
            var output = string.Empty;

            var procQuery = new Process();
            procQuery.StartInfo.FileName = Path.Combine(_mongoFolder, "mongo.exe");
            procQuery.StartInfo.Arguments = $"--host {Host} --port {Port} --quiet --eval \"{ query}\"";
            procQuery.StartInfo.UseShellExecute = false;
            procQuery.StartInfo.RedirectStandardOutput = true;
            procQuery.StartInfo.CreateNoWindow = true;
            procQuery.Start();

            while (!procQuery.StandardOutput.EndOfStream)
            {
                output += procQuery.StandardOutput.ReadLine() + Environment.NewLine;
            }

            if(!procQuery.WaitForExit(2000))
            {
                procQuery.Kill();
            }

            return output;
        }
    }
}
