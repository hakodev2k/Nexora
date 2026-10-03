#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
# SQL/API/frontend must share a reachable operator environment. Do not print credentials.
for name in NEXORA_SQL_CONNECTION_STRING NEXORA_E2E_ACCOUNTS NEXORA_E2E_BASE_URL NEXORA_E2E_RUN_ID NEXORA_E2E_OPERATOR_CLI; do
  if [[ -z "${!name:-}" ]]; then echo "BLOCKED: $name is required." >&2; exit 2; fi
done
export NEXORA_MIGRATIONS_DIR="$PWD/database/migrations"
dotnet build tests/Nexora.FunctionalTestOperator/Nexora.FunctionalTestOperator.csproj --configuration Release
if [[ -z "${NEXORA_E2E_SQL_OPERATOR:-}" ]]; then
  task_operator_dir="$(mktemp -d)"
  trap 'rm -rf "$task_operator_dir"' EXIT
  printf '#!/usr/bin/env bash\nexec dotnet %q "$@"\n' "$PWD/tests/Nexora.FunctionalTestOperator/bin/Release/net10.0/Nexora.FunctionalTestOperator.dll" > "$task_operator_dir/operator.sh"
  chmod 700 "$task_operator_dir/operator.sh"
  export NEXORA_E2E_SQL_OPERATOR="$task_operator_dir/operator.sh"
fi
# This is a read-only connectivity check, never an implicit seed or gate activation.
"$NEXORA_E2E_SQL_OPERATOR" inventory > /dev/null
npm run test:e2e --prefix web/Nexora.Web -- --config functional.config.ts "$@"
