using Microsoft.Extensions.Configuration;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;

namespace MongoDBMigrations.SmokeTests
{
    public class MongoDaemon : IDisposable
    {
        /*
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
*/
        public string ConnectionString { get; private set; }
        public string DatabaseName { get; private set; }
        public string Host { get; private set; }
        public string Port { get; private set; }

        private readonly string _dbFolder;
        private readonly string _mongoFolder;
        protected Process process;

        public MongoDaemon()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("app.json")
                .Build();

            ConnectionString = config["connectionString"];
            DatabaseName = config["databaseName"];
            Host = config["host"];
            Port = config["port"];

            _mongoFolder = Path.GetDirectoryName(config["mongoFolder"]);
            _dbFolder = Path.GetDirectoryName(config["dbFolder"]);

            //Re-create db folder if it exists
            if (Directory.Exists(_dbFolder))
            {
                Directory.Delete(_dbFolder, true);
                Directory.CreateDirectory(_dbFolder);
            }

            process = new Process();
            process.StartInfo.FileName = Path.Combine(_mongoFolder, "mongod.exe");
            process.StartInfo.Arguments = $"--dbpath {_dbFolder}  --storageEngine ephemeralForTest";
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
        }

        public void Dispose()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
        }

        public virtual string Query(string query)
        {
            var output = string.Empty;

            var procQuery = new Process();
            procQuery.StartInfo.FileName = Path.Combine(_mongoFolder, "mongo.exe");
            procQuery.StartInfo.Arguments = $"--host {Host} --port {Port} --quiet --eval \"{query}\"";
            procQuery.StartInfo.UseShellExecute = false;
            procQuery.StartInfo.RedirectStandardOutput = true;
            procQuery.StartInfo.CreateNoWindow = true;
            procQuery.Start();

            while (!procQuery.StandardOutput.EndOfStream)
            {
                output += procQuery.StandardOutput.ReadLine() + Environment.NewLine;
            }

            if (!procQuery.WaitForExit(2000))
            {
                procQuery.Kill();
            }

            return output;
        }
    }
}