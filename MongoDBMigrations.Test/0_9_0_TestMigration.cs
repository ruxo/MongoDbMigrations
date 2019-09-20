using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.Test
{
    public class _0_9_0_TestMigration : IMigration
    {
        public Version Version => new Version(0, 9, 0);
        public string Name => "Test migration. Less then zero.";
        public void Down(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("foobar");
            collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Rename("YearsFromBirth", "Age"));
        }

        public void Up(IMongoDatabase database)
        {
            var collection = database.GetCollection<BsonDocument>("foobar");
            collection.UpdateMany(FilterDefinition<BsonDocument>.Empty,
                Builders<BsonDocument>.Update.Rename("Age", "YearsFromBirth"));
        }
    }
}