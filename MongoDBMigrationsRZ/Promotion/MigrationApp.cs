using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class SourceBuilder
{
    readonly List<SourceStep> steps = new();
    public SourceBuilder Step(SourceStep step) { steps.Add(step); return this; }
    internal IReadOnlyList<SourceStep> Steps => steps;
}

public sealed class MigrationApp
{
    string? sourceName;
    IReadOnlyList<SourceStep> sourceSteps = Array.Empty<SourceStep>();
    string? bootstrapName;
    BootstrapStep? bootstrapStep;
    readonly Dictionary<string, DeltaStep> downstream = new();
    bool built;

    public static MigrationApp Create() => new();

    public MigrationApp Source(string name, Action<SourceBuilder> configure)
    {
        EnsureMutable();
        var builder = new SourceBuilder();
        configure(builder);
        sourceName = name;
        sourceSteps = builder.Steps;
        return this;
    }

    public MigrationApp Bootstrap(string name, BootstrapStep step)
    {
        EnsureMutable();
        bootstrapName = name;
        bootstrapStep = step;
        return this;
    }

    public MigrationApp Downstream(string name, DeltaStep delta)
    {
        EnsureMutable();
        downstream[name] = delta;
        return this;
    }

    public Outcome<MigrationApp> Build()
    {
        if (sourceName is null)
            return StepErrors.Registration("No Source registered. Call Source(name, ...).");

        var ids = sourceSteps.Select(s => s.Id).ToArray();
        for (var i = 1; i < ids.Length; i++)
            if (ids[i] <= ids[i - 1])
                return StepErrors.Registration(
                    $"Source step ids must be strictly increasing and unique; saw {ids[i - 1]} then {ids[i]}.");

        var names = new[] { sourceName }.Concat(downstream.Keys);
        if (bootstrapName is not null) names = names.Append(bootstrapName);
        var dupe = names.GroupBy(n => n).FirstOrDefault(g => g.Count() > 1);
        if (dupe is not null)
            return StepErrors.Registration($"Environment name '{dupe.Key}' is registered more than once.");

        built = true;
        return this;
    }

    public Outcome<long> Apply(string env, IMongoDatabase db, CancellationToken ct)
    {
        var checkpoints = new CheckpointStore(db);

        if (env == sourceName)
            return new SourceRunner(db, checkpoints, sourceSteps, ct).Apply();

        if (downstream.TryGetValue(env, out var delta))
            return new DownstreamRunner(db, checkpoints, delta, env, ct).Apply();

        return ErrorInfo.New(INVALID_REQUEST, $"No Source or Downstream environment named '{env}'.");
    }

    public Outcome<long> New(string env, IMongoDatabase db, CancellationToken ct)
    {
        if (bootstrapStep is null)
            return ErrorInfo.New(INVALID_REQUEST, "No Bootstrap step registered.");
        if (env != bootstrapName)
            return ErrorInfo.New(INVALID_REQUEST, $"No Bootstrap environment named '{env}'.");

        return new BootstrapRunner(db, new CheckpointStore(db), bootstrapStep, env, ct).Apply();
    }

    public Outcome<long> Status(IMongoDatabase db) => new CheckpointStore(db).Current();

    public bool Knows(string env) => env == sourceName || downstream.ContainsKey(env);

    void EnsureMutable()
    {
        if (built) throw new InvalidOperationException("MigrationApp is built; register environments before Build().");
    }
}
