# Nexora PR #4 — R5 remediation và test engineering

## Trạng thái bàn giao

- Trạng thái: reviewable local remediation, chưa commit và chưa push.
- Không merge, auto-merge, merge queue, approve, bypass checks, push main, force-push hoặc deploy.
- Repository / branch: hakodev2k/Nexora / impl/m01-s00-scaffold.
- Main normative: 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3.
- Reviewed implementation parent: aada36f089ca0ea85424dfa249a2228d70ca740b.
- Previous reviewed head: e12fc994d34801118b38b5ff306a468af3dd44b0.
- Remote check 2026-09-19: git fetch --dry-run origin exit 0; git ls-remote exit 0 and returned exactly main=8782f46... plus implementation=aada36f....
- Evidence source state: aada36f... plus the uncommitted local R5 changes listed below. This is not a claim about any previously pushed artifact.
- CI reference: none. The workflow is local/unpushed and was not triggered.
- Independent security/runtime review: Pending. Self-review and local CI-like commands are not independent review.

## Authority and task brief

| Field | Recorded fact |
| --- | --- |
| Approval used | DEC-20260909-001 from normative main: M01 S00–S11, scaffold, local scripts and synthetic/local verification only. |
| Not used as authority | PR-only docs, DEC014/code-only amendment, CI status, old PR body or historical report. |
| Scope completed here | R5-01/02 capture local; R5-03 runner/evidence/test automation; M01 S00/S02/S04/S05/S10/S11. |
| Scope not opened | Documents dirty transition, Favorites route, Project picker, Calendar DST/navigation/status, Sharing nullable Priority, Reminder/Files queues, mutation-read/replay business paths, audit/boundaries. FX30/34/35 remain Paused. |
| Stories / actions / AC | S02 identity.account.register/verify/resend, AC02; S04 reset_request/reset_confirm, AC04; S05 profile/settings, AC05; S10 delivery, AC10; S11 evidence/recovery, AC11; S00/AC00 runner reproducibility. |
| Requirements / goals | P01-AUT-009..012, P01-AUT-006, P01-PLT-001/004/006, P01-SHL-001; NXG-M01-01/02/03/05, NXG-FX01-G01/G02/G03, NXG-FX02-G01, NXG-FX03-G01/G02, NXG-FX06-G01/G02, NXG-FX09-G02/G03, NXG-X-01/X-07/X-09/X-15. |
| Rules / skills loaded | AGENTS, nexora-engineering skill, implementation profile, Technical Lead core rules, verification; backend authorization/background/migration/security/testing; React accessibility/browser-security/forms/testing; security privacy/data/identity/secrets/threat-model; architecture ownership/transaction/module boundary; database migration/isolation; QA contract/test strategy; owner-boundary and ground-truth gates. |
| Consumers reviewed | identity API/middleware, AccountMessageDeliveryWorker, local operator capture reader, request receipts, readiness, custom runner, xUnit, React/Vitest/MSW, Playwright, scripts and workflow. |
| Migration/recovery | No migration history, receipt TTL, secret, DB reset or data repair changed. SQL fixture only targets generated Nexora_Test_GUID databases. |

Normative documents were read from main: delivery scope, M01 story/API/data/UX/environment/readiness, identity/security/authorization, shared forms/accessibility and goals/evidence template. PR docs are implementation claims/proposals only.

## R5 findings

