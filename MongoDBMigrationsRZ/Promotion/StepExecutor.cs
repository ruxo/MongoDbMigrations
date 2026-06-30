using System;
using System.Threading;
using LanguageExt;
using MongoDB.Driver;

namespace MongoDBMigrations;

// Runs one MigrationStep inside its own session/transaction and appends a checkpoint
// record, atomically. Total (errors as values). The ONE sanctioned try/catch in the
// engine is Guarded(), which wraps the call to a user-authored step's Up so a
// contract-violating throw becomes a failed Outcome with a warning.
static class StepExecutor
{
    public static Outcome<Unit> Run(
        IMongoDatabase database, CheckpointStore checkpoints,
        MigrationStep step, CheckpointRecord record, CancellationToken ct)
    {
        if (Fail(TryCatch(() => database.Client.StartSession()), out var se, out var session)) return se.Trace();
        using var _ = session;

        if (Fail(TryCatch(() => session.StartTransaction()), out var te)) return te.Trace();

        var ctx = new StepContext(database, session, ct);

        if (Fail(Guarded(() => step.Up(ctx), step.Name), out var ue)) return AbortThen(session, ct, ue);

        if (Fail(checkpoints.Append(session, record), out var ae)) return AbortThen(session, ct, ae);

        var commit = TryCatch(() => session.CommitTransaction(ct));
        if (Fail(commit, out var ce))
            return ct.IsCancellationRequested ? ErrorInfo.New(CANCELLED) : ce.Trace();

        return commit;   // success Outcome<Unit>
    }

    static Outcome<Unit> AbortThen(IClientSessionHandle session, CancellationToken ct, ErrorInfo original)
    {
        if (session.IsInTransaction && Fail(TryCatch(() => session.AbortTransaction(ct)), out var ae)) return ae.Trace();
        return original.Trace();
    }

    // The one sanctioned try/catch: wraps the untrusted user step's Up/Down.
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
