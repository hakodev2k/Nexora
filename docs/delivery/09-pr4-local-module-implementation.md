# PR4 continuation — missing local API slices

Accountable owner: implementation agent. Authorization: the user's current instruction to implement the 15 missing API groups and commit to PR4, plus DEC-20260909-014. Existing explicit SQL/browser QA authorization in this session supersedes the repository's older code-only execution amendment. No merge, production or real provider execution.

Baseline: PR4 `def3e4b651c63935a35fd4eb47b68ca49d288fba`; 18 gated modules, 15 missing candidate API groups. The interrupted uncommitted Time/Focus implementation was lost with the runtime reset. Its earlier checks do not certify this reconstruction.

## First reconstructed slice: unlinked Time Tracking

Trace: NXG-SYS-02/03/05/09/11/13/14; NXG-FX18-G01/G03; FX-18-BR-001/002/003/005 and AC-001/002/003; UX FX18-S01–S04. This is a partial module, not full acceptance of FX18.

Exact SELF actions: `time.timer.read/start/stop/resume`, `time.entry.read/create/update/trash/restore/history`, `time.report.read`. SQL permission rows are exact full keys; Admin grantability is an explicit allowlist. Support, purge, source links and conversion are not implemented by this slice and are not granted. Unknown request fields, including OwnerId, status and source references, are rejected.

API: `/api/v1/time/capabilities`, `/entries`, `/entries/{id}`, `/entries/{id}/history`, `/timer`, `/report`; POST timer/stop/create/trash/restore and PUT entry. Commands require UUID Idempotency-Key; edits and transitions require If-Match (428 missing, 412 stale). Owner is resolved from the current authenticated PersonalSpace. No caller-selected owner. Conflict errors are safe 409/422 problem responses.

DB: additive replayable migration 0032; owner Entry and immutable Correction rows with composite owner FK; filtered unique Running slot. Stop writes an immutable server EndAt once; Resume creates a new ID. Manual UTC End must follow Start; overlap requires explicit confirmation. Gross report includes stopped records only and flags overlaps. Trash preserves records/history; no purge or Finance effects. Owner serialization, current action rechecks, receipt, audit and mutation share one SQL transaction; rejected commands roll back receipts/history. Replayed commands are still checked against current authority.

UI: compact create/edit/start popup with shared accessible ActionDialog, retained failed inputs/retry key, explicit overlap checkbox, confirmation for Trash/Restore, history pagination, loading/error/empty and safe denied-data clear. Server-projected per-action capabilities control requests/buttons; entry.read does not expose running timers. Dates are browser-zone input converted to UTC; labels identify the zone. Deep route/filter/state and full responsive/accessibility acceptance remain pending.

Migration enables the installed local FX18 subset and new-registration default only; existing user entitlements and Admin grants are preserved. Reversal: disable module system policy and deploy preceding build; retain Entry/Correction data. No destructive down migration.

## Second reconstructed slice: unlinked Focus

Trace: NXG-FX19-G01/G02 (offline subset)/G03 (notification subset); FX-19-BR-001/002/004; AC-001/003; FX19-S01–S04 surfaces. Six SELF keys: `focus.session.read/start/pause/resume/cancel`, `focus.preference.update`. `focus.session.finish_phase` is an internal SYSTEM effect, never an Admin grant or public endpoint.

Typed `/api/v1/focus` sessions/preferences/capabilities API rejects Task refs, elapsed/state/owner overrides. Commands use current SQL authority, UUID receipts and If-Match. Migration 0033 adds constrained owner preferences and one active slot (Running or Paused). Defaults/ranges are 25 (1–180), 5 (1–60), 15 (1–120), cycle 4 (1–12). Start pins planned duration; preference edits do not rewrite active sessions. Pause accumulates SQL elapsed; Resume starts a new server segment; Cancel retains history. Completion closes the one due phase at its exact planned server instant without offline cycle creation.

Worker polls at five seconds, bounded to 100 due rows. Owner lock and current normalized SQL role/action are checked in the same transaction as phase completion, audit and notification deduplication. SQL failure rolls back effects; persisted retry backoff is 30 seconds, maximum five completion failures. Exhausted completion repair/retry UI is still open; no unbounded hidden retries. Module/entitlement revocation blocks completion until current authority qualifies. Retrying/restarting cannot duplicate an already completed phase.

Notification-owned writer records one logical completion and three channel projections atomically. In-app is Delivered; local unavailable Email is NotApplicable/ProviderUnavailable and Push PermissionUnavailable/ProviderUnavailable. These are availability projections, not evidence of actual Email/Push transport. No provider call. Full configured delivery routing remains open.

UI has shared preferences popup (four fields), explicit phase selection, Pause/Resume, cancel confirmation and paginated history. Remaining seconds are fetched from the server at each sync. Errors/denials clear protected data. No automatic next phase, linked Task, conversion, fake completion control, or break-as-work entry.

FX18 remains a hard dependency in the current registry; test setup enables it through the normal signed SuperAdmin preview/commit flow, then restores dependent grants in reverse order. Existing grants are never globally rewritten by migration. FX19 installed local subset is Ready, whole module Partial. Rollback retains SQL records and disables the local module policy.

