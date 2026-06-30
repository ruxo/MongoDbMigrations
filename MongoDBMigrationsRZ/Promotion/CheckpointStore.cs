using LanguageExt;
using MongoDB.Driver;

namespace MongoDBMigrations;

public sealed class CheckpointStore(IMongoDatabase database)
{
    public const string CollectionName = "_migration_state";

    IMongoCollection<CheckpointRecord> Log => database.GetCollection<CheckpointRecord>(CollectionName);

    public Outcome<long> Current()
        => TryCatch(() => LastOk()?.To ?? 0L);

    public Outcome<Unit> Append(IClientSessionHandle session, CheckpointRecord record)
        => TryCatch(() => {
            record.Seq = (Last()?.Seq ?? 0) + 1;
            Log.InsertOne(session, record);
        });

    CheckpointRecord? Last()
        => Log.Find(FilterDefinition<CheckpointRecord>.Empty)
              .SortByDescending(x => x.Seq)
              .FirstOrDefault();

    CheckpointRecord? LastOk()
        => Log.Find(Builders<CheckpointRecord>.Filter.Eq(x => x.Ok, true))
              .SortByDescending(x => x.Seq)
              .FirstOrDefault();
}
