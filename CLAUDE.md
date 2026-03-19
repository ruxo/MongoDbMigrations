# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MongoDBMigrationsRZ — a .NET 9 library for running MongoDB schema migrations via a fluent API. Fork of the original MongoDBMigrations, published as [MongoDBMigrationsRZ on NuGet](https://www.nuget.org/packages/MongoDBMigrationsRZ). Supports on-premise MongoDB, Azure CosmosDB (Mongo API), and AWS DocumentDB.

## Build & Test Commands

```bash
# Build all projects
dotnet build

# Run tests (requires Windows — MongoSandbox runtime is win-x64 only)
dotnet test MongoDBMigrations.SmokeTests

# Run a single test
dotnet test MongoDBMigrations.SmokeTests --filter "FullyQualifiedName~TestMethodName"

# Pack NuGet packages (PowerShell) — outputs .nupkg to a destination folder
./build.ps1 <destination-folder>
```

## Solution Structure

| Project | Purpose |
|---|---|
| **MongoDBMigrationsRZ** | Core library. Root namespace: `MongoDBMigrations` |
| **MongoDbMigrations.SshTunnel** | Plugin for SSH tunnel connections (depends on SSH.NET) |
| **MongoDBMigrations.SmokeTests** | Integration tests using MSTest + MongoSandbox (in-process MongoDB) |

## Architecture

**Fluent entry point:** `MigrationEngine` — implements `ILocator`, `ISchemeValidation`, `IMigrationRunner`, and `IMigrationEnginePluginSupport`. A typical call chain:

```
new MigrationEngine()
  .UseDatabase(connStr, dbName, emulation)
  .UseAssembly(asm)
  .Run(targetVersion)
```

**Migration contract:** `IMigration` with `Version`, `Name`, `Up(IMongoDatabase, IClientSessionHandle)`, `Down(IMongoDatabase, IClientSessionHandle)`. Session parameter was added in v1.4.0 for transaction support.

**Key internals:**
- `MigrationLocator` — discovers `IMigration` types via reflection; skips abstract types and `[IgnoreMigration]`-attributed classes
- `DatabaseManager` — tracks applied migrations in a `_migrations` collection (stores `SpecificationItem` documents); handles CosmosDB/DocumentDB quirks
- `MongoSchemeValidator` — uses Buildalyzer + Roslyn to statically analyze migration code and validate collection schema consistency
- `Version` (struct) — semantic versioning with `Major.Minor.Revision`, custom BSON serializer (`VersionStructSerializer`), comparison operators
- `MigrationEnginePlugin` — abstract base for plugins (e.g., `DatabaseSshTunnelPlugin`)

**Session handling:** Attempts MongoDB sessions for transactional migrations; silently degrades on databases without session support (CosmosDB, DocumentDB).

## Testing

Tests use `MongoSandbox` which launches an in-process MongoDB with a single-node replica set (required for session/transaction support). `MongoDaemon.Prepare()` returns a `ConnectionInfo` with auto-incremented database names.

## Versioning

Package versions are derived from git tags via [MinVer](https://github.com/adamralph/minver). Tag format: `v1.4.0` → package version `1.4.0`.
