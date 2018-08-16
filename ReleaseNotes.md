# MongoDBMigrations

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