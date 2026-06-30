namespace MongoDBMigrations;

public abstract class DeltaStep : MigrationStep
{
    public abstract long From { get; }
    public abstract long To { get; }
}
