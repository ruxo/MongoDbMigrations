namespace MongoDBMigrations.Core
{
    public class MigrationRunnerOptions
    {
        /// <summary>
        /// Connection string to mongo database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database name
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Absolute path to *.csproj file of project which contain migrations
        /// </summary>
        public string MigrationProjectLocation { get; set; }

        /// <summary>
        /// If true, runner should validate scheme in collections which will be affected via migrations
        /// </summary>
        public bool IsSchemeValidationActive { get; set; }
    }
}
