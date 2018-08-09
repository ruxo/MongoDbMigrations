using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;

namespace MongoDBMigrations
{
    /// <summary>
    /// Runner for mongo migrations
    /// </summary>
    public class MigrationRunner
    {
        public IMongoDatabase Database { get; set; }
        public MigrationLocator Locator { get; set; }
        public DatabaseStatus Status { get; set; }

        public MigrationRunner(string connectionString, string databaseName)
            : this(new MongoClient(connectionString).GetDatabase(databaseName))
        { }

        public MigrationRunner(IMongoDatabase database)
        {
            this.Database = database;
            this.Locator = new MigrationLocator();
            this.Status = new DatabaseStatus(database);

            BsonSerializer.RegisterSerializer(typeof(Version), new VerstionSerializer());
        }

        /// <summary>
        /// Migrate to latest found version
        /// </summary>
        /// <returns>List of messages for each processed migration</returns>
        public IEnumerator<MigrationResult> UpdateToLatest()
        {
            return UpdateTo(Locator.GetNewestLocalVersion());
        }

        /// <summary>
        /// Migrate to specific version 
        /// </summary>
        /// <param name="targetVersion">Target version. Can be less or greater then current database version.</param>
        /// <returns>List of messages for each processed migration</returns>
        public IEnumerator<MigrationResult> UpdateTo(Version targetVersion)
        {
            var currentVerstion = Status.GetVersion();
            var migrations = Locator.GetMigrations(currentVerstion, targetVersion);
            var serverNames = string.Join(',', Database.Client.Settings.Servers);

            var isUp = targetVersion > currentVerstion;

            if (!migrations.Any())
            {
                yield return new MigrationResult
                {
                    MigrationName = string.Empty,
                    TargetVersion = targetVersion,
                    ServerAdress = serverNames,
                    DatabaseName = Database.DatabaseNamespace.DatabaseName,
                    Message = "Nothing to update."
                };
            }

            foreach (var migration in migrations)
            {
                if (isUp)
                    migration.Up(Database);
                else
                    migration.Down(Database);

                var m = Status.SaveMigration(migration, isUp);
                yield return new MigrationResult
                {
                    MigrationName = migration.Name,
                    TargetVersion = m.Ver,
                    ServerAdress = serverNames,
                    DatabaseName = Database.DatabaseNamespace.DatabaseName,
                    Message = string.Format("Applying migration {0}, to version {1}. Database: {2}. Servers: {3}",
                        migration.Name,
                        m.Ver,
                        Database.DatabaseNamespace.DatabaseName,
                        serverNames)
                };
            }

            yield return new MigrationResult
            {
                MigrationName = string.Empty,
                TargetVersion = targetVersion,
                ServerAdress = serverNames,
                DatabaseName = Database.DatabaseNamespace.DatabaseName,
                Message = $"Migration database {Database.DatabaseNamespace.DatabaseName} to v{targetVersion} has been completed."
            };
        }
    }
}