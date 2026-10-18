# FX16 target management continuation

## Task brief and authority

Accountable owner: implementation agent, Technical Lead role. User asks to implement
all remaining requirement-backed slices without another approval. Existing
DEC-20260909-014 applies; code-only amendment prohibits new tests, fixtures/demo
records, runtime verification and migration execution. Branch impl/m01-s00-scaffold,
HEAD1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, existing draft PR4, no merge.
Baseline206 product fingerprints and original12 user files verified zero drift.
Read AGENTS, nexora-engineering, implementation profile, Technical Lead README/core,
routing baseline and architecture/backend/frontend/security/database/owner-isolation/
verification paths. Independent review unavailable/Pending, not author self-approval.

Sources: current delivery01 and DEC014; feature16 validation/flow2/BR001/003;
action catalog16 target.create/update/remove/read/record_progress; UX16-S03/S04,
sections11/15/16; database05 GoalTarget/GoalProgress, physical0014; P02 scope and
NXG-P02-01, NXG-FX16-G01/G02, SYS02/03/05/08/09/11/13/14. Common acceptance and
FX-16-AC-002 apply. This slice adds Numeric target creation on an existing Goal,
editing Numeric/Boolean definitions and removing a target with impact preview.
Task-linked targets, advanced Goal lifecycle, full lifecycle history and providers
remain committed follow-on work, not removed from scope.

## Bounded implementation plan

1. Expand schema with nullable GoalTarget.RemovedAt/RemovedByUserId and actor FK/
   paired-null check, absent-only Resolved metadata target.update/remove. Register
   migration0078; no execution, records, backfill, Allow grants or destructive DDL.
2. Keep removed target evidence physically present, exclude only from current
   target lists/counts/equal-weight progress and future edits/progress. Never delete
   GoalProgress/audit; history shows a removed-target marker. Do not reuse positions.
3. Add Numeric target to own Draft/Active Goal using strict Goal If-Match, explicit
   exact decimals, existing target.create/read plus goal.read. Validate title1..200,
   decimal(28,8) without rounding and target>initial. Existing Boolean receipt
   canonicalization and numeric creation-on-Goal receipt remain byte-compatible.
4. Edit definition only: Numeric title/initial/target, Boolean title. Current numeric
   and Boolean progress state are unchanged, and still recorded only via dedicated
   progress commands. Target Kind is immutable. No owner/status/reorder overposting.
5. Preview both edit and remove against current own editable/nonTrash Goal and
   active target. Return before/after target count and Goal progress, source Goal
   and target ETags, normalized definition and expiring encrypted preview token.
   Numeric preview uses SQL decimal formula/precision matching actual projection;
   average uses the persisted SQL AVG/decimal projection rules, not CLR or displayed
   rounded percentage. Removing last target produces0/No targets, not Complete.
6. Explicit confirmation commits frozen token/key with strict original Goal ETag;
   same-TX source/revision/lifecycle/Trash/current-action proof, metadata audit,
   opaque receipt and final authority recheck. Tombstone only, no source destruction.
7. Native target table handles every Numeric/Boolean target; select exact target
   for progress, use common dialogs/dirty guards and uncertainty retry. Explain
   effects before confirmation, focus safe cancellation, one-decimal percentages,
   exact source decimals and accessible before/after values. No auto-write on preview.
   Separate Goal metadata drafts survive target commands; dirty metadata blocks
   lifecycle transitions until explicitly saved/discarded. No targets is labelled
   on list/detail as well as the target manager.

## API and transaction contract

- POST /api/v1/goals/{goalId}/targets/numeric with NumericTargetRequest:
  title/initialValue/currentValue/targetValue, decimal JSON strings allowed;201.
- POST /goals/{goalId}/targets/{targetId}/change-preview with
  {mode: Update|Remove,title?,initialValue?,targetValue?}; QUERY semantics via POST
  to keep private titles/values out of URLs. CSRF required. Remove rejects fields;
  Boolean rejects numeric fields; Numeric requires initial/target. Current exact
  action target.update or target.remove, plus goal.read/target.read. No receipt or
  persistent side effect for preview; original Goal If-Match required before
  reading the impact, stale draft412;5min AES-GCM context-bound token.
- POST /goals/{goalId}/targets/{targetId}/update-definition and /remove accept
  {previewToken}; strict Goal If-Match and nonempty UUID Idempotency-Key. Route
  binds operation and IDs, token binds actor/owner/Goal/target/ETags/mode/definition/
  expiry. Canonical receipt binds exact token and route/revision; original replay
  is reconciled before preview expiry or fresh lifecycle/removed-state checks.
  Same key/different request409, stale412, expired410, invalid422, missing/foreign/
  removed/Trash404, locked409, existing ModuleUnavailable409, persistence503.
  Update200 GoalTargetUpdated, remove200 GoalTargetRemoved; create201
  NumericTargetCreated. Opaque version2 receipts add only exact supported outcomes.

Serializable SQL transaction locks Goal first then own target; source read guards
are repeated after audit/receipt via final projection/authority. Position allocation
includes tombstones, int overflow rejected. All SQL parameterized, actor/owner
server-resolved, no Support/link mutation, no Redis cache/provider/background effect.
Preview is guidance plus authenticated intended definition; actual commit checks
original Goal and target revisions and never trusts submitted progress/count.

## Acceptance/evidence and rollback

| Goal | Exact source/action/AC | Code binding | Required owner assertions / evidence |
| --- | --- | --- | --- |
| NXG-FX16-G01; NXG-SYS-11 | BR001, FX-16-AC-002; target.create/update/read | SQL validation/formula, exact decimal UI | Multiple targets, target<=initial, decimal precision, projected ratios: NOT RUN |
| NXG-FX16-G01/G02; NXG-SYS-09 | target.remove preview/history guard; BR003 | Signed preview, soft removal, active-only aggregation, history marker | Last target0/No targets, history retained, no auto Complete: NOT RUN |
| NXG-SYS-02/03/05/08/13 | shared owner/concurrency/idempotency/UX AC | Current source/authority, encrypted preview, frozen retry, safe confirmation | Wrong owner/action/Trash/terminal, revoke/stale/expiry/replay, dirty/focus: NOT RUN |

Only compile/build/typecheck may detect implementation errors; they do not prove
SQL queries/constraints/locking, migration safety, precision at runtime or browser
acceptance. Independent DB/security/authorization review Pending. Rollback disables
new commands/UI but retains tombstones/history/audit. Do not revert to old binaries
that ignore RemovedAt: they would revive removed targets in aggregation. Retain
the active-only reader patch in any forward-fix/rollback; no destructive down SQL.
