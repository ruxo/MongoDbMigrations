using System;
using System.Diagnostics;
using System.Threading;
using MongoSandbox;

namespace MongoDBMigrations.SmokeTests;

public static class MongoDaemon
{
    public sealed class ConnectionInfo : IDisposable
    {
        public string ConnectionString => Server.ConnectionString;
        public required string DatabaseName { get; init; }

        public void Dispose() { }
    }

    static int dbCounter;
    public static ConnectionInfo Prepare()
        => new(){ DatabaseName = $"test{Interlocked.Increment(ref dbCounter)}"};

    static readonly MongoRunnerOptions MongoOptions = new() {
        UseSingleNodeReplicaSet = true,
        StandardOuputLogger = line => Trace.WriteLine($"| {line}"),
        StandardErrorLogger = line => Trace.WriteLine($"ERR: {line}"),

        KillMongoProcessesWhenCurrentProcessExits = true
    };
    static readonly IMongoRunner Server = MongoRunner.Run(MongoOptions);
}