using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.Test
{
    public class _1_1_0_TestMigration : IMigration
    {
        public Version Version => new Version(1, 1, 0);

        public string Name => "Test migration. Chaning name field.";

        public void Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("foobar");
            collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Rename("NewName", "Name"));
        }

        public void Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("foobar");
            collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Rename("Name", "NewName"));
        }
    }
}
