using System;
using System.Threading;
using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class DownstreamRunner(
    IMongoDatabase database,
    CheckpointStore checkpoints,
    DeltaStep delta,
    string envName,
    CancellationToken ct)
{
    public Outcome<long> Apply()
    {
        if (Fail(checkpoints.Current(), out var ce, out var current)) return ce.Trace();

        if (current == delta.To) return current;                                    // already up to date
        if (current != delta.From) return StepErrors.Drift(envName, delta.From, current);

        var record = new CheckpointRecord
        {
            StepName = delta.Name, Role = "downstream", Direction = "up",
            From = delta.From, To = delta.To, AppliedAtUtc = DateTime.UtcNow, Ok = true
        };

        if (Fail(StepExecutor.Run(database, checkpoints, delta, record, ct), out var se)) return se.Trace();

        return delta.To;
    }
}
