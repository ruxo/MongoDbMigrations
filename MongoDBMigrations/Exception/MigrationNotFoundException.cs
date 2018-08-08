using System;

namespace MongoDBMigrations
{
    public class MigrationNotFoundException : Exception
    {
        public MigrationNotFoundException(string assamblyName, Exception innerException)
            : base(string.Format("Migrations not found in assambly {0}", assamblyName), innerException)
        {}
    }
}