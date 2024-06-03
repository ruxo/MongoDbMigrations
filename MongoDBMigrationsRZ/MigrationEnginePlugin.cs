using System;
using MongoDB.Driver;

namespace MongoDBMigrations;

public interface IMigrationEnginePluginSupport
{
    IMigrationEnginePluginSupport AddPlugin(MigrationEnginePlugin plugin);
}

public abstract class MigrationEnginePlugin : IDisposable
{
    public virtual IMongoClient SetupMongoClient(IMongoClient client) => client;

    ~MigrationEnginePlugin() {
        Dispose(disposing: false);
    }

    public void Dispose() {
        Dispose(disposing: true);
    }

    protected virtual void Dispose(bool disposing) {
        if (disposing) GC.SuppressFinalize(this);
    }
}