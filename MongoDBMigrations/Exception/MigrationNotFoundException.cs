using System;

namespace MongoDBMigrations
{
    public class MigrationNotFoundException : Exception
    {
        public MigrationNotFoundException(string assemblyName, Exception innerException)
            : base(string.Format("Migrations not found in assambly {0}", assemblyName), innerException)
        {}
    }
}