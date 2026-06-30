using System;
using System.Threading;
using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class BootstrapRunner(
    IMongoDatabase database,
    CheckpointStore checkpoints,
    BootstrapStep step,
    string envName,
    CancellationToken ct)
{
    public Outcome<long> Apply()
    {
        if (Fail(checkpoints.Current(), out var ce, out var current)) return ce.Trace();

        if (current != 0) return StepErrors.NotEmpty(envName, current);

        var record = new CheckpointRecord
        {
            StepName = step.Name, Role = "bootstrap", Direction = "up",
            From = 0, To = step.To, AppliedAtUtc = DateTime.UtcNow, Ok = true
        };

        if (Fail(StepExecutor.Run(database, checkpoints, step, record, ct), out var se)) return se.Trace();

        return step.To;
    }
}
