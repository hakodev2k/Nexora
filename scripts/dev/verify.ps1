$ErrorActionPreference = 'Stop'
./scripts/dev/doctor.ps1 -Strict
python3 scripts/dev/verify-s00.py
