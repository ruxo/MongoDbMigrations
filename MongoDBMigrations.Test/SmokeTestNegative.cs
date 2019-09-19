using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDBMigrations.Core;

namespace MongoDBMigrations.Test
{
    [TestClass]
    public class SmokeTestNegative
    {
        private const string CONNECTION_STRING = "mongodb://localhost:27017";
        private const string DATABASE = "test";

        [TestMethod]
        [ExpectedException(typeof(MigrationNotFoundException))]
        public void Database_Migrate_To_Version_Without_Impl_Should_Throw_Exception()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = CONNECTION_STRING,
                DatabaseName = DATABASE
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.UpdateTo(new Version(999, 999, 999));
        }
    }
}
