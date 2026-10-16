#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${NEXORA_TEST_SQL_CONNECTION:-}" ]]; then
  echo "BLOCKED: NEXORA_TEST_SQL_CONNECTION is required and must point to an isolated loopback Nexora_Test_<GUID> target." >&2
  exit 2
fi

dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --logger "trx;LogFileName=sql-api-integration.trx" --results-directory artifacts/test-results --collect:"XPlat Code Coverage"
