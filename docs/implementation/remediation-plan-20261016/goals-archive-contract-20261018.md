# FX16 Archive/Unarchive local continuation

Task owner: implementation agent / Technical Lead, same branch impl/m01-s00-scaffold
and HEAD1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, existing PR4; no merge.
Current user authorizes remaining requirement-backed local code; DEC014 applies,
code-only amendment retained. Same routing as target-management contract:
baseline, architecture/backend/frontend/security/database/owner-isolation/verification.
Independent review Pending, no tests/fixtures/migration/runtime execution.

## Sources, goal binding and bounded plan

Feature16 flow3/BR004 requires readonly Archive and Unarchive to previous status;
UX16-S03 sections14/15/16 specifies explicit D-ARCHIVE/D-UNARCHIVE and preservation
of source prior state; exact actions goals.goal.archive/unarchive. Goal progress
must not decide status (BR003). Bind NXG-P02-01, NXG-FX16-G02, SYS02/03/05/09/13/14
to those actions and common owner/lifecycle/concurrency/idempotency/dirty AC.

Add nullable Goal.PreArchiveStatus with enum check for Draft/Active/Completed/
Abandoned, no fabricated prior state/backfill.0079 adds absent-only action metadata
and manifest; no Allow grants, data generation or runtime execution. Archive can
transition only those four current states to Archived, retaining exact prior state.
Unarchive requires current Archived and an actual stored prior state; legacy rows
without prior evidence fail409, never guess Active. Clear PreArchiveStatus only
when restoring that state. No target/history deletion or source effects.

Existing POST /api/v1/goals/{goalId}/transition additionally accepts Archived or
Unarchive, preserving existing Active/Completed/Abandoned semantics and receipts.
Strict original Goal If-Match, nonempty UUID idempotency key for new commands,
CSRF/common no-store; current SELF/module/action and own nonTrash Goal proof.
Current action before receipt lookup; original opaque replay reconciled before
fresh revision/lifecycle checks. Same key/different intent409, stale412, missing/
foreign/Trash404, invalid422, wrong state/missing prior409, persistence503; existing
ModuleUnavailable409. Serializable Goal lock and parameterized update, atomic
metadata audit/opaque version2 receipt/final current action/read projection.
Archived detail remains readable under current goal.read+target.read; generic
Goal update/progress/target-management routes already deny Archived writes.

Native Goal detail offers explicit Archive confirmation on four eligible states
and Unarchive on Archived. Explain readonly/previous-state restoration; safe cancel
focus and existing dirty/uncertainty guard. Metadata edit button excludes Archived.
No automatic transition from progress or new history/provider claim.

## Acceptance/evidence and rollback

| Goals | Exact source/action | Implementation | Owner assertions / result |
| --- | --- | --- | --- |
| NXG-FX16-G02; SYS09 | BR003/004; UX16-S03 D-ARCHIVE/D-UNARCHIVE; goal.archive/unarchive | Prior-state column, closed transitions, readonly source guards | Each of4 prior states restored,100% no autoComplete, legacy prior missing denied: NOT RUN |
| SYS02/03/05/13 | Shared owner/revision/retry/source/UX AC | Own source/Trash/current action, same-TX receipt/audit/final proof, native confirmations | Wrong owner/action/Trash/terminal, stale/replay/revoke, safe cancellation: NOT RUN |

Compile/build only may detect implementation errors. Independent security/DB/
authorization review Pending; SQL/browser/functional acceptance NOT RUN. Rollback
disables new transition entries and retains Archived/prior-state/evidence rows;
never delete or mass-unarchive records. Existing readonly guards must remain.
Full lifecycle history, Trash/purge, Task targets, providers remain separate work.
