using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class DownstreamRunnerTests
{
    MongoDaemon.ConnectionInfo _daemon = null!;
    IMongoDatabase _db = null!;

    [TestInitialize] public void SetUp()
    {
        _daemon = MongoDaemon.Prepare();
        _db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
    }
    [TestCleanup] public void TearDown() => _daemon.Dispose();

    sealed class Delta(long from, long to) : DeltaStep
    {
        public override long From => from;
        public override long To => to;
        public override string Name => $"delta-{from}-{to}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("promoted").InsertOne(ctx.Session, new BsonDocument("to", to)));
    }

    DownstreamRunner Runner(DeltaStep delta) => new(_db, new CheckpointStore(_db), delta, "staging", CancellationToken.None);
    long Promoted() => _db.GetCollection<BsonDocument>("promoted").CountDocuments(FilterDefinition<BsonDocument>.Empty);

    [TestMethod]
    public void Applies_delta_on_a_matching_env_and_sets_checkpoint_to_To()
    {
        var result = Runner(new Delta(0, 7)).Apply();

        Assert.AreEqual(7L, result.Unwrap());
        Assert.AreEqual(7L, new CheckpointStore(_db).Current().Unwrap());
        Assert.AreEqual(1L, Promoted());
    }

    [TestMethod]
    public void Rejects_when_env_checkpoint_does_not_match_From()
    {
        Runner(new Delta(0, 7)).Apply();                      // env now at 7

        var result = Runner(new Delta(0, 9)).Apply();         // expects From=0 but env is at 7

        Assert.IsTrue(Fail(result, out var e));
        Assert.AreEqual(StepErrors.DriftCode, e?.Code);
        Assert.AreEqual(7L, new CheckpointStore(_db).Current().Unwrap());
    }

    [TestMethod]
    public void Is_a_no_op_when_already_at_To()
    {
        Runner(new Delta(0, 7)).Apply();                      // env at 7

        var result = Runner(new Delta(0, 7)).Apply();         // To == current

        Assert.AreEqual(7L, result.Unwrap());
        Assert.AreEqual(1L, Promoted());                      // not applied twice
    }
}
