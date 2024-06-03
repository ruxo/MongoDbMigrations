using System.Diagnostics.CodeAnalysis;
using MongoDBMigrations.Document;
using Renci.SshNet;
// ReSharper disable UnusedMember.Global

namespace MongoDBMigrations;

[ExcludeFromCodeCoverage(Justification="Involve setting up SSH client and not much logic here")]
public static class MigrationEngineExtensions
{
    public readonly record struct SshConfig(SshClient SshClient, ForwardedPortLocal ForwardedPortLocal, int BoundPort, string BoundHost);

    const string Localhost = "127.0.0.1";

    public static MigrationEngine UseSshTunnel(this MigrationEngine engine, ServerAdressConfig sshAddress, string sshUser,
                                               string sshPassword, ServerAdressConfig mongoAddress) {
        return engine.EstablishConnectionViaSsh(new SshClient(sshAddress.Host, sshAddress.PortAsInt, sshUser, sshPassword), mongoAddress);
    }

    public static MigrationEngine UseSshTunnel(this MigrationEngine engine, ServerAdressConfig sshAddress, string sshUser,
                                               Stream privateKeyFileStream, ServerAdressConfig mongoAddress,
                                               string? keyFilePassPhrase = null) {
        var keyFile = keyFilePassPhrase == null
                          ? new PrivateKeyFile(privateKeyFileStream)
                          : new PrivateKeyFile(privateKeyFileStream, keyFilePassPhrase);
        return engine.EstablishConnectionViaSsh(new SshClient(sshAddress.Host, sshAddress.PortAsInt, sshUser, [keyFile]), mongoAddress);
    }

    static MigrationEngine EstablishConnectionViaSsh(this MigrationEngine engine, SshClient client, ServerAdressConfig mongoAdress) {
        client.Connect();
        var forwardedPortLocal = new ForwardedPortLocal(Localhost, mongoAdress.Host, mongoAdress.Port);
        client.AddForwardedPort(forwardedPortLocal);
        forwardedPortLocal.Start();

        var pluginSupporter = (IMigrationEnginePluginSupport)engine;
        pluginSupporter.AddPlugin(new DatabaseSshTunnelPlugin(new(client, forwardedPortLocal, (int) forwardedPortLocal.BoundPort, Localhost)));

        return engine;
    }
}