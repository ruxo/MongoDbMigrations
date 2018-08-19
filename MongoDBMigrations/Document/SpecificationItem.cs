using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace MongoDBMigrations.Document
{
    public class SpecificationItem
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        public string Id { get; set; }

        [BsonElement("n")]
        public string Name { get; set; }

        [BsonElement("v")]
        public Version Ver { get; set; }

        [BsonElement("d")]
        public bool isUp { get; set; }

        [BsonElement("applied")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc, Representation = BsonType.DateTime)]
        public DateTime ApplyingDateTime {get;set;}
    }
}