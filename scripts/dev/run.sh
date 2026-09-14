#!/usr/bin/env bash
set -euo pipefail

echo "Starting Nexora.Api. Apply database/migrations with scripts/dev/migrate.sh and start SQL/Redis with docker compose first."
dotnet run --project src/Nexora.Api/Nexora.Api.csproj
