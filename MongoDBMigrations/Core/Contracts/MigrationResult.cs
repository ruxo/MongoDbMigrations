using MongoDBMigrations.Core;
using System.Collections.Generic;

namespace MongoDBMigrations
{
    public class MigrationResult
    {
        public Version CurrentVersion;
        public List<InterimMigrationResult> InterimSteps;
        public string ServerAdress;
        public string DatabaseName;
        public bool Success;
    }
}