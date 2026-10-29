#!/usr/bin/env bash
set -euo pipefail

# Optional fallback for development hosts that cannot launch Docker containers.
# Install SQL Server 2022 Developer from Microsoft's official package first.
# Credentials are supplied by the caller; this script never prints or saves them.
: "${NEXORA_SQL_PASSWORD:?Supply the local synthetic SQL Server password}"
if [[ ! -x /opt/mssql/bin/sqlservr ]]; then
  echo 'SQL Server is missing. Reuse Docker when available or install the official SQL Server 2022 Developer package.' >&2
  exit 2
fi
export ACCEPT_EULA=Y MSSQL_PID=Developer
export MSSQL_SA_PASSWORD="$NEXORA_SQL_PASSWORD"
export MSSQL_TCP_PORT="${NEXORA_SQL_PORT:-14333}"
# Match the host memory limit. An artificially low cap can make SQL report zero
# available memory when other processes already consume more than that cap.
if [[ -n "${NEXORA_SQL_MEMORY_MB:-}" ]]; then
  export MSSQL_MEMORY_LIMIT_MB="$NEXORA_SQL_MEMORY_MB"
fi
if [[ -n "${NEXORA_SQL_CPU_SET:-}" ]]; then
  exec taskset -c "$NEXORA_SQL_CPU_SET" /opt/mssql/bin/sqlservr
fi
exec /opt/mssql/bin/sqlservr
