using System;
using MongoDB.Driver;

namespace MongoDBMigrations.Core
{
    internal static class IMongoClientExtensions
    {
        public static IMongoClient SetTls(this IMongoClient instance, SslSettings config)
        {
            if (config != null)
            {
                instance.Settings.UseTls = true;
                instance.Settings.SslSettings = config;
            }

            return instance;
        }

        public static IMongoClient SetSsh(this IMongoClient instance, MigrationEngine.SshConfig config)
        {
            if(config != null)
            {
                instance.Settings.Server = new MongoServerAddress(config.BoundHost, checked((int)config.BoundPort));
            }
            return instance;
        }
    }
}
