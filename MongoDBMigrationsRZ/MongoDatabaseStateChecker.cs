using System.Reflection;
using MongoDB.Driver;
using MongoDBMigrations.Document;

namespace MongoDBMigrations
{
    public static class MongoDatabaseStateChecker
    {
        public static void ThrowIfDatabaseOutdated(string connectionString, string databaseName, Assembly? migrationAssembly = null, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName, migrationAssembly, emulation);
            if (availableVersion > dbVersion)
                throw new DatabaseOutdatedException(dbVersion, availableVersion);
        }

        public static bool IsDatabaseOutdated(string connectionString, string databaseName, Assembly? migrationAssembly = null, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var (dbVersion, availableVersion) = GetCurrentVersions(connectionString, databaseName, migrationAssembly, emulation);
            return availableVersion > dbVersion;
        }

        static (Version dbVersion, Version availableVersion) GetCurrentVersions(string connectionString, string databaseName, Assembly? migrationAssembly, MongoEmulationEnum emulation)
        {
            var locator = new MigrationManager();
            if(migrationAssembly is not null) 
                locator.SetAssembly(migrationAssembly);
            
            var highestAvailableVersion = locator.GetNewestLocalVersion();

            var dbStatus = new DatabaseManager(new MongoClient(connectionString).GetDatabase(databaseName), emulation);
            var currectDbVersion = dbStatus.GetVersion();

            return (currectDbVersion, highestAvailableVersion);
        }
    }
}