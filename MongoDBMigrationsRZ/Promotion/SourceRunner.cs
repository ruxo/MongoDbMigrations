using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class SourceRunner(
    IMongoDatabase database,
    CheckpointStore checkpoints,
    IReadOnlyList<SourceStep> steps,
    CancellationToken ct)
{
    public Outcome<long> Apply()
    {
        if (Fail(checkpoints.Current(), out var ce, out var current)) return ce.Trace();

        foreach (var step in steps.Where(s => s.Id > current).OrderBy(s => s.Id))
        {
            if (ct.IsCancellationRequested) return ErrorInfo.New(CANCELLED);

            var record = new CheckpointRecord
            {
                StepName = step.Name, Role = "source", Direction = "up",
                From = current, To = step.Id, AppliedAtUtc = DateTime.UtcNow, Ok = true
            };

            if (Fail(StepExecutor.Run(database, checkpoints, step, record, ct), out var se)) return se.Trace();

            current = step.Id;
        }

        return current;
    }
}
