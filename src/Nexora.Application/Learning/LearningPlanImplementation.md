# Learning Plan worker brief

Start: 2026-10-21T20:49:46.9320168+07:00. Accountable author: Learning vertical worker.
Source: impl/m01-s00-scaffold, 33e68006efdc75ed86551679aaf7fa6ccd8b3a5a.
Fork: C:/Users/hakodev/.codex/worktrees/8278/Nexora. No push, no merge.
Authorization: user explicitly approved full local implementation and synthetic QA;
this task supersedes the run-specific code-only restriction. Do not edit AGENTS.
D01-D09=A retained: no secrets implementation, advanced finance/task extensions,
real providers, sensitive non-SELF projections, merging or production activation.

Outcome: an installable, initially disabled Learning Plan flow referencing Goal,
Task and Course only. New API/Application/Infrastructure/web/tests files only;
migration 0086 reserved for Learning. Main agent owns registration/policy/manifest,
App.tsx and LearningScreen.tsx. No existing shared interfaces changed.

Authority: DEC-20260909-014, DEC-20261019-PR4 D06; P07-LRN-001;
FX-40-BR-001/002/006, FX-40-AC-002, FX40-S05, LRN3.
Goals: NXG-FX40-G01/G02; system owner isolation/current authorization/consistency.
Exact actions: learning.plan.read/create/update/link/unlink/reorder/complete.
Archive/unarchive can be prepared under existing lifecycle rules; Trash/restore/
purge remain gated until precise lifecycle/retention contracts and review suffice.
No invented progress: source participants may report NoProgressMetric, never fake
100%; plan completion is explicit and never mutates sources.

Read paths: AGENTS.md; .agents/skills/nexora-engineering/SKILL.md;
.ai/profiles/nexora-implementation-agent.md; .ai/roles/technical-lead/README.md;
.ai/roles/technical-lead/rules/core-rules.md; .ai/routing.json; .ai/verification.md;
routing architecture/backend/frontend/security/database/verification/owner-isolation
paths; docs/README.md; docs/delivery/README.md; docs/delivery/01-current-scope.md;
docs/goals/README.md, 01-system-goals.md, 05-task-and-evidence-template.md,
modules/40-learning-and-work-log.md; current D01-D09 decision register and index;
docs/implementation/pr4-decision-contracts-20261019/04-learning-plan.md;
docs/features/40-learning-and-work-log.md; docs/action-catalog/modules/40-learning.md;
docs/ux-ui/modules/40-learning.md; docs/design-database/10-assets-career-learning.md.

Plan: typed validated commands; additive owner-FK schema; module-owned source
participants sharing SQL Serializable transaction; current action/owner/registry
proof; strict ETag, audit and opaque receipt in one transaction; encrypted bound
seek cursor; accessible React flow with frozen uncertain intent; meaningful negative
contract/unit and SQL scenarios. Source read permission precedes private source SQL.
Write lock order: PersonalSpace owner row, current policy, receipt, plan, typed
sources in stable kind/id order, items, registry/audit/receipt; verify SQL deadlocks.
Rollback: disable registration/options/writes, preserve rows/receipts/audit. No down
migration, no source data modification or relabeling/backfill.

Required gates: source contract/API/schema integration review, independent owner/
security/DB review; real SQL migration/concurrency/rollback evidence; current-head
CI; browser/accessibility/runtime QA. These remain pending until actually run.


## Concrete integration handoff (main agent owns wiring)

Keep PR4 Draft on impl/m01-s00-scaffold; cherry-pick this worker's commit locally.
No push/merge/provider execution occurred. No existing shared interface or central
registration file was edited. Migration0087 remains Toolbox's reservation.

1. Add `20261021_0086_learning_plans.sql` to M01MigrationManifest in numeric order
   after0085/before0087, update exact expected filename/checksum assertions in the
   existing independent manifest tests. The migration creates no business fixture,
   grant, module enablement or destructive down path. It inserts missing Plan
   permissions as Gated and never upgrades existing permission states. Source types
   Plan/Goal/Task have exact version checks; unexpected existing Plan schemas abort.
   Transactional table contract markers allow rerun after schema commit before the
   runner writes its journal. Test actual clean upgrade and crash-gap replay in SQL.
2. In Program.cs use existing Learning imports and add:
   ```csharp
   builder.Services.AddSingleton<ILearningPlanService>(s => new SqlLearningPlanService(
       s.GetRequiredService<SqlConnectionFactory>(), idempotencySecret!,
       new LearningPlanReadiness(false)));
   // Next to existing Learning endpoint registration:
   app.MapLearningPlanEndpoints();
   ```
   Default false is intentional. Source participant/owner/DB/security review and
   real SQL acceptance must precede activation. Independent reviewer performed
   static review only; this worker does not authorize switching the flag to true.
   SQL tests instantiate true only inside the disposable synthetic test context.
