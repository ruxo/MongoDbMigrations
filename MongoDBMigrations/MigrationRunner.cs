using System;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDBMigrations.Core;


namespace MongoDBMigrations
{
    /// <summary>
    /// Runner for mongo migrations
    /// </summary>
    public class MigrationRunner
    {
        /// <summary>
        /// Event rised when each migration has been applied to the database.
        /// </summary>
        public event EventHandler<MigrationResult> MigrationApplied;

        /// <summary>
        /// Event rised when some action needs confirmation.
        /// </summary>
        public event EventHandler<ConfirmationEventArgs> Confirm;

        public IMongoDatabase Database { get; set; }
        public MigrationLocator Locator { get; set; }
        public DatabaseStatus Status { get; set; }

        private readonly MigrationRunnerOptions _options;

        [Obsolete("This ctor is obsolete and can be removed in feature releases. Please use MigrationRunner(MigrationRunnerOptions options) instead.")]
        public MigrationRunner(string connectionString, string databaseName)
            : this(new MongoClient(connectionString).GetDatabase(databaseName))
        { }

        static MigrationRunner()
        {
            BsonSerializer.RegisterSerializer(typeof(Version), new VerstionSerializer());
        }

        public MigrationRunner(MigrationRunnerOptions options)
        {
            var database = new MongoClient(options.ConnectionString).GetDatabase(options.DatabaseName);

            this.Database = database;
            this.Locator = new MigrationLocator();
            this.Status = new DatabaseStatus(database);
            _options = options;
        }

        [Obsolete("This ctor is obsolete and can be removed in feature releases. Please use MigrationRunner(MigrationRunnerOptions options) instead.")]
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
        /// <returns>Message about result of migrating.</returns>
        public MigrationResult UpdateToLatest()
        {
            return UpdateTo(Locator.GetNewestLocalVersion());
        }

        /// <summary>
        /// Migrate to specific version 
        /// </summary>
        /// <param name="targetVersion">Target version. Can be less or greater then current database version.</param>
        /// <returns>Message about result of migrating.</returns>
        public MigrationResult UpdateTo(Version targetVersion)
        {
            var currentVerstion = Status.GetVersion();
            var migrations = Locator.GetMigrations(currentVerstion, targetVersion).ToArray();
            var serverNames = string.Join(',', Database.Client.Settings.Servers);

            var isUp = targetVersion > currentVerstion;

            if (!migrations.Any())
            {
                return new MigrationResult
                {
                    MigrationName = string.Empty,
                    TargetVersion = targetVersion,
                    ServerAdress = serverNames,
                    DatabaseName = Database.DatabaseNamespace.DatabaseName,
                    Message = "Nothing to update."
                };
            }

            if (_options.IsSchemeValidationActive && !string.IsNullOrEmpty(_options.MigrationProjectLocation))
            {
                var validator = new MongoSchemeValidator();
                var validationResult = validator.Validate(migrations, isUp, _options.MigrationProjectLocation, Database);
                if (validationResult.FailedCollections.Any())
                {
                    if (Confirm != null)
                    {
                        var confirmation = new ConfirmationEventArgs
                        {
                            Question = string.Format("Next collection in your database failed document scheme validation: \n {0} \n Continue? (y/n):",
                            string.Join("\n", validationResult.FailedCollections))
                        };
                        Confirm(this, confirmation);
                        if (!confirmation.Continue)
                            return new MigrationResult
                            {
                                MigrationName = string.Empty,
                                TargetVersion = targetVersion,
                                ServerAdress = serverNames,
                                DatabaseName = Database.DatabaseNamespace.DatabaseName,
                                Message = string.Format("Next collection in your database failed document scheme validation: \n {0}",
                                    string.Join("\n", validationResult.FailedCollections))
                            };
                    }
                    else
                    {
                        return new MigrationResult
                        {
                            MigrationName = string.Empty,
                            TargetVersion = targetVersion,
                            ServerAdress = serverNames,
                            DatabaseName = Database.DatabaseNamespace.DatabaseName,
                            Message = string.Format("Next collection in your database failed document scheme validation: \n {0}",
                                    string.Join("\n", validationResult.FailedCollections))
                        };
                    }
                }
            }

            var totalCount = migrations.Length;
            for (int i = 0; i < totalCount; i++)
            {
                if (isUp)
                    migrations[i].Up(Database);
                else
                    migrations[i].Down(Database);

                var m = Status.SaveMigration(migrations[i], isUp);

                if (MigrationApplied == null)
                    continue;

                MigrationApplied(this, new MigrationResult
                {
                    MigrationName = migrations[i].Name,
                    TargetVersion = m.Ver,
                    ServerAdress = serverNames,
                    DatabaseName = Database.DatabaseNamespace.DatabaseName,
                    Message = string.Format("Applying migration {0}, to version {1}. Database: {2}. Servers: {3}",
                        migrations[i].Name,
                        m.Ver,
                        Database.DatabaseNamespace.DatabaseName,
                        serverNames),
                    CurrentNumber = i,
                    TotalCount = totalCount
                });
            }

            return new MigrationResult
            {
                MigrationName = string.Empty,
                TargetVersion = targetVersion,
                ServerAdress = serverNames,
                DatabaseName = Database.DatabaseNamespace.DatabaseName,
                Message = $"Migration database {Database.DatabaseNamespace.DatabaseName} to v{targetVersion} has been completed.",
                CurrentNumber = totalCount,
                TotalCount = totalCount
            };
        }
    }
}