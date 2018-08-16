using System;

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
    }
}