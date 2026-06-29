namespace MongoDBMigrations;

public abstract class SourceStep : MigrationStep
{
    public abstract long Id { get; }
}
