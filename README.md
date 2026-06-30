# MongoDBMigrationsRZ

[![NuGet](https://img.shields.io/nuget/v/MongoDBMigrationsRZ.svg?label=MongoDBMigrationsRZ)](https://www.nuget.org/packages/MongoDBMigrationsRZ)

A **.NET 10** library for evolving MongoDB schemas safely across environments — built around an **environment‑promotion** model. A fork of the original [MongoDBMigrations](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/), published on NuGet as **[MongoDBMigrationsRZ](https://www.nuget.org/packages/MongoDBMigrationsRZ)**.

> [!NOTE]
> **3.0 is in active development on the `vNext` branch.** Milestone 1 — the **Source** history (apply pending steps, append‑only checkpoints, a minimal CLI) — is implemented. **Downstream** promotion, **Bootstrap**, and **rollback** are in progress (see [Status & roadmap](#status--roadmap)). The published **2.x** fluent `MigrationEngine` API remains available on NuGet until 3.0 ships. Full design: [`docs/IN-PROGRESS`](docs/IN-PROGRESS/2026-06-29-environment-promotion-migrations-design.md).

---

## Abstract

In real deployments your databases sit at **different schema checkpoints**: Dev is ahead of Staging, which is ahead of Prod. During development you make *many* small changes — some of which revert or redesign earlier ones. Replaying that churn onto Staging and Prod is noisy and risky.

MongoDBMigrationsRZ flips the usual model. Instead of embedding migrations inside your application and replaying a long version history everywhere, the **migration project *is* the application** — a small .NET console app that DevOps runs. It is organized around three roles:

- **Source** (`Dev`) — an *append‑only* history of many fine‑grained C# steps, applied step by step. This is where you iterate.
- **Downstream** (`Staging`, `Prod`, …) — each receives **one squashed delta**: the net effect of the Source's changes since that environment was last promoted, applied as a single clean change (authored with AI assistance).
- **Bootstrap** (`New`) — a single step that stands up a brand‑new environment matching the Source's current structure, without replaying the whole history.

Each environment tracks its own **checkpoint** (a monotonic id) in its own database, so promotions are verified (no drift) and idempotent to re‑run. Errors are returned as values (`Outcome<T>`), never thrown for expected failures, so every command drops cleanly into CI/CD.

It targets standard on‑premise MongoDB, **Azure CosmosDB** (Mongo API), and **AWS DocumentDB**.

## The model at a glance

```
Migration project (a console app)
├── Dev/        ← Source:    many ordered C# steps (append-only)  + bundled data
├── New/        ← Bootstrap: one step — build a fresh env to the Source's current
├── Staging/    ← Downstream: one squashed delta (From → To checkpoint)
├── Prod/       ← Downstream: one squashed delta (From → To checkpoint)
└── Program.cs  ← registers each environment + RunCliAsync(args)
```

- **Source step** — has a monotonic `Id`. Applying it advances the env's checkpoint to that `Id`.
- **Delta step** — declares `From → To`; the engine refuses to run it unless the target env is actually at `From`, then advances it to `To`.
- **Bootstrap step** — declares the `To` it builds to (the Source's current id).
- **Checkpoint** — stored append‑only in a `_migration_state` collection in each environment's own database.

## Install

```bash
dotnet add package MongoDBMigrationsRZ
dotnet add package MongoDbMigrations.SshTunnel   # optional: SSH-tunnelled connections
```

---

## Basic usage

### 1. Write a Source step

A step is **C# logic**, optionally with **bundled data files**. Wrap MongoDB operations in the `TryCatch` helper so the step returns failures as values rather than throwing. Irreversibility is the default — opt into `Down` only when the change is reversible.

```csharp
using MongoDBMigrations;
using MongoDB.Bson;
using MongoDB.Driver;

public sealed class _0001_CreateClients : SourceStep
{
    public override long Id => 1;
    public override string Name => "Create clients collection with a firstName index";
    public override Reversibility Reversibility => Reversibility.Reversible;

    public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
    {
        var clients = ctx.Database.GetCollection<BsonDocument>("clients");
        clients.Indexes.CreateOne(ctx.Session,
            new CreateIndexModel<BsonDocument>(Builders<BsonDocument>.IndexKeys.Ascending("firstName")));
    });

    public override Outcome<Unit> Down(StepContext ctx) => TryCatch(() =>
        ctx.Database.DropCollection(ctx.Session, "clients"));
}
```

`StepContext` gives the step its `Database`, the per‑step transaction `Session`, a `Cancellation` token, and access to its bundled data via `ctx.OpenData("file.json")`.

### 2. Register the project and run it

`Program.cs` declares each environment by **role + name** (the library knows roles, never specific names) and resolves connections from configuration:

```csharp
using MongoDBMigrations;

return await MigrationApp.Create(args)
    .Source("dev", d => d
        .Step(new _0001_CreateClients())
        .Step(new _0002_RenameNameToFirstName())
        .Step(new _0007_SeedCountries()))          // append-only, ids strictly increasing
    .Bootstrap("new", new _BuildToCurrent())        // optional, single step
    .Downstream("staging", new _Staging_0_to_7())   // one squashed delta per env
    .Downstream("prod",    new _Prod_0_to_5())
    .UseConnections(configuration)                  // standard .NET IConfiguration
    .RunCliAsync();
```

### 3. Apply to Dev and check status

The migration project is a normal console app, so you run it with the CLI verbs:

```bash
$ dotnet run -- status --env dev
dev  checkpoint=0   pending: steps 1, 2, 7

$ dotnet run -- apply --env dev
✓ dev  applied 3 steps   checkpoint=7
```

`apply` runs each pending Source step (`Id > current`) in id order, **each inside its own transaction**, advancing the checkpoint per step. `status` reports where the environment sits.

---

## The promotion workflow

This is the core of the model: Dev accumulates many steps; downstream environments receive one clean, verified delta.

### Promote Dev → Staging

When Dev is ahead, an AI agent (aided by `pending`) authors a **single squashed delta** representing the net change. It declares the checkpoint range it bridges:

```csharp
public sealed class _Staging_0_to_7 : DeltaStep
{
    public override long From => 0;     // Staging must currently be here
    public override long To   => 7;     // it will be here after Up
    public override string Name => "Promote staging to dev checkpoint 7";
    public override Reversibility Reversibility => Reversibility.Lossy;  // honest: data shape changed

    public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
    {
        // The net effect of Dev steps 1..7, squashed — reverts/redesigns already collapsed out.
        // ...
    });
}
```

```bash
$ dotnet run -- status --env staging
staging  checkpoint=0   pending: delta 0 → 7 (Lossy)

$ dotnet run -- apply --env staging
✓ staging  delta 0 → 7 applied   checkpoint=7
```

The engine verifies `staging.checkpoint == 0` (the delta's `From`) before running, so a drifted or already‑promoted environment is rejected rather than corrupted. Staging and Prod can lag at different checkpoints and are promoted independently.

### Bootstrap a brand‑new environment

To stand up a fresh environment (a new region, a new tenant, a local DB) without replaying the whole Dev history, run the single bootstrap step against an empty database:

```bash
$ dotnet run -- new --env staging-eu2 --connection "mongodb://.../app_eu2"
✓ staging-eu2  bootstrapped   checkpoint=7
```

---

## Advanced scenarios

### Reversibility & rollback pre‑flight *(planned — M3)*

Every step carries a reversibility classification — `Reversible`, `Lossy`, or **`Irreversible`** (the default). Before a rollback, the engine runs a pre‑flight and **refuses to roll back through an irreversible step**, printing a report:

```
Rollback staging  7 → 2  BLOCKED.
  ✗  step 5  "Drop legacy audit collection"   IRREVERSIBLE — data is gone
  ✓  step 6  "Rename field"                    Reversible
Restore a backup to roll back past step 5, or re-run with --force.
```

### Bundled data files

Steps can ship data alongside their logic (seed/reference data, fixtures), resolved by step id:

```csharp
public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
{
    using var stream = ctx.OpenData("countries.json");          // Dev/data/0007/countries.json
    var docs = BsonSerializer.Deserialize<List<BsonDocument>>(new StreamReader(stream).ReadToEnd());
    ctx.Database.GetCollection<BsonDocument>("countries").InsertMany(ctx.Session, docs);
});
```

### Errors as values

Every library function that can fail is **total** — it returns `Outcome<T>` and never throws for expected failures. You consume results without `try/catch`:

```csharp
var result = await MigrationApp.Create(args)./* … */.RunCliAsync();   // returns an exit code

// Or programmatically:
var outcome = app.ApplySource(connectionString, databaseName);        // Outcome<long>
if (Fail(outcome, out var error)) Console.Error.WriteLine($"[{error.Code}] {error.Message}");
else Console.WriteLine($"checkpoint = {outcome.Unwrap()}");
```

The **only** sanctioned `try/catch` in the engine wraps the call to *your* `Up`/`Down`: if a step body throws instead of returning an `Outcome` failure, the engine converts it to a failure **and logs a warning** nudging you to wrap the operation in `TryCatch`.

### Azure CosmosDB & AWS DocumentDB

Select the target flavor per environment so the engine applies provider‑specific behavior — most notably auto‑creating/validating the timestamp index CosmosDB requires, and tolerating the lack of multi‑document sessions on managed Mongo‑API services.

### TLS and SSH tunnels

Per‑environment TLS (client certificate) and **SSH‑tunnelled** connections (password or private‑key auth, automatic local port forwarding) are supported via configuration and the `MongoDbMigrations.SshTunnel` package — useful when a database is only reachable through a jump host.

### CI/CD

Every command returns an `Outcome` mapped to a **process exit code** (0 success / non‑zero failure) with the error printed — so it slots directly into a pipeline. A `pending --env <name>` command lists the Source steps an environment hasn't yet incorporated, which is what feeds the AI that authors the squashed delta.

---

## Connection configuration

Environment **names** are your project's choice (in code); their **connections** live in standard .NET configuration, keyed by the same name:

```jsonc
{
  "Migrations": {
    "Environments": {
      "dev":     { "ConnectionString": "mongodb://localhost/app_dev" },
      "staging": { "ConnectionString": "mongodb://.../app_staging", "Emulation": "None" },
      "prod":    { "ConnectionString": "mongodb://.../app_prod",    "Tls": { "CertPath": "..." } }
    }
  }
}
```

The identity threads through three places by the same string: **registration (code) ↔ connection (config) ↔ `--env <name>` (CLI)**, with a `--connection` override (used by `new` to target a database not yet in config). Secrets come from environment variables / user‑secrets / a secret store via the normal configuration providers.

## CLI reference

| Command | Behaviour |
|---|---|
| `status --env <name>` | current checkpoint, pending range, reversibility summary |
| `apply --env <name> [--to <id>]` | Source: run pending steps in id order. Downstream: verify `From`, run the delta, set `To` |
| `new --env <name> \| --connection <conn>` | run the bootstrap against a fresh database |
| `rollback --env <name> --to <id> [--force]` | pre‑flight, then run `Down`s in reverse *(M3)* |
| `pending --env <name>` | list Source steps not yet in this environment |

---

## Status & roadmap

3.0 is being built in milestones; each produces working, tested software:

- **M1 — done.** Source role: the Step model, `_migration_state` checkpoints, per‑step transactions, the registration builder + validation, and a minimal `apply`/`status` CLI. Errors‑as‑values throughout.
- **M2 — in progress.** Downstream `DeltaStep` (drift rejection, already‑up‑to‑date short‑circuit), `BootstrapStep`, `--env`/config‑by‑name, the SSH/TLS reuse.
- **M3.** Rollback + reversibility pre‑flight; `backup`/`restore`.
- **M4.** Bundled‑data ergonomics, CosmosDB/DocumentDB hardening, and removal of the legacy 2.x API.

Until 3.0 ships, the **2.x** fluent `MigrationEngine` API (implement `IMigration`, build with `new MigrationEngine().UseDatabase(...).UseAssembly(...).Run(version)`) remains available and is what the current NuGet package exposes.

## License

[MIT](MIT.md). Free software.
