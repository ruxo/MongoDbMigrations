using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class SchemaValidatorTests
    {
        MongoDaemon.ConnectionInfo _daemon;
        IMongoCollection<BsonDocument> db;

        [TestInitialize]
        public void SetUp() {
            _daemon = MongoDaemon.Prepare();
            var database = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
            //Create test collection with some data
            database.CreateCollection("clients");
            db = database.GetCollection<BsonDocument>("clients");
        }

        [TestCleanup]
        public void TearDown()
        {
            _daemon.Dispose();
        }

        static readonly Lazy<string> ProjectPath = new(() => {
            var finder = new DirectoryInfo(Directory.GetCurrentDirectory());
            FileInfo file;
            while ((file = finder.EnumerateFiles("MongoDBMigrations.SmokeTests.csproj").FirstOrDefault()) is null)
                finder = finder.Parent;
            return file.FullName;
        });

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ValidatorShouldThrowExceptionBecauseSchemaIsInconsistent()
        {
            db.InsertMany(new[]{
                new BsonDocument{ {"name", "Alex"}, {"isActive", true}},
                new BsonDocument{ {"name", "Max"}}
            });
            var target = new Version(1,0,0);
            new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
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
            var result = new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(true, ProjectPath.Value)
                .Run(target);

            Assert.IsTrue(result.InterimSteps.Count > 0);
            Assert.AreEqual(target, result.CurrentVersion);
        }
    }
}