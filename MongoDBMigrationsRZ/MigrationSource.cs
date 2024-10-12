using System;
using System.Linq;
using System.Reflection;

namespace MongoDBMigrations;

public interface IMigrationSource
{
    IMigration[] Migrations { get; }
    string SourceDescription { get; }

    /// Find the highest version of migrations.
    Version NewestLocalVersion => Migrations.Max(m => m.Version);
}

public sealed class MigrationSource(Lazy<IMigration[]> migrations, string sourceDescription) : IMigrationSource
{
    public static MigrationSource FromAssembly(Assembly assembly)
        => new(new(() => MigrationManager.GetAllMigrations(assembly)), $"assembly {assembly.FullName!}");

    public static readonly MigrationSource Empty = new(new([]), "(uninitialized)");

    public IMigration[] Migrations => migrations.Value;
    public string SourceDescription => sourceDescription;
}