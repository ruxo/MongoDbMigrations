namespace MongoDBMigrations;

public abstract class BootstrapStep : MigrationStep
{
    public abstract long To { get; }
}
