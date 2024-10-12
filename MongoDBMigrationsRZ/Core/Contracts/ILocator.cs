using System;
using System.Reflection;

// ReSharper disable CheckNamespace

namespace MongoDBMigrations;

[PublicAPI]
public interface ILocator
{
    ISchemeValidation UseAssemblyOfType(Type type);
    ISchemeValidation UseAssemblyOfType<T>();
    ISchemeValidation UseAssembly(Assembly assembly);
    ISchemeValidation Use(IMigrationSource source);
}