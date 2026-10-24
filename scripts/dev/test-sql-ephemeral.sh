#!/usr/bin/env bash
# User-approved isolated local/CI verification. No retained database or credential.
set +x
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."

for dependency in docker openssl python3; do
  command -v "$dependency" >/dev/null || { echo "BLOCKED: $dependency is required for disposable SQL verification." >&2; exit 2; }
done

# This harness is for a local Linux daemon (including the hosted Ubuntu runner),
# never an ambient SSH/TCP Docker context or an existing registry credential store.
if [[ ! -S /var/run/docker.sock ]]; then
  echo 'BLOCKED: the isolated SQL CI harness requires a local Linux Docker socket.' >&2
  exit 2
fi
export DOCKER_HOST='unix:///var/run/docker.sock'
unset DOCKER_CONTEXT DOCKER_TLS_VERIFY DOCKER_CERT_PATH
task_docker_config="$(mktemp -d /tmp/nexora-sql-docker-XXXXXXXX)"
export DOCKER_CONFIG="$task_docker_config"

task_sql_run="nexora-sql-test-$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
task_sql_database="Nexora_Test_$(python3 -c 'import uuid; print(uuid.uuid4().hex)')"
task_sql_password="Nx!$(openssl rand -hex 24)"
if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
  printf '::add-mask::%s\n' "$task_sql_password"
fi
export MSSQL_SA_PASSWORD="$task_sql_password"
export SQLCMDPASSWORD="$task_sql_password"
task_sql_container=""

cleanup() {
  local result=$?
  trap - EXIT INT TERM
  # docker run may create the named container and then fail before returning its
  # ID. Recover only this run's exact generated name, still requiring its label.
  if [[ -z "$task_sql_container" ]]; then
    task_sql_container="$(docker inspect --format '{{.Id}}' "$task_sql_run" 2>/dev/null || true)"
  fi
  if [[ -n "$task_sql_container" ]]; then
    local owner
    owner="$(docker inspect --format '{{ index .Config.Labels "nexora.test.run" }}' "$task_sql_container" 2>/dev/null || true)"
    if [[ "$owner" != "$task_sql_run" ]]; then
      echo 'Disposable SQL cleanup refused: the container ownership label does not match this run.' >&2
      exit 1
    fi
    if ! docker rm --force "$task_sql_container" >/dev/null; then
      echo 'Disposable SQL cleanup failed.' >&2
      exit 1
    fi
  fi
  if [[ "$task_docker_config" =~ ^/tmp/nexora-sql-docker-[A-Za-z0-9]+$ && -d "$task_docker_config" && ! -L "$task_docker_config" ]]; then
    rm -rf -- "$task_docker_config"
  else
    echo 'Refusing an unexpected Docker configuration cleanup directory.' >&2
    exit 1
  fi
  unset MSSQL_SA_PASSWORD SQLCMDPASSWORD NEXORA_TEST_SQL_CONNECTION task_sql_password
  exit "$result"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

# Microsoft SQL Server Developer image and sqlcmd18 connection contract:
# https://learn.microsoft.com/sql/linux/quickstart-install-connect-docker
# The image is versioned to SQL 2022; record its resolved digest for run evidence.
task_sql_image='mcr.microsoft.com/mssql/server:2022-latest'
task_sql_container="$(docker run --detach --name "$task_sql_run" \
  --label "nexora.test.run=$task_sql_run" \
  --publish 127.0.0.1::1433 \
  --env ACCEPT_EULA=Y --env MSSQL_PID=Developer --env MSSQL_SA_PASSWORD \
  "$task_sql_image")"
docker image inspect --format 'SQL image: {{index .RepoDigests 0}}' "$task_sql_image"

task_sql_ready=false
for attempt in $(seq 1 60); do
  if docker exec --env SQLCMDPASSWORD "$task_sql_container" \
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b -l 2 \
    -Q 'SET NOCOUNT ON; SELECT 1 AS Readiness;' >/dev/null 2>&1; then
    task_sql_ready=true
    break
  fi
  sleep 2
done
if [[ "$task_sql_ready" != true ]]; then
  echo 'BLOCKED: disposable SQL did not pass its real readiness query within the bounded wait.' >&2
  exit 2
fi
task_sql_binding="$(docker port "$task_sql_container" 1433/tcp)"
if [[ ! "$task_sql_binding" =~ ^127\.0\.0\.1:([0-9]+)$ ]]; then
  echo 'Disposable SQL binding is not the expected loopback endpoint.' >&2
  exit 2
fi
task_sql_port="${BASH_REMATCH[1]}"
export NEXORA_TEST_SQL_CONNECTION="Server=127.0.0.1,$task_sql_port;Initial Catalog=$task_sql_database;User ID=sa;Password=$task_sql_password;Encrypt=True;TrustServerCertificate=True"
echo 'Disposable loopback SQL readiness passed; running generated-database SQL/API fixtures.'
bash scripts/dev/test-sql.sh
