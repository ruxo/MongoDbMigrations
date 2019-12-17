﻿using System;
using MongoDBMigrations.Core;
using System.Threading;

namespace MongoDBMigrations
{
    public interface IMigrationRunner
    {
        IMigrationRunner UseProgressHandler(Action<InterimMigrationResult> action);
        IMigrationRunner UseCancelationToken(CancellationToken token);
        MigrationResult Run(Version version);
        MigrationResult Run();
    }
}