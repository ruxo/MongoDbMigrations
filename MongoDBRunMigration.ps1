param($connectionString, $databaseName, $backupLocation, $migrationsAssemblyPath)

#Make a backup of database
$backupLocation = Join-Path $backupLocation (Get-Date -f yyyy_MM_dd_HH_mm_ss)
mongodump --host="`"$connectionString`"" --db="`"$databaseName`"" --out="`"$backupLocation`""

#Load assembly with migrations and migration engine
$migrationAssembly = [System.Reflection.Assembly]::LoadFrom($migrationsAssemblyPath)
$migrationEnginePath = Join-Path([System.IO.Path]::GetDirectoryName($migrationsAssemblyPath)) 'MongoDBMigrations.dll'
[System.Reflection.Assembly]::LoadFrom($migrationEnginePath)

#Start migration
try {
	$engine = New-Object MongoDBMigrations.MigrationEngine
	$engine.UseDatabase($connectionString, $databaseName).UseAssembly($migrationAssembly).UseSchemaValidation(0).Run()
}
catch [System.Exception] {
    #Attemp restore on failure
    Write-Output "Migration failed: "
    Write-Host $_.Exception.ToString();
    Write-Output "Attempting restore from " + $backupLocation
    mongorestore --host="`"$connectionString`"" --dir="`"$backupLocation`"" --db="`"$databaseName`"" --drop
    throw
}
