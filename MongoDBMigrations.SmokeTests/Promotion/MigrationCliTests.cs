using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBMigrations;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class MigrationCliTests
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

    sealed class FailingStep(long id) : SourceStep
    {
        public override long Id => id;
        public override string Name => "failing";
        public override Outcome<Unit> Up(StepContext ctx) => ErrorInfo.New(INVALID_REQUEST, "boom");
    }

    MigrationApp App() => MigrationApp.Create()
        .Source("dev", d => d.Step(new InsertStep(1)).Step(new InsertStep(2)))
        .Build().Unwrap();

    [TestMethod]
    public async Task Apply_command_returns_zero_and_advances()
    {
        var code = await MigrationCli.RunAsync(App(), new[] { "apply" }, _daemon.ConnectionString, _daemon.DatabaseName);

        Assert.AreEqual(0, code);
        Assert.AreEqual(2L, App().CurrentCheckpoint(_daemon.ConnectionString, _daemon.DatabaseName).Unwrap());
    }

    [TestMethod]
    public async Task Status_command_returns_zero()
    {
        var code = await MigrationCli.RunAsync(App(), new[] { "status" }, _daemon.ConnectionString, _daemon.DatabaseName);
        Assert.AreEqual(0, code);
    }

    [TestMethod]
    public async Task Unknown_command_returns_two()
    {
        var code = await MigrationCli.RunAsync(App(), new[] { "frobnicate" }, _daemon.ConnectionString, _daemon.DatabaseName);
        Assert.AreEqual(2, code);
    }

    [TestMethod]
    public async Task Null_args_returns_two()
    {
        var code = await MigrationCli.RunAsync(App(), null!, _daemon.ConnectionString, _daemon.DatabaseName);
        Assert.AreEqual(2, code);
    }

    [TestMethod]
    public async Task Apply_with_failing_step_returns_one()
    {
        var app = MigrationApp.Create()
            .Source("dev", d => d.Step(new FailingStep(1)))
            .Build().Unwrap();

        var code = await MigrationCli.RunAsync(app, new[] { "apply" }, _daemon.ConnectionString, _daemon.DatabaseName);

        Assert.AreEqual(1, code);
    }
}
