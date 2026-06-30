using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class MigrationAppTests
{
    MongoDaemon.ConnectionInfo _daemon = null!;
    IMongoDatabase _db = null!;

    [TestInitialize] public void SetUp()
    {
        _daemon = MongoDaemon.Prepare();
        _db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
    }
    [TestCleanup] public void TearDown() => _daemon.Dispose();

    sealed class Ins(long id) : SourceStep
    {
        public override long Id => id;
        public override string Name => $"s-{id}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("w").InsertOne(ctx.Session, new BsonDocument("id", id)));
    }
    sealed class Delta(long from, long to) : DeltaStep
    {
        public override long From => from; public override long To => to;
        public override string Name => $"d-{from}-{to}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() => { });
    }
    sealed class Boot(long to) : BootstrapStep
    {
        public override long To => to;
        public override string Name => $"boot-{to}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("seed").InsertOne(ctx.Session, new BsonDocument("to", to)));
    }

    MigrationApp Built() => MigrationApp.Create()
        .Source("dev", d => d.Step(new Ins(1)).Step(new Ins(2)))
        .Downstream("staging", new Delta(0, 2))
        .Build().Unwrap();

    [TestMethod]
    public void Build_rejects_duplicate_env_names()
    {
        var built = MigrationApp.Create()
            .Source("dev", d => d.Step(new Ins(1)))
            .Downstream("dev", new Delta(0, 1))          // name clash
            .Build();

        Assert.IsTrue(Fail(built, out var e));
        Assert.AreEqual(StepErrors.RegistrationCode, e?.Code);
    }

    [TestMethod]
    public void Build_freezes_registration()
    {
        var app = MigrationApp.Create().Source("dev", d => d.Step(new Ins(1))).Build().Unwrap();
        Assert.ThrowsExactly<System.InvalidOperationException>(() => app.Downstream("staging", new Delta(0, 1)));
    }

    [TestMethod]
    public void Apply_dispatches_Source_by_role()
    {
        var app = Built();

        Assert.AreEqual(2L, app.Apply("dev", _db, CancellationToken.None).Unwrap());
        Assert.AreEqual(2L, app.Status(_db).Unwrap());
    }

    [TestMethod]
    public void Apply_to_unknown_env_is_invalid_request()
    {
        var app = Built();
        Assert.IsTrue(Fail(app.Apply("nope", _db, CancellationToken.None), out var e));
        Assert.AreEqual(INVALID_REQUEST, e?.Code);
    }

    [TestMethod]
    public void New_dispatches_Bootstrap_by_role()
    {
        var app = MigrationApp.Create()
            .Source("dev", d => d.Step(new Ins(1)).Step(new Ins(2)))
            .Bootstrap("eu2", new Boot(2))
            .Build().Unwrap();

        Assert.AreEqual(2L, app.New("eu2", _db, CancellationToken.None).Unwrap());
        Assert.AreEqual(2L, app.Status(_db).Unwrap());
    }

    [TestMethod]
    public void New_against_wrong_env_is_invalid_request()
    {
        var app = MigrationApp.Create()
            .Source("dev", d => d.Step(new Ins(1)))
            .Bootstrap("eu2", new Boot(1))
            .Build().Unwrap();

        Assert.IsTrue(Fail(app.New("staging", _db, CancellationToken.None), out var e));
        Assert.AreEqual(INVALID_REQUEST, e?.Code);
    }
}
