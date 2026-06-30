using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace MongoDBMigrations;

public static class ConnectionConfig
{
    public static Outcome<IMongoDatabase> Resolve(IConfiguration config, string env, string? connectionOverride = null)
    {
        var section = config.GetSection($"Migrations:Environments:{env}");
        var connectionString = connectionOverride ?? section["ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            return ErrorInfo.New(INVALID_REQUEST, $"No connection string for environment '{env}'.");

        if (Fail(TryCatch(() => new MongoUrl(connectionString)), out var ue, out var url)) return ue.Trace();
        var databaseName = section["DatabaseName"] is { Length: > 0 } d ? d : url.DatabaseName;
        if (string.IsNullOrWhiteSpace(databaseName))
            return ErrorInfo.New(INVALID_REQUEST, $"No database name for environment '{env}' (set DatabaseName or include it in the connection string).");

        return TryCatch(() => new MongoClient(url).GetDatabase(databaseName));
    }
}
