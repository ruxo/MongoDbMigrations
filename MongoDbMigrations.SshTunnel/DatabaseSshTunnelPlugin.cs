using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class DatabaseSshTunnelPlugin(MigrationEngineExtensions.SshConfig sshConfig) : MigrationEnginePlugin
{
    public override IMongoClient SetupMongoClient(IMongoClient client) {
        client.Settings.Server = new MongoServerAddress(sshConfig.BoundHost, sshConfig.BoundPort);
        return base.SetupMongoClient(client);
    }

    protected override void Dispose(bool disposing) {
        if (sshConfig is { SshClient.IsConnected: true }){
            sshConfig.SshClient.Dispose();
            sshConfig.ForwardedPortLocal.Dispose();
        }
        base.Dispose(disposing);
    }
}