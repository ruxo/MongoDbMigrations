using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class DatabaseCheckerTests
    {
        private readonly MongoDaemon _daemon;


        public DatabaseCheckerTests()
        {
            _daemon = new MongoDaemon();
        }

        [TestInitialize]
        public void SetUp()
        {
            //Drop all data from the database
            _daemon.Execute("db.clients.drop()");
            _daemon.Execute("db.getCollection('_migrations').drop()");
            //Create test collection with some data
            _daemon.Execute("db.createCollection('clients')");
            _daemon.Execute("db.clients.insertMany([{name:'Alex', age: 17},{name:'Max', age: 25}])");
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
                .UseAssemblyOfType<MongoDaemon>()
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
