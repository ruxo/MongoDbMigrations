
# MongoDBMigrations [![NuGet](https://img.shields.io/badge/nuget%20package-v2.2.0-brightgreen.svg)](https://www.nuget.org/packages/MongoDBMigrations/)

You can support me in the development of this useful library. I have big plans you can find them in todo below. I will appreciate pizza and beer;)

[Just follow the link, and donate me any amount you want :)](https://send.monobank.ua/PRwBX6QKN)

MongoDBMigrations using the official [MongoDB C# Driver](https://github.com/mongodb/mongo-csharp-driver) to migrate your documents in database. This library supports on-premis Mongo instances, Azure CosmosDB (MongoAPI) and AWS DocumentDB.

No more downtime for schema-migrations. Just write small and simple `migrations`.

We need migrations when:

**1.** Rename collections

**2.** Rename keys

**3.** Manipulate data types

**4.** Index manipulation

**5.** Removing collections / data

  

### New Features!
- Added: SSH support
- Added: TLS/SSL support (experimental feature)
- Added: Custom specification collection name
- Added: New overload for the method `UseDatabase`
- Changed: Upgraded to the newest version of .NET Mongo Driver
- Chnaged: Fixed [list of bugs](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/issues?version=v2.1.0)

-  [See more...](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/ReleaseNotes.md)

### Contribution
Guys, unfortunately, I can't spend much time on this project, that is way **since now you are able to create a pull request** and develop some features, which can be useful. I will review that requests and merge them. Please don't forget about unit tests :joy: I hope this step will speed up the evolution of this project. And just to set a vector below I specified a list of preferred features for the community:

- Support replicas
- Migration inside transsaction
- Check the availability to work with Mongo Atlas
- Extend functionality for scheme validator (base implementation is already in place)

### Next Feature/Todo
- Diff calculation
- Detailed migration report
- Auto generated migrations

### Installation
MongoDBMigrations tested with .NET Core 2.0+
https://www.nuget.org/packages/MongoDBMigrations/

```

PM> Install-Package MongoDBMigrations -Version 2.2.0

```

### How to use

Create a migration by implementing the interface `IMigration`. The best practice for the version is to use [Semantic Versioning](http://semver.org/) but ultimately it is up to you. You could simply use the patch version to count the number of migrations. If there is a duplicate for a specific type an exception is thrown on initialization.

This is a simple migration template. Method Up is used to migrate your database forward and Down to rollback thus these methods must do the opposite things. Please keep it in mind. You can use any version number greater than `0.0.0`. In case you already have some migrations you should choose a version upper than the existing ones.

  

```csharp

//Create migration

public  class  MyTestMigration : IMigration

{

public MongoDBMigrations.Version Version => new MongoDBMigrations.Version(1, 1, 0);

public  string  Name => "Some descrioption about this migration.";

public  void  Up(IMongoDatabase  database)

{

// ...

}

public  void  Down(IMongoDatabase  database)

{

// ...

}

}

```

#### It is really easy to use this library, just follow all these steps below (you should use *one or more* methods from each step):
|Step #0|Step #1|Step #2|Step #3|Step #4|Step #5|
|:---|:---|:---|:---|:---|:---|
|Create an engine|Database, connection features|Migration classes| Validations|Hadling features|Excecution
|`new MigrationEngine()`|`UseSshTunnel(...)` `UseTls(...)` `UseDatabase(...)`|`UseAssemblyOfType(...)` `UseAssemblyOfType<T>()` `UseAssembly(...)`|`UseSchemeValidation(...)`| `UseProgressHandler(...)` `UseCancelationToken(...)` `UseCustomSpecificationCollectionName(...)`|`Run()`|


Use the following code for initialize `MigrationEngine` and start migration.

```csharp

new  MigrationEngine()

.UseSshTunnel(sshServerAdress, user, privateKeyFileStream, mongoAdress, keyFilePassPhrase) //Use if you want to connect to your DB via SSH tunel. keyFilePassPhrase is optional.

.UseTls(cert) //Use if your database requires TLS. Please use X509Certificate2 instance as a cert value

.UseDatabase(connectionString, databaseName) //Required to use specific db

.UseAssembly(assemblyWithMigrations) //Required

.UseSchemeValidation(bool, string) //Optional if you want to ensure that all documents in collections, that will be affected in the current run, has a consistent structure. Set a true and absolute path to *.csproj file with migration classes or just false.

.UseCancelationToken(token) //Optional if you wanna have the possibility to cancel the migration process. Might be useful when you have many migrations and some interaction with the user.

.UseProgressHandler(Action<> action) // Optional some delegate that will be called each migration

.Run(targetVersion) // Execution call. Might be called without targetVersion, in that case, the engine will choose the latest available version.

```

**In case if the handler does not found and validation has failed** - migration process will be canceled automatically.
If you haven't tested your migration yet, mark it with `IgnoreMigration` attribute, and the runner will skip it.
You can't check if the database is outdated by dint of static class `MongoDatabaseStateChecker`

  

| Method | Description |
| :--- | :--- |
| `ThrowIfDatabaseOutdated(connectionString, databaseName, migrationAssambly, emulation)` | Check is DB outdated and throw `DatabaseOutdatedExcetion` if yes. MigrationAssambly is optional. If not set method will find migration in executing assembly. Emulation has a `None` value by default for Mongo databases, but you should use the `AzureCosmos` option in case of Azure Cosmos DB |
|`IsDatabaseOutdated(connectionString, databaseName, migrationAssambly, emulation)`|Returns `true` if DB outdated (you have unapplied migrations) otherwise `false`. MigrationAssambly is optional. If not set method will find migration in executing assembly. Emulation has a `None` value by default for Mongo databases, but you should use the `AzureCosmos` option in case of Azure Cosmos DB|

  

#### Azure CosmosDB support
Begins from `v2.1.0` this library supports databases in Azure CosmosDB service. There might be two cases:
* You haven't use this library before. No manual action needed, everithing will work ok.
* You already have some executed migrations with earlier version of this library. In this case you should ensure that you have an ascending index for filed `applied` in `_migrations` collection. If you don't have this index please create them prior you strart the migration run.

#### AWS DocumentDB support
Bagins from `v2.2.0` this library supports databases in AWS DocumentDB service.

  

#### CI/CD
Now you have a chance to integrate the mongo database migration engine in your CI pipeline. In repository you can found `MongoDBRunMigration.ps1` script. This approach allows you to have some backup rollback in case of any failure during migration.

Call the following commands prior to using this PS1 file:

```ps1

Set-Alias mongodump <path_without_spaces>

Set-Alias mongorestore <path_without_spaces>

```

Paths should lead to executable files (*.exe). Please, modify the PS1 file if you have any authorization in your database.

  

|Parameter|Description|
|:---|:---|
|connectionString|Database connection string e.g. localhost:27017|
|databaseName|Name of the database|
|backupLocation|Folder for the backup that will be created befor migration|
|migrationsAssemblyPath|Path to the assembly with migration classes|


----
Tips

1. Use **{migrationVerstion}_{migrationName}.cs** pattern of you migration classes.
2. Save your migrations in non-production assemblies and use the method `LookInAssemblyOfType<T>()` of `MigratiotionLocator` to finding them.
3. Keep migrations as simple as possible
4. Do not couple migrations to your domain types, they will be brittle to change, and the point of migration is to update the data representation when your model changes.
5. Stick to the mongo BsonDocument interface or use javascript based mongo commands for migrations, much like with SQL, the mongo javascript API is less likely to change which might break migrations
6. Add an application startup check that the database is at the correct version
7. Write tests of your migrations, TDD them from existing data scenarios to new forms. Use `IgnoreMigration`attribute while WIP.
8. Automate the deployment of migrations

----
License
MongoDbMigrations is licensed under [MIT](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/src/master/MIT.md  "Read more about the MIT license form"). Refer to license.txt for more information.

**Free Software, Hell Yeah!**