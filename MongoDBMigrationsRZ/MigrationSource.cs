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

    /// <summary>
    /// Find all migrations in executing assembly or assembly whitch found by &lt;LookInAssemblyOfType&gt; method.
    /// Between current and target versions
    /// </summary>
    /// <param name="currentVersion">Version of database.</param>
    /// <param name="targetVersion">Target version for migrating.</param>
    /// <returns>List of all found migrations.</returns>
    IMigration[] GetMigrationsForExecution(Version currentVersion, Version targetVersion) {
        var migrations = Migrations;
        if (migrations.All(x => x.Version != targetVersion) && targetVersion != Version.Zero)
            throw new MigrationNotFoundException(SourceDescription, targetVersion, migrations.Select(x => x.Version));

        if (targetVersion == currentVersion)
            return [];

        migrations = targetVersion > currentVersion
                         ? migrations
                          .Where(x => x.Version > currentVersion && x.Version <= targetVersion)
                          .OrderBy(x => x.Version).ToArray()
                         : migrations
                          .Where(x => x.Version <= currentVersion && x.Version > targetVersion)
                          .OrderByDescending(x => x.Version).ToArray();

        if (!migrations.Any())
            throw new MigrationNotFoundException(SourceDescription, targetVersion, migrations.Select(x => x.Version));

        return migrations;
    }
}

[PublicAPI]
public sealed class MigrationSource(Lazy<IMigration[]> migrations, string sourceDescription) : IMigrationSource
{
    public static MigrationSource FromAssembly(Assembly assembly)
        => new(new(() => MigrationLocator.GetAllMigrations(assembly)), $"assembly {assembly.FullName}");

    public static MigrationSource FromNamespaceOfType<T>()
        => new(new(MigrationLocator.GetMigrationFromNamespaceOfType<T>), $"namespace of type {typeof(T).FullName}");

    public static MigrationSource FromMigrations(IMigration[] migrations)
        => new(new(() => migrations), "direct supplied");

    public static readonly MigrationSource Empty = new(new([]), "(uninitialized)");

    public IMigration[] Migrations => migrations.Value;
    public string SourceDescription => sourceDescription;
}