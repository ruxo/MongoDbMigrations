using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoDBMigrations.SmokeTests
{
    [TestClass]
    public class SimpleUpDownTests
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
                .UseAssembly(Assembly.GetExecutingAssembly())
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
                .UseAssembly(Assembly.GetExecutingAssembly())
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
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(false)
                .Run(target);
        }

        /*
        [TestMethod]
        public void SimpleMigrationViaSSHTunnel()
        {
            var target = new Version(1, 0, 0);

            using(var fs = File.OpenRead("/Users/arthur_osmokiesku/Git/SSH keys/vm-mongodb-server_key.pem"))
            {
                var result = new MigrationEngine().UseSshTunnel(
                        new Document.ServerAdressConfig { Host = "40.127.203.104", Port = 22 },
                        "azureuser",
                        fs,
                        new Document.ServerAdressConfig { Host = "127.0.0.1", Port = 27017 })
                    .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                    .UseAssembly(Assembly.GetExecutingAssembly())
                    .UseSchemeValidation(false)
                    .Run(target);

                Assert.AreEqual(target, result.CurrentVersion);
            }
        }

        [TestMethod]
        public void SimpleMigrationViaTls()
        {
            var target = new Version(1, 0, 0);

            var cert = new X509Certificate2("/Users/arthur_osmokiesku/Git/SSH keys/test-client.pfx", "Test1234", X509KeyStorageFlags.Exportable);
            var result = new MigrationEngine()
                .UseTls(cert)
                .UseDatabase(_daemon.ConnectionString, _daemon.DatabaseName)
                .UseAssembly(Assembly.GetExecutingAssembly())
                .UseSchemeValidation(false)
                .Run(target);

            Assert.AreEqual(target, result.CurrentVersion);
        }
        */
    }
}