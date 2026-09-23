# PR #4 continuation handoff — 2026-09-11

Repository: `hakodev2k/Nexora`  
PR: #4  
Branch: `impl/m01-s00-scaffold`  
Base: `main`  
Authority: `DEC-20260909-014`  
Execution amendment: code-only; do not add new tests, fixtures, demo records or runtime data.

## Snapshot before this handoff note

- Remote branch head before this note: `dea1ac61782f5b85cf45b238f212f51af50429de`.
- Main code implementation commit: `e42516e2360ad2afd7d8a0144412a15086770983`.
- Evidence/CI revision: `a3a0a7b84a124a8a9063d35d65641119e2824718`.
- Current implementation CI: workflow `Nexora local implementation checks`, run `34508111597` / run `149`, **Pass**. Agent baseline run `34508111463` / run `166`, **Pass**.
- PR is open, unmerged and must not be merged by this work.

## Completed in the code commit

The code commit addresses the known review blockers and local source-contract gaps:

1. API and Bootstrap Release builds compile with warnings-as-errors.
2. SQL Identity password reset reads `[identity].[MfaCredential]`; MFA-enabled reset fails closed as `MfaRecoveryRequired` before password, security-stamp or session mutation.
3. Login CSRF rotation is captured from every successful response by frontend `apiFetch`.
4. Developer Toolbox sends UUID `Idempotency-Key`; global CSRF/idempotency filters remain enabled.
5. Module catalog migration `20260910_0018_local_runtime_catalog_gate.sql` keeps incomplete modules disabled and leaves FX30/FX34/FX35 paused/provider-gated; the new FX04/05/07 source slices remain fail-closed until their open contracts and runtime evidence are complete.
6. SELF authorization preserves User own-resource baseline for Admin/SuperAdmin while administrative/cross-user/support authorization remains separate.
7. Readiness checks SQL, migration journal, required migrations through `0021`, and bootstrap/security invariant; liveness is process-only.
8. Idempotency fallback, request-size metadata, required separate `NEXORA_IDEMPOTENCY_SECRET`, account/security `NOLOCK` removal and stale catalog naming were hardened.
9. FX11–FX13 source alignment adds project/task title/description bounds, A–Z project ordering, time-bound confirmations, terminal locks, Task Overdue, one-way Task → Calendar projection, Calendar Task projection edit denial and Calendar view selectors.
10. Documents creation now requires explicit DocumentType/EditorMode and exposes Grid/Table selection.
11. Migrations `20260910_0019_productivity_contract_alignment.sql`, `20260910_0020_task_calendar_projection.sql` and `20260911_0021_core_sharing_support_files.sql` are included. `scripts/dev/migrate.sh/.ps1` delegate to `Nexora.Local migrate`, which owns the deployment lock, checksum journal and pending-file replay.
12. `docs/implementation/r1-requirement-traceability-matrix.md` records all FX rows and honest Partial/Not implemented/Blocked/Paused/Not run states.

No new automated tests or synthetic fixtures were added because the repository amendment assigns functional QA/runtime verification to the human owner.

## Verified evidence

Pass:

- CI run 149: `bash scripts/dev/verify.sh`.
- CI run 149: API and Bootstrap Release builds, both 0 warnings/0 errors.
- CI run 149: `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release`, 24 tests, 0 failed.
- CI run 149: `npm ci --prefix web/Nexora.Web` and `npm run build --prefix web/Nexora.Web`.
- Local API build: Pass, 0 warnings/0 errors.
- Local Bootstrap build: Pass, 0 warnings/0 errors.
- Local frontend build: Pass.
- Local `git diff --check`: Pass.

Not run:

- Local `bash scripts/dev/verify.sh`: Windows WSL/Bash creation returned `E_ACCESSDENIED`; CI command passed.
- SQL Server integration/migration execution, `NEXORA_TEST_SQL_CONNECTION`, readiness runtime and SQL isolation/lifecycle checks.
- Browser/manual CSRF rotation, Developer Toolbox idempotency, owner-isolation and lifecycle QA.
- Independent security review.

## Continue next

