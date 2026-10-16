$ErrorActionPreference = 'Stop'
Write-Host "Starting Nexora.Api. Apply database migrations and start SQL/Redis with docker compose first."
dotnet run --project src/Nexora.Api/Nexora.Api.csproj
