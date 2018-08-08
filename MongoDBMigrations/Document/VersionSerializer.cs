using System;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace MongoDBMigrations
{
    public class VerstionSerializer : SerializerBase<Version>
    {
        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Version value)
        {
            context.Writer.WriteString(value.ToString());
        }

        public override Version Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var ver = context.Reader.ReadString();
            return new Version(ver);
        }
    }
}