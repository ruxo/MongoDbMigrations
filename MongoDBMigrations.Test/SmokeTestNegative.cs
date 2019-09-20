using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDBMigrations.Core;

namespace MongoDBMigrations.Test
{
    [TestClass]
    [DoNotParallelize]
    public class SmokeTestNegative
    {
        [TestMethod]
        [ExpectedException(typeof(MigrationNotFoundException))]
        public void Database_Migrate_To_Version_Without_Impl_Should_Throw_Exception()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.UpdateTo(new Version(999, 999, 999));
        }
    }
}
