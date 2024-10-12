using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MongoDBMigrations.Document;

namespace MongoDBMigrations;

/// <summary>
/// Works with local migrations
/// </summary>
static class MigrationManager
{
    [PublicAPI]
    public static Assembly GetCurrentAssemblyMigrations() {
        var stackFrames = new StackTrace().GetFrames();
        if (stackFrames == null)
            throw new InvalidOperationException("Can't find assembly with migrations. Try use LookInAssemblyOfType() method before.");

        var currentAssembly = Assembly.GetExecutingAssembly();
        return stackFrames.First(a => a.GetMethod()!.DeclaringType!.Assembly != currentAssembly).GetMethod()!.DeclaringType!.Assembly;
    }

    /// <summary>
    /// Find all migrations in specific assembly
    /// </summary>
    /// <param name="assembly">Assembly with migrations classes.</param>
    /// <returns>List of all found migrations.</returns>
    [PublicAPI]
    public static IMigration[] GetAllMigrations(Assembly assembly) {
        try{
            var result = assembly.GetTypes()
                                 .Where(type =>
                                            typeof(IMigration).IsAssignableFrom(type)
                                         && !type.IsAbstract
                                         && type.GetCustomAttribute<IgnoreMigrationAttribute>() == null)
                                 .Select(Activator.CreateInstance)
                                 .OfType<IMigration>()
                                 .ToArray();

            if (result.Length == 0)
                throw new MigrationNotFoundException(assembly.FullName!, null, []);

            return result;
        }
        catch (Exception exception){
            throw new InvalidOperationException($"Can't find migrations in assembly {assembly.FullName}", exception);
        }
    }

    /// <summary>
    /// Find all migrations in executing assembly or assembly whitch found by &lt;LookInAssemblyOfType&gt; method.
    /// Between current and target versions
    /// </summary>
    /// <param name="source">Migration source.</param>
    /// <param name="currentVersion">Version of database.</param>
    /// <param name="targetVersion">Target version for migrating.</param>
    /// <returns>List of all found migrations.</returns>
    public static IMigration[] GetMigrationsForExecution(IMigrationSource source, Version currentVersion, Version targetVersion) {
        var migrations = source.Migrations;
        if (migrations.All(x => x.Version != targetVersion) && targetVersion != Version.Zero)
            throw new MigrationNotFoundException(source.SourceDescription, targetVersion, migrations.Select(x => x.Version));

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
            throw new MigrationNotFoundException(source.SourceDescription, targetVersion, migrations.Select(x => x.Version));

        return migrations;
    }
}