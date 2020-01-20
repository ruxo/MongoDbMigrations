using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoDBMigrations.SmokeTests
{
    [TestFixture]
    public class SomeTest
    {
        private readonly MongoDaemon _daemon;

        public SomeTest()
        {
            _daemon = new MongoDaemon();
        }

        [SetUp]
        public void SetUp()
        {
            //Drop all data from database
            _daemon.Query("db.users.drop()");
            _daemon.Query("db.getCollection('_migrations').drop()");
            //Create test collection with some data
            _daemon.Query("db.createCollection('users')");
            _daemon.Query("db.users.insertMany([{name:'Alex', age: 17},{name:'Max', age: 25}])");
        }

        [TestCase("1.0.0")]
        [TestCase("0.0.0")]
        [TestCase(null)] //To highest version
        public void UpdateTestSuccess(string version)
        {
            var target = new Version(version);
            var result = MigrationEngine.UseDatabase(MongoDaemon.ConnectionString, MongoDaemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run(target);

            Assert.IsTrue(result.InterimSteps.Count > 0);
            Assert.AreEqual(target, result.CurrentVersion);
        }
    }
}
