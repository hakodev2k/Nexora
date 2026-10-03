$ErrorActionPreference = 'Stop'

$SqlServer = if ($env:NEXORA_SQL_SERVER) { $env:NEXORA_SQL_SERVER } else { 'localhost,14333' }
$SqlUser = if ($env:NEXORA_SQL_USER) { $env:NEXORA_SQL_USER } else { 'sa' }
$SqlPassword = $env:NEXORA_SQL_PASSWORD
$SqlDatabase = if ($env:NEXORA_SQL_DATABASE) { $env:NEXORA_SQL_DATABASE } else { 'Nexora_Dev' }
$MigrationsDir = if ($env:NEXORA_MIGRATIONS_DIR) { $env:NEXORA_MIGRATIONS_DIR } else { 'database/migrations' }

$serverHost = ($SqlServer -replace '^tcp:', '') -split '[,\\]' | Select-Object -First 1
if ($serverHost -notin @('localhost', '127.0.0.1', '.', '(local)', '(localdb)')) {
    throw "Migration target must be loopback; got $serverHost"
}
if ($SqlDatabase -notmatch '^(Nexora_Dev|Nexora_Test_[0-9A-Fa-f]{32})$') {
    throw 'Migration database must be Nexora_Dev or Nexora_Test_<32 hex chars>.'
}

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required to run SQL Server migrations. Install sqlcmd or run inside the SQL Server tools container.'
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'dotnet is required to run the journaled migration runner. Install the .NET SDK 10 or run inside the SDK container.'
}
if ([string]::IsNullOrEmpty($SqlPassword)) {
    throw 'NEXORA_SQL_PASSWORD is required; no default database credential is permitted.'
}
if (-not (Test-Path -LiteralPath $MigrationsDir -PathType Container) -or
    @(Get-ChildItem -LiteralPath $MigrationsDir -Filter '*.sql' -File).Count -eq 0) {
    throw "No SQL migrations found in $MigrationsDir."
}

sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -C -Q "IF DB_ID(N'$SqlDatabase') IS NULL CREATE DATABASE [$SqlDatabase];"

# Nexora.Local owns the deployment lock, normalized content hashes and
# pending-migration journal. Discrete environment variables let the CLI escape
# passwords safely through SqlConnectionStringBuilder.
$previousEnvironment = $env:DOTNET_ENVIRONMENT
$previousConnection = $env:NEXORA_SQL_CONNECTION
$previousServer = $env:NEXORA_SQL_SERVER
$previousUser = $env:NEXORA_SQL_USER
$previousPassword = $env:NEXORA_SQL_PASSWORD
$previousDatabase = $env:NEXORA_SQL_DATABASE
$previousMigrations = $env:NEXORA_MIGRATIONS_DIR
try {
    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:NEXORA_SQL_CONNECTION = ''
    $env:NEXORA_SQL_SERVER = $SqlServer
    $env:NEXORA_SQL_USER = $SqlUser
    $env:NEXORA_SQL_PASSWORD = $SqlPassword
    $env:NEXORA_SQL_DATABASE = $SqlDatabase
    $env:NEXORA_MIGRATIONS_DIR = $MigrationsDir
    & dotnet run --project src/Nexora.Local/Nexora.Local.csproj --configuration Release -- migrate
    if ($LASTEXITCODE -ne 0) {
        throw "Journaled migration runner failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:DOTNET_ENVIRONMENT = $previousEnvironment
    $env:NEXORA_SQL_CONNECTION = $previousConnection
    $env:NEXORA_SQL_SERVER = $previousServer
    $env:NEXORA_SQL_USER = $previousUser
    $env:NEXORA_SQL_PASSWORD = $previousPassword
    $env:NEXORA_SQL_DATABASE = $previousDatabase
    $env:NEXORA_MIGRATIONS_DIR = $previousMigrations
}

Write-Host "Applied pending local SQL migrations to $SqlDatabase on $SqlServer."
