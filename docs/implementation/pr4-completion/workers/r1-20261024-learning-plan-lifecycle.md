# Learning Plan lifecycle worker — 2026-10-24

## Task brief and contract before implementation

Accountable author: assigned Learning Plan worker; independent reviews scheduled by coordinator.
Source: `impl/m01-s00-scaffold`, `14c76bcb6960e4ce53369dce388a8480b1c0299a` (confirmed locally).
Authorization: explicit current session scoped implementation, DEC-20260909-014 and
DEC-20261019-PR4 D01–D09=A. Shared workspace only; no commit/push, no agents,
no new tests/fixtures/runtime. Build/typecheck only detects implementation errors.
Scope: Plan contracts/endpoints/service/cursor, Plan API/screen, migration0089 and this report.
Root owns App/router/CourseScreen/LearningScreen/Program/policy/manifest.

Sources read: AGENTS; nexora-engineering skill; implementation profile; TL README/core;
.ai/routing.json and .ai/verification.md; routed architecture/backend/frontend/security/
database/owner-isolation files; docs README/delivery README/current-scope; goals README,
system/phase/module40/evidence template; DEC014 and D01–D09; D06 Plan contract;
FX40 feature/action/UX/physical design; phase07; FX08 feature/local Trash contract;
UX08 common lifecycle dialogs; existing0086 schema and Plan source participants.

Bindings: P07-LRN-001, FX-40-BR-002/006, FX40-S05; NXG-P07-01,
NXG-FX40-G01/G02, NXG-SYS-02/03/05/06/08/09/13/14.
Actions implemented here: `learning.plan.archive`, `learning.plan.unarchive`,
with preview authorized by the exact intended action and current plan.read.
FX-40-AC-002 source preservation is inherited; no new acceptance IDs invented.

Bounded plan: extend existing Serializable owner-lock/receipt/ETag write path;
retain PlanItem order, dates, references and source state; synchronize registry;
record safe from/to audit metadata. SQL already has PreArchiveState and paired
state constraint. Add only missing lifecycle permissions as Gated; never grant,
resolve or enable. Rollback keeps schema/data/audit/receipts and readiness false.

Concrete API: GET /{id}/lifecycle-preview?operation=archive|unarchive requires
current quoted If-Match. Returns planId, operation, ETag, fromState, toState,
retainedItems, expiresAt, opaque previewToken. Preview token is bound to actor,
owner, plan, action, revision, states and item count, expires after five minutes.
POST /{id}/archive or /unarchive takes {confirm:true,previewToken}, UUID key,
original quoted If-Match. Fresh commit revalidates all preview fields under lock.
428 malformed/missing revision;412 stale;422 bad operation/token/confirmation;
410 expired preview;409 lifecycle/prior-state conflict;403 unavailable authority;
404 unavailable root;503 SQL/unconfirmed. Exact committed replay precedes fresh
preview expiry/state validation, requires current original-write authority and
returns only opaque ACK. No second mutation/audit on replay.

Archive supports Planned/Active/Completed, saving that exact state; unarchive
requires Archived and a valid actual PreArchiveState, never defaults Planned.
Only the root changes lifecycle. All links are retained, source projections still
use current source participants. Preview contains count only, no source title.
React uses existing accessible modal, current server preview, frozen original
intent on uncertainty, and optional onOpenSource(ref) only after a fresh available
source projection; root owns route resolution and destination authorization.

## Exact Trash/restore gap

FX40-BR006 and action rows approve manual Trash in principle; the D06 document
still specifies lifecycle HTTP as candidates. Installed0086 has no TrashBatchId,
PreTrashState or Trash state; registry trash capability is false. Logical Plan
schema lists TrashBatchId but its Status domain excludes Trash. There is no exact
Plan cohort/member contract stating whether retained PlanItems are members or
references, how archive prior state is retained across Trash/restore, or a typed
Plan preview/restore request binding cohort revision and registry lifecycle.
WorkLog's precise contract applies only to WorkLog and cannot silently authorize
Plan behavior. Need aligned Plan-specific API/state/schema/cohort/registry contract
and source-owner/DB/security review before implementation. No guessed Trash,
restore or purge handlers; this is a remaining R1 capability, not removed scope.

