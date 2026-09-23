#!/usr/bin/env bash
set -euo pipefail

python3 scripts/dev/verify-s00.py

dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release
dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --logger "trx;LogFileName=unit-xunit.trx" --results-directory artifacts/test-results --collect:"XPlat Code Coverage"
