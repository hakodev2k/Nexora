# Support existing-source/runtime gate reconciliation — 2026-10-27

Owner: shopping_completion; accountable coordinator/independent reviewer: root. Source HEAD `a0215f7b3e85d33503cff9077b41cc065240cb81` plus current worktree; branch `impl/m01-s00-scaffold`. No commit/stage/reset performed by this worker. Current DEC014 amendment authorizes isolated synthetic automated QA; D05/Q02/Q04 and runtime readiness remain authoritative. Engineering skill, Technical Lead, backend/security/database/verification/owner-isolation routing applied. Source/goals/action binding and conditional remediation are in [the bounded contract](support-consent-20261026-contract.md).

## Outcome and discrepancy

E05a/E05b have registered endpoint, SQL and dormant UI source, but are **GATED**, not active verified workflows. Report21 originally classified both `A` from source presence; its MD/CSV now explicitly retain the original classification alongside the Oct27 correction. This does not silently change the earlier inventory or claim the positive implementation is complete.

| Evidence | Exact conclusion |
|---|---|
|`database/migrations/20260910_0018_local_runtime_catalog_gate.sql:30` and grant-disable statement|FX05 intentionally `Blocked`, SystemEnabled0, RegistrationEnabled0; retained user grants disabled.|
|`database/migrations/20260911_0021_core_sharing_support_files.sql:242`|Adds missing action catalog rows only; explicitly preserves authoritative catalog. No later migration restores FX05 Ready.|
|`src/Nexora.Infrastructure/Support/SqlSupportService.cs:72/127/166/276`|Consent/read/revoke/session-end check current FX05 capability before data or receipt work.|
|Same service `OpenEmergency`|Explicit `DecisionBlocked` shell pending approved duration/recent-auth Q02; no access/session write.|
|D05 sensitive-projection contract|Default-deny source-specific diagnostic projections; no broad nonSELF field exposure or readiness permission.|
|`web/Nexora.Web/src/App.tsx:1412/1464/1525`|Owner navigation hides Support while disabled; direct `/support` renders unavailable, not the dormant form.|

No Support production source, module state, permission, grant, projection, provider or migration was changed. No SQL fixture hacks were used to create a positive path. Root explicitly decided to preserve this gate and ship negative verification only.

## Actual SQL execution (separate uniquely generated disposable fixture DBs)

Command pattern: `dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Debug --filter FullyQualifiedName~SqlSupportConsentTests --logger trx --results-directory <run-specific-folder>`. Existing private owned local SQL connection loaded into process without printing its value. `SqlApiFixture` creates/cleans its own loopback-only `Nexora_Test_<GUID>` database and actual TestServer host, running the real migrations. Shared browser API process was neither rebuilt nor restarted.

| Run / cases | Result | Evidence | Classification |
|---|---|---|---|
|Original default24h same-key positive setup,1 case|FAIL before consent request: expected Ready, actual Blocked|`artifacts/test-results/support-20261027-before/support-before.trx`|Incorrect setup assumption discovered actual gate; positive digest/replay assertion **BLOCKED**, never executed.|
|Initial negative gate,3 role cases|FAIL before requests: unquoted reserved `identity` schema in test count SQL|`artifacts/test-results/support-20261027-gate/support-gate.trx`|Test-oracle SQL identifier corrected to `[identity].[RequestReceipt]`; all assertions retained.|
|Final negative gate,3 cases (User/Admin/SuperAdmin)|**3 PASS,0 FAIL,0 SKIP**,2s test duration|`artifacts/test-results/support-20261027-gate-final/support-gate-final.trx`; `tests/Nexora.IntegrationTests/SqlSupportConsentTests.cs`|Only gate/no-effect scope verified.|

Each final role case asserts actual catalog Blocked/off and no enabled FX05 user grant; grant/session GET409 `ModuleUnavailable` without records; same body/key grant POST repeated twice409 without committed acknowledgement; revoke/end409; SQL contains no new owner SupportGrant/AccessSession or mutation audit; Support receipt count unchanged. Three cases are one parameterized gate scenario, not three positive business workflows or full FX05 coverage.

Curated copies of all three TRX reports and SHA256 manifest are retained under `docs/implementation/pr4-completion/evidence/support-gate-20261027/`. Final SQL test source SHA256: `72091270182ACD2BCD9EABE2EB2528972E8E74A37867644FBE8F5F937D550BA1`. No Support production code changed between those runs. Strict TypeScript check of the new browser spec and the bounded Focus assertion passed; this does not count as browser execution.

