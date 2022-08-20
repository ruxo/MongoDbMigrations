using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class DatabaseCheckerTests
    {
        MongoDaemon.ConnectionInfo _daemon;

        [TestInitialize]
        public void SetUp()
        {
            _daemon = MongoDaemon.Prepare();
            
            var db = new MongoClient(_daemon.ConnectionString).GetDatabase(_daemon.DatabaseName);
            //Create test collection with some data
            db.CreateCollection("clients");
            db.GetCollection<BsonDocument>("clients")
              .InsertMany(new[]{
                   new BsonDocument{ {"name", "Alex"}, {"age", 17}},
                   new BsonDocument{ {"name", "Max"}, {"age", 25}}
               });
        }

        [TestCleanup]
        public void TearDown()
        {
            _daemon.Dispose();
        }

        [TestMethod]
        public void IsDatabaseOutdatedShouldReturnTrue()
        {
            var result = MongoDatabaseStateChecker.IsDatabaseOutdated(_daemon.ConnectionString, _daemon.DatabaseName, typeof(DatabaseCheckerTests).Assembly);
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsDatabaseOutdatedShoudReturnFalse()
        {
            var target = new Version("1.1.0");
            new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(false)
                .Run(target);

            var result = MongoDatabaseStateChecker.IsDatabaseOutdated(_daemon.ConnectionString, _daemon.DatabaseName, typeof(DatabaseCheckerTests).Assembly);
            Assert.IsFalse(result);
        }

        [TestMethod]
        [ExpectedException(typeof(DatabaseOutdatedExcetion))]
        public void ThrowIfDatabaseOutdatedShouldThrowException()
        {
            MongoDatabaseStateChecker.ThrowIfDatabaseOutdated(_daemon.ConnectionString, _daemon.DatabaseName, typeof(DatabaseCheckerTests).Assembly);
        }
    }
}