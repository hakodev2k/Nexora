# FX39 manual Company/Job evidence

Main authority `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; PR4 implementation base `0ce56b636441534382b6fbe3afa1fa5dc35189ea`. Exact requirement/goal/action/screen, DTO/API/data/security/merge/history/Trash/rollback bounds are in career-manual-contract.md. P07-COM001/P07-JOB001/002/004/005, NXG-P07-01/NXG-FX39-G01, partial FX39-BR001/002/004 and AC002; S01/S02/S03 only. Full FX39 and parent scope remain Partial.

Twelve exact SELF keys company.read/create/update/merge and job.read/create/update/transition/history/trash/restore/purge are installed. Typed SQL Company/JobApplication/ApplicationEvent, SqlCareerService, /api/v1/career, normal-session/CSRF endpoints, actual Company/Job views and additive migration0040 persist real metadata with transactional audit/receipts, private immutable events and owner-safe references. Contact encryption, Resume/version references, Calendar/Files/tags/sharing/support and retired Interview are excluded from this bounded implementation, not claimed complete.

| Executed check | Actual result |
| --- | --- |
| API Release build | Passed with zero warnings/errors |
| FunctionalTestOperator Release build | Passed with zero warnings/errors |
| Backend unit build/test | 12 passed, zero failed/skipped; out/evidence/career-unit.trx |
| Native Windows unit boundary runner | 30 passed, zero failed, four pre-existing platform/identity skips |
| Full real SQL/API regression | 54 passed, zero failed/skipped; out/evidence/career-regression.trx |
| Career cases in that final regression | Eight passed, zero failed/skipped; same final TRX |
| Earlier focused SQL | Six initial, then seven reviewed cases passed; final eight-case evidence is the full regression above |
| Frontend units | 71 passed, zero failed, one pre-existing DST-policy skip |
| TypeScript/Vite build | Passed; JS642.48kB, existing >500kB warning |
| Normal-login Career browser | Five passed, zero failed/skipped/flaky at1920/1366/768/390/320; sanitized career-browser-evidence.json |
| Retained synthetic sandbox migration/replay | 41 approved migrations applied normally and replayed; prior records retained |
| Independent review | course_review rereview closed backend/dynamic-replay and four UI findings; no remaining source blocker; reviewer ran no tests/builds |

SQL exercises fresh/replay/preceding-schema upgrade/checksum/readiness infrastructure; typed metadata and exact signed decimal28,8 salary, entered dates, inert URLs, strict unknown-field rejection, owner/FK isolation, unauthenticated401, foreign404 and denied403 boundaries, missing428/stale412, explicit stage and accepted-to-progress confirmation/reason, immutable stored source labels after Company merge, frozen Trash/restore/purge with receipt replay after root lifecycle changes, independent Company edits during Job Trash, Admin Allow/Deny/Unset/current read prerequisites and denied-label/query/preview redaction. Original Company-read requirements are stored for create/changed association and rechecked on receipt replay; unchanged associations remain independent. Merge binds exact Company/Job/reference revisions, rejects changed jobs/references/foreign targets/new expired previews/Trash jobs, retains source metadata/reference identity, and replays committed opaque results after expiry. Tests assert SQL atomic audit-failure rollback, same-key concurrency, immutable UPDATE trigger51042, frozen out-of-band payload conflict and retention-pinned purge refusal. Bounds1000/1MiB/2048 and session-renewal replay are source-reviewed, not individually runtime-asserted.

Browser uses real normal form login and current SuperAdmin policy/access preview/commit with restoration. It verifies dirty cancel/resume/discard, Company creation, explicit Job stage, source picker, exact salary display and actual owner/association SQL, full-metadata concurrency comparison/reapply, stage reason/reopen confirmation, previewed explicit KeepTargetMetadata merge, read-only merged source, preserved old/new labels, actual history paging/filter, Trash/restore/purge persistence,29Company/Job records through actual25-item controls, source picker pagination, Table/Kanban same cohort,20,000-character note wrapping and page overflow checks, and revoke clearing with recovery Reload. BrowserBack/SaveAndLeave, read-only-grant-only detail expansion and full pre-existing browser regression remain pending coverage, not passed claims.

Pre-final attempts: first API build failed because sandbox DLLs were locked; verified owned processes were stopped and final Release build passed0/0. First test compile had an await inside a non-async assertion lambda, corrected before real SQL execution. An initial frontend wiring helper was called from the wrong directory and did not run; after correct wiring an existing duplicate FX39 label caused TypeScript failure and was corrected. npm test was not a configured script; npm run test:unit ran successfully. No checks were weakened. The cross-column Company CHECK was moved to a table constraint and event rowversion added before the first schema application. Migration0040 is now applied in the retained database and must not be rewritten.

Published-source hashes: career-source-hashes.json. Raw reports, runtime state, synthetic credentials/captures/files and vite.functional.local.ts stay local and excluded. The latest full existing browser suite and all remaining active modules must still be completed before full parent-task acceptance. No provider/production/newPR/merge authority or claim.