| ID | Disposition | Root cause and code | Regression case | Actual result |
| --- | --- | --- | --- | --- |
| R5-01 | Source fixed; source-level regression passed; runtime incomplete | LocalAccountMessageSink.Publish/CreateTemporaryCapture/SweepExpiredCore/IsFileReadyForCleanup now create Unix temp 0600 before bytes; apply/validate Windows file ACL before serialization; isolate invalid per-file temp ACL/mode; close/unlock before deletion. Only a temp created by the current publish invocation is deleted in finally, so a pre-existing same-name temp is not blanket-deleted. | xUnit old partial temp cleanup + later delivery; xUnit existing same-name partial file survives failed CreateNew and later unrelated message prepares; custom direct orphan and Linux active-writer/unsafe-orphan cases. | xUnit passed 11/11. Windows direct source cases passed. Process kill after create/write/flush/before ACL, Linux umask 022 and Windows A/B/C remain Not run. |
| R5-02 | Source fixed; regression passed | ReadCaptured, SweepExpiredCore, RemovePathIfOwned, IsCaptureForFence and TryGetOwnedCaptureIdentity require nonempty filename ID and filename/payload ID agreement before read/delete. Valid matching legacy JSON remains accepted. | Foreign valid JSON, expired foreign valid JSON, filename/payload mismatch, empty ID, owned malformed after grace and matching legacy JSON. | xUnit passed; custom runner passed applicable cases. Foreign/mismatch data is left untouched. |
| R5-03 | Source fixed; runner/script evidence passed | TestRunner now records pass/fail/skip reason and required platform skips make exit nonzero. PowerShell scripts propagate native exit codes; SQL/E2E preflight reports blocked exit 2. | Current Windows custom runner; require-os windows; SQL/E2E scripts with inputs absent. | 30 pass/0 fail/4 skip; require-os windows exit 1 for the deliberately skipped actual A/B/C case; SQL/E2E preflight exit 2. |

### Compatibility and recovery

- Legacy final JSON is compatible only if its filename matches its payload Message.Id.
- Owned malformed final/pending data waits for grace; cleanup requires adapter naming, private ACL/mode, age and no active advisory lock.
- Foreign names, empty GUIDs, payload mismatch and unsafe ACL/mode are not read as captures or deleted. The sink does not broaden ACL.
- Directory insecurity remains fail-closed. A single unsafe temp is isolated so later unrelated delivery can progress.
- No raw token, password, key, hash, cookie or connection string is logged or stored in test evidence. All test data is synthetic and held in process memory.
- Receipt dual-read, signed anonymous binding, IdentityV3/220000, AdminGrantable, readiness 0025/0026 and profile timezone fixes remain intact.

## Test inventory: before to after

| Layer | Reviewed head aada36f | Current local remediation |
| --- | --- | --- |
| Backend unit | Custom console TestRunner with real assertions; not discovered by dotnet test. | Preserved runner plus xUnit/Test SDK/coverlet. xUnit discovers 11; custom runner has 34 checks and explicit OS skips. |
| SQL integration | Console Program with migration/bootstrap assertions, not CI-called; DB_ID null bug and retained synthetic DB. | Preserved runner requires Nexora_Test_GUID, handles null correctly and cleans only test-owned DB. New xUnit/TestServer fixture has three real SQL + API/middleware cases and fails closed without SQL. |
| Frontend unit | No Vitest/RTL/MSW runner or test source; CI built only. | Vitest + RTL + user-event + MSW: 5 active tests, 1 explicit R2-10 policy skip, JUnit/coverage. |
| Browser E2E | No Playwright, browser test or sensitive-artifact policy. | Playwright + axe, loopback-only target, desktop/mobile projects, JUnit only; trace/video/screenshot disabled. Four test instances discoverable. |
| Scripts / CI | PowerShell native exit propagation not explicit; one CI job. | Unit, SQL, filesystem and E2E scripts have fail-closed exits; CI split into backend, frontend, SQL/API, Linux filesystem, Windows ACL and browser jobs without continue-on-error. |

Existing custom assertions were retained; no test was deleted merely to change framework.

## Requirement to evidence matrix

