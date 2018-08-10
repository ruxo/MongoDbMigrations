# MongoDBMigrations

[![NuGet](https://img.shields.io/badge/nuget%20package-v1.0.0-brightgreen.svg)](https://www.nuget.org/packages/MongoDBMigrations/)


MongoDBMigrations using the official [MongoDB C# Driver]( https://github.com/mongodb/mongo-csharp-driver) to migrate your documents in database
No more downtime for schema-migrations. Just write small and simple `migrations`.

We need migrations when:
1. Rename collections
1. Rename keys
1. Manipulate data types
1. Index creation
1. Removing collections / data

### New Features!
  - Roll forward/back manual created migrations
  - Auto find migrations in assemblies for migration beetwen current and target versions.

### Next Feature/Todo
  - Diff calculation
  - Auto generated migrations
  - Testable migrations
  - Migration as part of CI
  - Async implementation
  - 
### Installation
MongoDBMigrations tested with .NET Core 2.0+
https://www.nuget.org/packages/MongoDBMigrations/
```
PM> Install-Package MongoDBMigrations
```
### How to use
Create a migration by impelmeting the interface `IMigration`. Best practice for the version is to use [Semantic Versioning](http://semver.org/) but ultimately it is up to you. You could simply use the patch version to count the number of migrations. If there is a duplicate for a specific type an exception is thrown on initialization.
This is the simple migration template. Method `Up` used for migrate your database forward and `Down` to rollback thus these methods must do the opposite things. Please keep it in mind.
```csharp
//Create migration
public class MyTestMigration : IMigration
{
    public Verstion Version => new Version(1,1,0);
    public string Name => "Some descrioption about this migration.";
    public void Up(IMongoDatabase database)
    {
        // ...
    }
    
    public void Down(IMongoDatabase database)
    {
        // ...
    }
}
```
  
Use next code for initialize `MigrationRunner` and start migration.
```csharp
var connectionString = "mogno://localhost:27017";
var dbName = "testDatabase";
//Create instance of runner
var runner = new MigrationRunner(connectionString, dbName);
//Find and set assembly with our migrations. 
runner.Locator.LookInAssemblyOfType<MyTestMigration>();
//Start migration to version 1.1.0 when you don't need result
runner.UpdateTo(new Version(1,1,0));

//Start migration to version 1.0.0 and getting result of each migration between current and target versions
var result = runner.UpdateTo(new Version(1,0,0));
while(result.MoveNext())
{
    Console.WriteLine(result.Current.Message);
}
```

Tips
--
1. Use **{migrationVerstion}_{migrationName}.cs** pattern of you migration classes.
1. Save you migrations in non-production assamblies and use method `LookInAssemblyOfType<T>()` of `MigratiotionLocator` for find them.
1. Keep migrations as simple as possible
1. Do not couple migrations to your domain types, they will be brittle to change, and the point of a migration is to update the data representation when your model changes.
1. Stick to the mongo BsonDocument interface or use javascript based mongo commands for migrations, much like with SQL, the mongo javascript API is less likely to change which might break migrations
1. Add an application startup check that the database is at the correct version **(I plan to implement helpers in feature releases)**
1. Write tests of your migrations, TDD them from existing data scenarios to new forms **(I plan to implement helpers in feature releases)**
1. Automate the deployment of migrations **(I plan to implement helpers in feature releases)**


License
----
MongoDbMigrations is licensed under [MIT](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/MIT.md "Read more about the MIT license form"). Refer to license.txt for more information.
**Free Software, Hell Yeah!**