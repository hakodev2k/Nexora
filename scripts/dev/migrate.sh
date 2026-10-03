#!/usr/bin/env bash
set -euo pipefail

SQL_SERVER="${NEXORA_SQL_SERVER:-localhost,14333}"
SQL_USER="${NEXORA_SQL_USER:-sa}"
if [[ -z "${NEXORA_SQL_PASSWORD:-}" ]]; then
  echo "[missing] set NEXORA_SQL_PASSWORD; no default database credential is permitted"
  exit 1
fi
SQL_PASSWORD="${NEXORA_SQL_PASSWORD:-}"
SQL_DATABASE="${NEXORA_SQL_DATABASE:-Nexora_Dev}"
MIGRATIONS_DIR="${NEXORA_MIGRATIONS_DIR:-database/migrations}"

SQL_HOST="${SQL_SERVER#tcp:}"
SQL_HOST="${SQL_HOST%%,*}"
SQL_HOST="${SQL_HOST%%\\*}"
case "$SQL_HOST" in
  localhost|127.0.0.1|.|'(local)'|'(localdb)') ;;
  *) echo "[blocked] migration target must be loopback; got $SQL_HOST"; exit 1 ;;
esac
if [[ ! "$SQL_DATABASE" =~ ^Nexora_Dev$|^Nexora_Test_[0-9A-Fa-f]{32}$ ]]; then
  echo "[blocked] migration database must be Nexora_Dev or Nexora_Test_<32 hex chars>"
  exit 1
fi

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "[missing] sqlcmd is required to run SQL Server migrations. Install sqlcmd or run inside the SQL Server tools container."
  exit 1
fi
if ! command -v dotnet >/dev/null 2>&1; then
  echo "[missing] dotnet is required to run the journaled migration runner. Install the .NET SDK 10 or run inside the SDK container."
  exit 1
fi

if [[ ! -d "$MIGRATIONS_DIR" ]] || ! compgen -G "$MIGRATIONS_DIR/*.sql" >/dev/null; then
  echo "[missing] no SQL migrations found in $MIGRATIONS_DIR"
  exit 1
fi

sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -Q "IF DB_ID(N'$SQL_DATABASE') IS NULL CREATE DATABASE [$SQL_DATABASE];"

# Nexora.Local owns the deployment lock, normalized content hashes and
# pending-migration journal. Pass discrete credentials so a password containing
# connection-string punctuation is escaped by SqlConnectionStringBuilder.
env \
  DOTNET_ENVIRONMENT=Development \
  NEXORA_SQL_CONNECTION= \
  NEXORA_SQL_SERVER="$SQL_SERVER" \
  NEXORA_SQL_USER="$SQL_USER" \
  NEXORA_SQL_PASSWORD="$SQL_PASSWORD" \
  NEXORA_SQL_DATABASE="$SQL_DATABASE" \
  NEXORA_MIGRATIONS_DIR="$MIGRATIONS_DIR" \
  dotnet run --project src/Nexora.Local/Nexora.Local.csproj --configuration Release -- migrate

echo "Applied pending local SQL migrations to $SQL_DATABASE on $SQL_SERVER."
