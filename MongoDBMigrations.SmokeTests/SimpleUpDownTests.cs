using NUnit.Framework;
using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Text;

namespace MongoDBMigrations.SmokeTests
{
    [TestFixture]
    public class SimpleUpDownTests
    {
        private readonly MongoDaemon _daemon;

        public SimpleUpDownTests()
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

        [OneTimeTearDown]
        public void TearDown()
        {
            _daemon.Dispose();
        }

        [TestCase("1.0.0")]
        [TestCase("1.1.0")]
        public void DefaultUpdateTestSuccess(string version)
        {
            var target = new Version(version);
            var result = new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run(target);

            Assert.IsTrue(result.InterimSteps.Count > 0);
            Assert.AreEqual(target, result.CurrentVersion);
        }

        [TestCase("1.0.0")]
        [TestCase("1.1.0")]
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

        [Test]
        public void MigrationNotFoundShouldThrowException()
        {
            var target = new Version(99,99,99);
            ActualValueDelegate<object> testDelegate = () => new MigrationEngine().UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssemblyOfType<MongoDaemon>()
                .UseSchemeValidation(false)
                .Run(target);

            Assert.That(testDelegate, Throws.TypeOf<MigrationNotFoundException>());
        }
    }
}
