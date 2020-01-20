using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Migrations
{
    public class _1_1_0_SecondMigrationTest : IMigration
    {
        public Version Version => new Version(1, 1, 0);

        public string Name => "Changing type of age type";

        public void Down(IClientSessionHandle session, IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("user");
            var list = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
            FieldDefinition<BsonDocument, int> fieldDefenition = "users.age";
            foreach (var item in list)
            {
                collection.UpdateOne(session, new BsonDocument("_id", item["_id"]),
                    Builders<BsonDocument>.Update.Set(fieldDefenition, item["age"].ToInt32()));
            }
        }

        public void Up(IClientSessionHandle session, IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("user");
            var list = collection.Find(FilterDefinition<BsonDocument>.Empty).ToList();
            FieldDefinition<BsonDocument, string> fieldDefenition = "users.age";
            foreach (var item in list)
            {
                collection.UpdateOne(session, new BsonDocument("_id", item["_id"]),
                    Builders<BsonDocument>.Update.Set(fieldDefenition, item["age"].ToString()));
            }
        }
    }
}
