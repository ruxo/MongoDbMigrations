using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class ComplexMigrationTests
    {
        MongoDaemon.ConnectionInfo _daemon;
        
        [TestInitialize]
        public void SetUp() {
            _daemon = MongoDaemon.Prepare();

            var db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
            //Create test collection with some data
            db.CreateCollection("clients");
            db.GetCollection<BsonDocument>("clients")
              .InsertMany(new[]{
                   new BsonDocument{ {"name", "Alex"}, {"age", 17}},
                   new BsonDocument{ {"name", "Max"}, {"age", 25}}
               });

            //Run up to the latest migration
            new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(false)
                .Run();
        }

        [TestCleanup]
        public void TearDown()
        {
            _daemon.Dispose();
        }

        [TestMethod]
        public void SawLikeMigrationDownAndThenUp()
        {
            var downTarget = new Version("1.0.0");
            var downMigrationResult = new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(false)
                .Run(downTarget);

            Assert.AreEqual(downTarget, downMigrationResult.CurrentVersion);
            Assert.IsTrue(downMigrationResult.Success);

            var upMigrationResult = new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(false)
                .Run();

            Assert.IsTrue(upMigrationResult.Success);
        }
    }
}