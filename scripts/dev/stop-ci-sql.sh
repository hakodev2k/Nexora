#!/usr/bin/env bash
set -euo pipefail
[[ "${GITHUB_ACTIONS:-}" == true && "${CI:-}" == true && "${GITHUB_RUN_ID:-}" =~ ^[0-9]+$ && "${GITHUB_RUN_ATTEMPT:-}" =~ ^[0-9]+$ && "${GITHUB_JOB:-}" =~ ^[a-zA-Z0-9_-]+$ ]] || { echo 'BLOCKED: SQL cleanup requires an identified GitHub Actions job.' >&2; exit 2; }
container="nexora-ci-sql-${GITHUB_RUN_ID}-${GITHUB_RUN_ATTEMPT}-${GITHUB_JOB}"
command -v docker >/dev/null || exit 0
docker container inspect "$container" >/dev/null 2>&1 || exit 0
[[ "${NEXORA_CI_SQL_INVOCATION:-}" =~ ^[0-9a-f]{32}$ ]] || { echo 'BLOCKED: SQL invocation ownership missing; refusing removal.' >&2; exit 2; }
invocation="$(docker inspect --format '{{index .Config.Labels "nexora.invocation"}}' "$container")"
owner="$(docker inspect --format '{{index .Config.Labels "nexora.owner"}}' "$container")"
run="$(docker inspect --format '{{index .Config.Labels "nexora.run"}}' "$container")"
attempt="$(docker inspect --format '{{index .Config.Labels "nexora.attempt"}}' "$container")"
job="$(docker inspect --format '{{index .Config.Labels "nexora.job"}}' "$container")"
[[ "$invocation" == "$NEXORA_CI_SQL_INVOCATION" && "$owner" == ci-runtime && "$run" == "$GITHUB_RUN_ID" && "$attempt" == "$GITHUB_RUN_ATTEMPT" && "$job" == "$GITHUB_JOB" ]] || { echo 'BLOCKED: container ownership does not match this run; refusing removal.' >&2; exit 2; }
docker rm --force "$container" >/dev/null
echo 'Removed this job-owned disposable SQL container.'
