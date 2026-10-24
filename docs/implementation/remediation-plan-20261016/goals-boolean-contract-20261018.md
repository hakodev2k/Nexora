# FX16 Boolean target local continuation

Task owner: implementation agent. Branch impl/m01-s00-scaffold, revision
1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, existing draft PR #4; no merge.
DEC-20260909-014 and current owner instruction authorize this local code continuation.
Code-only amendment applies: no tests, fixtures, runtime QA or database execution.
Rules: AGENTS.md; nexora-engineering SKILL.md; implementation profile; Technical
Lead core; routed backend/frontend/database/security/owner-isolation rules.

## Sources and bounded plan

Feature16 data validation and flow2 approve immutable Boolean target kind and
explicit value; BR003 leaves Goal lifecycle explicit. Action catalog16
goals.target.create/read/record_progress and UX FX16-S03/S04 supply the entry points.
Database05 GoalTarget.BooleanValue and GoalProgress.Checked plus physical migration
0014 already provide bit state and append-only evidence; no migration/backfill.
P02 Goals scope and NXG-FX16-G01/G02 bind this continuation. Tasks targets,
history browsing, edit/remove, archive/trash, reminders and providers remain separate.

Add a Boolean target to an existing editable own Draft/Active Goal, then record
explicit true/false with optional note (max2000). Title trimmed1..200. Initial
value must be explicitly supplied. Boolean is0/1 in the existing equal-weight
aggregation, never automatically Complete. Kind is immutable. No linked sources.

POST /api/v1/goals/{goalId}/targets/boolean accepts {title, booleanValue};
POST /api/v1/goals/{goalId}/targets/{targetId}/boolean-progress accepts
{booleanValue, note}. Missing/null Boolean is422 rather than implicit false.
Both require UUID Idempotency-Key, strict Goal If-Match and authenticated SELF,
current FX16/actor/owner/action proof, CSRF and common no-store/error envelope.
Create requires target.create + goal.read + target.read; progress requires
target.record_progress + target.read. Resolve receipt before current revision or
lifecycle; new typed Boolean canonical intent cannot match a numeric receipt.
Same key/different payload409, stale412, missing/wrong owner404, terminal409.
Existing Goals unavailable-authority response is ModuleUnavailable409. Own Goal
must also have no active TrashItem; immutable source/Trash proof is held in the
same transaction. Receipt replay remains before fresh lifecycle/Trash checks.

Serializable transaction locks own Goal first, checks revision/lifecycle, allocates
next target Position under that lock (bounded int); creates only Kind=Boolean with
numeric columns NULL. Progress locks own target under own Goal, rejects Numeric/Tasks,
updates BooleanValue and appends GoalProgress.Checked (Value NULL). Both update Goal
revision, audit metadata only, store opaque version2 receipt and perform final
authority proof atomically. Read DTO adds nullable booleanValue; legacy numeric
routes, canonicalization and exact decimal fields remain compatible. New create
receipt namespace is existing target.create action, outcome BooleanTargetCreated201.

Native Goal detail lists Boolean labels/value/progress and opens explicit create or
progress dialogs. Editable lifecycle only; safe cancellation, dirty/unload guard,
common busy handling; frozen route/payload/revision/key use existing Goal uncertain
retry flow. No automatic toggle command, no request from highlight. Numeric UI stays.

## Evidence binding and rollback

| Goal | Source/action/AC | Implementation | Acceptance evidence |
| --- | --- | --- | --- |
| NXG-FX16-G01 | Feature16 Boolean data; FX16-S03/S04; target.create/read/record_progress; common owner/concurrency/idempotency AC | Boolean service, DTO/API, native dialogs | Equal weighting, false/true, wrong-owner/kind, replay/conflict/stale: NOT RUN |
| NXG-FX16-G02 | FX-16-BR-003; explicit lifecycle | Same-TX progress leaves Status unchanged | 100% does not Complete: NOT RUN |
| NXG-SYS-02/03/05/08 | Shared security/evidence AC | Current source authority, opaque receipt, final proof | Revocation/terminal/races/audit: NOT RUN |

Compile/build only may detect implementation errors; record exact results later.
Independent authorization/security/transaction review Pending. Human functional
testing and SQL runtime verification NOT RUN. Rollback is revert Boolean routes/UI
and additive response field; retain existing Boolean/history/audit rows, no delete
or destructive SQL. Unresolved gates do not imply full FX16 completion.
