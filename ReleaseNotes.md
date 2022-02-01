# MongoDBMigrations

### v2.2.0
  - Added: SSH support
  - Added: TLS/SSL support (experimental feature)
  - Added: Custom specification collection name
  - Added: New overloads for the method `UseDatabase`
  - Changed: Upgrade to the newest version of .NET Mongo Driver
  - Chnaged: Fixed [list of bugs](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/issues?version=v2.1.0)

### v2.1.0
  - Added: Azure CosmosDB support
  - Added: Increased C# Mongo Driver API coverage in schema validator feature
  - Changed: Fixed CI/CD script
  - Changed: Updated dependencies
  - Changed: Fixed [list of bugs](https://bitbucket.org/i_am_a_kernel/mongodbmigrations/issues?version=v2.0.0)

***

### v2.0.0
  - Added: Totaly brand new fluent API
  - Added: Callback for steps
  - Added: On-flight cancelation
  - Added: PowerShell script that can be integrated in CI/CD flow to make migration
  - Removed: All obsolete APIs
  - Removed: Async version of methods
  - Removed: Confiramtion event in case of failed database scheeme validation (Now if validations failed migration process will be stoped)
  - Fixed: Some amount of bugs
  - Fixed: All test has been refactored to increase quality of library.

***

### v1.1.2
  - Added: Ignore migration attribute
  - Fixed: Supporting migrations with version less then 1.0.0
  - Fixed: Critical bugs

***

### v1.1.1
  - Change target framework from netcoreapp2.1 to netstandard2.0

***

### v1.1.0
  - Added: MongoDB document schema uniformity validation
  - Added: Async impl for runner and database locator
  - Added: Progress returning mechanism
  - Added: Cancelation mechanism

***

### v1.0.1
  - Fixed: Search assemble with migrations when method `LookInAssemblyOfType<T>()` doesn't used
  - Fixed: Runner crash when `runner.UpdateTo()` called without result handling
  - Fixed: Behavior when target migration not found
  - Added: Testable migrations
  - Added: Overload for `LookInAssemblyOfType` method
  - Added: Fields in `MigrationResult` for progress handling

***

### v1.0.0
  - Roll forward/back manual created migrations
  - Auto find migrations in assemblies for migration beetwen current and target versions.