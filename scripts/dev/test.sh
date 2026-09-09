#!/usr/bin/env bash
set -euo pipefail

python3 scripts/dev/verify-s00.py

echo "S00 static tests completed. Add SQL Server-backed integration tests with M01-S01/S02 implementation."
