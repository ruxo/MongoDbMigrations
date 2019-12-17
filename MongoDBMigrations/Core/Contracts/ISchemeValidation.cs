namespace MongoDBMigrations
{
    public interface ISchemeValidation
    {
        IMigrationRunner UseSchemeValidation(bool enabled, string migrationProjectLocation = null);
    }
}