| Requirement / goal | Risk / code | Test layer and source | Status |
| --- | --- | --- | --- |
| AC02/AC04/AC10/AC11; NXG-M01-02/05; NXG-X-07/X-09/X-15 | Capture confidentiality, fence, crash orphan, retention | M01PolicyAndCaptureContractTests and LocalAccountMessageSinkTests | Source cases passed; real kill/Linux/Windows-process proof Not run. |
| P01-PLT-001/004/006; AC10 | Durable delivery, current authority and no fake Delivered | sink regression plus worker/SQL fixture design | Partial; real lease/revoke/reclaim runtime Not run. |
| AC02 / NXG-X-01 | Same anonymous idempotency key/body one effect; changed body 409 | SqlApiIntegrationTests anonymous CSRF HTTP case | Blocked: NEXORA_TEST_SQL_CONNECTION absent. |
| Receipt replay contract / NXG-M01-02 | Old/current subject namespace, original safe result, authority recheck | SqlApiIntegrationTests receipt HTTP case | Blocked: real SQL absent. |
| AC00/AC11 / NXG-M01-01/05 | Migration journal, readiness and bootstrap rollback | staged SQL fixture plus preserved runner | Blocked: real SQL absent. |
| AC03/AC04 / NXG-FX01-G01/G02 | lifecycle, MFA fail-closed, password policy | custom runner plus xUnit lifecycle theory | Passed in unit/source scope; API/session runtime Not run. |
| AC08/AC09 / NXG-FX02-G01 / NXG-FX03-G01/G02 | AdminGrantable, paused/module policy | policy tests | Unit passed; SQL race Not run. |
| AC05/AC06 / NXG-M01-03 / NXG-FX09-G02/G03 | vi/en, profile IANA session display, CSRF/idempotency UI | App.test.tsx with MSW | 5 passed; DST R2-10 deliberately skipped; browser Not run. |
| AC02/AC04 / NXG-M01-02/05 | register/capture/verify/login and reset/new-password login | Playwright M01 journey with local operator boundary | Discovery only; full browser/API/SQL run blocked. |
| S00/S11 | skipped/blocked must not look Passed | TestRunner, scripts and CI job structure | Passed source/tool behavior; CI not run. |

Coverage is diagnostic only, never acceptance evidence.

## Executed verification log

Local host: Windows 10.0.26200 x64; .NET SDK 10.0.302/runtime 10.0.10; Node v24.17.0; npm 11.13.0. No production service, secret, provider or database was used.

| Command/check | Exit | Result |
| --- | ---: | --- |
| git fetch --dry-run origin | 0 | Remote read check succeeded. |
| git ls-remote origin refs/heads/main refs/heads/impl/m01-s00-scaffold | 0 | Exact supplied SHAs confirmed. |
| dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release --no-restore | 0 | API/dependencies, 0 warnings/errors. |
| dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release --no-restore | 0 | Bootstrap, 0 warnings/errors. |
| dotnet build tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --no-restore | 0 | Unit project, 0 warnings/errors. |
| dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --no-restore | 0 | SQL/API project, 0 warnings/errors. |
| powershell -NoProfile -ExecutionPolicy Bypass -File scripts/dev/test.ps1 | 0 | Static verifier passed; custom 30/0/4; xUnit 11/0/0. TRX artifacts/test-results/unit-xunit.trx. |
| dotnet test UnitTests with TRX + XPlat coverage | 0 | 11 passed. Fresh TRX artifacts/test-results/r5-final/unit-xunit-r5-final-2.trx. |
| dotnet run UnitTests -- --require-os windows | 1 | Correct required-gate failure: actual separate A/B/C process test is skipped. |
| powershell test-sql.ps1 with connection unset | 2 | BLOCKED; connection value not printed. |
| dotnet test IntegrationTests with connection unset | inner 1 | Three discovered tests fail closed as BLOCKED; fresh TRX artifacts/test-results/r5-final/sql-api-r5-final-2.trx. This is Not run/Blocked, not product failure evidence. |
| dotnet run IntegrationTests with connection unset | inner 2 | Preserved runner reports NOT RUN; no SQL created. |
| npm run test:unit:coverage | 0 | Vitest 5 passed, 1 explicit skip; JUnit web/Nexora.Web/artifacts/vitest-junit.xml. Coverage 5.54 percent statements, diagnostic only. |
| npm run build | 0 | TypeScript + Vite built after test additions. |
| npx playwright test --list with synthetic loopback placeholders | 0 | Four desktop/mobile instances discovered; no browser launched. |
| npm run test:e2e without config | inner 1 | Playwright config blocks explicitly. |
| powershell test-e2e.ps1 without config | 2 | Blocks before npm ci. |
| WSL capability probe | 0 | Only docker-desktop WSL; repo mount and dotnet unavailable. Linux suite Not run. |

