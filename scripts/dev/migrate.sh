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

sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -Q "IF DB_ID(N'$SQL_DATABASE') IS NULL CREATE DATABASE [$SQL_DATABASE];"
shopt -s nullglob
migrations=("$MIGRATIONS_DIR"/*.sql)
if [[ "${#migrations[@]}" == "0" ]]; then
  echo "[missing] no SQL migrations found in $MIGRATIONS_DIR"
  exit 1
fi

for migration in "${migrations[@]}"; do
  echo "Applying $migration"
  sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -i "$migration"
done

echo "Applied all local SQL migrations to $SQL_DATABASE on $SQL_SERVER."
