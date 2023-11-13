using System;

namespace MongoDBMigrations
{
    public class MigrationNotFoundException : Exception
    {
        public MigrationNotFoundException(string assemblyName, Exception innerException)
            : base(string.Format("Migrations are not found in assembly {0}", assemblyName), innerException)
        {}
    }
}