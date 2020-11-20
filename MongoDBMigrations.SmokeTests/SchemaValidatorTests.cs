using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class SchemaValidatorTests
    {
        private readonly MongoDaemon _daemon;

        public SchemaValidatorTests()
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
        }

        [TestCleanup]
        public void TearDown()
        {
            _daemon.Dispose();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ValidatorShouldThrowExceptionBecauseSchemaIsInconsistant()
        {
            _daemon.Execute("db.clients.insertMany([{name:'Alex', isActive:true, age: 17},{name:'Max'}])");
            var target = new Version(1,0,0);
            new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(true, "/Users/arthur_osmokiesku/Git/mongodbmigrations/MongoDBMigrations.SmokeTests/MongoDBMigrations.SmokeTests.csproj")
                .Run(target);
        }

        [TestMethod]
        public void ValidatorShouldPass()
        {
            _daemon.Execute("db.clients.insertMany([{name:'Alex', age: 17},{name:'Max', age: 25}])");
            var target = new Version(1, 0, 0);
            var result = new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(true, "/Users/arthur_osmokiesku/Git/mongodbmigrations/MongoDBMigrations.SmokeTests/MongoDBMigrations.SmokeTests.csproj")
                .Run(target);

            Assert.IsTrue(result.InterimSteps.Count > 0);
            Assert.AreEqual(target, result.CurrentVersion);
        }
    }
}
