#!/usr/bin/env bash
set -euo pipefail

read -r -p "Bootstrap SuperAdmin email: " NEXORA_BOOTSTRAP_EMAIL
read -r -s -p "Bootstrap SuperAdmin password (not echoed): " NEXORA_BOOTSTRAP_PASSWORD
printf '\n'
export NEXORA_BOOTSTRAP_EMAIL NEXORA_BOOTSTRAP_PASSWORD
export NEXORA_BOOTSTRAP_TIMEZONE="${NEXORA_BOOTSTRAP_TIMEZONE:-UTC}"

dotnet run --project src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release
