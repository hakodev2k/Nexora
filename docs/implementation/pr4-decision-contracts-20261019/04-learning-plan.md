# D06=A — FX40 Learning Plan contracts: Goal, Task and Course references only

**Confirmed choice:** DEC-20261019-PR4 D06=A. Normative source: `docs/features/40-learning-and-work-log.md` flow 4 and FX40-BR-002 (Goals/Tasks/Courses; unavailable source not fake Complete; unlink never deletes source), `docs/action-catalog/modules/40-learning.md` (`learning.plan.*`), UX FX40-S05, DB `docs/design-database/10-assets-career-learning.md` `learning.Plan` and `learning.PlanItem`, `docs/implementation/remediation-plan-20261016/p1-work-packages.md` LRN3; goal `NXG-FX40-G01..G03` and cross-module owner/source boundaries. Old action text “skill/course” and PlanItem text “Course/skill/task” are **reconciled by this owner decision**: PlanItem allowed resource types = **Goal, Task, Course**; Skill objects belong in Skills/Evidence and are not plan targets.

## State and dependency

No registered Learning Plan endpoint is confirmed at PR review HEAD; this is a **proposed installable vertical contract**, not shipped code. Existing Skill/Course/WorkLog functionality and current resource IDs remain unchanged. All Plan actions stay inactive until contract, migrations and required independent review/verification are completed. Must use current server-resolved `OwnerId` and module/action gates, never request OwnerId from client. Planned TargetOn is just date-only intent and **does not reschedule** source Task/Course/Goal.

## Typed source participation

Typed union `PlanResourceRef` = `{kind:"Goal"|"Task"|"Course", id: Guid}`, no arbitrary `ResourceId` without independently checked registry type; input rejects kind `Skill` and unknown versions/properties (`422` or existing common validation code). Provider map owned by Goal/Task/Course modules, with same transaction participant for (OwnerId, module/action + source state + read projection, protected stable summary, eligible lifetime). No SQL join that reads source titles without their source permission. A source's unavailable or deleted state is not equivalent to zero percent and must not be automatically “completed”.

**Progress projection:** optional per-item source progress `percent(0..100)` only when a source module has an approved exact progress definition and current read permission; otherwise `status=Unavailable|Unknown|NoProgressMetric`, `percent=null`. Do not derive Task progress from dates or infer Course completion from hours. `Plan` completion is explicit `learning.plan.complete`; aggregated display never auto-completes the plan. If some sources are unavailable, avoid computing a falsely complete percentage; show `Known N / Unavailable M` with bounded numerator/denominator and a partially-known label. Empty Plan = “No items”, not 100%. Any aggregate algorithm beyond these invariants must be reconciled to current UX/requirement rules in review before acceptance.

## Conditional HTTP API (proposed paths, not existing)

| Intent / action | HTTP candidate + exact input | Result / error/guards |
|---|---|---|
| `learning.plan.read` | `GET /api/v1/learning/plans?status=&cursor=&limit=25` and `GET .../plans/{id}` | Owner-scoped typed summaries with source-proof redaction, current ETag; bound seek cursor (OwnerId, filters, action/module context), never title/count leak |
| `learning.plan.create` | `POST /api/v1/learning/plans`; `{title, startsOn?,endsOn?,notes?}`, `Idempotency-Key` | One Plan registry+row+audit+receipt transaction; source effects none; readable DTO only if current plan.read, else opaque ACK |
| `learning.plan.update` | `PUT .../plans/{id}`; exact metadata diff, `If-Match` + key | Reject protected fields/unknown types/state, stale 412, preserve draft |
| `learning.plan.link` | `POST .../plans/{id}/items`; `{source:{kind,id},targetOn?}` + Plan ETag + key | Both Plan owner/action and **source current read** and eligible state checked in same TX; duplicate typed ref conflict or exact same-key ACK |
| `learning.plan.unlink` | `DELETE .../plans/{id}/items/{itemId}`, ETag + key | Removes only PlanItem, not source Task/Goal/Course, external reminders or history. Owned PlanItem binding checked before mutation |
| `learning.plan.reorder` | `PUT .../plans/{id}/order`, complete ordered ItemIds, Plan ETag + key | Exact current active item set, distinct IDs, all-or-nothing transaction, position unique, concurrent stale 412 |
| `learning.plan.complete` | `POST .../plans/{id}/complete`, ETag + key | Explicit confirmation and source/unavailable status indicated; no background inferred completion; audit current prior state |
| `learning.plan.archive/unarchive/trash/restore/purge` | Typed existing lifecycle conventions with impact preview, expected prior state/Trash batch/revision | Archive read-only; restore exact prior state; purge only if current approved retention/reference policy and independent review. No cascading deletion of linked sources |

