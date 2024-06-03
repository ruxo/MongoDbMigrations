using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

[ExcludeFromCodeCoverage]
public class VersionStringTooLongException(string version)
    : Exception($"Versions must have the format: major.minor.revision, this doesn't match: {version}");