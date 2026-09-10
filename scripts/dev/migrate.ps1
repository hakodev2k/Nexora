$ErrorActionPreference = 'Stop'

$SqlServer = if ($env:NEXORA_SQL_SERVER) { $env:NEXORA_SQL_SERVER } else { 'localhost,14333' }
$SqlUser = if ($env:NEXORA_SQL_USER) { $env:NEXORA_SQL_USER } else { 'sa' }
$SqlPassword = $env:NEXORA_SQL_PASSWORD
$SqlDatabase = if ($env:NEXORA_SQL_DATABASE) { $env:NEXORA_SQL_DATABASE } else { 'NexoraLocal' }
$MigrationsDir = if ($env:NEXORA_MIGRATIONS_DIR) { $env:NEXORA_MIGRATIONS_DIR } else { 'database/migrations' }

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required to run SQL Server migrations. Install sqlcmd or run inside the SQL Server tools container.'
}
if ([string]::IsNullOrEmpty($SqlPassword)) {
    throw 'NEXORA_SQL_PASSWORD is required; no default database credential is permitted.'
}

sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -C -Q "IF DB_ID(N'$SqlDatabase') IS NULL CREATE DATABASE [$SqlDatabase];"
$Migrations = Get-ChildItem -Path $MigrationsDir -Filter '*.sql' | Sort-Object Name
if ($Migrations.Count -eq 0) {
    throw "No SQL migrations found in $MigrationsDir."
}

foreach ($Migration in $Migrations) {
    Write-Host "Applying $($Migration.FullName)"
    sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -C -d $SqlDatabase -b -i $Migration.FullName
}

Write-Host "Applied all local SQL migrations to $SqlDatabase on $SqlServer."
