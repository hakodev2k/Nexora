# FX40 personal Course evidence

Authority and bounded API/data/security/UX/rollback contract: `learning-course-contract.md`. Main source revision remains `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; preceding PR4 head is `c5f37acca75b02005b2c63b71017f5fb14285c4e`. Source fingerprints are `course-source-hashes.json`.

Exactly twelve installed SELF actions `learning.course.read/create/update/progress/milestone/complete/abandon/archive/unarchive/trash/restore/purge` bind FX40-S03, P07-CRS-001, FX-40-BR-001/006, FX-40-AC-001 and the source-retention portion of AC-002, NXG-FX40-G01/G02 and system owner/permission/atomicity/concurrency goals. `/api/v1/learning/courses` uses `ICourseService` / `SqlCourseService`, typed Course and O-profile CourseMilestone, private completion history, Resource root registration and actual one-root Trash cohort. Every child write advances the Course ETag; child edits also require their own version. Full Course fields, full AC-002 Task/Goal journeys and the whole FX40 module are not accepted by this subset.

Executed checks:

| Check | Actual result |
| --- | --- |
| Standalone API Release build | 0 warnings, 0 errors |
| FunctionalTestOperator Release build | 0 warnings, 0 errors |
| Full real SQL/API regression | 36 passed, 0 failed, 0 skipped; `out/evidence/full-sql-courses.trx` |
| Final strengthened Course cases | 5 passed, 0 failed, 0 skipped; `out/evidence/courses-reviewed-final.trx` |
| Backend units | 12 passed, 0 failed, 0 skipped; `out/evidence/unit-courses.trx` |
| Native boundary runner | 30 passed, 0 failed, 4 existing platform skips |
| Final frontend units | 71 passed, 0 failed, 1 existing DST-policy skip |
| Final TypeScript/Vite build | Passed; bundle-size warning remains, JS 579.76 kB |
| Final normal-login Course browser workflow | 5 passed, 0 failed, 0 skipped; sanitized `course-browser-evidence.json` |
| Existing Skills workflow after Learning navigation change | 5 passed, 0 failed, 0 skipped; all five viewports through normal login |
| SQL migration/readiness | Fresh application, replay, previous-schema upgrade, bootstrap and checksum checks through the real fixture |
| Existing browser database upgrade | 38 approved migrations applied normally; no reseed or removal of prior records |

The full 36-case run preceded test-only strengthening for independent child ETag, Abandon and owned completion-history rollback/purge; all five strengthened Course cases reran against the unchanged compiled product binaries. Test execution with `BuildProjectReferences=false` rebuilt the tests only. Standalone product builds above were completed before the running browser API locked the assemblies.

SQL evidence covers exact eight-place fraction storage and validation, explicit start/completion, 100% without auto-completion, Completed/Abandoned progress without reopening, actual dates and preserved history, durable mode locking after undo/delete, empty done/total, aggregate and child revision conflicts, owner/parent separation, 31-row Course and milestone keyset traversal and literal filtering, exact Admin Allow/Deny/Unset composition, read prerequisites and current dependency authority on deleted-child receipt replay, disabled module denial, same-owner FKs and NULL-safe lifecycle/date/marker checks, corruption fingerprint denial, retained-reference pins, concurrent idempotency, failed-audit rollback of Course/children/history/registry/cohort and successful retry/replay with one purge audit. The linked-source retention fixture uses a second Course; it does not claim a real Task/Goal integration test.

Browser evidence at 1920, 1366, 768, 390 and 320 widths uses normal form login and actual API/SQL writes. It covers explicit progress-mode choice, dirty Cancel and browser Back, metadata and progress conflict comparison/reapply, exact percentage input, 26 API-created records and visible pagination, explicit completion, archive/Trash restore preserving Completed, owned milestone create/complete/undo/reorder, durable mode lock, frozen root-cohort restoration and purge of owned children, revoked-grant clearing and no horizontal overflow. Browser tab closing, internal tab dirty-guard invocation, milestone child conflict UI and milestone pagination UI were not exercised by this new workflow; their implemented handlers are source-reviewed, and SQL child paging/conflicts were executed.

Independent reviewer `/root/course_review` reviewed contract, authorization, migration, lifecycle, private receipt dependency, UI and test sources without editing or executing tests. Its completion-date constraint and current-conflict/removal-dialog findings were corrected and rereviewed; no blocking source findings remain for this slice. Runtime results above belong to the implementation agent.

Installation is named Skills and Courses only; prior system/default policy and user grants remain intact. Other Learning actions, files/Finance links/reminders/search/provider execution and Course type/date filters remain explicit follow-up work. Sensitive projections and paused modules remain closed. No whole-module, full-scope, provider or production readiness claim. Raw reports and generated credentials/session/runtime state remain private ignored sandbox artifacts. Rollback retains additive schema/data and uses the preceding binary plus normal module disable.
