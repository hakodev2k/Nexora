#!/usr/bin/env bash
set -euo pipefail

if [[ -z "${NEXORA_E2E_BASE_URL:-}" || -z "${NEXORA_E2E_OPERATOR_CLI:-}" || -z "${NEXORA_E2E_RUN_ID:-}" ]]; then
  echo "BLOCKED: E2E needs a loopback synthetic target, local operator CLI, and isolated run id." >&2
  exit 2
fi

npm ci --prefix web/Nexora.Web
npm run test:e2e --prefix web/Nexora.Web
