#!/usr/bin/env bash
set -euo pipefail
# This harness creates only a disposable CI-owned loopback SQL Server.
[[ "${GITHUB_ACTIONS:-}" == true && "${CI:-}" == true && "${GITHUB_RUN_ID:-}" =~ ^[0-9]+$ && "${GITHUB_RUN_ATTEMPT:-}" =~ ^[0-9]+$ && "${GITHUB_JOB:-}" =~ ^[a-zA-Z0-9_-]+$ ]] || { echo 'BLOCKED: isolated SQL harness requires an identified GitHub Actions job.' >&2; exit 2; }
[[ -n "${GITHUB_ENV:-}" && -f "$GITHUB_ENV" && ! -L "$GITHUB_ENV" ]] || { echo 'BLOCKED: trusted job environment file required.' >&2; exit 2; }
script_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
container="nexora-ci-sql-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}-${GITHUB_JOB}"
command -v docker >/dev/null || { echo 'BLOCKED: Docker unavailable.' >&2; exit 2; }
docker info >/dev/null 2>&1 || { echo 'BLOCKED: Docker daemon unavailable.' >&2; exit 2; }
if docker container inspect "$container" >/dev/null 2>&1; then echo 'BLOCKED: target container already exists; refusing reuse.' >&2; exit 2; fi
umask 077
private_log="$(mktemp)"
started=false
ready=false
cleanup() {
  rm -f "$private_log"
  if [[ "$started" == true && "$ready" != true ]]; then bash "$script_root/scripts/dev/stop-ci-sql.sh"; fi
}
trap cleanup EXIT
MSSQL_SA_PASSWORD="$(python3 -c 'import secrets; print("Nx!7"+secrets.token_hex(24))')"
export MSSQL_SA_PASSWORD
NEXORA_CI_SQL_INVOCATION="$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
export NEXORA_CI_SQL_INVOCATION
printf 'NEXORA_CI_SQL_INVOCATION=%s\n' "$NEXORA_CI_SQL_INVOCATION" >> "$GITHUB_ENV"
printf '::add-mask::%s\n' "$MSSQL_SA_PASSWORD"
started=true
if ! docker run --detach --name "$container" --label nexora.owner=ci-runtime --label "nexora.run=$GITHUB_RUN_ID" --label "nexora.attempt=$GITHUB_RUN_ATTEMPT" --label "nexora.job=$GITHUB_JOB" --label "nexora.invocation=$NEXORA_CI_SQL_INVOCATION" --publish 127.0.0.1:14339:1433 --env ACCEPT_EULA=Y --env MSSQL_PID=Developer --env MSSQL_SA_PASSWORD mcr.microsoft.com/mssql/server:2022-latest >"$private_log" 2>&1; then
  echo 'BLOCKED: disposable SQL container could not start; no private output published.' >&2; exit 2
fi
started=true
export SQLCMDPASSWORD="$MSSQL_SA_PASSWORD"
for attempt in $(seq 1 40); do
  if docker exec --env SQLCMDPASSWORD "$container" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -l 3 -t 3 -Q 'SET NOCOUNT ON; SELECT 1;' >"$private_log" 2>&1; then ready=true; break; fi
  sleep 2
done
[[ "$ready" == true ]] || { echo 'BLOCKED: real SQL SELECT 1 readiness failed.' >&2; exit 2; }
database="Nexora_Test_$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
connection="Server=127.0.0.1,14339;Database=$database;User Id=sa;Password=$MSSQL_SA_PASSWORD;Encrypt=True;TrustServerCertificate=True;Persist Security Info=False"
printf '::add-mask::%s\n' "$connection"
printf 'NEXORA_TEST_SQL_CONNECTION=%s\nNEXORA_CI_SQL_CONTAINER=%s\n' "$connection" "$container" >> "$GITHUB_ENV"
echo 'Disposable loopback SQL Server ready (real SELECT 1).'
