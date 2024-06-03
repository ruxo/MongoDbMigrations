using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using MongoDBMigrations.Document;

namespace MongoDBMigrations
{
    /// <summary>
    /// Works with local migrations
    /// </summary>
    internal class MigrationManager
    {
        private Assembly? _assembly;

        /// <summary>
        /// Sets assembly
        /// </summary>
        /// <param name="assembly">Assembly where migration classes located</param>
        public void SetAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            _assembly = assembly;
        }

        /// <summary>
        /// Set assembly for finding migrations.
        /// </summary>
        /// <typeparam name="T">Type in assembly with migrations.</typeparam>
        public void LookInAssemblyOfType<T>()
        {
            var assembly = typeof(T).Assembly;
            _assembly = assembly;
        }

        /// <summary>
        /// Set assembly for finding migrations.
        /// </summary>
        /// <param name="type">Type in assembly with migrations.</param>
        public void LookInAssemblyOfType(Type type)
        {
            var assembly = type.Assembly;
            _assembly = assembly;
        }

        /// <summary>
        /// Find all migrations in executing assembly or assembly whitch found by &lt;LookInAssemblyOfType&gt; method.
        /// </summary>
        /// <returns>List of all found migrations.</returns>
        public List<IMigration> GetAllMigrations()
        {
            if (_assembly != null)
            {
                return GetAllMigrations(_assembly);
            }

            // Ok no problem let's try to find mingrations in excecuting assembly
            var stackFrames = new StackTrace().GetFrames();
            if (stackFrames == null)
                throw new InvalidOperationException("Can't find assembly with migrations. Try use LookInAssemblyOfType() method before.");

            var currentAssembly = Assembly.GetExecutingAssembly();
            Assembly trueCallingAssembly = stackFrames
                .First(a => a.GetMethod()!.DeclaringType!.Assembly != currentAssembly).GetMethod()!.DeclaringType!.Assembly;

            if (trueCallingAssembly == null)
                throw new InvalidOperationException("Can't find assembly with migrations. Try use LookInAssemblyOfType() method before.");


            return GetAllMigrations(trueCallingAssembly);
        }

        /// <summary>
        /// Find all migrations in specific assembly
        /// </summary>
        /// <param name="assembly">Assembly with migrations classes.</param>
        /// <returns>List of all found migrations.</returns>
        public List<IMigration> GetAllMigrations(Assembly assembly)
        {
            List<IMigration> result;
            try
            {
                result = assembly.GetTypes()
                    .Where(type =>
                        typeof(IMigration).IsAssignableFrom(type)
                        && !type.IsAbstract
                        && type.GetCustomAttribute<IgnoreMigrationAttribute>() == null)
                    .Select(Activator.CreateInstance)
                    .OfType<IMigration>()
                    .ToList();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Can't find migrations in assembly {assembly.FullName}", exception);
            }

            if (!result.Any())
                throw new MigrationNotFoundException(assembly.FullName!);

            return result;
        }

        /// <summary>
        /// Find all migrations in executing assembly or assembly whitch found by &lt;LookInAssemblyOfType&gt; method.
        /// Between current and target versions
        /// </summary>
        /// <param name="currentVersion">Version of database.</param>
        /// <param name="targetVersion">Target version for migrating.</param>
        /// <returns>List of all found migrations.</returns>
        public List<IMigration> GetMigrations(Version currentVersion, Version targetVersion)
        {
            Debug.Assert(_assembly is not null);
            var migrations = GetAllMigrations();
            if (migrations.All(x => x.Version != targetVersion) && targetVersion != Version.Zero())
            {
                throw new MigrationNotFoundException(_assembly.FullName!);
            }

            if (targetVersion > currentVersion)
            {
                migrations = migrations
                    .Where(x => x.Version > currentVersion && x.Version <= targetVersion)
                    .OrderBy(x => x.Version).ToList();
            }
            else if (targetVersion < currentVersion)
            {
                migrations = migrations
                    .Where(x => x.Version <= currentVersion && x.Version > targetVersion)
                    .OrderByDescending(x => x.Version).ToList();
            }
            else
                return Enumerable.Empty<IMigration>().ToList();

            if (!migrations.Any())
                throw new MigrationNotFoundException(_assembly.FullName!);

            return migrations;
        }

        /// <summary>
        /// Find the highest version of migrations.
        /// </summary>
        /// <returns>Highest version in semantic view.</returns>
        public Version GetNewestLocalVersion()
        {
            var migrations = GetAllMigrations();
            if (!migrations.Any())
            {
                return Version.Zero();
            }

            return migrations.Max(m => m.Version);
        }
    }
}