One earlier automatic approval review failed due host usage-limit infrastructure before command execution. The approved retry subsequently ran successfully; this did not grant any product gate exception.

## Browser evidence

`web/Nexora.Web/functional/support-gate.spec.ts` executed with normal owner login, disabled capability, hidden navigation, direct-route unavailable, no consent form/success, real denied same-key POST and metadata GET, unchanged capability, responsive screenshot. **Desktop1366×768:1PASS0.901s; mobile390×844:1PASS0.669s**,0failed/0skipped. Desktop batch additionally includes separate FN-042 Focus1PASS77.773s; do not count that as another Support scenario. Actual runtime keeper owned synthetic SQL/API/Vite on loopback; no positive mock APIs, permission changes or provider calls. Parent released the shared writer before execution; original reports copied immediately before next run.

Reports and both viewport PNGs are curated with hashes in `evidence/support-gate-20261027/`; raw JSON reports remain `web/Nexora.Web/artifacts/support-gate-20261027/{desktop,mobile}-results.json`. Author opened both actual PNGs through `view_image`: clear unavailable heading, readable copy, no consent controls or horizontal overflow in either viewport. This visually verifies only the unavailable state. Copy still uses technical `PersonalSpace`/`disable` terms, a low-priority shared unavailable-screen wording improvement for coordinator consideration; no dormant consent UX claim.

Browser spec SHA256 `6473A3A30DB13691E89F77E10801863D76B6533050135240622000C1F2617F8C`; unchanged Support service SHA256 `C94A0442F04C9926A2A852FCDDE9B331199B9B2DCD01F0BE2C21AAF72E61475C`. Both browser projects are actual Chromium viewport runs, not a resized static screenshot. Browser writer released after preserving results; no API restart.

## Remaining conditional remediation (not shipped)

| Issue | Severity / root cause | User impact / current mitigation | Work before an approved ready slice |
|---|---|---|---|
|SUP-L01 default24h digest|Medium latent; hashes newly resolved `now+24h`, so equal request/key retries differ|Would409 instead of original commit recovery if enabled; current Blocked gate prevents use|Immutable requested-expiry canonical digest in same namespace; legacy mismatch fail closed; real SQL fail-first/pass and collision/current-authority tests.|
|SUP-L02 opaque recovery/current authority|Medium latent; legacy receipts lack strict proof and generic409; mutation authority not rechecked inside transaction|Would lack reliable unknown-result recovery; current gate prevents use|Minimal versioned actor/owner/action/resource receipt proof, live-session/current-action checks in transaction, opaque ACK only; endpoint/client union validated; independent security review.|
|SUP-L03 consent/recovery UI|Medium latent; immediate POST, no normative final audience confirmation, mutation latch or uncertain intent retention; failed protected refresh may retain private state|Dormant form is unavailable; no successful user journey claimed|Explicit one-module/any-qualified-Admin/read-only consent, frozen command/key/ETag, retry/abandon, epoch/dirty leave, denied-state clearing plus component/SQL/browser proof.|
|SUP-L04 exact precommit24h expiry|Contract gap; no server authoritative preview API|Cannot promise exact server expiry before commit|Keep transparent24h-from-confirmation wording; resolve strict preview requirements without invented lifetime/security policy.|
|SUP-L05 custom duration contract naming|Requirement/source discrepancy: PDS-SUP-003 says `CustomExpiry`; existing API/SQL/UI uses `Custom`|Positive contract not reconciled; currently gated|Resolve exact wire mapping/compatibility against authoritative requirement before activation; do not silently rewrite requirement or claim either enum verified.|
|SUP-G01 module readiness and Q02/Q04/D05|SECURITY-GATED / contract+readiness|E05a/b remain GATED|Complete reviewed readiness and exact permitted metadata/source/session contracts; no fixture or UI authorization bypass.|

Positive owner grant/revoke, support open/end, expired/revoked opaque replay, foreign-owner business isolation and real Support UI recovery are **BLOCKED / NOT EXECUTED**, not covered by negative gate PASS. Emergency/business projections are not introduced. Human UI acceptance, independent security review and D09 current-head CI remain pending. Overall FX05 verdict: **GATED / NOT RELEASE READY**.
