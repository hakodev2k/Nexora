$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../../src/Nexora.Local/Nexora.Local.csproj'
dotnet run --project $project --configuration Release -- bootstrap-superadmin
exit $LASTEXITCODE