For all mutations, completed original key/body proof precedes *fresh-state* validation but never bypasses current actor/action and original resource authority; a replay returns exact original committed outcome as readable projection only with current read proof, else safe opaque ACK. Bounded lock timeout and uncertain UI keep original key/intent, no duplicate write. The HTTP route names, request fields and statuses are **candidates** until contract/code integration review; source action keys remain exact.

## Proposed database design and required schema reconciliation

Existing logical `learning.Plan` and `learning.PlanItem` are not installed just because `docs/design-database` describes them. Add additive migration only after confirming current real SQL and resource registry:
- `learning.Plan`: Id + OwnerId composite Resource identity, CreatedAt/UpdatedAt/actor, RowVersion quoted ETag, Title (1..200 normalization), optional StartsOn/EndsOn DateOnly with start ≤ end, Status `Planned/Active/Completed/Archived` + precise Trash metadata and PreArchiveState, notes. Same-owner registry/audit.
- `learning.PlanItem`: Id, OwnerId, PlanId same-owner FK, `ResourceKind` required constrained enumeration {Goal,Task,Course}, `ResourceId` same-owner platform Resource FK, integer Position, optional TargetOn (date), rowversion; `UQ(OwnerId,PlanId,Position)` and **`UQ(OwnerId,PlanId,ResourceKind,ResourceId)`**. Original physical PlanItem has ResourceId without kind and prose “Course/skill/task”: amend physical normative design before migrating. If existing data appears, explicit typed backfill with validated platform.Resource registry; unknown/Skill rows cannot be silently rewritten/discarded.
- Type-specific resolver validates registry module+resource and cross-module current source capability, not just FK existence. Sorted owner/plan row locks and deterministic item ordering; transaction includes source participant owner/auth proof → Plan/Items → audit and original receipt in consistent lock order with Tasks/Goals/Courses; verify deadlock patterns in SQL.
- Deleted/archived source leaves PlanItem intact and masked; restore can revalidate but never resurrect blocked source data. Retained references guard source purge only if source contracts explicitly require it; unrelated source delete isn't cascaded.
- Rollback disables new Plan writes while preserving root/items/receipts/audit; no destructive down migration or substitution of Skill with Goal. Verify checksum and clean upgrade/restore before accepting.

## React UX (FX40-S05)

Learning → Plans native list/filter → Create Plan (title, optional dates/notes) → Detail with ordered typed source picker split into Goals/Tasks/Courses. Pickers only show current eligible owner-authorized sources; disabled sources labelled Unavailable, never stale private title. Link and reorder require visible revision handling, accessible keyboard controls and explicit no-items/unknown-progress states. Completion, Archive, Trash and purge show impact previews and confirmation; dirty draft/unreceived ACK navigation protected. Partial read/revoke clears source private summaries without incorrectly declaring success. No external LMS enrollment or autogenerated Course/Task upon Plan create.

## Acceptance matrix (tests to be authored/run only under applicable QA authorization)

| Case | Expected observable outcome |
|---|---|
| Create plan+three kinds; reload and filter >25 | Exactly one root, typed stable refs/order/TargetOn, current source-only summaries, safe owner-bound cursor |
| `kind=Skill`/unknown/spoofed ResourceId/foreign owner | Validation or common unavailable; no registry or item mutation, no source existence leak |
| User has `learning.plan.link` but lost `goals.goal.read` | Goal picker and link fail closed; no title or count leakage |
| Source removed/Trash/module disabled after linking | PlanItem retained; source summary masked, progress Unknown/Unavailable, no fake completion |
| Unlink | Source record/reminder untouched, independent plan item/audit mutation only |
| Reorder under two tabs; duplicate ItemIds/partial set | Stale 412/conflict or 422; atomic original ordering, no position collision |
| Lost response after commit / same UUID key-body | One PlanItem and one audit effect; same ACK; changed body conflict |
| Plan archive/trash/restore | Original state and reference identity preserved, never auto-resurrect source |
| Keyboard/mobile and per-owner context change | Picker/filter/dirty draft/selection inaccessible cross-owner; no stale private data |
| SQL crash/migration/rollback/independent review | No partial root/items or unauthorized source DTO; rollback retains all original data |

**Readiness:** Product source-type ambiguity **RESOLVED BY OWNER**; full source-participant lock-order, typed physical schema and actual API/UI implementation/SQL acceptance/independent review remain `IN_PROGRESS/NOT_RUN`. Do not mark Learning Plan Implemented merely because this contract is complete.
