using System;
using System.Collections.Generic;
using System.Text;

namespace MongoDBMigrations.Core
{
    public class InterimMigrationResult
    {
        public string MigrationName;
        public Version TargetVersion;
        public string ServerAdress;
        public string DatabaseName;
        public int CurrentNumber;
        public int TotalCount;
    }
}
