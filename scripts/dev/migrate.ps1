$ErrorActionPreference = 'Stop'

$SqlServer = if ($env:NEXORA_SQL_SERVER) { $env:NEXORA_SQL_SERVER } else { 'localhost,14333' }
$SqlUser = if ($env:NEXORA_SQL_USER) { $env:NEXORA_SQL_USER } else { 'sa' }
$SqlPassword = if ($env:NEXORA_SQL_PASSWORD) { $env:NEXORA_SQL_PASSWORD } else { 'Nexora_local_2026!' }
$SqlDatabase = if ($env:NEXORA_SQL_DATABASE) { $env:NEXORA_SQL_DATABASE } else { 'NexoraLocal' }
$Migration = 'database/migrations/20260909_0001_m01_identity_platform.sql'

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw 'sqlcmd is required to run SQL Server migrations. Install sqlcmd or run inside the SQL Server tools container.'
}

sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -C -Q "IF DB_ID(N'$SqlDatabase') IS NULL CREATE DATABASE [$SqlDatabase];"
sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -C -d $SqlDatabase -b -i $Migration

Write-Host "Applied M01 local SQL migration to $SqlDatabase on $SqlServer."
