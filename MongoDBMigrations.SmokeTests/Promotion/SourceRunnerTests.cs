using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class SourceRunnerTests
{
    MongoDaemon.ConnectionInfo _daemon = null!;
    IMongoDatabase _db = null!;

    [TestInitialize]
    public void SetUp()
    {
        _daemon = MongoDaemon.Prepare();
        _db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
    }

    [TestCleanup]
    public void TearDown() => _daemon.Dispose();

    sealed class InsertStep(long id, string marker) : SourceStep
    {
        public override long Id => id;
        public override string Name => $"insert-{marker}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("widgets")
               .InsertOne(ctx.Session, new BsonDocument("marker", marker)));
    }

    sealed class FailingStep(long id) : SourceStep
    {
        public override long Id => id;
        public override string Name => "failing";
        public override Outcome<Unit> Up(StepContext ctx) => ErrorInfo.New(INVALID_REQUEST, "nope");
    }

    sealed class ThrowingStep(long id) : SourceStep
    {
        public override long Id => id;
        public override string Name => "throwing";
        public override Outcome<Unit> Up(StepContext ctx) => throw new InvalidOperationException("boom");
    }

    SourceRunner Runner(params SourceStep[] steps)
        => new(_db, new CheckpointStore(_db), steps, CancellationToken.None);

    long Widgets() => _db.GetCollection<BsonDocument>("widgets").CountDocuments(FilterDefinition<BsonDocument>.Empty);

    [TestMethod]
    public void Applies_pending_steps_in_order_and_advances_checkpoint()
    {
        var result = Runner(new InsertStep(1, "a"), new InsertStep(2, "b")).Apply();

        Assert.IsFalse(Fail(result, out _));
        Assert.AreEqual(2L, result.Unwrap());
        Assert.AreEqual(2L, Widgets());
        Assert.AreEqual(2L, new CheckpointStore(_db).Current().Unwrap());
    }

    [TestMethod]
    public void Skips_already_applied_steps()
    {
        var first = Runner(new InsertStep(1, "a")).Apply();
        Assert.AreEqual(1L, first.Unwrap());

        var result = Runner(new InsertStep(1, "a"), new InsertStep(2, "b")).Apply();

        Assert.AreEqual(2L, result.Unwrap());
        Assert.AreEqual(2L, Widgets());          // step 1 not re-run
    }

    [TestMethod]
    public void Failing_step_stops_run_and_leaves_checkpoint_at_last_success()
    {
        var result = Runner(new InsertStep(1, "a"), new FailingStep(2)).Apply();

        Assert.IsTrue(Fail(result, out var e));
        Assert.AreEqual(INVALID_REQUEST, e?.Code);
        Assert.AreEqual(1L, new CheckpointStore(_db).Current().Unwrap());
        Assert.AreEqual(1L, Widgets());          // failing step's transaction aborted
    }

    [TestMethod]
    public void Throwing_step_is_caught_as_failure_and_does_not_advance()
    {
        var result = Runner(new InsertStep(1, "a"), new ThrowingStep(2)).Apply();

        Assert.IsTrue(Fail(result, out _));
        Assert.AreEqual(1L, new CheckpointStore(_db).Current().Unwrap());
    }
}
