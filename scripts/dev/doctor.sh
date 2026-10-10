#!/usr/bin/env bash
set -euo pipefail

STRICT=0
if [[ "${1:-}" == "--strict" ]]; then
  STRICT=1
fi

missing=0
check_required() {
  local name="$1"
  local cmd="$2"
  if command -v "$cmd" >/dev/null 2>&1; then
    echo "[ok] $name: $($cmd --version 2>/dev/null | head -n 1)"
  else
    echo "[missing] $name ($cmd)"
    missing=1
  fi
}

check_optional() {
  local name="$1"
  local cmd="$2"
  if command -v "$cmd" >/dev/null 2>&1; then
    echo "[ok] $name: $($cmd --version 2>/dev/null | head -n 1)"
  else
    echo "[optional-missing] $name ($cmd)"
  fi
}

check_required ".NET SDK 10" dotnet
check_required "Node.js" node
check_required "npm" npm
check_required "Python" python3
check_optional "Docker" docker

if [[ "$STRICT" == "1" && "$missing" != "0" ]]; then
  echo "S00 doctor failed: required local tooling is missing."
  exit 1
fi

if [[ "$missing" != "0" ]]; then
  echo "S00 doctor completed with missing required tools; rerun with --strict to fail."
else
  echo "S00 doctor completed successfully."
fi
