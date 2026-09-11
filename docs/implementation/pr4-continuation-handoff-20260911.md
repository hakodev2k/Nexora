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

## Remote reconciliation update — 2026-09-11

- Local HEAD before push: `2ba68f62fa14291cb701c60ebc3f076017e1d613`.
- Remote `origin/impl/m01-s00-scaffold` before push: `1de199040bebc600cf75800a55b807dc63b5c1f2`.
- The local handoff and implementation commits exist locally and are two commits
  ahead after `git fetch origin`.
- `git push origin HEAD:impl/m01-s00-scaffold` was attempted but blocked because
  the environment has no GitHub HTTPS credential (`could not read Username for
  'https://github.com'`). This handoff does not claim the commits are on GitHub.
