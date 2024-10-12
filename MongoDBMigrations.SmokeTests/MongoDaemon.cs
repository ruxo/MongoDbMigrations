using System;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Mongo2Go;

namespace MongoDBMigrations.SmokeTests;

public static class MongoDaemon
{
    public sealed class ConnectionInfo(MongoDbRunner runner) : IDisposable
    {
        public string ConnectionString => runner.ConnectionString;
        public required string DatabaseName { get; init; }

        public void Dispose() {
            runner.Dispose();
        }
    }

    // ReSharper disable InconsistentNaming
    sealed class AppConfig
    {
        public required string databaseName { get; init; }
        public bool isLocal { get; init; }
        public string? dbFolder { get; init; }
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
              .GetRequiredSection(section)
              .Get<AppConfig>()!;
    });
}