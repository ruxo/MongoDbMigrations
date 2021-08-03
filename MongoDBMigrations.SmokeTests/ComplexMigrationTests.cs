using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class ComplexMigrationTests
    {
        private readonly MongoDaemon _daemon;

        public ComplexMigrationTests()
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

            //Run up to the latest migration
            new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
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
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run(downTarget);

            Assert.AreEqual(downTarget, downMigrationResult.CurrentVersion);
            Assert.IsTrue(downMigrationResult.Success);

            var upMigrationResult = new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run();

            Assert.IsTrue(upMigrationResult.Success);
        }
    }
}