3. Main-owned ActionGrantPolicy must explicitly list these SELF Admin-grantable
   keys after reviewed integration: learning.plan.read, learning.plan.create,
   learning.plan.update, learning.plan.link, learning.plan.unlink,
   learning.plan.reorder, learning.plan.complete. Do not add lifecycle keys yet or
   bypass explicit Allow/Deny/current-session/module policy. A separately reviewed
   activation migration may resolve exactly those permissions after the gates;
   never modify an already journaled0086 or silently enable modules/user grants.
4. App.tsx imports `LearningPlanScreen` from `./LearningPlanScreen`. Add the native
   `/learning/plans` surface using the same routing conventions as WorkLogScreen.
   Render `<LearningPlanScreen ownerContext={...} />` where the string combines
   authenticated PersonalSpaceId and current session/authority generation; changing
   owner/context must remount and clear all private in-memory state. This prop is
   an in-memory invalidation key, never sent as authority to the server. Add the
   Plans entry from LearningScreen.tsx. Do not render it as a working active feature
   while the readiness gate is false. Detail selection currently stays inside this
   screen; direct `/learning/plans/:id` routing is a pending integration extension.
5. Stop/rebuild/restart the local API before exercising new schema/routes; worker
   did not interfere with main's running SQL/API/browser processes.

## Exact prepared API contract

Root `/api/v1/learning/plans`: GET list with status/cursor (fixed25), GET capabilities,
GET sources?kind=Goal|Task|Course&cursor, GET /{id}, POST create metadata,
PUT /{id} metadata, POST /{id}/items typed source + TargetOn,
DELETE /{id}/items/{itemId}, PUT /{id}/order {itemIds: complete current set},
POST /{id}/complete {confirm:true}. Unknown JSON fields are rejected by DTOs.
Every mutation uses UUID Idempotency-Key; existing Plan commands also use If-Match.
Strong quoted binary8 ETags, 428 missing/malformed,412 stale;422 validation,
409 duplicate/different intent/in-progress,403 current capability/readiness denial,
404 common source/resource unavailable,503 persistence/unconfirmed outcome.
All committed responses are `{id,committed:true,detailAvailable:false}` with201 for
create and200 otherwise. UI separately rechecks current read before a fresh GET;
no stale readable DTO is stored in receipts. Domain4xx received after a previously
uncertain attempt cannot erase the unresolved original key. Modal-local retry,
revocation clears editor/private projections, valid ACK remains final even when
post-write capability refresh fails. No browser persistent storage is used.

## Reconciled technical choices and gates

Goal/Task at the base revision have no source registry lifecycle participation.
The new Goal/Task-owned participants authorize and validate current own source
payload before same-TX lazy registry enrollment on link. Existing registry IDs are
never relabeled or overwritten. On every projection the source payload and current
read capability are authoritative, regardless of stale reference registry state.
Archived/deleted/Trash/unreadable sources mask title/state/percent; PlanItem remains.
Goal native active TrashItem is excluded even if payload Status has not changed.
Course uses its existing courses-v1 registry. No consumer reads another module's
private SQL outside this explicit participant contract; no source mutation occurs.
This is a technical candidate under D06, requiring source-owner/DB/security review
and real SQL lock-order/concurrency acceptance before use.

All source progress is conservatively NoProgressMetric/percent=null in this subset;
Unavailable has no title/state/percent. No inferred Task percentage, hours-based
Course completion, aggregate percent or automatic Plan completion. Known/unavailable
counts and partially-known/no-items labels carry no false completion claim.
Source progress-provider enrichment, title/date list filters, direct detail deep
links, Archive/Unarchive preview+handlers, Trash/Restore/Purge/retention and global
Trash integration remain unimplemented/pending contracts and review. No silent R1
scope exclusion; do not label the entire Plan/FX40 module accepted or complete.

## Trace to authored evidence

| Goal | Source | Exact actions | Code | Evidence / state |
|---|---|---|---|---|
| NXG-FX40-G01, NXG-SYS-02/03/06 | D06, P07-LRN-001, FX-40-BR-001/002 | learning.plan.read/link/complete | typed sources, conservative projection, explicit Complete | LearningPlanContractTests; SqlLearningPlanTests source-mask/explicit-state cases; SQL NOT RUN |
| NXG-FX40-G02, NXG-SYS-02/06/09 | FX-40-BR-002, FX-40-AC-002, FX40-S05 | learning.plan.create/update/link/unlink/reorder | owner FKs, source participants, root revisions/receipt/audit transaction | SqlLearningPlanTests owner/replay/order/source-preservation cases; SQL NOT RUN |
| NXG-SYS-13/14 | FX40-S05, D09 common uncertainty/privacy | learning.plan.create/update/read | frozen HTTP intent, modal retry, revoke clearing, final ACK | LearningPlanApi.test.ts + LearningPlanScreen.test.tsx:6 PASS; real browser NOT RUN |

## Execution facts