1. Fetch the current PR head and read this handoff plus the traceability matrix before changing code.
2. Keep PR body evidence aligned with the actual latest SHA and CI run; never call docs/action contracts runtime proof.
3. If a synthetic local SQL Server is available, run the existing integration suite and migration/readiness checks using only `NEXORA_TEST_SQL_CONNECTION`; otherwise record **Not run**.
4. Have the human owner execute the documented manual flow in `docs/implementation/pr4-review-qa-script.md`.
5. Continue remaining matrix gaps only when their contract is sufficient. Do not enable FX30/FX34/FX35 real provider execution or production behavior.
6. Do not merge PR #4.

## Known remaining status

This is not “R1 complete”, “Accepted”, “Runtime verified” or production-ready. FX04/05/07 now have bounded source slices but remain deliberately catalog-gated: FX05 Emergency is blocked by Q-02 and FX07 still lacks the approved cover/replacement/cleanup contract; all three also lack SQL/browser isolation evidence. FX10/14/15/17/18/19/28/29/31/33/36/37/38/39/40 remain blocked/unimplemented in the local catalog; FX30/34/35 remain paused. FX11–FX13 are source-aligned but still Partial until SQL/browser evidence and remaining ICS, DST/all-day and aggregate restore requirements are handled. The full matrix is authoritative for the per-module gap list.

## Continuation update — 2026-09-11

The current worktree continues on `impl/m01-s00-scaffold` and remains code-only
under `DEC-20260909-014`. It adds the following bounded source slices; this
section supersedes the earlier “FX04/05/07 absent” snapshot above:

Source implementation commit: `6fb229ab7d56b6ee7091b7e85ea028c47c1a893b`.

| Slice | Source delivered | Current limitation |
| --- | --- | --- |
| FX04 Sharing Engine | `security.ShareLink`/`ShareAllowedUser`, SHA-256 capability hashes, owner/source/viewer gates, expiry/revoke, sharing epoch invalidation, safe Project/Published Document projections, API routes and `/share/{token}` UI | Migration `0018` still keeps FX04 Blocked; SQL/browser/token-exposure/runtime evidence not run; no write-through sharing action. |
| FX05 Support/Emergency | `security.SupportGrant`/`AccessSession`, owner consent duration modes, AdminPermission + exact module/read-action checks, durable opening audit, session end/revoke, API/UI shell | Migration `0018` still keeps FX05 Blocked; no support business-data read projection; Q-02 emergency returns explicit `DecisionBlocked` pending duration/recent-auth decision. |
| FX07 Files/Attachments | Private generated storage keys, upload-handle hashes, bounded local staging/scan, MIME/extension/signature/text/Office archive checks, owner/source/reference/lifecycle guards, API/UI upload/list/download/rename/trash flow | Migration `0018` still keeps FX07 Blocked; cover 5 MiB/25MP, binary replacement revisions, persisted detected type, cleanup worker and external AV are not implemented; purge cleanup worker is still needed. |

Additional security/runtime changes:

- `SqlSelfCapability` now requires an existing `Permission` row with
  `EffectiveStatus='Resolved'`, `RegistrationEnabled=1`, and the current
  module/dependency grants; all decisions remain SQL-backed per request.
- `/health/ready` now requires
  `20260911_0021_core_sharing_support_files.sql` in the migration journal.
- Unsafe JSON mutations retain the 64 KiB limit; raw file upload uses a
  separate 25 MiB endpoint group and still requires CSRF plus UUID
  `Idempotency-Key`.
- Raw share tokens and upload handles are returned only from their initiating
  response and are not written to SQL, browser storage, audit payloads or logs.

### Current source evidence

Actually run in this environment after the continuation changes:

- `python3 .ai/scripts/verify-baseline.py` — **Pass**; 15 baseline gate tests,
  application tests not run.
- `python3 scripts/dev/verify-s00.py` — **Pass**; structural migration,
  source-slice and no-bypass checks.
- `npm ci --prefix web/Nexora.Web --ignore-scripts` — **Pass**.
- `npm run build --prefix web/Nexora.Web` — **Pass**; TypeScript and Vite
  production bundle completed.
- `git diff --check` — **Pass**.
- `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release` —
  **Blocked** because this environment has no `dotnet` executable
  (`/bin/bash: dotnet: command not found`). The Bootstrap build and unit-test
  command have the same missing-tool blocker.

