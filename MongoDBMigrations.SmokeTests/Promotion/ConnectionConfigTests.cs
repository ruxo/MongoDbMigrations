using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class ConnectionConfigTests
{
    static IConfiguration Config(string env, string conn, string db) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"Migrations:Environments:{env}:ConnectionString"] = conn,
            [$"Migrations:Environments:{env}:DatabaseName"]     = db,
        }).Build();

    [TestMethod]
    public void Resolves_database_by_env_name()
    {
        var cfg = Config("staging", "mongodb://localhost:27017", "app_staging");

        var result = ConnectionConfig.Resolve(cfg, "staging");

        Assert.IsFalse(Fail(result, out _));
        Assert.AreEqual("app_staging", result.Unwrap().DatabaseNamespace.DatabaseName);
    }

    [TestMethod]
    public void Missing_environment_is_invalid_request()
    {
        var cfg = Config("staging", "mongodb://localhost:27017", "app_staging");

        Assert.IsTrue(Fail(ConnectionConfig.Resolve(cfg, "prod"), out var e));
        Assert.AreEqual(INVALID_REQUEST, e?.Code);
    }

    [TestMethod]
    public void Connection_override_is_honored()
    {
        var cfg = Config("new", "", "");   // not configured

        var result = ConnectionConfig.Resolve(cfg, "new", "mongodb://localhost:27017/app_eu2");

        Assert.AreEqual("app_eu2", result.Unwrap().DatabaseNamespace.DatabaseName);
    }
}
