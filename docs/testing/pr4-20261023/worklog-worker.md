# Manual Work Log SQL coverage — 2026-10-23

Task: continue user-authorized functional testing of shipped code; synthetic disposable SQL/API fixtures are authorized by the user's current testing instruction. Starting source `fbafbaa482b094f26f067f1697d1f9d610e5367c`, branch `impl/m01-s00-scaffold`, existing PR4. No production change, provider, source linking, Plan or Certification behavior. Frozen `SqlReleaseCoverageTests.cs` remains unchanged by this task.

Rules loaded: root AGENTS, nexora-engineering skill, implementation profile, Technical Lead core rules, routing and verification; existing QA test-strategy/contract-testing/backend testing rules retained from this test session. Source: feature40, module goal40, UX40-S06 and current manual/filter-history/trash continuation contracts plus WorkLog API/application/SQL implementations. Bound goals: NXG-P07-01, NXG-FX40-G03, NXG-SYS02/03/05/08/09/11/13/14; P07-WRK-001, FX40-BR005/006, common action/owner/revision/receipt AC. AC004 linked-TimeEntry deduplication remains outside this manual slice.

## Added cases

New file: `tests/Nexora.IntegrationTests/SqlWorkLogCoverageTests.cs`, **9 SQL/HTTP cases**. Business setup and mutations use HTTP. Direct SQL is limited to disposable fixture Admin permission setup and read-only persistence/audit evidence.

| Case | Contract / action | Acceptance oracle |
| --- | --- | --- |
| Manual create/update receipt and immutable history | worklog-manual + filter-history contracts; create/update/read | Original/replay ACK has exactly id/committed/detailAvailable, no ETag/content. Changed intent conflicts. One Created and one Updated snapshot with exact sequence/content, no write ETag in history and no extra successful audit on replay. |
| Minute validation | FX40-BR005, manual contract | 0/1441 denied; exact1/1440 persist; list contains only accepted records. Recorded date is asserted as date-only on read. |
| Archive readonly / Unarchive | FX40-BR006; archive/unarchive | Explicit false confirmation denied; archive→readonly blocks edit; original replay succeeds without extra history; unarchive returns Active and expected snapshot chronology. |
| Trash/Restore Active | worklog-trash contract; trash/restore | Preview has no source mutation; committed Trash retains actual Active prior; same receipt replay adds no snapshot; exact batch restore returns Active and clears Trash prior. |
| Trash/Restore Archived | same | Restores actual Archived, retaining readonly edit protection and exact expected snapshot count. |
| Purge and replay after deletion | worklog-trash; purge | Preview reports private versions/zero references. Purge makes source/history404, deletes private rows and snapshots, retains Purged registry tombstone, and original opaque receipt still replays without extra audit. |
| Owner and revision | SYS02/03; read/history/update/trash-preview | Foreign source/history/edit404; stale edit/preview412, missing ETag428; exact current source and history/audit counts survive denied writes. |
| Current Admin permission | SYS02/05/08; update/read/trash | Unset update denied; Allow commits; Deny prevents original receipt replay; preview does not retain authority after trash grant revoked; source/history unchanged; read Deny blocks private history. |
| Literal category/date list/report | FX40-BR005; read/report.read | Percent title query is literal; exact category excludes extended category; inclusive date filters agree with gross totals; report declares NotDeduplicated; trashed source removed from Active gross totals. |

## Verification and review

- `git diff --check -- tests/Nexora.IntegrationTests/SqlWorkLogCoverageTests.cs docs/testing/pr4-20261023/worklog-worker.md`: passed.
- Independent source review requested from `/root/qa_review_oct23`; pending response.
- Compilation/real SQL execution: pending parent-run CI. Local integration restore already blocked in this session by NU1301/proxy HTTP403 for Microsoft.AspNetCore.Mvc.Testing metadata. This is an infrastructure limitation, not a test pass/fail. Locked dependencies remain unchanged.

## Limits

This is a bounded manual WorkLog suite, not complete FX40 coverage. Cursor pagination/tamper, preview expiry/reference changes and multiwriter lock schedules, legacy HistoryStarted baseline, injected rollback failures, and UI focus/dirty/uncertainty remain separate evidence gaps. No claim of TimeEntry/source-link deduplication, global Trash registration, learning plans, certifications, or complete Release1 acceptance.
