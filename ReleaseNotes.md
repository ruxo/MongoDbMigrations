# MongoDBMigrations

### v1.1.2
  - Ignore migration attribute
  - Supporting migrations with version less then 1.0.0
  - Fixed critical bugs

### v1.1.1
  - Change target framework from netcoreapp2.1 to netstandard2.0

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