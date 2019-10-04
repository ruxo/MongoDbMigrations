using System;

namespace MongoDBMigrations
{
    public class BrokenMigrationSequence: Exception
    {
        public BrokenMigrationSequence()
            : base("Broken migration sequence")
        { }
    }
}