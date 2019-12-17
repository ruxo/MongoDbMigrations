using System;
using System.Linq;
using MongoDB.Driver;
using MongoDBMigrations.Document;

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
            _database = database ?? throw new TypeInitializationException("Database can't be null", null);

            if (!_database.ListCollectionNames().ToList().Contains(SPECIFICATION_COLLECTION_NAME))
            {
                _database.CreateCollection(SPECIFICATION_COLLECTION_NAME);
            }
        }

        private IMongoCollection<SpecificationItem> GetAppliedMigrations()
        {
            return _database.GetCollection<SpecificationItem>(SPECIFICATION_COLLECTION_NAME);
        }

        private bool IsNotLatestVersion(Version newestVersion)
        {
            return newestVersion != GetVersion();
        }

        /// <summary>
        /// Return database version based on last applied migration.
        /// </summary>
        /// <returns>Database version in semantic view.</returns>
        public Version GetVersion()
        {
            var lastMigration = GetLastAppliedMigration();
            if (lastMigration == null || lastMigration.isUp)
                return lastMigration?.Ver ?? Version.Zero();

            var migration = GetAppliedMigrations()
                .Find(item => item.isUp && item.Ver < lastMigration.Ver)
                .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime))
                .FirstOrDefault();

            return migration?.Ver ?? Version.Zero();
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
            var appliedMigration = new SpecificationItem
            {
                Name = migration.Name,
                Ver = migration.Version,
                isUp = isUp,
                ApplyingDateTime = DateTime.UtcNow
            };
            GetAppliedMigrations().InsertOne(appliedMigration);
            return appliedMigration;
        }
    }
}