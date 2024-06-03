using System;
using System.Linq;
using System.Reflection;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDBMigrations.Document;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

/// <summary>
/// Works with applied migrations
/// </summary>
public class DatabaseManager
{
    private const string SPECIFICATION_COLLECTION_DEFAULT_NAME = "_migrations";
    private string _specCollectionName = string.Empty;
    private readonly IMongoDatabase _database;

    public string SpecCollectionName
    {
        set
        {
            if (!string.IsNullOrEmpty(value))
                _specCollectionName = value;
        }
        get
        {
            return string.IsNullOrEmpty(_specCollectionName) ? SPECIFICATION_COLLECTION_DEFAULT_NAME : _specCollectionName;
        }
    }

    #region Compatibility Checks
    private bool IsAzureCosmosDBCompatible(bool isInitial)
    {
        if(_database == null)
        {
            throw new TypeInitializationException(nameof(DatabaseManager), new Exception($"{nameof(_database)} hasn't been initialized."));
        }

        if (isInitial) //If it's a fist migration run and there are no records in the _migrations collection.
        {
            //Just create an index
            var indexOptions = new CreateIndexOptions<SpecificationItem>();
            var indexKey = Builders<SpecificationItem>.IndexKeys.Ascending(x => x.ApplyingDateTime);
            var indexModel = new CreateIndexModel<SpecificationItem>(indexKey, indexOptions);
            var collection = _database.GetCollection<SpecificationItem>(SpecCollectionName);
            collection.Indexes.CreateOne(indexModel);
            return true;
        }

        //Check that index exisist and return true, otherwise false.
        var indexes = _database.GetCollection<SpecificationItem>(SpecCollectionName).Indexes.List().ToList();
        var targetIndex = typeof(SpecificationItem)
                         .GetProperty(nameof(SpecificationItem.ApplyingDateTime))!
                         .GetCustomAttribute<BsonElementAttribute>()!
                         .ElementName;
        return indexes.Any(x => x.GetValue("name").ToString()!.StartsWith(targetIndex));
    }

    #endregion

    public DatabaseManager(IMongoDatabase database, MongoEmulationEnum emulation)
    {
        _database = database ?? throw new TypeInitializationException("Database can't be null", null);
        bool isInitial = false;
        if (!_database.ListCollectionNames().ToList().Contains(SpecCollectionName))
        {
            _database.CreateCollection(SpecCollectionName);
            isInitial = true;
        }

        switch(emulation)
        {
            case MongoEmulationEnum.AzureCosmos when !IsAzureCosmosDBCompatible(isInitial):
                throw new InvalidOperationException($@"Your current setup isn't ready for this migration run.
                        Please create an ascending index to the filed '{SpecificationItem.ApplyDateTimeField}'
                        at collection '{SpecCollectionName}' manually and retry the migration run. Be aware that indexing may take some time.");
            default:
                return;
        }

    }
    private IMongoCollection<SpecificationItem> GetAppliedMigrations()
    {
        return _database.GetCollection<SpecificationItem>(SpecCollectionName);
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
    public SpecificationItem? GetLastAppliedMigration() =>
        GetAppliedMigrations()
           .Find(FilterDefinition<SpecificationItem>.Empty)
           .Sort(Builders<SpecificationItem>.Sort.Descending(x => x.ApplyingDateTime))
           .FirstOrDefault();

    /// <summary>
    /// Commit migration to the database.
    /// </summary>
    /// <param name="migration">Migration instance.</param>
    /// <param name="isUp">True if roll forward otherwise roll back.</param>
    /// <returns>Applied migration.</returns>
    internal SpecificationItem SaveMigration(IMigration migration, bool isUp)
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