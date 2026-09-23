#!/usr/bin/env bash
set -euo pipefail

required_os="${1:-}"
if [[ "${required_os}" != "windows" && "${required_os}" != "linux" ]]; then
  echo "Usage: scripts/dev/test-filesystem.sh windows|linux" >&2
  exit 2
fi

dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release -- --require-os "${required_os}"
