using System;
using System.Collections.Generic;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDBMigrations.Core;
using System.Reflection;
using System.Threading;
using System.Linq;
using MongoDBMigrations.Document;
using System.Security.Cryptography.X509Certificates;
// ReSharper disable MemberCanBePrivate.Global

namespace MongoDBMigrations
{
    public sealed class MigrationEngine : ILocator, ISchemeValidation, IMigrationRunner, IMigrationEnginePluginSupport
    {
        readonly List<MigrationEnginePlugin> _plugins = new();
        readonly List<Action<InterimMigrationResult>> _progressHandlers = new();

        private IMongoDatabase _database = default!;
        private MigrationManager _locator = default!;
        private DatabaseManager _status = default!;
        private bool _schemeValidationNeeded;
        private string _migrationProjectLocation = string.Empty;
        private CancellationToken _token = CancellationToken.None;

        private SslSettings? _tlsSettings;

        static MigrationEngine()
        {
            BsonSerializer.RegisterSerializer(typeof(Version), new VerstionStructSerializer());
        }

        public ILocator UseDatabase(string connectionString, string databaseName, MongoEmulationEnum emulation = MongoEmulationEnum.None)
        {
            var setting = MongoClientSettings.FromConnectionString(connectionString);
            var client = new MongoClient(setting);
            return UseDatabase(client, databaseName, emulation);
        }

        public ILocator UseDatabase(IMongoClient mongoClient, string databaseName, MongoEmulationEnum emulation = MongoEmulationEnum.None) {
            var database = _plugins.Aggregate(mongoClient.SetTls(_tlsSettings), (client, plugin) => plugin.SetupMongoClient(client))
                                   .GetDatabase(databaseName);
            return new MigrationEngine
            {
                _database = database,
                _locator = new MigrationManager(),
                _status = new DatabaseManager(database, emulation)
            };
        }

        public MigrationEngine UseTls(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));

            _tlsSettings = new SslSettings
            {
                ClientCertificates = new[] { certificate },
            };
            return this;
        }

        public IMigrationRunner UseProgressHandler(Action<InterimMigrationResult> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            this._progressHandlers.Add(action);

            return this;
        }

        private MigrationResult RunInternal(Version version)
        {
            try{
                var currentDatabaseVersion = _status.GetVersion();
                var migrations = _locator.GetMigrations(currentDatabaseVersion, version);

                var result = new MigrationResult {
                    ServerAdress = string.Join(",", _database.Client.Settings.Servers),
                    DatabaseName = _database.DatabaseNamespace.DatabaseName,
                    InterimSteps = new List<InterimMigrationResult>(),
                    Success = true
                };

                if (!migrations.Any()){
                    result.CurrentVersion = currentDatabaseVersion;
                    return result;
                }

                if (_token.IsCancellationRequested){
                    _token.ThrowIfCancellationRequested();
                }

                var isUp = version > currentDatabaseVersion;

                if (_schemeValidationNeeded){
                    var validator = new MongoSchemeValidator();
                    var validationResult = validator.Validate(migrations, isUp, _migrationProjectLocation, _database);
                    if (validationResult.FailedCollections.Any()){
                        result.Success = false;
                        var failedCollections = string.Join(Environment.NewLine, validationResult.FailedCollections);
                        throw new InvalidOperationException($"Some schema validation issues found in: {failedCollections}");
                    }
                }

                int counter = 0;

                foreach (var m in migrations){
                    if (_token.IsCancellationRequested){
                        _token.ThrowIfCancellationRequested();
                    }

                    counter++;
                    var increment = new InterimMigrationResult();

                    try{
                        if (isUp)
                            m.Up(_database);
                        else
                            m.Down(_database);

                        var insertedMigration = _status.SaveMigration(m, isUp);

                        increment.MigrationName = insertedMigration.Name;
                        increment.TargetVersion = insertedMigration.Ver;
                        increment.ServerAdress = result.ServerAdress;
                        increment.DatabaseName = result.DatabaseName;
                        increment.CurrentNumber = counter;
                        increment.TotalCount = migrations.Count;
                        result.InterimSteps.Add(increment);
                    }
                    catch (Exception ex){
                        result.Success = false;
                        throw new InvalidOperationException("Something went wrong during migration", ex);
                    }
                    finally{
                        foreach (var action in _progressHandlers)
                            action(increment);
                        result.CurrentVersion = _status.GetVersion();
                    }
                }
                return result;

            }
            finally{
                foreach (var plugin in _plugins)
                    try{
                        plugin.Dispose();
                    }
                    catch (Exception e){
                        Console.WriteLine($"Dispose plugin {plugin.GetType().FullName} error: {e}");
                    }
            }
        }

        public MigrationResult Run(Version? version) =>
            RunInternal(version ?? _locator.GetNewestLocalVersion());

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
            if (!token.CanBeCanceled)
                throw new ArgumentException("Invalid token or it's canceled already.", nameof(token));
            this._token = token;
            return this;
        }

        public IMigrationRunner UseSchemeValidation(bool enabled, string? location)
        {
            this._schemeValidationNeeded = enabled;
            if (enabled)
            {
                if (string.IsNullOrEmpty(location))
                    throw new ArgumentNullException(nameof(location));
                this._migrationProjectLocation = location;
            }
            return this;
        }

        public IMigrationRunner UseCustomSpecificationCollectionName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            _status.SpecCollectionName = name;

            return this;
        }

        IMigrationEnginePluginSupport IMigrationEnginePluginSupport.AddPlugin(MigrationEnginePlugin plugin) {
            _plugins.Add(plugin);
            return this;
        }
    }
}