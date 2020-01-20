using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Migrations
{
    public class _1_0_0_FirstMigrationTest : IMigration
    {
        public Version Version => new Version(1, 0, 0);

        public string Name => "Changing column name";

        public void Down(IClientSessionHandle session, IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("user");
            collection.UpdateMany(session, FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Rename("firstName", "name"));
        }

        public void Up(IClientSessionHandle session, IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("user");
            collection.UpdateMany(session, FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Rename("name", "firstName"));
        }
    }
}
