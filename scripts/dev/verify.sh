#!/usr/bin/env bash
set -euo pipefail

bash scripts/dev/doctor.sh --strict
python3 scripts/dev/verify-s00.py
