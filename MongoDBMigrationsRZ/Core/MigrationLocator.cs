using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MongoDBMigrations.Document;

namespace MongoDBMigrations;

public static class MigrationLocator
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
    /// <param name="namespace">Specify a namespace to look for migration classes.</param>
    /// <returns>List of all found migrations.</returns>
    [PublicAPI]
    public static IMigration[] GetAllMigrations(Assembly assembly, string? @namespace = null) {
        try{
            var result = assembly.GetTypes()
                                 .Where(type =>
                                            typeof(IMigration).IsAssignableFrom(type)
                                         && !type.IsAbstract
                                         && type.GetCustomAttribute<IgnoreMigrationAttribute>() is null
                                         && (@namespace is null || type.Namespace == @namespace))
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

    [PublicAPI]
    public static IMigration[] GetMigrationFromNamespaceOfType<T>()
        => GetAllMigrations(typeof(T).Assembly, typeof(T).Namespace);
}