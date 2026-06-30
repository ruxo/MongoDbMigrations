using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MongoDBMigrations;

public static class MigrationCli
{
    public static Task<int> RunCliAsync(MigrationApp app, string[] args, IConfiguration config)
    {
        var command = args?.FirstOrDefault();
        var env = Flag(args, "--env");
        var connectionOverride = Flag(args, "--connection");

        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; cts.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            switch (command)
            {
                case "apply":
                case "status":
                case "new":
                    if (string.IsNullOrWhiteSpace(env))
                        return Fail2("Missing required option: --env <name>.");

                    if (Fail(ConnectionConfig.Resolve(config, env, connectionOverride), out var ce, out var db))
                        return Fail1(ce);

                    var outcome = command switch
                    {
                        "apply"  => app.Apply(env, db, cts.Token),
                        "status" => app.Status(db),
                        _        => app.New(env, db, cts.Token),
                    };
                    return Finish(outcome, c => command == "status" ? $"{env}: checkpoint = {c}" : $"{env}: checkpoint now {c}");

                default:
                    Console.Error.WriteLine($"Unknown command '{command}'. Expected: apply | status | new.");
                    return Task.FromResult(2);
            }
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }
    }

    static string? Flag(string[]? args, string name)
    {
        if (args is null) return null;
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    static Task<int> Finish(Outcome<long> outcome, Func<long, string> onSuccess)
    {
        if (Fail(outcome, out var e, out var value))
        {
            Console.Error.WriteLine($"FAILED [{e?.Code}] {e?.Message}");
            return Task.FromResult(1);
        }
        Console.WriteLine(onSuccess(value));
        return Task.FromResult(0);
    }

    static Task<int> Fail1(ErrorInfo? e) { Console.Error.WriteLine($"FAILED [{e?.Code}] {e?.Message}"); return Task.FromResult(1); }
    static Task<int> Fail2(string message) { Console.Error.WriteLine(message); return Task.FromResult(2); }
}
