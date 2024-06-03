using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

[ExcludeFromCodeCoverage]
public class MigrationNotFoundException(string assemblyName, Exception innerException)
    : Exception($"Migrations are not found in assembly {assemblyName}", innerException);