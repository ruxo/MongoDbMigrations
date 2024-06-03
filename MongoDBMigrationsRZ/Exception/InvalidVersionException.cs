using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace MongoDBMigrations;

[ExcludeFromCodeCoverage]
public class InvalidVersionException(string version) : Exception($"Invalid value: {version}");