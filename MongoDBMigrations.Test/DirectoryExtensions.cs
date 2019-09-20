using System.IO;

namespace MongoDBMigrations.Test
{
    public static class DirectoryExtensions
    {
        public static string GetCsprojWithTestsDirectoryFullPath()
        {
            var currentDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory());
            // CurrentDirectory = *.Test/bin/Debug/netcoreapp2.1/
            // 1st parent = *.Test/bin/Debug/
            // 2nd parent = *.Test/bin/
            // 3rd parent = *.Test/
            var csprojParentDirectoryInfo = currentDirectoryInfo.Parent?.Parent?.Parent;

            return Path.Combine(csprojParentDirectoryInfo?.FullName, $"{csprojParentDirectoryInfo?.Name}.csproj");
        }
    }
}