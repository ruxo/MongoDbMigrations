using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

[ExcludeFromCodeCoverage]
public class MigrationNotFoundException(string sourceDescription, Version? target, IEnumerable<Version> available)
    : Exception($"Migrations are not found in {sourceDescription} (Target = {target}). Available versions: {string.Join(", ", available)}");