Not run: SQL migration execution/replay (including `scripts/dev/migrate.*`), SQL
readiness, API/Bootstrap compile, application unit tests, browser/manual QA, E2E,
owner-isolation journeys, storage/scan tests, independent security review and CI
for this continuation. No tests, fixtures, demo data, provider calls, secrets or
production changes were added. Do not merge PR #4.

## FX14 continuation update — 2026-09-11

The next coherent local batch implements FX14 Reminders/Scheduling. It adds
`20260911_0022_reminders_scheduling.sql`, `calendar.Reminder`, owner/source
uniqueness and due-state indexes, source reconciliation triggers, action
catalog rows and local FX14 enablement. `SqlReminderService` and the Minimal API
route `/api/v1/reminders/{sourceType}/{sourceId}` enforce owner scope, allowed
Task/manual-Event sources, source and reminder ETags, UUID idempotency and
source lifecycle checks. Schedule/invalidation intent and sensitive changes are
recorded in SQL outbox/audit tables.

`ReminderDispatchWorker` is deliberately a local dispatcher shell: it rechecks
current source revision/lifecycle and FX06/FX14/source module grants, handles a
late window of at most 15 minutes, deduplicates by owner/source/revision/due,
and produces an owner-local Inbox notification plus three delivery rows. It
never invokes a real Email or BrowserPush provider; those rows are recorded as
`NotApplicable`/`PermissionUnavailable` so local UI can show degradation
honestly. `/modules/FX14` consumes typed API data and exposes loading, empty,
error and ETag conflict states.

FX10 remains the next blocked batch. Its docs explicitly defer code until the
Vault/key portability decision (Q-04) and production backup/RPO/RTO decision
(Q-08) are approved; no backup, restore, user-file import or export claim was
added here. The migration has not been executed in this environment because no
SQL runtime is available. Do not merge PR #4.

## FX15 and FX17 continuation update — 2026-09-11

The next coherent local batch implements FX15 Planner and FX17 Habits with
forward-only migration `20260911_0023_planner_habits.sql`. It creates
`productivity.PlannerPin`, `Habit`, effective-dated `HabitSchedule`, and
owner-scoped `HabitCheckIn` tables. The migration has unique owner/task/date
and owner/habit/local-date constraints, index support for the local queries,
and a trigger that rejects overlapping Habit schedule intervals. It promotes
FX15 and FX17 to `Ready + SystemEnabled + RegistrationEnabled` only for the
local runtime catalog and active-user grants.

`SqlPlannerService` exposes `GET /api/v1/planner`, pin/update/reorder/unpin
commands, and the `/planner` React route. It checks FX15/FX12 authority plus
same-owner active Task/Project lifecycle at every actionable source boundary.
Planner metadata never changes Task status, Task start/end, Task reminder or
the Task-owned Calendar Event; completed/terminal sources stay visible only as
unavailable history and can be unpinned.

`SqlHabitService` exposes `/api/v1/habits*` and `/habits`. It validates IANA
timezone, `Boolean`/`Count` mode, target and weekday mask, prevents future
check-ins, stores the check-in's local date and effective schedule, and makes
same Habit/local-date writes unique. Current streaks skip unscheduled and
paused intervals and stop at a missed scheduled target day. Active/Paused/
Archived transitions preserve schedule history and write audit records.
Habit reminder local time is stored as local metadata only: no FX14 dispatch,
email, BrowserPush, external provider, Calendar Event, social/team feature,
Trash or purge endpoint is claimed in this batch.

Both slices have SQL owner filters, current SQL capability checks, quoted ETag/
If-Match for update/lifecycle mutations, UUID idempotency for creates and
commands, thin Minimal API handlers, and typed frontend loading/empty/error/
conflict surfaces. Required migration readiness now extends through `0023`.
The source must still undergo real SQL replay, API/browser owner-isolation,
lifecycle race, timezone/DST and idempotency journeys; no runtime verification
or production claim is made. FX10 remains Blocked by Q-04/Q-08. Do not merge
PR #4.

## Remote reconciliation update — 2026-09-11

- Local HEAD before push: `2ba68f62fa14291cb701c60ebc3f076017e1d613`.
- Remote `origin/impl/m01-s00-scaffold` before push: `1de199040bebc600cf75800a55b807dc63b5c1f2`.
- The local handoff and implementation commits exist locally and are two commits
  ahead after `git fetch origin`.
