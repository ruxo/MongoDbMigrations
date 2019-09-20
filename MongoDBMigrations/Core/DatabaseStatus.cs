using System;
using System.Linq;
using System.Threading.Tasks;
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
        /// Return database version based on last applied migration asynchronously.
        /// </summary>
        /// <returns>Database version in semantic view.</returns>
        public async Task<Version> GetVersionAsync()
        {
            var lastMigration = await GetLastAppliedMigrationAsync().ConfigureAwait(false);

            if (lastMigration == null || lastMigration.isUp)
                return await (lastMigration == null
                    ? Task.FromResult(Version.Zero())
                    : Task.FromResult(lastMigration.Ver));

            var migration = GetAppliedMigrations()
                .Find(item => item.isUp && item.Ver < lastMigration.Ver)
                .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime))
                .FirstOrDefault();

            return migration?.Ver ?? Version.Zero();
        }

        public Version GetPreviousVersion()
        {
            var currentVer = GetVersion();
            var migrations = GetAppliedMigrations()
                .Find(FilterDefinition<SpecificationItem>.Empty)
                .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime)).ToList();

            var migration = migrations.FirstOrDefault(x => x.Ver < currentVer);
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
        /// Find last applied migration by applying date and time asynchronously.
        /// </summary>
        /// <returns>Applied migration.</returns>
        public async Task<SpecificationItem> GetLastAppliedMigrationAsync()
        {
            IAsyncCursor<SpecificationItem> asyncCursor = await GetAppliedMigrations()
                .FindAsync(FilterDefinition<SpecificationItem>.Empty, new FindOptions<SpecificationItem, SpecificationItem>
                {
                    Sort = Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime)
                }).ConfigureAwait(false);
            return await asyncCursor.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Commit migration to the database.
        /// </summary>
        /// <param name="migration">Migration instance.</param>
        /// <param name="isUp">True if roll forward otherwise roll back.</param>
        /// <returns>Applied migration.</returns>
        public SpecificationItem SaveMigration(IMigration migration, bool isUp)
        {
            /*
            var rollbackSpecification = _database.GetCollection<SpecificationItem>(SPECIFICATION_COLLECTION_NAME)
                .Find(x => x.Ver < migration.Version)
                .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime))
                .FirstOrDefault();

            var rollbackVersion = GetPreviousVersion();
            if (rollbackSpecification != null)
                rollbackVersion = rollbackSpecification.Ver;
                */
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

        /// <summary>
        /// Commit migration to the database asynchronously.
        /// </summary>
        /// <param name="migration">Migration instance.</param>
        /// <param name="isUp">True if roll forward otherwise roll back.</param>
        /// <returns>Applied migration.</returns>
        public async Task<SpecificationItem> SaveMigrationAsync(IMigration migration, bool isUp)
        {
            /*
            var rollbackSpecifications = await _database.GetCollection<SpecificationItem>(SPECIFICATION_COLLECTION_NAME)
                .FindAsync(x => x.Ver < migration.Version, new FindOptions<SpecificationItem, SpecificationItem>
                {
                    Sort = Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime)
                }).ConfigureAwait(false);

            var rollbackVersion = GetPreviousVersion();
            var specification = await rollbackSpecifications.FirstOrDefaultAsync().ConfigureAwait(false);
            if (specification != null)
                rollbackVersion = specification.Ver;
                */
            var appliedMigration = new SpecificationItem
            {
                Name = migration.Name,
                Ver = migration.Version,
                isUp = isUp,
                ApplyingDateTime = DateTime.UtcNow
            };
            await GetAppliedMigrations().InsertOneAsync(appliedMigration).ConfigureAwait(false);
            return await Task.FromResult(appliedMigration);
        }
    }
}