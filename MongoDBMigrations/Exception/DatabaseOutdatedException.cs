using System;

namespace MongoDBMigrations
{
    public class DatabaseOutdatedExcetion : Exception
    {
        public DatabaseOutdatedExcetion(Version databaseVersion, Version targetVersion)
            : base(string.Format("Current database version: {0}. You must update database to {1}.", databaseVersion, targetVersion))
        { }
    }
}