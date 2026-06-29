using System.Threading;
using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class StepContext(IMongoDatabase database, IClientSessionHandle session, CancellationToken cancellation)
{
    public IMongoDatabase Database { get; } = database;
    public IClientSessionHandle Session { get; } = session;
    public CancellationToken Cancellation { get; } = cancellation;
}
