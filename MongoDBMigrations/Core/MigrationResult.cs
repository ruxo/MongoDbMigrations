using System.Collections.Generic;

namespace MongoDBMigrations
{
    public class MigrationResult
    {
        public string MigrationName;
        public Version TargetVersion;
        public string ServerAdress;
        public string DatabaseName;
        public string Message;
        public int CurrentNumber;
        public int TotalCount;

        internal static MigrationResult BuildSchemeValidationFailedResult(IEnumerable<string> collections)
        {
            return new MigrationResult
            {
                Message = string.Format("Next collection in your database failed document scheme validation: \n {0}",
                                    string.Join("\n", collections))
            };
        }

        internal static MigrationResult BuildNothingToUpdateResult()
        {
            return new MigrationResult
            {
                Message = "Nothing to update."
            };
        }

        internal static MigrationResult BuildSuccessResult(Version targetVersion, string serverName, string databaseName, int count)
        {
            return new MigrationResult
            {
                TargetVersion = targetVersion,
                ServerAdress = serverName,
                DatabaseName = databaseName,
                Message = $"Migration database {databaseName} to v{targetVersion} has been completed.",
                CurrentNumber = count,
                TotalCount = count
            };
        }
    }
}