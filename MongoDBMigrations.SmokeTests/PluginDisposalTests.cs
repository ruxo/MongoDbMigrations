using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests;

[TestClass]
public sealed class PluginDisposalTests
{
    sealed class DisposeCountingPlugin : MigrationEnginePlugin
    {
        public int DisposeCount { get; private set; }

        protected override void Dispose(bool disposing) {
            if (disposing) DisposeCount++;
            base.Dispose(disposing);
        }
    }

    MongoDaemon.ConnectionInfo _daemon = null!;

    [TestInitialize]
    public void SetUp() {
        _daemon = MongoDaemon.Prepare();

        var db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
        db.CreateCollection("clients");
    }

    [TestCleanup]
    public void TearDown() => _daemon.Dispose();

    [TestMethod]
    public void RegisteredPluginIsDisposedExactlyOnceAfterRun() {
        var plugin = new DisposeCountingPlugin();
        var engine = new MigrationEngine();
        ((IMigrationEnginePluginSupport)engine).AddPlugin(plugin);

        var result = engine
                    .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                    .UseAssembly(Assembly.GetExecutingAssembly())
                    .UseSchemeValidation(false)
                    .Run(new Version("1.0.0"));

        result.Unwrap(); // asserts the migration run succeeded
        Assert.AreEqual(1, plugin.DisposeCount);
    }
}
