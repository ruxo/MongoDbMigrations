using System;

namespace MongoDBMigrations
{
    public class InvalidVersionException : Exception
    {
        public InvalidVersionException(string version)
            : base(string.Format("Invalid value: {0}", version))
        {}
    }
}