# MongoDBMigrations

[![NuGet](https://img.shields.io/badge/nuget%20package-v2.0.0-brightgreen.svg)](https://www.nuget.org/packages/MongoDBMigrations/)


MongoDBMigrations using the official [MongoDB C# Driver]( https://github.com/mongodb/mongo-csharp-driver) to migrate your documents in database
No more downtime for schema-migrations. Just write small and simple `migrations`.

We need migrations when:  
  **1.** Rename collections  
  **2.** Rename keys  
  **3.** Manipulate data types  
  **4.** Index creation  
  **5.** Removing collections / data  
  

### New Features!
  - Added: Fluent API
  - Added: Cancelation feature
  - Added: Callback feature
  - Added: CI/CD integration
  - Changed: Whole arhitecture
  - [See more...](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/ReleaseNotes.md)

### Next Feature/Todo
  - Diff calculation
  - Auto generated migrations
  - Migrations inside transaction

### Installation
MongoDBMigrations tested with .NET Core 2.0+  
https://www.nuget.org/packages/MongoDBMigrations/
```
PM> Install-Package MongoDBMigrations -Version 2.0.0
```
### How to use
Create a migration by impelmeting the interface `IMigration`. Best practice for the version is to use [Semantic Versioning](http://semver.org/) but ultimately it is up to you. You could simply use the patch version to count the number of migrations. If there is a duplicate for a specific type an exception is thrown on initialization.
This is the simple migration template. Method `Up` used for migrate your database forward and `Down` to rollback thus these methods must do the opposite things. Please keep it in mind. You can use any version number grater then `0.0.0`. In case if you alread have some migrations you shoud choose vertion upper then existed ones.

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
  
Use next code for initialize `MigrationEngine` and start migration.
```csharp
new MigrationEngine().UseDatabase(connectionString, databaseName) //Required to use specific db
    .UseAssembly(assemblyWithMigrations) //Required
    .UseSchemeValidation(bool) //Optional true or false
    .UseCancelationToken(token) //Optional if you wanna have posibility to cancel migration process. Might be usefull when you have many migrations and some interaction with user.
    .UseProgressHandler(Action<> action) // Optional some delegate that will be called each migration
    .Run(targetVersion) // Execution call. Might be called without targetVersion, in that case, the engine will choose the latest available version.
```
**In case if handler does not found and validation has failed** - migration process will cancel automatically.

If you not test your migration yet, mark it by `IgnoreMigration` attribute, and runner will skip it.

You can't check if database is outdated.
```csharp
var dbChecker = new DatabaseManager(database); //database is a IMongoDatabase instance
var isOutdated = dbChecker.IsNotLatestVersion(newestVersion) //newestVersion is a newest available migration.
```
### CI/CD
Now you have a chance to integrate mongo database migration engine in your CI pipeline. In repository you can found `MongoDBRunMigration.ps1` script. This approach allows you to have some backup rollback in case of any failure during migration.
|Parameter|Description|
|-|-|
|connectionString|Database connection string|
|databaseName|Name of the database|
|backupLocation|Folder for the backup that will be created befor migration|
|migrationsAssemblyPath|Path to the assembly with migration classes|
Tips
--
1. Use **{migrationVerstion}_{migrationName}.cs** pattern of you migration classes.
1. Save you migrations in non-production assamblies and use method `LookInAssemblyOfType<T>()` of `MigratiotionLocator` for find them.
1. Keep migrations as simple as possible
1. Do not couple migrations to your domain types, they will be brittle to change, and the point of a migration is to update the data representation when your model changes.
1. Stick to the mongo BsonDocument interface or use javascript based mongo commands for migrations, much like with SQL, the mongo javascript API is less likely to change which might break migrations
1. Add an application startup check that the database is at the correct version **(I plan to implement helpers in feature releases)**
1. Write tests of your migrations, TDD them from existing data scenarios to new forms. Use `IgnoreMigration`attribute while WIP.
1. Automate the deployment of migrations **(I plan to implement helpers in feature releases)**


License
----
MongoDbMigrations is licensed under [MIT](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/MIT.md "Read more about the MIT license form"). Refer to license.txt for more information.  
**Free Software, Hell Yeah!**