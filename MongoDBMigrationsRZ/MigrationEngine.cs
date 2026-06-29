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

namespace MongoDBMigrations;

[PublicAPI]
public sealed class MigrationEngine : ILocator, ISchemeValidation, IMigrationRunner, IMigrationEnginePluginSupport
{
    readonly List<MigrationEnginePlugin> plugins = new();
    readonly List<Action<InterimMigrationResult>> progressHandlers = new();

    IMongoDatabase database = null!;
    IMigrationSource locator = MigrationSource.FromAssembly(MigrationLocator.GetCurrentAssemblyMigrations());
    DatabaseManager status = null!;
    bool schemeValidationNeeded;
    string migrationProjectLocation = string.Empty;
    CancellationToken token = CancellationToken.None;

    SslSettings? tlsSettings;

    static MigrationEngine() {
        BsonSerializer.RegisterSerializer(typeof(Version), new VersionStructSerializer());
    }

    public ILocator UseDatabase(string connectionString, string databaseName, MongoEmulationEnum emulation = MongoEmulationEnum.None) {
        var setting = MongoClientSettings.FromConnectionString(connectionString);
        var client = new MongoClient(setting);
        return UseDatabase(client, databaseName, emulation);
    }

    public ILocator UseDatabase(IMongoClient mongoClient, string databaseName, MongoEmulationEnum emulation = MongoEmulationEnum.None) {
        var db = plugins.Aggregate(mongoClient.SetTls(tlsSettings), (client, plugin) => plugin.SetupMongoClient(client))
                        .GetDatabase(databaseName);
        return new MigrationEngine {
            database = db,
            status = new DatabaseManager(db, emulation)
        };
    }

    public MigrationEngine UseTls(X509Certificate2 certificate) {
        if (certificate == null)
            throw new ArgumentNullException(nameof(certificate));

        tlsSettings = new SslSettings { ClientCertificates = [certificate] };
        return this;
    }

    public IMigrationRunner UseProgressHandler(Action<InterimMigrationResult> action) {
        if (action == null)
            throw new ArgumentNullException(nameof(action));

        progressHandlers.Add(action);

        return this;
    }

    Outcome<MigrationResult> RunInternal(Version version) {
        try{
            var currentDatabaseVersion = status.GetVersion();
            var migrations = locator.GetMigrationsForExecution(currentDatabaseVersion, version);

            var result = new MigrationResult {
                ServerAdress = string.Join(",", database.Client.Settings.Servers),
                DatabaseName = database.DatabaseNamespace.DatabaseName,
                InterimSteps = new List<InterimMigrationResult>(),
                Success = true
            };

            if (!migrations.Any()){
                result.CurrentVersion = currentDatabaseVersion;
                return result;
            }

            if (token.IsCancellationRequested) return ErrorInfo.New(CANCELLED);

            var isUp = version > currentDatabaseVersion;

            if (schemeValidationNeeded){
                var validator = new MongoSchemeValidator();
                var validationResult = validator.Validate(migrations, isUp, migrationProjectLocation, database);
                if (validationResult.FailedCollections.Any()){
                    result.Success = false;
                    var failedCollections = string.Join(Environment.NewLine, validationResult.FailedCollections);
                    return ErrorInfo.New(INVALID_REQUEST, $"Some schema validation issues found in: {failedCollections}");
                }
            }

            var counter = 0;
            bool? sessionSupported = null;

            foreach (var m in migrations){
                if (token.IsCancellationRequested) return ErrorInfo.New(CANCELLED);

                ++counter;
                var increment = new InterimMigrationResult();

                using var session = database.Client.StartSession();

                var supportSession = sessionSupported ?? true;
                if (sessionSupported is null){
                    if (TryCatch(() => session.StartTransaction()).IsFail){
                        supportSession = false;
                        Console.WriteLine("Mongo DB does not support session. Migration will be executed without session.");
                    }
                    sessionSupported = supportSession;
                }
                else if (supportSession)
                    session.StartTransaction();

                try{
                    if (isUp)
                        m.Up(database, session);
                    else
                        m.Down(database, session);

                    if (Fail(status.SaveMigration(session, m, isUp), out var e, out var insertedMigration)) return e.Trace();

                    increment.MigrationName = insertedMigration.Name;
                    increment.TargetVersion = insertedMigration.Ver;
                    increment.ServerAdress = result.ServerAdress;
                    increment.DatabaseName = result.DatabaseName;
                    increment.CurrentNumber = counter;
                    increment.TotalCount = migrations.Length;
                    result.InterimSteps.Add(increment);

                    if (supportSession)
                        session.CommitTransaction(token);
                }
                catch (Exception ex){
                    if (supportSession && session.IsInTransaction)
                        session.AbortTransaction(token);
                    return ErrorFrom.Exception(ex);
                }
                finally{
                    foreach (var action in progressHandlers)
                        action(increment);
                    result.CurrentVersion = status.GetVersion();
                }
            }
            return result;
        }
        catch (Exception ex){
            return ErrorFrom.Exception(ex);
        }
        finally{
            foreach (var plugin in plugins)
                try{
                    plugin.Dispose();
                }
                catch (Exception e){
                    Console.WriteLine($"Dispose plugin {plugin.GetType().FullName} error: {e}");
                }
        }
    }

    public Outcome<MigrationResult> Run(Version? version)
        => RunInternal(version ?? locator.NewestLocalVersion);

    public ISchemeValidation UseAssembly(Assembly assembly) {
        locator = new MigrationSource(new(MigrationLocator.GetAllMigrations(assembly)), $"assembly {assembly.FullName!}");
        return this;
    }

    public ISchemeValidation Use(IMigrationSource source) {
        locator = source;
        return this;
    }

    public ISchemeValidation UseAssemblyOfType(Type type)
        => UseAssembly(type.Assembly);

    public ISchemeValidation UseAssemblyOfType<T>()
        => UseAssembly(typeof(T).Assembly);

    public IMigrationRunner UseCancelationToken(CancellationToken token) {
        if (!token.CanBeCanceled)
            throw new ArgumentException("Invalid token or it's canceled already.", nameof(token));
        this.token = token;
        return this;
    }

    public IMigrationRunner UseSchemeValidation(bool enabled, string? location) {
        schemeValidationNeeded = enabled;
        if (enabled){
            if (string.IsNullOrEmpty(location))
                throw new ArgumentNullException(nameof(location));
            migrationProjectLocation = location;
        }
        return this;
    }

    public IMigrationRunner UseCustomSpecificationCollectionName(string name) {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));
        status.SpecCollectionName = name;

        return this;
    }

    IMigrationEnginePluginSupport IMigrationEnginePluginSupport.AddPlugin(MigrationEnginePlugin plugin) {
        plugins.Add(plugin);
        return this;
    }
}