Environment Windows, SDK10.0.302/runtime10.0.10, Node/npm existing local tools.
Source base33e68006efdc75ed86551679aaf7fa6ccd8b3a5a. Only the listed scope changed.
- Initial `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore` FAILED
  NETSDK1004 because the new fork had no assets; no source compilation occurred.
- Locked restore unit and integration projects with `--ignore-failed-sources
  --locked-mode`: PASS, no package/lockfile changes.
- `npm ci --offline --ignore-scripts --no-audit --no-fund`: FAILED ENOTCACHED
  zwitch2.0.4 and Windows cleanup EPERM. Did not fetch network dependencies.
  Copied existing installed main-checkout node_modules into this fork with
  robocopy /E; source dependency tree was only read, not changed. Robocopy exit1
  means files copied, not an npm clean-install success.
- `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore
  --filter FullyQualifiedName~LearningPlanContractTests --logger
  "console;verbosity=minimal"`: PASS25, failed0, skipped0 (final backend state).
- `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj
  --no-restore`: PASS,0 warnings/errors; includes API/Application/Infrastructure.
- `npm run test:unit -- LearningPlanApi.test.ts LearningPlanScreen.test.tsx`:
  PASS2 files/6 cases, no skips. Includes modal original-intent retry, revoke after
  uncertainty key retention/privacy and ACK→readfailure→new definitive422.
- `npm run build`: PASS TypeScript+Vite. Existing >500kB chunk advisory remains.
  Because App wiring is intentionally unchanged, new screen is typechecked but
  is not included in the served application bundle until main integrates it.
- Real SQL tests/migration execution NOT RUN here: NEXORA_TEST_SQL_CONNECTION absent.
  Compile succeeded. Main can run:
  `dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj
  --filter FullyQualifiedName~SqlLearningPlanTests --logger "console;verbosity=normal"`.
  Two real-SQL tests cover typed three-source flow, foreign owner/ref/FK denial,
  source deletion/revocation masking, stale/invalid reorder, same-key replay/race,
  source-preserving unlink/target date, explicit completion, pagination25+ and
  owner/filter cursor binding/session revocation. Their assertions are unweakened.
- Browser E2E/accessibility/mobile, actual SQL migration/rerun/rollback/deadlock and
  final integrated CI NOT RUN by this worker; main-owned release gates remain.

Independent reviewer: Codex task01a15ec5-7960-7570-a7ee-096383384d7d, read-only.
Initial four findings plus follow-up postACK uncertainty finding were remediated.
Final focused static re-review: no remaining findings; NOT runtime acceptance or
permission to enable. Reviewer performed no writes or test execution. Subsequent
small UI change displays authoritative form error inside the modal and adds an
assertion; author ran the focused tests/build again as recorded at handoff.

## Exact changed paths

- database/migrations/20261021_0086_learning_plans.sql
- docs/design-database/10-assets-career-learning.md
- docs/ux-ui/modules/40-learning.md
- src/Nexora.Api/Features/Learning/LearningPlanEndpoints.cs
- src/Nexora.Application/Learning/LearningPlanContracts.cs
- src/Nexora.Application/Learning/LearningPlanImplementation.md
- src/Nexora.Infrastructure/Goals/SqlLearningPlanGoalSource.cs
- src/Nexora.Infrastructure/Learning/LearningPlanCursor.cs
- src/Nexora.Infrastructure/Learning/SqlLearningPlanCourseSource.cs
- src/Nexora.Infrastructure/Learning/SqlLearningPlanService.cs
- src/Nexora.Infrastructure/Learning/SqlLearningPlanSource.cs
- src/Nexora.Infrastructure/Productivity/SqlLearningPlanTaskSource.cs
- tests/Nexora.IntegrationTests/SqlLearningPlanTests.cs
- tests/Nexora.UnitTests/LearningPlanContractTests.cs
- web/Nexora.Web/src/LearningPlanApi.test.ts
- web/Nexora.Web/src/LearningPlanApi.ts
- web/Nexora.Web/src/LearningPlanScreen.test.tsx
- web/Nexora.Web/src/LearningPlanScreen.tsx

## Resume and packaging record

Resumed: 2026-10-22T23:34:23.7180394+07:00. All18 prepared paths recovered,
HEAD remains33e68006efdc75ed86551679aaf7fa6ccd8b3a5a. The Oct21 final commit
command did not execute: automatic approval review hit an external usage limit.
No staged/committed result was claimed. User explicitly resumed the same authorized
scope Oct22; no shared central file changes are included.

Resumed verification: identical executable state on Oct22 passed25 backend unit
cases (0 skipped),6 frontend cases (0 skipped), API/SQL-test compilation with
0 warnings/errors, and TypeScript+Vite build (existing chunk-size advisory only).
SQL test environment still absent; no SQL execution claimed. Staged diff check
passed across the exact18 files.
End (before packaging commit): 2026-10-22T23:36:35.785967+07:00
