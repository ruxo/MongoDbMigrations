using MongoDB.Driver;

namespace MongoDBMigrations
{
    public static class MongoDatabaseStateChecker
    {
        public static void ThrowIfDatabaseOutdated(string connectionString, string databaseName)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName);
            if (dbVersion < availableVersion)
                throw new DatabaseOutdatedExcetion(dbVersion, availableVersion);
        }

        public static bool IsDatabaseOutdated(string connectionString, string databaseName)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName);
            return availableVersion > dbVersion;
        }

        private static (Version dbVersion, Version availableVersion) GetCurrentVersions(string connectionString, string databaseName)
        {
            var locator = new MigrationLocator();
            var highestAvailableVersion = locator.GetNewestLocalVersion();
            var dbStatus = new DatabaseStatus(new MongoClient(connectionString).GetDatabase(databaseName));
            var currectDbVersion = dbStatus.GetVersion();
            return (currectDbVersion, highestAvailableVersion);
        }
    }
}
