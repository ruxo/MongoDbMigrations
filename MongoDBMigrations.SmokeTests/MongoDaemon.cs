using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace MongoDBMigrations.SmokeTests
{
    public class MongoDaemon : IDisposable
    {
        public string ConnectionString { get; private set; }
        public string DatabaseName { get; private set; }
        public string Host { get; private set; }
        public string Port { get; private set; }

        private readonly string _dbFolder;
        protected Process process;

        public MongoDaemon()
        {
            //This class is only for test/dev purposes. And supports only windows and osx platform as a development environment.
            string section = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "osx";

            var config = new ConfigurationBuilder()
                .AddJsonFile("local.json")
                .Build()
                .GetSection(section);

            ConnectionString = config["connectionString"];
            DatabaseName = config["databaseName"];
            Host = config["host"];
            Port = config["port"];

            _dbFolder = Path.GetDirectoryName(config["dbFolder"]);

            //Re-create db folder if it exists
            if (Directory.Exists(_dbFolder))
            {
                Directory.Delete(_dbFolder, true);
                Directory.CreateDirectory(_dbFolder);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "mongod",
                Arguments = $"--dbpath {_dbFolder}  --storageEngine ephemeralForTest",
                UseShellExecute = false
            };

            process = new Process
            {
                StartInfo = psi
            };

            process.Start();
        }

        public void Dispose()
        {
            if (process != null && !process.HasExited)
            {
                process.Kill();
            }
        }

        public virtual void Execute(string query)
        {
            string output = null;


            var tlsSupport = "--sslAllowInvalidHostnames --tls --tlsCAFile /Users/arthur_osmokiesku/Downloads/rds-combined-ca-bundle.pem --username adminarthur --password Test1234";
            var nonTlsSupport = $"--host {Host} --port {Port}";
            var psi = new ProcessStartInfo
            {
                FileName = "mongo",
                Arguments = $"{tlsSupport} --quiet --eval \"{query}\"",
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            var procQuery = new Process
            {
                StartInfo = psi
            };
            procQuery.Start();

            while (!procQuery.StandardOutput.EndOfStream)
            {

                output += procQuery.StandardOutput.ReadLine() + Environment.NewLine;
            }
            if (!procQuery.WaitForExit(2000))
            {
                procQuery.Kill();
            }
        }
    }
}