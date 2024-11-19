using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests;

[TestClass]
public class SchemaValidatorTests
{
    MongoDaemon.ConnectionInfo daemon = default!;
    IMongoCollection<BsonDocument> db = default!;

    [TestInitialize]
    public void SetUp() {
        daemon = MongoDaemon.Prepare();
        var database = new MongoClient(daemon.ConnectionString).GetDatabase(daemon.DatabaseName);
        //Create test collection with some data
        database.CreateCollection("clients");
        db = database.GetCollection<BsonDocument>("clients");
    }

    [TestCleanup]
    public void TearDown()
    {
        daemon.Dispose();
    }

    static readonly Lazy<string> ProjectPath = new(() => {
        var finder = new DirectoryInfo(Directory.GetCurrentDirectory());
        FileInfo? file;
        while ((file = finder!.EnumerateFiles("MongoDBMigrations.SmokeTests.csproj").FirstOrDefault()) is null)
            finder = finder.Parent;
        return file.FullName;
    });

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void ValidatorShouldThrowExceptionBecauseSchemaIsInconsistent()
    {
        Console.WriteLine($"Database = {daemon.DatabaseName}");

        db.InsertMany(new[]{
            new BsonDocument{ {"name", "Alex"}, {"isActive", true}},
            new BsonDocument{ {"name", "Max"}}
        });
        var target = new Version(1,0,0);
        new MigrationEngine().UseDatabase(daemon.ConnectionString, daemon.DatabaseName)
                             .UseAssembly(Assembly.GetExecutingAssembly())
                             .UseSchemeValidation(true, ProjectPath.Value)
                             .Run(target);
    }

    [TestMethod]
    public void ValidatorShouldPass() {
        db.InsertMany(new[]{
            new BsonDocument{ { "name", "Alex" },{ "age", 17 } },
            new BsonDocument{ { "name", "Max" },{ "age", 25 } }
        });
        var target = new Version(1, 0, 0);
        var result = new MigrationEngine().UseDatabase(daemon.ConnectionString, daemon.DatabaseName)
                                          .UseAssembly(Assembly.GetExecutingAssembly())
                                          .UseSchemeValidation(true, ProjectPath.Value)
                                          .Run(target);

        Assert.IsTrue(result.InterimSteps.Count > 0);
        Assert.AreEqual(target, result.CurrentVersion);
    }
}