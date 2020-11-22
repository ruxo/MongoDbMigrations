using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class SimpleUpDownTests
    {
        private readonly MongoDaemon _daemon;

        public SimpleUpDownTests()
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

        [DataTestMethod]
        [DataRow("1.0.0")]
        [DataRow("1.1.0")]
        public void DefaultUpdateTestSuccess(string version)
        {
            var target = new Version(version);
            var result = new MigrationEngine()
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run(target);

            Assert.IsTrue(result.InterimSteps.Count > 0);
            Assert.AreEqual(target, result.CurrentVersion);
        }

        [DataTestMethod]
        [DataRow("1.0.0")]
        [DataRow("1.1.0")]
        public void WithProgressHandlingUpdateTestSuccess(string version)
        {
            var actions = new List<string>();

            var target = new Version(version);
            var result = new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .UseProgressHandler((i) => actions.Add(i.MigrationName))
                .Run(target);

            Assert.IsTrue(actions.Count == result.InterimSteps.Count);
            Assert.IsTrue(result.InterimSteps.Count > 0);
            Assert.AreEqual(target, result.CurrentVersion);
        }

        [TestMethod]
        [ExpectedException(typeof(MigrationNotFoundException))]
        public void MigrationNotFoundShouldThrowException()
        {
            var target = new Version(99,99,99);
            new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run(target);
        }
    }
}
