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
            if (session.IsInTransaction && Fail(TryCatch(() => session.AbortTransaction(ct)), out var ae)) return ae.Trace();
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

    // The one sanctioned try/catch: the library wraps the call to a user-authored step's
    // Up/Down (special case) so a contract-violating throw becomes a failed Outcome WITH a
    // warning instead of escaping. Engine code itself never wraps an Outcome-returning call —
    // this boundary exists only because step bodies are user code that may break the contract.
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
