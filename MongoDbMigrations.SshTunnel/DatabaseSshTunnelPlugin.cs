using System.Diagnostics.CodeAnalysis;
using MongoDB.Driver;

namespace MongoDBMigrations;

[ExcludeFromCodeCoverage(Justification="Involve setting up SSH client and not much logic here")]
public sealed class DatabaseSshTunnelPlugin(MigrationEngineExtensions.SshConfig sshConfig) : MigrationEnginePlugin
{
    public override IMongoClient SetupMongoClient(IMongoClient client) {
        client.Settings.Server = new MongoServerAddress(sshConfig.BoundHost, sshConfig.BoundPort);
        return base.SetupMongoClient(client);
    }

    protected override void Dispose(bool disposing) {
        if (disposing){
            if (sshConfig is { SshClient.IsConnected: true })
                sshConfig.ForwardedPortLocal.Dispose();  // 🤔 not sure if this is really needed since we are disposing the SshClient
            sshConfig.SshClient.Dispose();
        }
        base.Dispose(disposing);
    }
}