## Verification at the first commit checkpoint

- API Release build: succeeded, 0 warnings and 0 errors.
- Frontend TypeScript and Vite production build: succeeded; existing bundle-size warning (533.26 kB).
- Fresh SQL integration, real browser workflows, current migration replay and five-viewport checks: not run yet at the checkpoint.
- Independent authorization/migration review: Pending. Self-inspection is not independent review.

## Open implementation work

FX18 linked Task/Project lifecycle contracts, manual purge/Trash provider, tag/date/source filters and Focus conversion remain open. FX19 source links and explicit Time conversion remain open. Other missing groups: FX10/28/29/30/31/33/34/35/36/37/38/39/40. FX04/05/07 already have candidate handlers but their module gates still need contract/evidence reconciliation. Paused FX30/34/35 real execution stays disabled; local simulation must be explicit and disabled by default. Vault must implement the approved cryptographic recovery/portability design, not metadata CRUD masquerading as Vault.

The historical 200-workflow run and 733-action ledger are unchanged historical evidence. No action-wide, module-wide or R1 completion is asserted here.


## Reconstruction checks and failure provenance

API Release build after Focus: 0 warnings/errors. Frontend TypeScript/Vite build passed (539.08 kB chunk warning). Real SQL Server 2025 applied 34 manifest migrations successfully. Unit 12/12; frontend 69 passed / 1 pre-existing skip. Fresh tests are FN-041 Time, FN-042 Focus, FN-043 Admin/foreign-owner. One intermediate FN-042 desktop execution passed in 72.534 seconds; full final viewport evidence is pending at this writing.

Attempt 1 found client `eTag` incompatible with the repo's `etag` wire naming and missing FX18 prerequisite in Focus setup. Fixed client naming and normal signed dependency grant setup. Attempt 2: Focus passed, Time edit selector waited on an implicit textarea label whose existing content changed its label text. Explicit htmlFor/id labels fix the product form. Attempt 3: Time reached retained Trash/Restore checks, but the intentionally overlapping fixture duplicated the edited row's name. Give that separate fixture a distinct name; do not weaken business selectors or clear SQL records. All failed evidence remains recorded; timeout extensions do not constitute a pass. Final current-source run and independent review remain required.

Final desktop reconstruction run: **3/3 passed** (FN-041, FN-042, FN-043), real SQL/browser, no retries/skips. FN-042 also asserts one logical completion with exactly InApp/Email/BrowserPush availability projections; FN-043 verifies Admin Unset/Allow/Deny/Unset and foreign-owner read/update/history 404. Attempt 4 additionally exposed no-op access preview rejection and asynchronous Trash pagination setup; fixture helpers now check current effect and wait for the target card or visible next page. No business assertion or gate was relaxed. Five-viewport run is Pending.


## Completed reconstruction evidence (product source ba692f8)

One full browser run: 15/15 passed, 3 workflows × 5 viewports (1920, 1366, 768, 390, 320 widths), 0 skipped/retried/flaky attempts. Real isolated SQL Server 2025 and normal paced form logins. The run included owner mutations, rollback/conflict/idempotency, concurrent timer start, stopped-end retention, new-entry Resume, Trash/Restore, Admin SELF projection/gates, foreign-owner 404 and a real one-minute offline phase completion with one three-channel notification projection. No source/provider clock was simulated.

SQL integration regression: 15/15 passed, 0 skipped/failed, fresh generated databases including migration replay and required-migration readiness. Unit 12/12; frontend 69 passed / 1 existing skip; API and operator builds 0 warnings/errors; frontend and functional-test typecheck pass. The first ad hoc functional typecheck missed the web project's Node type root and found a static absolute browser import in a helper; explicit type roots and the identical import URL via a variable resolved it. Product code stayed unchanged during the full viewport run. The evidence records this helper-only source timing explicitly.

Current SQL catalog confirms Ready=24, Blocked=13, Paused=3. Ready denotes installed subset availability, not whole-module acceptance. FX18/19 still have open committed capabilities. The other 13 missing handler groups remain missing; 3 existing-handler groups remain gated. No whole-action, whole-screen, whole-feature or R1 sign-off.

Evidence: [machine-readable results and source hashes](../implementation/evidence/time-focus-20261001/results.json). New stronger Time report oracle reads only the stopped-entry timestamp series from real SQL and independently sums durations in JavaScript; its five-viewport rerun is recorded separately after it finishes.

Supplementary final Time report run: **5/5 passed**, one FN-041 workflow on five viewports, independent real-SQL timestamp sum equals API gross duration/count and actual overlap. This is separate from the 15/15 run. All 18 bound SELF/SYSTEM keys remain Partial-tested-subset with fullActionPassed=false.

Supplementary dependency run: **5/5 passed**, FN-041 on five viewports after strengthening setup to exercise an enabled Focus dependent, atomic Time/Focus disable and restoration through normal signed grant preview. SQL report oracle remains enabled in this run. No dependency guard was bypassed.
