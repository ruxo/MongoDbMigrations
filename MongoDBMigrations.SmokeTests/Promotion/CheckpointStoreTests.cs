using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests.Promotion;

[TestClass]
public sealed class CheckpointStoreTests
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

    [TestMethod]
    public void Current_is_zero_when_no_records()
    {
        var store = new CheckpointStore(_db);

        var current = store.Current();

        Assert.IsFalse(Fail(current, out _));
        Assert.AreEqual(0L, current.Unwrap());
    }

    [TestMethod]
    public void Append_advances_current_to_the_record_to_value()
    {
        var store = new CheckpointStore(_db);
        using var session = _db.Client.StartSession();

        var append = store.Append(session, new CheckpointRecord {
            StepName = "first", Role = "source", Direction = "up",
            From = 0, To = 3, AppliedAtUtc = DateTime.UtcNow, Ok = true
        });

        Assert.IsFalse(Fail(append, out _));
        Assert.AreEqual(3L, store.Current().Unwrap());
    }

    [TestMethod]
    public void Append_assigns_increasing_seq()
    {
        var store = new CheckpointStore(_db);
        using var session = _db.Client.StartSession();

        store.Append(session, new CheckpointRecord { StepName = "a", Role = "source", Direction = "up", From = 0, To = 1, AppliedAtUtc = DateTime.UtcNow, Ok = true });
        store.Append(session, new CheckpointRecord { StepName = "b", Role = "source", Direction = "up", From = 1, To = 2, AppliedAtUtc = DateTime.UtcNow, Ok = true });

        Assert.AreEqual(2L, store.Current().Unwrap());
    }
}
