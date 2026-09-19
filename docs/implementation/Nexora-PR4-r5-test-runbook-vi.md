# Nexora R5 test runbook

## Purpose and boundaries

This runbook executes the M01 remediation test portfolio from a clean checkout. It is for local synthetic targets only.

- Branch expected: impl/m01-s00-scaffold.
- Never point SQL commands at production, shared development data or an existing application catalog.
- Do not expose a connection string, token, cookie, password, key or capture JSON in console, CI artifacts or chat.
- No real Email/Push/OAuth/provider execution is required or allowed.
- A missing required runtime is a blocked/nonzero result, not Passed or Skipped-success.

## Clean checkout preparation

1. Check the commit and working tree. For R5 evidence, main is 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3 and reviewed parent is aada36f089ca0ea85424dfa249a2228d70ca740b.
2. Restore locked dependencies only.

Windows:

    dotnet restore --locked-mode src/Nexora.Api/Nexora.Api.csproj
    dotnet restore --locked-mode tests/Nexora.UnitTests/Nexora.UnitTests.csproj
    dotnet restore --locked-mode tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj
    npm ci --prefix web/Nexora.Web

Linux:

    dotnet restore --locked-mode src/Nexora.Api/Nexora.Api.csproj
    dotnet restore --locked-mode tests/Nexora.UnitTests/Nexora.UnitTests.csproj
    dotnet restore --locked-mode tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj
    npm ci --prefix web/Nexora.Web

3. Record tool versions and lock-file state without printing configuration values.

    dotnet --info
    node --version
    npm --version
    git status --short

The approved docs give a research pin, but actual tool compatibility is evidence only after these commands execute. Do not download a latest dependency merely to make a suite pass.

## Fast backend checks

Windows:

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev/test.ps1

Linux:

    bash scripts/dev/test.sh

Expected: static verifier, custom runner and xUnit suite execute. The custom runner may show skip entries; those are not passed tests.

Direct xUnit output with TRX and coverage:

    dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --logger "trx;LogFileName=unit-xunit.trx" --results-directory artifacts/test-results --collect:"XPlat Code Coverage"

Expected: nonzero only for an assertion/runner failure. TRX and Cobertura are evidence artifacts, not acceptance by themselves.

Required filesystem platform gate:

Windows:

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev/test-filesystem.ps1 -RequireOs windows

Linux:

    bash scripts/dev/test-filesystem.sh linux

Expected: exit 0 only when all tests tagged for that actual OS execute. Exit 1 means a required test was skipped or failed. The R5 Windows A/B/C test requires distinct API runtime, operator CLI and foreign-principal processes; an SID argument passed within one process is insufficient.

## SQL/API integration

Preconditions:

- A local loopback SQL Server with TLS validation intact.
- An input connection string in NEXORA_TEST_SQL_CONNECTION that validates as Development/local and names a fresh Nexora_Test_GUID database.
- The fixture creates a unique generated database and temporary capture root, and cleans only resources it owns.
- No EF InMemory/SQLite substitute.

Windows:

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev/test-sql.ps1

Linux:

    bash scripts/dev/test-sql.sh

Direct execution:

    dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --logger "trx;LogFileName=sql-api-integration.trx" --results-directory artifacts/test-results --collect:"XPlat Code Coverage"

Expected live coverage:

- clean migrations, staged upgrade/readiness and journal/checksum behavior;
- anonymous register same-key/body replay, changed-body conflict and middleware CSRF;
- old/current receipt namespace compatibility, original safe replay and current-authority rejection;
- preserved bootstrap rollback/concurrency checks.

Required follow-on cases before calling M01 local runtime verified:

- RCSI ON/OFF and actual queue claim queries;
- two worker lease/reclaim, retry limit and stale fence;
- revoke/resend/consume before effect;
- crash after prepare, SQL Delivered and before/after promotion;
- register/capture/verify and reset/session/MFA boundaries;
- cross-owner response and mutation-read projection cases.

If the variable is absent, the script exits 2 and the xUnit fixture reports BLOCKED. Do not change the script to skip and return green.

## Frontend component tests

    npm run test:unit:coverage --prefix web/Nexora.Web
    npm run build --prefix web/Nexora.Web

Expected component coverage includes English registration copy/accessibility validation, pending duplicate submit, CSRF retry/idempotency key retention and profile IANA session display. The R2-10 DST case is intentionally skipped because its post-M01 ambiguity policy is not approved; it must not be reported as passed.

Artifacts:

- web/Nexora.Web/artifacts/vitest-junit.xml
- web/Nexora.Web/artifacts/coverage

Coverage percentage is a diagnostic, not acceptance evidence.

## Browser E2E

Preconditions:

- Frontend, API and SQL are an isolated loopback candidate with synthetic data only.
- NEXORA_E2E_BASE_URL is loopback HTTP(S), has no credentials/query/fragment.
- NEXORA_E2E_OPERATOR_CLI points to the approved local operator CLI, not an HTTP mailbox.
- NEXORA_E2E_RUN_ID is an isolated nexora-e2e identifier.
- The local capture directory is private and operator-only.
- No trace/video/screenshot capture on sensitive journeys.

Discovery only:

    NEXORA_E2E_BASE_URL=https://127.0.0.1:59999 NEXORA_E2E_OPERATOR_CLI=synthetic-operator-not-executed NEXORA_E2E_RUN_ID=nexora-e2e-synthetic-20260919 npx playwright test --list --config web/Nexora.Web/playwright.config.ts

Run:

Windows:

    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev/test-e2e.ps1

Linux:

    bash scripts/dev/test-e2e.sh

The configured suite covers desktop/mobile register to local capture to verify to login, keyboard/axe registration checks and reset to replacement-password login without auto-login. It must still be extended/executed for invalid/expired/replayed proof, MFA denial, logout/revoke, profile conflict/locale reload, SuperAdmin preview/dependency/last-SA, denied routes and network failure.

Missing E2E configuration exits nonzero before browser installation or a network request. Playwright JUnit is the only browser artifact currently retained.

## CI orchestration

The local workflow defines separate required-style jobs:

| Job | Purpose | Result if environment is absent |
| --- | --- | --- |
| backend-unit | static verifier, API/bootstrap build, preserved custom runner, xUnit/TRX/coverage | failing test/build |
| frontend-unit | locked install, build, Vitest/JUnit/coverage | failing test/build |
| sql-api-integration | real SQL/TestServer contract suite | BLOCKED/nonzero |
| filesystem-linux | private Unix modes and cross-process locking | nonzero if required test skips/fails |
| filesystem-windows-acl | required actual Windows ACL boundary | nonzero until A/B/C environment exists |
| browser-e2e | local loopback browser journey | BLOCKED/nonzero |

No job has continue-on-error or retry masking. CI must not be edited to pass just because a required runtime is unavailable. A workflow file alone is not a CI run: record the commit, run URL, OS, artifact and result after it executes.

## Evidence checklist

For every handoff record:

- main SHA, implementation parent SHA and local tested source tree;
- exact command, exit code, OS/runtime and artifact path;
- Passed, Failed, Skipped and Blocked/Not run separately;
- what test layer actually proves and what it cannot prove;
- no raw sensitive values in command output or uploaded artifacts;
- migration/recovery cleanup target ownership;
- independent reviewer identity/evidence, or Pending.

No merge, deployment or production claim is authorized by this runbook.