No screenshot, trace, video, raw body, cookie, token or password artifact was generated. Playwright disables trace/video/screenshot.

## Runtime status

| Layer | Passed | Skipped | Blocked / Not run |
| --- | ---: | ---: | --- |
| Custom runner on Windows | 30 | 4 | Actual Windows A/B/C; all Linux-only cases. |
| xUnit backend | 11 | 0 | None within source scope. |
| Real SQL/API xUnit | 0 | 0 | 3 blocked because isolated SQL is absent. |
| Frontend component | 5 | 1 DST policy | Browser journeys. |
| Playwright | 0 executed | 0 | No loopback app + SQL + local operator CLI/run identity. |
| Windows ACL A/B/C | 0 | 1 required skip | Needs runtime A, CLI B and foreign C processes. |
| Linux filesystem/lock | 0 | 0 | No usable Linux repo/.NET host. |
| CI | 0 | 0 | Unpushed/untriggered. |
| Independent review | 0 | 0 | Pending. |

## Freshness of final local source evidence

This addendum controls the earlier execution tables where they differ. After those
functional runs, final LocalAccountMessageSink hardening added an active-writer
readiness check immediately before final deletion and leaves a pending file whose
payload message/fence does not match its system filename for operator review.
The custom/xUnit/frontend results below are useful pre-change evidence, but are
not asserted as final-worktree acceptance evidence.

The final local worktree was built after those changes with these commands, each
exit 0 and 0 warnings/errors: `dotnet build src/Nexora.Api/Nexora.Api.csproj
--configuration Release --no-restore`, `dotnet build
tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release
--no-restore`, and `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj
--configuration Release --no-restore`.

The host enforcement rejected a test rerun because the checked-out PR AGENTS
contains a code-only amendment. That conflicts with the requested test-engineering
scope; no workaround was attempted. The specific cross-process lock regression for
an expired final JSON or a pending filename/fence mismatch is consequently Not run,
not passed.

## Carried-forward dispositions

| ID | Current disposition |
| --- | --- |
| R3-01 | Source fixed hint change retained; real SQL RCSI/concurrency Not run. |
| R3-02 | Source fixed dual-read retained; new receipt SQL/API case Blocked. |
| R3-03 | Source fixed readiness 0025/0026 retained; staged SQL check Blocked. |
| R3-04 | Source fixed AdminGrantable retained; explicit xUnit policy passed. |
| R3-05 | Partial: fence/reconciliation retained; reclaim/revoke effect runtime Not run. |
| R3-06 | Partial/source fixed: stable runtime/operator policy retained; actual A/B/C blocked. |
| R3-07 | Source fixed with parser/ownership tests; real OS crash/idle proof Not run. |
| R3-08 | Source fixed; component profile-timezone passed; browser proof Not run. |
| R3-09 | Partial: current manifest/test harness fixes evidence quality but does not make runtime or independent review pass. |
| R4-01 | Retained source fix; same-process policy check is not claimed as A/B/C proof. |
| R4-02 | Retained/extended malformed grace and ownership cleanup source tests; kill/restart runtime pending. |
| R4-03 | Improved but Partial: current report, TRX/JUnit/coverage and fail-closed gates exist; CI/runtime/review pending. |
| R2-01, R2-02 | Partial; main authority and source policy improvements retained; SQL/race proof pending. |
| R2-03 through R2-22 | Not Fixed by R5. Reminder current-user gate, queue hints/crash, DST/navigation, picker, sharing, mutation-read/replay, audit and boundaries remain their prior open/partial or scope-blocked disposition. |
| R2-23 | Source fixed retained: IdentityV3/220000 and bounded rehash; benchmark/runtime pending. |
| R2-24 | Source fixed retained: signed anonymous-session binding; browser/race pending. |
| F01–F22 | IDs/history retained. R5 does not relabel unrelated F findings Fixed. F02/F03 remain source-fixed; F09 local capture/E2E remains partial; F10 receipt namespace source-fixed/runtime-pending; F16 locale partial; F20 evidence partial. |

