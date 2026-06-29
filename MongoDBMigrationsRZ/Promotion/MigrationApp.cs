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

    public static MigrationApp Create() => new();

    public MigrationApp Source(string name, Action<SourceBuilder> configure)
    {
        var builder = new SourceBuilder();
        configure(builder);
        sourceName = name;
        sourceSteps = builder.Steps;
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

        return this;
    }

    public Outcome<long> ApplySource(string connectionString, string databaseName, CancellationToken ct = default)
    {
        var db = new MongoClient(connectionString).GetDatabase(databaseName);
        return new SourceRunner(db, new CheckpointStore(db), sourceSteps, ct).Apply();
    }

    public static Outcome<long> CurrentCheckpoint(string connectionString, string databaseName)
        => new CheckpointStore(new MongoClient(connectionString).GetDatabase(databaseName)).Current();
}
