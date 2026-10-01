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

## Verification at this checkpoint

- API Release build: succeeded, 0 warnings and 0 errors.
- Frontend TypeScript and Vite production build: succeeded; existing bundle-size warning (533.26 kB).
- Fresh SQL integration, real browser workflows, current migration replay and five-viewport checks: not run yet at the checkpoint.
- Independent authorization/migration review: Pending. Self-inspection is not independent review.

## Open implementation work

FX18 linked Task/Project lifecycle contracts, manual purge/Trash provider, tag/date/source filters and Focus conversion remain open. FX19 reconstruction remains open. Other missing groups: FX10/28/29/30/31/33/34/35/36/37/38/39/40. FX04/05/07 already have candidate handlers but their module gates still need contract/evidence reconciliation. Paused FX30/34/35 real execution stays disabled; local simulation must be explicit and disabled by default. Vault must implement the approved cryptographic recovery/portability design, not metadata CRUD masquerading as Vault.

The historical 200-workflow run and 733-action ledger are unchanged historical evidence. No action-wide, module-wide or R1 completion is asserted here.
