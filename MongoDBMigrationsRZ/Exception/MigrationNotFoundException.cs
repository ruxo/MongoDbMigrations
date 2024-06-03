using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

[ExcludeFromCodeCoverage]
public class MigrationNotFoundException : Exception
{
    public MigrationNotFoundException(string assemblyName, Exception innerException) : base(ErrorMessage(assemblyName), innerException) {
    }

    public MigrationNotFoundException(string assemblyName) : base(ErrorMessage(assemblyName)) {
    }

    static string ErrorMessage(string assemblyName) => $"Migrations are not found in assembly {assemblyName}";
}