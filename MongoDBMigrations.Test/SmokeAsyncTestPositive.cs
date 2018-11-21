using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDBMigrations.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace MongoDBMigrations.Test
{
    [TestClass]
    public class SmokeAsyncTestPositive
    {
        private const string CONNECTION_STRING = "mongodb://localhost:27017";
        private const string DATABASE = "test";

        [TestMethod]
        public void Database_Migrate_Async_Succeed_Without_Progress()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = CONNECTION_STRING,
                DatabaseName = DATABASE
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            var result = runner.UpdateToAsync(new Version(1, 1, 0), null).Result;
            Debug.WriteLine(result.Message);

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestMethod]
        public void Database_Migrate_Async_Succeed_With_Validation()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = CONNECTION_STRING,
                DatabaseName = DATABASE,
                IsSchemeValidationActive = true,
                MigrationProjectLocation = @"C:\Users\artur\source\repos\MongoDBMigrations\MongoDBMigrations.Test\MongoDBMigrations.Test.csproj"
            };

            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            var result = runner.UpdateToAsync(new Version(1, 1, 0), (validationResult) =>
            {
                return true;
            }).Result;
            Debug.WriteLine(result.Message);

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestMethod]
        public void Database_Migrate_Async_Succeed()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = CONNECTION_STRING,
                DatabaseName = DATABASE
            };

            var runner = new MigrationRunner(options);
            var progress = new Progress<MigrationResult>(ReportProgress);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            var result = runner.UpdateToAsync(new Version(1, 1, 0), null, progress).Result;
            Debug.WriteLine(result.Message);

            Assert.AreEqual(new Version(1, 1, 0).ToString(), result.TargetVersion.ToString());
        }

        [TestCleanup]
        public void CleanUp()
        {
            var options = new MigrationRunnerOptions
            {
                ConnectionString = CONNECTION_STRING,
                DatabaseName = DATABASE
            };
            var runner = new MigrationRunner(options);
            runner.Locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            runner.UpdateTo(Version.V1());
        }

        private void ReportProgress(MigrationResult step)
        {
            Debug.WriteLine(step.Message);
        }
    }
}
