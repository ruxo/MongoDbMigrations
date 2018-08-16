using System;
using System.Collections.Generic;
using System.Text;

namespace MongoDBMigrations.Core
{
    public class MigrationRunnerOptions
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
    }
}
