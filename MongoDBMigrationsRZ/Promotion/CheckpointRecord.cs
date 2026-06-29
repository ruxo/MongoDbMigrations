using System;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoDBMigrations;

[BsonIgnoreExtraElements]
public sealed class CheckpointRecord
{
    [BsonElement("seq")]          public long Seq { get; set; }
    [BsonElement("stepName")]     public string StepName { get; set; } = string.Empty;
    [BsonElement("role")]         public string Role { get; set; } = string.Empty;
    [BsonElement("direction")]    public string Direction { get; set; } = string.Empty;
    [BsonElement("from")]         public long From { get; set; }
    [BsonElement("to")]           public long To { get; set; }
    [BsonElement("appliedAtUtc")] public DateTime AppliedAtUtc { get; set; }
    [BsonElement("ok")]           public bool Ok { get; set; }
}
