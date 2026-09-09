#!/usr/bin/env bash
set -euo pipefail
project="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)/src/Nexora.Local/Nexora.Local.csproj"
dotnet run --project "$project" --configuration Release -- migrate
