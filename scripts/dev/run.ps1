$ErrorActionPreference = 'Stop'
Write-Host "Starting Nexora.Api. Start SQL/Redis with docker compose when later M01 stories require persistence."
dotnet run --project src/Nexora.Api/Nexora.Api.csproj