- `git push origin HEAD:impl/m01-s00-scaffold` was attempted but blocked because
  the environment has no GitHub HTTPS credential (`could not read Username for
  'https://github.com'`). This handoff does not claim the commits are on GitHub.

## Continuation update — 2026-09-12 — FX15/FX17 owner-integrity hardening

### Task brief and authority

- Requested outcome: continue PR #4 on `impl/m01-s00-scaffold` with a real new
  source batch for the local FX15 Planner and FX17 Habits slices, commit it and
  push it to the open PR without merging.
- Local revision before this batch: `5230f3bfe98dde3894ae6ca37393a980914a0c85`.
  The branch and observed tracking ref were both `impl/m01-s00-scaffold` at
  that revision before source changes; the initial fetch was blocked by the
  workspace `.git/FETCH_HEAD` permission boundary and must be retried with the
  authorized Git runtime before push.
- Authority: current Product Owner decision `DEC-20260909-014`; execution
  amendment is code-only. No new tests, fixtures, demo records, provider calls,
  production configuration, secrets or runtime data are authorized in this run.
- Bound goals: `NXG-FX15-G01..G03` and `NXG-FX17-G01..G03`. Evidence is bound to
  `FX-15-BR-001..005` / `FX-15-AC-001..003` and
  `FX-17-BR-001..005` / `FX-17-AC-001..003` in the current feature, module-goal,
  action-catalog, UX and shared-behavior contracts.
- Selected operating rules: `.ai/roles/technical-lead/rules/core-rules.md`,
  `.ai/rules/architecture.md`, `.ai/rules/backend.md`, `.ai/rules/frontend.md`,
  `.ai/rules/security.md`, `.ai/rules/database.md`, `.ai/rules/verification.md`,
  and the repository `nexora-engineering` skill routing. The work preserves
  PersonalSpace owner isolation, SQL authority, fail-closed lifecycle checks,
  quoted ETag/If-Match, durable UUID idempotency and no provider execution.
- Unresolved gates: SQL replay/readiness, API/browser/manual QA, timezone/DST
  and concurrency evidence, independent security review, and final CI for this
  revision. Support, Trash/purge, social/team behavior and FX14 reminder
  dispatch remain unavailable for these modules.

### Source batch

The batch adds `20260912_0024_planner_habits_owner_integrity.sql`, which fails
closed on legacy owner mismatches, adds same-owner composite foreign keys for
Task/Project and Planner/Habit relationships, and adds nullable actor metadata
for new Planner, schedule and check-in writes. Planner now uses the owner's
profile timezone when no date range is supplied, locks source rows during
mutations, treats deleted sources as unavailable and checks the exact update
capability. Habits use stable check-in idempotency, literal-safe search,
effective schedule rowversion checks and actor attribution. The React screens
use owner-timezone date formatting and UTC date-only range arithmetic to avoid
browser/DST drift.

The runtime catalog/readiness and traceability documents now include migration
`0024` and record FX15/FX17 as locally usable source slices rather than runtime
verified. The required source behavior remains explicit: Planner only references
Tasks and never mutates Task/Calendar/reminder state; Habits keep local-date
history and store reminder time as metadata only.

### Verification for this update

- `npm ci --prefix web/Nexora.Web --ignore-scripts` — **Pass** after an
  authorized retry for the local npm-cache `EPERM`.
- `npm run build --prefix web/Nexora.Web` — **Pass**.
- API and Bootstrap Release builds — **Pass**, 0 warnings and 0 errors.
- Existing unit-test executable — **Pass**, 24 tests and 0 failures; no tests
  were added.
- `git diff --check` — **Pass**.
- `python3 scripts/dev/verify-s00.py` — **Not run** because Python is not
  available in this Windows environment.
- SQL migration/replay — **Not run**; local SQLEXPRESS was discovered, but no
  explicit `NEXORA_SQL_PASSWORD` was configured and the repository runner does
  not permit a default credential. Browser/manual QA, DST/isolation journeys,
  independent security review and final CI remain pending.

### Handoff

Retry Git fetch, commit and push using the approved repository Git runtime, then
record the resulting local/remote SHA and CI state in the final response. Do not
merge PR #4. If SQL or CI remains unavailable, report it as `Not run`/`Pending`,
not as passed evidence.
