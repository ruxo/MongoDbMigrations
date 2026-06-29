using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class MigrationAppTests
{
    MongoDaemon.ConnectionInfo _daemon = null!;

    [TestInitialize] public void SetUp() => _daemon = MongoDaemon.Prepare();
    [TestCleanup]    public void TearDown() => _daemon.Dispose();

    sealed class InsertStep(long id) : SourceStep
    {
        public override long Id => id;
        public override string Name => $"step-{id}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("widgets")
               .InsertOne(ctx.Session, new BsonDocument("id", id)));
    }

    [TestMethod]
    public void Build_rejects_duplicate_source_ids()
    {
        var built = MigrationApp.Create()
            .Source("dev", d => d.Step(new InsertStep(1)).Step(new InsertStep(1)))
            .Build();

        Assert.IsTrue(Fail(built, out var e));
        Assert.AreEqual(StepErrors.RegistrationCode, e?.Code);
    }

    [TestMethod]
    public void Build_rejects_missing_source()
    {
        var built = MigrationApp.Create().Build();

        Assert.IsTrue(Fail(built, out var e));
        Assert.AreEqual(StepErrors.RegistrationCode, e?.Code);
    }

    [TestMethod]
    public void ApplySource_then_CurrentCheckpoint_reflects_progress()
    {
        var app = MigrationApp.Create()
            .Source("dev", d => d.Step(new InsertStep(1)).Step(new InsertStep(2)))
            .Build()
            .Unwrap();

        var applied = app.ApplySource(_daemon.ConnectionString, _daemon.DatabaseName, CancellationToken.None);

        Assert.AreEqual(2L, applied.Unwrap());
        Assert.AreEqual(2L, MigrationApp.CurrentCheckpoint(_daemon.ConnectionString, _daemon.DatabaseName).Unwrap());
    }
}
