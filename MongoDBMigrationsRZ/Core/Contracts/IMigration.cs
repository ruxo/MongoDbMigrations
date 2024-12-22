using MongoDB.Driver;

namespace MongoDBMigrations;

public interface IMigration
{
    /// <summary>
    /// Field which consist semantic version in format MAJOR.MINOR.REVISION.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Name of migration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Roll forward method.
    /// </summary>
    /// <param name="database">Instance of MongoDatabase</param>
    /// <param name="session">Current migration session</param>
    void Up(IMongoDatabase database, IClientSessionHandle session);

    /// <summary>
    /// Roll back method.
    /// </summary>
    /// <param name="database">Instance of MongoDatabase</param>
    /// <param name="session">Current migration session</param>
    void Down(IMongoDatabase database, IClientSessionHandle session);
}