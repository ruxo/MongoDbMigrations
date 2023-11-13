using System;

namespace MongoDBMigrations
{
    public class VersionStringTooLongException : Exception
    {
        public VersionStringTooLongException(string version)
            : base(string.Format("Versions must have the format: major.minor.revision, this doesn't match: {0}", version))
        {}
    }
}