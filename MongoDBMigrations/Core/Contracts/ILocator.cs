using System;
using System.Reflection;

namespace MongoDBMigrations
{
    public interface ILocator
    {
        ISchemeValidation UseAssemblyOfType(Type type);
        ISchemeValidation UseAssemblyOfType<T>();
        ISchemeValidation UseAssembly(Assembly assembly);
    }
}