using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Mongo2Go;

namespace MongoDBMigrations.SmokeTests
{
    public static class MongoDaemon
    {
        public sealed class ConnectionInfo : IDisposable
        {
            readonly MongoDbRunner runner;
            public ConnectionInfo(MongoDbRunner runner) {
                this.runner = runner;
            }

            public string ConnectionString => runner.ConnectionString;
            public string DatabaseName { get; init; }

            public void Dispose() {
                runner.Dispose();
            }
        }

        // ReSharper disable InconsistentNaming
        sealed class AppConfig
        {
            public string connectionString { get; init; }
            public string databaseName { get; init; }
            public string host { get; init; }
            public string port { get; init; }
            public bool isLocal { get; init; }
            public string dbFolder { get; init; }
        }
        // ReSharper restore InconsistentNaming

        public static ConnectionInfo Prepare() {
            var dbFolder = Configuration.Value.isLocal ? Configuration.Value.dbFolder : null;
            return new (MongoDbRunner.Start(dbFolder)){ DatabaseName = Configuration.Value.databaseName };
        }

        static readonly Lazy<AppConfig> Configuration = new(() => {
            //This class is only for test/dev purposes. And supports only windows and osx platform as a development environment.
            var section = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "osx";

            return new ConfigurationBuilder()
                  .AddJsonFile("local.json")
                  .Build()
                  .GetSection(section)
                  .Get<AppConfig>();
        });
    }
}