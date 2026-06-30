using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class BootstrapRunnerTests
{
    MongoDaemon.ConnectionInfo _daemon = null!;
    IMongoDatabase _db = null!;

    [TestInitialize] public void SetUp()
    {
        _daemon = MongoDaemon.Prepare();
        _db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
    }
    [TestCleanup] public void TearDown() => _daemon.Dispose();

    sealed class Boot(long to) : BootstrapStep
    {
        public override long To => to;
        public override string Name => "bootstrap";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("seed").InsertOne(ctx.Session, new BsonDocument("ready", true)));
    }

    BootstrapRunner Runner(BootstrapStep step) => new(_db, new CheckpointStore(_db), step, "staging-eu2", CancellationToken.None);

    [TestMethod]
    public void Bootstraps_an_empty_env_and_sets_checkpoint_to_To()
    {
        var result = Runner(new Boot(7)).Apply();

        Assert.AreEqual(7L, result.Unwrap());
        Assert.AreEqual(7L, new CheckpointStore(_db).Current().Unwrap());
    }

    [TestMethod]
    public void Refuses_to_bootstrap_a_non_empty_env()
    {
        Runner(new Boot(7)).Apply();                          // now initialized

        var result = Runner(new Boot(7)).Apply();

        Assert.IsTrue(Fail(result, out var e));
        Assert.AreEqual(StepErrors.NotEmptyCode, e?.Code);
    }
}
