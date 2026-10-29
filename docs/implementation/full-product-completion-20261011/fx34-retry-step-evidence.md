# FX34 RetryStep — implementation, pending human acceptance

Task owner root; independent source reviewer baseline_review. Main 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3, PR4 parent f97dd0c6378bcf73da5151899d84da1d682ddfc2. Explicit current PO authorizes local FX34 implementation; AGENTS code-only run reserves functional tests/fixtures for human QA. No provider, production or merge authorization.

Source contracts: P06-AUT-004/005/006/007/008/010/014; FX-34-BR-002/003/004/005; FX-34-AC-001/002/004; automation.run.retry_step SELF, FX34-S05. Goals NXG-FX34-G02/G03. Sources docs/features/34-automation-and-scheduler.md, docs/action-catalog/modules/34-automation.md, docs/ux-ui/modules/34-automation.md, phase06 requirements. Rules loaded through .ai/routing.json baseline/backend/database/frontend/security; no historical pause silently changed. Previous session authority resumes local implementation only.

## Production contract

POST /api/v1/automation/runs/{id}/steps/{stepId}/retry requires live session/PersonalSpace, exact owner, separate definition/read/run.read/retry_step/run.start and current Project/Task authority, operator LocalTaskExecutionEnabled, nonzero UUID, canonical If-Match. SQL Serializable follows existing owner/run locking. Only Failed/PartiallySucceeded, EffectUncertain=false, enabled Active definition, selected Failed step with no TaskId, no uncertain effect and cumulative Attempts1..3 is eligible. Earlier steps must be Succeeded; later steps must be never-attempted Skipped with no effect. Busy same-definition runs block.

Pinned immutable definition version is parsed/rechecked; original CommandKey and Attempts are retained. Selected failure and untouched later steps requeue atomically with audit and receipt, refreshed requesting session authority and bounded worker deadline. Original CreatedAt is preserved: runs beyond existing10-minute queue-age budget fail RetryWindowExpired409 before queue. No successful Task replay, unknown-effect reconciliation, attempt-budget replenishment or arbitrary adapter.

Receipt binds owner/run/step/ETag. Same-key replay rechecks current authority and returns historical identity202; UI fetches current run, never assumes Queued. Existing one logical failure notice per run is retained; no new duplicate alert is emitted by RetryStep.

React run detail explains affected failed and never-run downstream steps, requires explicit confirmation, preserves immutable run/step/ETag/key after network/5xx/in-progress/refresh failure, blocks competing commands and refresh/close while uncertain. Only HTTP202 with matching run identity is an ACK. Permission capability mirrors retry/read/start and local execution switch.

Migration0050 only registers exact local retry permission as Resolved; enables no module, grants or execution switch. 0047/48/49 remain immutable; no schema/binary/data reset. Remove execution toggle to stop local work; no deployment performed. Pending human apply/replay/upgrade validation.

## Verification and findings

PASS API Release build0 warning/error; PASS TypeScript/Vite build with pre-existing bundle warning; PASS scoped diffcheck; independent source review found queue-age mismatch and confirmed failclosed repair plus exact202/same-step ACK. Capability conjunction reviewed before publication. No new tests/fixtures created or existing valid tests weakened.

NOT_RUN unit/SQL/API/browser/worker/migration runtime suites under code-only directive. IMPLEMENTED_UNVERIFIED, not VERIFIED_COMPLETE. Human QA: known failure successful retry; foreignowner; staleETag; lostACK/replay afterlaterstate; exhausted4attempts; unknown effect rejection; permission/target/session revocation; busydefinition; >10minute run; immutable version; unchanged successes; no duplicate finalnotice; SQL rollback/concurrency; migration replay. Unknown non-idempotent outcomes remain denied rather than blindly repeated.

Worker source review: ownerUPDLOCK/ProjectHOLDLOCK plus existing Task domain second connection do not prove deterministic lock cycle (current reads compatible). Lock escalation/range/concurrent third transaction is SOURCE_REVIEW_RISK, runtime NOT_RUN. No speculative lock-removal refactor. Reviewed CI anchor: Agent baseline/frontend-unit/Linux success; backend-unit/SQL/browser/Windows jobs failure. This slice does not claim CI restoration or human runtime verification.

Remaining FX34: schedule.update/durable scheduler, verified simulation/dry_run, definition.purge/support.read, engine concurrency/recovery full QA. FIX01/02/03 repairs already exist, not reimplemented; runtime replay/pagination remain unverified. Full733-action/202-screen semantic ledger still incomplete; no completion percentage inferred from markers. FX30/35 live approval remains inactive. Next code slice: schedule contract and durable occurrence persistence, preserving platform jobs independence.
