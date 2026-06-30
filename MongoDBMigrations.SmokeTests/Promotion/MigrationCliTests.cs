using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class MigrationCliTests
{
    MongoDaemon.ConnectionInfo _daemon = null!;

    [TestInitialize] public void SetUp() => _daemon = MongoDaemon.Prepare();
    [TestCleanup]    public void TearDown() => _daemon.Dispose();

    sealed class Ins(long id) : SourceStep
    {
        public override long Id => id;
        public override string Name => $"s-{id}";
        public override Outcome<Unit> Up(StepContext ctx) => TryCatch(() =>
            ctx.Database.GetCollection<BsonDocument>("w").InsertOne(ctx.Session, new BsonDocument("id", id)));
    }
    sealed class FailStep(long id) : SourceStep
    {
        public override long Id => id; public override string Name => "fail";
        public override Outcome<Unit> Up(StepContext ctx) => ErrorInfo.New(INVALID_REQUEST, "boom");
    }

    IConfiguration Config(string env) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"Migrations:Environments:{env}:ConnectionString"] = _daemon.ConnectionString,
            [$"Migrations:Environments:{env}:DatabaseName"]     = _daemon.DatabaseName,
        }).Build();

    MigrationApp App(params SourceStep[] steps)
    {
        var b = MigrationApp.Create();
        return b.Source("dev", d => { foreach (var s in steps) d.Step(s); }).Build().Unwrap();
    }

    [TestMethod]
    public async Task Apply_returns_zero_and_advances()
    {
        var code = await MigrationCli.RunCliAsync(App(new Ins(1), new Ins(2)), new[] { "apply", "--env", "dev" }, Config("dev"));
        Assert.AreEqual(0, code);

        var db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
        Assert.AreEqual(2L, new CheckpointStore(db).Current().Unwrap());
    }

    [TestMethod]
    public async Task Status_returns_zero()
    {
        var code = await MigrationCli.RunCliAsync(App(new Ins(1)), new[] { "status", "--env", "dev" }, Config("dev"));
        Assert.AreEqual(0, code);
    }

    [TestMethod]
    public async Task Apply_failure_returns_one()
    {
        var code = await MigrationCli.RunCliAsync(App(new FailStep(1)), new[] { "apply", "--env", "dev" }, Config("dev"));
        Assert.AreEqual(1, code);
    }

    [TestMethod]
    public async Task Unknown_command_returns_two()
    {
        var code = await MigrationCli.RunCliAsync(App(new Ins(1)), new[] { "frobnicate", "--env", "dev" }, Config("dev"));
        Assert.AreEqual(2, code);
    }

    [TestMethod]
    public async Task Missing_env_returns_two()
    {
        var code = await MigrationCli.RunCliAsync(App(new Ins(1)), new[] { "apply" }, Config("dev"));
        Assert.AreEqual(2, code);
    }
}
