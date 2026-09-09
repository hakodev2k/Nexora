#!/usr/bin/env bash
set -euo pipefail

SQL_SERVER="${NEXORA_SQL_SERVER:-localhost,14333}"
SQL_USER="${NEXORA_SQL_USER:-sa}"
SQL_PASSWORD="${NEXORA_SQL_PASSWORD:-Nexora_local_2026!}"
SQL_DATABASE="${NEXORA_SQL_DATABASE:-NexoraLocal}"
MIGRATION="database/migrations/20260909_0001_m01_identity_platform.sql"

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "[missing] sqlcmd is required to run SQL Server migrations. Install sqlcmd or run inside the SQL Server tools container."
  exit 1
fi

sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -Q "IF DB_ID(N'$SQL_DATABASE') IS NULL CREATE DATABASE [$SQL_DATABASE];"
sqlcmd -S "$SQL_SERVER" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -i "$MIGRATION"

echo "Applied M01 local SQL migration to $SQL_DATABASE on $SQL_SERVER."
