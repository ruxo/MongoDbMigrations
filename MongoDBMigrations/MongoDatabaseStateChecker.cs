using MongoDB.Driver;
using MongoDBMigrations.Document;

namespace MongoDBMigrations
{
    public static class MongoDatabaseStateChecker
    {
        public static void ThrowIfDatabaseOutdated(string connectionString, string databaseName, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName, emulation);
            if (dbVersion < availableVersion)
                throw new DatabaseOutdatedExcetion(dbVersion, availableVersion);
        }

        public static bool IsDatabaseOutdated(string connectionString, string databaseName, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName, emulation);
            return availableVersion > dbVersion;
        }

        private static (Version dbVersion, Version availableVersion) GetCurrentVersions(string connectionString, string databaseName, MongoEmulationEnum emulation)
        {
            var locator = new MigrationManager();
            var highestAvailableVersion = locator.GetNewestLocalVersion();
            var dbStatus = new DatabaseManager(new MongoClient(connectionString).GetDatabase(databaseName), emulation);
            var currectDbVersion = dbStatus.GetVersion();
            return (currectDbVersion, highestAvailableVersion);
        }
    }
}
