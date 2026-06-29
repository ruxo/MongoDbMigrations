using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MongoDBMigrations;

public static class MigrationCli
{
    public static Task<int> RunAsync(MigrationApp app, string[] args, string connectionString, string databaseName)
    {
        var command = args?.FirstOrDefault();

        switch (command)
        {
            case "apply":
                return Task.FromResult(Finish(
                    app.ApplySource(connectionString, databaseName, CancellationToken.None),
                    checkpoint => $"applied — checkpoint now {checkpoint}"));

            case "status":
                return Task.FromResult(Finish(
                    app.CurrentCheckpoint(connectionString, databaseName),
                    checkpoint => $"checkpoint = {checkpoint}"));

            default:
                Console.Error.WriteLine($"Unknown command '{command}'. Expected: apply | status.");
                return Task.FromResult(2);
        }
    }

    static int Finish(Outcome<long> outcome, Func<long, string> onSuccess)
    {
        if (Fail(outcome, out var e, out var value))
        {
            Console.Error.WriteLine($"FAILED [{e?.Code}] {e?.Message}");
            return 1;
        }

        Console.WriteLine(onSuccess(value));
        return 0;
    }
}
