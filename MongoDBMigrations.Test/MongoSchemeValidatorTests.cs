using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoDBMigrations.Core;

namespace MongoDBMigrations.Test
{
    [TestClass]
    public class MongoSchemeValidatorTests
    {
        private const string CONNECTION_STRING = "mongodb://localhost:27017";
        private const string DATABASE = "test";

        [TestMethod]
        public void TryAddMethodMarkerSuccess()
        {
            var validator = new MongoSchemeValidator();
            string newMarker = "TestMethodName";

            validator.RegisterMethodMarker(newMarker);

            Assert.IsTrue(validator.MethodMarkers.Contains(newMarker));
        }

        [TestMethod]
        public void TryAddDuplicateOfMethodMarker()
        {
            var validator = new MongoSchemeValidator();
            string newMarker = "getCollection";
            int standardCount = validator.MethodMarkers.Count;

            validator.RegisterMethodMarker(newMarker);

            Assert.AreEqual(standardCount, validator.MethodMarkers.Count);
        }

        [TestMethod]
        public void TryValidateCollectionsSuccess()
        {
            var validator = new MongoSchemeValidator();
            var locator = new MigrationLocator();
            locator.LookInAssemblyOfType<_1_1_0_TestMigration>();
            var migration = locator.GetMigrations(Version.V1(), new Version(1, 1, 0));
            var database = new MongoClient(CONNECTION_STRING).GetDatabase(DATABASE);
            
            var result = validator.Validate(migration
                , true
                , DirectoryExtensions.GetCsprojWithTestsDirectoryFullPath()
                , database);

            Assert.IsFalse(result.FailedCollections.Any());
        }
    }
}
