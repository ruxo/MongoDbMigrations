using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MongoDBMigrations
{
    /// <summary>
    /// Works with local migrations
    /// </summary>
    public class MigrationLocator
    {
        private Assembly _assembly;

        public MigrationLocator()
        {
        }

        public MigrationLocator(Assembly assembly)
        {
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
        /// Find all migrations in executing assembly or assembly whitch found by <LookInAssemblyOfType> method.
        /// </summary>
        /// <returns>List of all found migrations.</returns>
        public IEnumerable<IMigration> GetAllMigrations()
        {
            if (_assembly == null)
                return GetAllMigrations(Assembly.GetExecutingAssembly());

            return GetAllMigrations(_assembly);
        }

        /// <summary>
        /// Find all migrations in specific assembly
        /// </summary>
        /// <param name="assembly">Assembly with migrations classes.</param>
        /// <returns>List of all found migrations.</returns>
        public IEnumerable<IMigration> GetAllMigrations(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes()
                    .Where(type => typeof(IMigration).IsAssignableFrom(type) && !type.IsAbstract)
                    .Select(Activator.CreateInstance)
                    .OfType<IMigration>();
            }
            catch (Exception exception)
            {
                throw new MigrationNotFoundException(assembly.FullName, exception);
            }
        }

        /// <summary>
        /// Find all migrations in executing assembly or assembly whitch found by <LookInAssemblyOfType> method.
        /// Between current and target versions
        /// </summary>
        /// <param name="currentVersion">Version of database.</param>
        /// <param name="targetVerstion">Target version for migrating.</param>
        /// <returns>List of all found migrations.</returns>
        public IEnumerable<IMigration> GetMigrations(Version currentVersion, Version targetVerstion)
        {
            var migrations = GetAllMigrations();
            if (targetVerstion > currentVersion)
            {
                migrations = migrations
                    .Where(x => x.Version > currentVersion && x.Version <= targetVerstion)
                    .OrderBy(x => x.Version);
            }
            else if (targetVerstion < currentVersion)
            {
                migrations = migrations
                    .Where(x => x.Version <= currentVersion && x.Version > targetVerstion)
                    .OrderByDescending(x => x.Version);
            }
            else
                return Enumerable.Empty<IMigration>();

            if (targetVerstion != Version.V1() && migrations.Last().Version != targetVerstion)
                throw new MigrationNotFoundException(_assembly.FullName, null);

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
                return new Version(1, 0, 0);
            }

            return migrations.Max(m => m.Version);
        }
    }
}