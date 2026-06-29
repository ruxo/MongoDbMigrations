using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LanguageExt;
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

        var pending = steps.Where(s => s.Id > current).OrderBy(s => s.Id).ToArray();

        foreach (var step in pending)
        {
            if (ct.IsCancellationRequested) return ErrorInfo.New(CANCELLED);

            if (Fail(ApplyOne(step, current), out var se, out var to)) return se.Trace();

            current = to;
        }

        return current;
    }

    Outcome<long> ApplyOne(SourceStep step, long from)
    {
        if (Fail(TryCatch(() => database.Client.StartSession()), out var e, out var session)) return e.Trace();
        using var _ = session;

        if (Fail(TryCatch(() => session.StartTransaction()), out e)) return e.Trace();

        var ctx = new StepContext(database, session, ct);

        if (Fail(Guarded(() => step.Up(ctx), step.Name), out var ue))
        {
            if (session.IsInTransaction) session.AbortTransaction(ct);
            return ue.Trace();
        }

        var record = new CheckpointRecord
        {
            StepName = step.Name, Role = "source", Direction = "up",
            From = from, To = step.Id, AppliedAtUtc = DateTime.UtcNow, Ok = true
        };

        if (Fail(checkpoints.Append(session, record), out e))
        {
            if (session.IsInTransaction && Fail(TryCatch(() => session.AbortTransaction(ct)), out var ae)) return ae.Trace();

            return e.Trace();
        }

        if (Fail(TryCatch(() => session.CommitTransaction(ct)), out e)) return e.Trace();

        return step.Id;
    }

    // Errors-as-values boundary (spec §2.1): an escaped exception becomes a failed
    // Outcome AND logs a warning nudging the author toward TryCatch.
    static Outcome<Unit> Guarded(Func<Outcome<Unit>> body, string stepName)
    {
        try
        {
            return body();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"WARN: step '{stepName}' threw instead of returning an Outcome failure; " +
                $"wrap MongoDB operations in TryCatch. {ex.GetType().Name}: {ex.Message}");
            return ErrorFrom.Exception(ex);
        }
    }
}
