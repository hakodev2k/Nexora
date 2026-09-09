#!/usr/bin/env bash
set -euo pipefail

bash scripts/dev/doctor.sh --strict
python3 scripts/dev/verify-s00.py
dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release
npm install --prefix web/Nexora.Web
npm run build --prefix web/Nexora.Web
