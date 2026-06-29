namespace MongoDBMigrations;

public static class StepErrors
{
    public const string NotReversibleCode = "migration.not_reversible";
    public const string DriftCode = "migration.drift";
    public const string RegistrationCode = "migration.registration";

    public static ErrorInfo NotReversible(string stepName)
        => ErrorInfo.New(NotReversibleCode, $"Step '{stepName}' is not reversible.");

    public static ErrorInfo Drift(string env, long expected, long actual)
        => ErrorInfo.New(DriftCode, $"Environment '{env}' is at checkpoint {actual}, expected {expected}.");

    public static ErrorInfo Registration(string message)
        => ErrorInfo.New(RegistrationCode, message);
}