## Files

- Capture/security: src/Nexora.Infrastructure/Identity/LocalAccountMessageSink.cs.
- Backend tests: tests/Nexora.UnitTests/TestRunner.cs, Program.cs, LocalAccountMessageSinkTests.cs, M01PolicyAndCaptureContractTests.cs, project and lock files.
- SQL/API tests: tests/Nexora.IntegrationTests/Program.cs, SqlApiFixture.cs, SqlApiIntegrationTests.cs, project and lock files.
- Frontend tests: web/Nexora.Web/src/App.test.tsx, src/test, vitest.config.ts, e2e/m01-identity.spec.ts, playwright.config.ts, package files; App.tsx exports test seams only.
- Orchestration: scripts/dev/test*.ps1/sh and .github/workflows/m01-ci.yml.
- Evidence: this report, paired runbook and unposted PR draft.

## Residual risks / next gates

1. Supply isolated loopback SQL through NEXORA_TEST_SQL_CONNECTION; run migrations/readiness, old-to-new receipt, RCSI ON/OFF, two-worker lease, authority revoke/resend/consume and crash/promotion cases.
2. Run Windows API identity A, operator CLI B and foreign C as actual OS processes. Verify A/B works, C cannot read, unsafe inherited ACL rejects and restart holds.
3. Supply a real Linux host with repository mount and dotnet; run 0600/0700, unsafe orphan isolation and cross-process lock tests.
4. Start loopback frontend/API/SQL with operator-only local capture and isolated run ID; execute browser desktop/mobile journeys. Add remaining M01 MFA/session/profile/SuperAdmin/denied-route journeys before calling browser verified.
5. Obtain independent review for filesystem/worker/SQL-fixture/CI changes.
6. Configure and trigger CI only after review; missing SQL/Windows/E2E environments intentionally fail required jobs. Do not change branch protection.
7. Keep post-M01 backlog out without explicit affected-slice approval.

## PR description draft — not posted

### R5 remediation and test engineering

Scope: M01-only local capture ownership/crash recovery and test automation. No production provider, deploy, migration rewrite, data reset, PR merge or branch-protection change.

Key changes:
- Secure temporary capture creation, bounded ownership-gated recovery and unsafe orphan isolation.
- Final JSON filename/payload identity gate; valid legacy captures preserved.
- Explicit Passed/Failed/Skipped behavior; required OS skips and absent SQL/E2E configuration fail closed.
- Preserved receipt compatibility, readiness, signed anonymous binding, IdentityV3 rehash, AdminGrantable and profile timezone fixes.

Evidence:
- Backend build, static verifier and xUnit passed locally.
- Custom runner: 30 passed, 4 explicit skips; required Windows A/B/C gate correctly nonzero.
- Frontend component: 5 passed, 1 explicit DST policy skip; frontend build passed.
- SQL/API and browser E2E are blocked by absent isolated runtime configuration.
- No CI run exists for this unpushed worktree; independent review is pending.

Reviewer attention:
1. Review local capture ACL/fence/recovery and ownership boundaries.
2. Require real SQL RCSI/concurrency/receipt and actual Windows/Linux evidence before local-verified claims.
3. Do not merge from this draft; normal PR review/checks remain required.
