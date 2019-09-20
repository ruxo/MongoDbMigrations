# MongoDBMigrations

[![NuGet](https://img.shields.io/badge/nuget%20package-v1.1.1-brightgreen.svg)](https://www.nuget.org/packages/MongoDBMigrations/)


MongoDBMigrations using the official [MongoDB C# Driver]( https://github.com/mongodb/mongo-csharp-driver) to migrate your documents in database
No more downtime for schema-migrations. Just write small and simple `migrations`.

We need migrations when:  
  **1.** Rename collections  
  **2.** Rename keys  
  **3.** Manipulate data types  
  **4.** Index creation  
  **5.** Removing collections / data  
  

### New Features!
  - Added: MongoDB document schema uniformity validation
  - Added: Async impl for runner and database locator
  - [See more...](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/ReleaseNotes.md)

### Next Feature/Todo
  - Diff calculation
  - Auto generated migrations
  - Migration as part of CI

### Installation
MongoDBMigrations tested with .NET Core 2.0+  
https://www.nuget.org/packages/MongoDBMigrations/
```
PM> Install-Package MongoDBMigrations -Version 1.1.1
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
var options = new MigrationRunnerOptions
{
    ConnectionString = CONNECTION_STRING,
    DatabaseName = DATABASE,
    IsSchemeValidationActive = true, // Use true for engage schema validation, otherwise false
    MigrationProjectLocation = @"<some_path_here>" //also needs for schema validation, it's absolute path for *.csproj file with migration classes
};
//Create instance of runner
var runner = new MigrationRunner(options);
//Find and set assembly with our migrations. If you don't call this method, runner try to find migrations in assembly from which the call is made
runner.Locator.LookInAssemblyOfType<MyTestMigration>();
//Start migration to version 1.1.0 when you don't need result
runner.UpdateTo(new Version(1,1,0));

//Start migration to version 1.0.0 and getting result of each migration between current and target versions
var result = runner.UpdateTo(new Version(1,0,0));
```


**Please note:** min version of your migration must be **greater** than 1.0.0. If your migration version less than or equal `Version(1, 0, 0)` then Runner will thrown exception "Sequence contains no elements" with next stacktrace:
```
   at System.Linq.Enumerable.Last[TSource](IEnumerable`1 source)
   at MongoDBMigrations.MigrationLocator.GetMigrations(Version currentVersion, Version targetVerstion)
   at MongoDBMigrations.MigrationRunner.UpdateTo(Version targetVersion)
   at MongoDBMigrations.MigrationRunner.UpdateToLatest()
```


You also can get progress of migration process, just subscribe to `MigrationApplied` event
```csharp
runner.MigrationApplied += Handle;
var result = runner.UpdateTo(new Version(1, 1, 0));
runner.MigrationApplied -= Handle;
```
where `Handle` is:
```csharp
private void Handle(object sender, MigrationResult result)
{
    //Result handling
    Debug.WriteLine(result.Message);
}
```
If you wanna use database document schema validation, please subscribe on event `Confirm` in runner. Inside of handler you can display some message and ask confirmation in following way:
```csharp
private void ConfirmHandler(object sender, ConfirmationEventArgs eventArgs)
{
    Console.WriteLine("Documents in db are inconsistent.");
    // Some code for handling confirmation
    eventArgs.Continue = true; //True if you still want to continue (it can brake you data), otherwise false. 
}
```
**In case if handler does not found and validation has failed** - migration process will cancel automatically.

If you not test your migration yet, mark it by `IgnoreMigration` attribute, and runner will skip it.

You can't check if database is outdated by calling `runner.Status.IsNotLatestVersion(newestVersion))` or `runner.Status.ThrowIfNotLatestVersion(newestVersion)`. The last one throw `DatabaseOutdatedExcetion` when database is outdated.

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