using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

[ExcludeFromCodeCoverage]
public class DatabaseOutdatedException(Version databaseVersion, Version targetVersion) : Exception(
    $"Current database version: {databaseVersion}. You must update database to {targetVersion}.");