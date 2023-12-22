namespace MongoDBMigrations.Core;

public class InterimMigrationResult
{
    public string MigrationName = null!;
    public Version TargetVersion;
    public string ServerAdress= null!;
    public string DatabaseName= null!;
    public int CurrentNumber;
    public int TotalCount;
}