## Verification and handoff

Scoped archive/unarchive implementation is code-present; exact results follow. All functional tests,
SQL execution, migration rehearsal, browser QA and independent review are Not run/
Pending under the current code-only instruction. Readiness remains false.


### Changed files (worker only)

- src/Nexora.Application/Learning/LearningPlanContracts.cs
- src/Nexora.Api/Features/Learning/LearningPlanEndpoints.cs
- src/Nexora.Infrastructure/Learning/SqlLearningPlanService.cs
- src/Nexora.Infrastructure/Learning/LearningPlanCursor.cs
- web/Nexora.Web/src/LearningPlanApi.ts
- web/Nexora.Web/src/LearningPlanScreen.tsx
- database/migrations/20261024_0089_learning_plan_lifecycle.sql
- docs/implementation/pr4-completion/workers/r1-20261024-learning-plan-lifecycle.md

No other worker/root file was edited by this worker. Shared-tree status includes
other workers' ongoing changes; they were neither reverted nor packaged here.
No commit, push, migration execution, permission grant or module activation.

### Exact root handoff

Screen props remain compatible with existing ownerContext/planId/onSelectPlan/
onAuthLost. New optional prop:

```ts
onOpenSource?: (source: { kind: 'Goal' | 'Task' | 'Course'; id: string }) => void;
```

This is the exported LearningPlanRef shape from LearningPlanApi.ts. The screen
renders a button only when a callback exists and the item projection has a title
and is not Unavailable; disabled while locked (busy/pending/loading). Clicking
clears old detail/choices and rereads the Plan/current source projection. Only a
still-present available item reaches the callback. No title is placed in a route,
no href/destination is constructed here. Root maps Course to native Course detail
and Goal/Task to existing resource detail; destination must resolve authority again.
Root may now wire this prop in App.tsx; worker edits are complete.

API exports added:

```ts
type LearningPlanLifecycleOperation = 'archive' | 'unarchive';
type LearningPlanLifecyclePreview = {
  planId: string; operation: LearningPlanLifecycleOperation; etag: string;
  fromState: string; toState: string; retainedItems: number;
  expiresAt: string; previewToken: string;
};
learningPlanLifecyclePreview(id: string, operation: LearningPlanLifecycleOperation,
  etag: string, signal?: AbortSignal): Promise<LearningPlanLifecyclePreview>;
```

ILearningPlanService adds:

```cs
IdentityOperationResult<LearningPlanLifecyclePreview> PreviewLifecycle(
    IdentityPrincipal actor, Guid id, string? etag, string operation);
IdentityOperationResult<object?> Archive(IdentityPrincipal actor, Guid id,
    string? etag, LearningPlanLifecycleConfirmation body, string key, string? trace);
IdentityOperationResult<object?> Unarchive(IdentityPrincipal actor, Guid id,
    string? etag, LearningPlanLifecycleConfirmation body, string key, string? trace);
// strict JSON body: LearningPlanLifecycleConfirmation(bool Confirm, string PreviewToken)
```

New endpoints are registered by the existing MapLearningPlanEndpoints call;
no additional Program registration is required. GET preview carries If-Match;
POST archive/unarchive carries original If-Match + UUID Idempotency-Key and
{confirm:true,previewToken}. Response remains opaque PlanCommitted ACK, then
separate current read. Existing map group CSRF applies to the new POSTs.
No new standalone preview permission key: exact archive/unarchive action plus
plan.read authorize it. Root owns policy allowlist/manifest; do not activate or
grant these actions here. SqlLearningPlanService.Actions includes the two keys.

Root must register `20261024_0089_learning_plan_lifecycle.sql` in its manifest,
after0088/before0090. SHA256:
`260C6541AC5BF117A5653F11AF59D651ADB7F8DF708A38DEF78DF4C41A72B68E`.
Migration checks0086 marker, PreArchiveState, trusted archive constraint, valid
prior state and exact Plan registry; inserts only absent permission metadata as
Gated and transactionally records lifecycle marker. No update to existing grants,
permission state, ResourceType capabilities, module enablement or source payloads.
No destructive down migration. Old/new code rollout must keep Plan readiness
false; rollback preserves Plan/Items, receipt and audit state. No DDL table rewrite
or backfill; real lock/replay/rollback evidence still requires owner verification.

