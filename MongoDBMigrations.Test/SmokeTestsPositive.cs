using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDBMigrations.Core;

namespace MongoDBMigrations.Test
{
    [TestClass]
    [DoNotParallelize]
    public class SmokeTestsPositive
    {
        [TestMethod]
        public void Database_Migrate_Simple_WithValidation_Success()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName,
                MigrationProjectLocation = DirectoryExtensions.GetCsprojWithTestsDirectoryFullPath(),
                IsSchemeValidationActive = true
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.MigrationApplied += Handle;
            var result = runner.UpdateTo(new Version(1, 1, 0));
            Debug.WriteLine(result.Message);
            runner.MigrationApplied -= Handle;

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestMethod]
        public void UpdateTo_TryDownVersionWithMultipleMigrationsButWithoutSchemaValidation_LatestVersionWasIncreased()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            var ver090 = new Version(0,9,0);
            var ver110 = new Version(1,1,0);
            runner.UpdateToLatest();
            var expectedVersion = runner.Status.GetVersion();

            Assert.AreEqual(ver110, expectedVersion);
            runner.UpdateTo(ver090);
            expectedVersion = runner.Status.GetVersion();
            Assert.AreEqual(ver090, expectedVersion);
        }

        [TestMethod]
        public void Database_Migrate_Simple_WithValidationAndConfirmTrue_Success()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName,
                MigrationProjectLocation = DirectoryExtensions.GetCsprojWithTestsDirectoryFullPath(),
                IsSchemeValidationActive = true
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.MigrationApplied += Handle;
            runner.Confirm += PositiveConfirmResult;
            var result = runner.UpdateTo(new Version(1, 1, 0));
            Debug.WriteLine(result.Message);
            runner.MigrationApplied -= Handle;
            runner.Confirm -= PositiveConfirmResult;

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestMethod]
        public void Database_Migrate_Simple_Success()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.MigrationApplied += Handle;
            var result = runner.UpdateTo(new Version(1, 1, 0));
            Debug.WriteLine(result.Message);
            runner.MigrationApplied -= Handle;

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestMethod]
        public void Database_Migrate_Without_LookIn_Call_Success()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.MigrationApplied += Handle;
            var result = runner.UpdateTo(new Version(1, 1, 0));
            Debug.WriteLine(result.Message);
            runner.MigrationApplied -= Handle;

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestMethod]
        public void Database_Outdated_False()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            runner.UpdateTo(Version.Zero());
            var result = runner.Status.IsNotLatestVersion(Version.Zero());

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void Database_Outdated_True()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            var result = runner.Status.IsNotLatestVersion(new Version(999, 999, 999));

            Assert.IsTrue(result);
        }

        [TestMethod]
        [ExpectedException(typeof(DatabaseOutdatedExcetion))]
        public void Database_Outdated_Throw_Exception()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };

            var runner = new MigrationRunner(options);
            runner.Status.ThrowIfNotLatestVersion(new Version(999, 999, 999));
        }

        [TestCleanup]
        public void CleanUp()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = Const.TestDatabase.ConnectionString,
                DatabaseName = Const.TestDatabase.DatabaseName
            };
            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.UpdateTo(Version.Zero());
        }

        private void PositiveConfirmResult(object sender, ConfirmationEventArgs eventArgs)
        {
            Debug.WriteLine(eventArgs.Question);
            eventArgs.Continue = true;
        }

        private void Handle(object sender, MigrationResult result)
        {
            Debug.WriteLine(result.Message);
        }
    }
}
