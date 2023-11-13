using System.Reflection;
using MongoDB.Driver;
using MongoDBMigrations.Document;

namespace MongoDBMigrations
{
    public static class MongoDatabaseStateChecker
    {
        public static void ThrowIfDatabaseOutdated(string connectionString, string databaseName, Assembly migrationAssambly = null, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName, migrationAssambly, emulation);
            if (availableVersion > dbVersion)
                throw new DatabaseOutdatedExcetion(dbVersion, availableVersion);
        }

        public static bool IsDatabaseOutdated(string connectionString, string databaseName, Assembly migrationAssambly = null, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName, migrationAssambly, emulation);
            return availableVersion > dbVersion;
        }

        private static (Version dbVersion, Version availableVersion) GetCurrentVersions(string connectionString, string databaseName, Assembly migrationAssambly, MongoEmulationEnum emulation)
        {
            var locator = new MigrationManager();
            if(migrationAssambly != null)
            {
                locator.SetAssembly(migrationAssambly);
            }
            var highestAvailableVersion = locator.GetNewestLocalVersion();

            var dbStatus = new DatabaseManager(new MongoClient(connectionString).GetDatabase(databaseName), emulation);
            var currectDbVersion = dbStatus.GetVersion();

            return (currectDbVersion, highestAvailableVersion);
        }
    }
}
