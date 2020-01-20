using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDBMigrations.Core;
using System.Reflection;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;

namespace MongoDBMigrations
{
    public sealed class MigrationEngine : ILocator, ISchemeValidation, IMigrationRunner
    {
        private IMongoDatabase _database;
        private MigrationLocator _locator;
        private DatabaseStatus _status;
        private bool _schemeValidationNeeded;
        private string _migrationProjectLocation;
        private CancellationToken _token;
        private IList<Action<InterimMigrationResult>> _progressHandlers;

        static MigrationEngine()
        {
            BsonSerializer.RegisterSerializer(typeof(Version), new VerstionSerializer());
        }

        public static ILocator UseDatabase(string connectionString, string databaseName)
        {
            var database = new MongoClient(connectionString).GetDatabase(databaseName);
            return new MigrationEngine
            {
                _database = database,
                _locator = new MigrationLocator(),
                _status = new DatabaseStatus(database)
            };
        }

        public IMigrationRunner UseProgressHandler(Action<InterimMigrationResult> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (this._progressHandlers == null)
                this._progressHandlers = new List<Action<InterimMigrationResult>>();

            this._progressHandlers.Add(action);

            return this;
        }

        private MigrationResult RunInternal(Version version)
        {
            var currentDatabaseVersion = _status.GetVersion();
            var migrations = _locator.GetMigrations(currentDatabaseVersion, version);

            var result = new MigrationResult
            {
                ServerAdress = string.Join(",", _database.Client.Settings.Servers),
                DatabaseName = _database.DatabaseNamespace.DatabaseName,
                InterimSteps = new List<InterimMigrationResult>()
            };

            if (!migrations.Any())
            {
                result.CurrentVersion = currentDatabaseVersion;
                return result;
            }

            if (_token.IsCancellationRequested)
            {
                _token.ThrowIfCancellationRequested();
            }

            var isUp = version > currentDatabaseVersion;

            if (_schemeValidationNeeded)
            {
                var validator = new MongoSchemeValidator();
                var validationResult = validator.Validate(migrations, isUp, _migrationProjectLocation, _database);
                if (validationResult.FailedCollections.Any())
                {
                    var failedCollections = string.Join(Environment.NewLine, validationResult.FailedCollections);
                    throw new InvalidOperationException($"Some schema validation issues found in: {failedCollections}");
                }
            }

            int counter = 0;
            foreach (var m in migrations)
            {
                if (_token.IsCancellationRequested)
                {
                    _token.ThrowIfCancellationRequested();
                }

                counter++;
                var increment = new InterimMigrationResult();
                using (var session = _database.Client.StartSession())
                {
                    try
                    {
                        session.StartTransaction();
                        if (isUp)
                            m.Up(session, _database);
                        else
                            m.Down(session, _database);

                        var insertedMigration = _status.SaveMigration(session, m, isUp);
                        session.CommitTransaction();

                        increment.MigrationName = insertedMigration.Name;
                        increment.TargetVersion = insertedMigration.Ver;
                        increment.ServerAdress = result.ServerAdress;
                        increment.DatabaseName = result.DatabaseName;
                        increment.CurrentNumber = counter;
                        increment.TotalCount = migrations.Count;
                        result.InterimSteps.Add(increment);
                    }
                    catch (Exception ex)
                    {
                        session.AbortTransaction();
                        throw new InvalidOperationException("Something went wrong during migration", ex);
                    }
                    finally
                    {
                        if (_progressHandlers != null && _progressHandlers.Any())
                        {
                            foreach (var action in _progressHandlers)
                            {
                                action(increment);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public MigrationResult Run(Version version)
        {
            if(object.ReferenceEquals(version, null))
            {
                version = this._locator.GetNewestLocalVersion();
            }

            if(_token == null || !_token.CanBeCanceled)
            {
                return RunInternal(version);
            }
            else
            {
                return Task.Factory.StartNew(() => RunInternal(version), _token).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        public MigrationResult Run()
        {
            var targetVersion = this._locator.GetNewestLocalVersion();
            return Run(targetVersion);
        }

        public ISchemeValidation UseAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            this._locator.SetAssembly(assembly);
            return this;
        }

        public ISchemeValidation UseAssemblyOfType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            this._locator.LookInAssemblyOfType(type);
            return this;
        }

        public ISchemeValidation UseAssemblyOfType<T>()
        {
            this._locator.LookInAssemblyOfType<T>();
            return this;
        }

        public IMigrationRunner UseCancelationToken(CancellationToken token)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token));

            if (!token.CanBeCanceled)
                throw new ArgumentException($"Invalid token or it's canceled already.", nameof(token));
            this._token = token;
            return this;
        }

        public IMigrationRunner UseSchemeValidation(bool enabled, string _migrationProjectLocation)
        {
            this._schemeValidationNeeded = enabled;
            if(enabled)
            {
                this._migrationProjectLocation = _migrationProjectLocation;
            }
            return this;
        }
    }
}