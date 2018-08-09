using System;
using System.Linq;
using MongoDB.Driver;

namespace MongoDBMigrations
{
    /// <summary>
    /// Works with applied migrations
    /// </summary>
    public class DatabaseStatus
    {
        private const string SPECIFICATION_COLLECTION_NAME = "_migrations";
        private readonly IMongoDatabase _database;

        public DatabaseStatus(IMongoDatabase database)
        {
            if (database == null)
                throw new TypeInitializationException("Database can't be null", null);

            _database = database;

            if (!_database.ListCollectionNames().ToList().Contains(SPECIFICATION_COLLECTION_NAME))
            {
                _database.CreateCollection(SPECIFICATION_COLLECTION_NAME);
            }
        }

        /// <summary>
        /// Find all applied to the database migrations.
        /// </summary>
        /// <returns>Collection of applied migrations.</returns>
        public IMongoCollection<SpecificationItem> GetAppliedMigrations()
        {
            return _database.GetCollection<SpecificationItem>(SPECIFICATION_COLLECTION_NAME);
        }

        /// <summary>
        /// Check is database is up to date.
        /// </summary>
        /// <param name="newestVersion">Newest IMigration implementation.</param>
        /// <returns>True if database not needs update, otherwise false.</returns>
        public bool IsNotLatestVersion(Version newestVersion)
        {
            return newestVersion != GetVersion();
        }

        /// <summary>
        /// Throw specific excetion if database needs update.
        /// </summary>
        /// <param name="newestVersion">Newest IMigration implementation.</param>
        public void ThrowIfNotLatestVersion(Version newestVersion)
        {
            if (!IsNotLatestVersion(newestVersion))
                return;

            var databaseVersion = GetVersion();
            throw new DatabaseOutdatedExcetion(databaseVersion, newestVersion);
        }

        /// <summary>
        /// Return database version based on last applied migration.
        /// </summary>
        /// <returns>Database version in semantic view.</returns>
        public Version GetVersion()
        {
            var lastMigrations = GetLastAppliedMigration();
            return lastMigrations == null
                ? new Version(1, 0, 0)
                : lastMigrations.Ver;
        }

        /// <summary>
        /// Find last applied migration by applying date and time.
        /// </summary>
        /// <returns>Applied migration.</returns>
        public SpecificationItem GetLastAppliedMigration()
        {
            return GetAppliedMigrations()
                .Find(FilterDefinition<SpecificationItem>.Empty)
                .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime))
                .FirstOrDefault();
        }

        /// <summary>
        /// Commit migration to the database.
        /// </summary>
        /// <param name="migration">Migration instance.</param>
        /// <param name="isUp">True if roll forward otherwise roll back.</param>
        /// <returns>Applied migration.</returns>
        public SpecificationItem SaveMigration(IMigration migration, bool isUp)
        {
            var rollbackSpecification = _database.GetCollection<SpecificationItem>(SPECIFICATION_COLLECTION_NAME)
                .Find(x => x.Ver < migration.Version)
                .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime))
                .FirstOrDefault();

            var rollbackVersion = Version.V1();
            if (rollbackSpecification != null)
                rollbackVersion = rollbackSpecification.Ver;

            var appliedMigration = new SpecificationItem
            {
                Name = migration.Name,
                Ver = isUp ? migration.Version : rollbackVersion,
                isUp = isUp,
                ApplyingDateTime = DateTime.UtcNow
            };
            GetAppliedMigrations().InsertOne(appliedMigration);
            return appliedMigration;
        }
    }
}