### Source checks (author inspection, not independent/runtime verification)

- OriginalAuthority still binds OwnerId + exact registry module/type/version and
  lifecycle. Archived registry/read state is coherent after transition.
- Write still takes PersonalSpace OwnerId/UserId lock, checks current intended
  action, claims digest-bound receipt, then validates fresh revision/state.
- Completed receipt proof executes before fresh preview decode/expiry/state, so
  exact retry can ACK after preview expiration or later lifecycle changes while
  current original action/read/root authority is still mandatory.
- Archive preserves actual Planned/Active/Completed in PreArchiveState; unarchive
  accepts only actual saved state, clears it, and never invents a replacement.
- Payload and registry availability/revision, one safe from/to audit row and one
  receipt commit together. No source participant/source write is invoked by these
  lifecycle operations; all PlanItems/order/TargetOn remain intact.
- Preview uses a separate purpose-derived AES-GCM key; actor/owner/plan/action are
  authenticated scope; ETag/from/to/count/expiry are authenticated payload. Root
  rowversion changes on every existing item mutation. Forged/cross-context/stale
  previews cannot authorize a fresh commit. Tokens are memory-only in the UI.
- Readiness default in contracts remains false; existing Program registration
  explicitly uses new LearningPlanReadiness(false) (read-only source check).
- Source callbacks refresh authority and carry only typed ID; unavailable source
  private title is neither rendered as a link nor passed to the shell.

### Executed checks

Source revision: base14c76bcb6960e4ce53369dce388a8480b1c0299a plus the scoped
uncommitted diff above, in a concurrently edited shared tree. Evidence applies
to this worker's final executable edits; no current-head CI claim.

1. `dotnet build src/Nexora.Api/Nexora.Api.csproj --no-restore`: exit0,
   Build succeeded,0 warnings,0 errors (Application/Infrastructure/API included).
2. `npm run build --prefix web/Nexora.Web`: exit0, `tsc -b && vite build`
   succeeded;373 modules transformed. Vite advisory: some minified chunks exceed
   500kB. No dependency update/install performed; npm update notice is informational.
3. `git -c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora diff --check --`
   followed by the six modified executable file paths above: exit0, no output.
   Command-local safe.directory was used after Git's ownership warning; no global
   Git configuration changed. New migration/report reviewed directly.
4. `Get-FileHash database/migrations/20261024_0089_learning_plan_lifecycle.sql -Algorithm SHA256`
   produced the exact checksum above. Branch/HEAD read-only checks matched source.

No functional tests, new tests, test fixtures, mocks, SQL/migration execution,
API launch/runtime, browser/keyboard/accessibility QA, provider calls or independent
agents were performed. Independent security/source-owner/DB review is Pending and
scheduled by root; author source checks are not that review. Readiness remains
false until existing review/acceptance gates. No FX40/R1 completion claimed.

### Trace and remaining evidence

| Goals | Source / action | Code-present result | Required evidence still pending |
|---|---|---|---|
| NXG-FX40-G02, NXG-SYS-02/03/05/09 | P07-LRN-001, FX40-BR006; learning.plan.archive/unarchive | Exact prior state, strong conditional preview/commit, owner/action/registry checks | All three archive starting states, cycles, stale/forged preview, revoke races and SQL transaction/rollback proof |
| NXG-FX40-G01/G02, NXG-SYS-06/08 | FX40-BR002, source preservation from AC002; archive/unarchive/read | One opaque receipt/audit, retained refs, source-safe callback | Lost-response replay after expiry/later transitions; revoked source masked; no source side effects |
| NXG-SYS-13/14, NXG-P07-01 | FX40-S05, D09 | Accessible common preview dialog, frozen retry and locked typed source open | Owner browser/keyboard/mobile QA, root routing integration, independent review and final-source CI |

All runtime evidence is Not run, not Passed. Trash/restore/purge exact gaps are
recorded above; they remain committed R1 work for a subsequent aligned contract.
