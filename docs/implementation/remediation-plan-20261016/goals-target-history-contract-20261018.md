# Numeric/Boolean target definition history

Before-code bounded continuation, implementation agent / Technical Lead; same
user/DEC014 local approval, branch impl/m01-s00-scaffold and HEAD
1cd27a46a2ba1f24ab2b6f32f84771230f4b1761. Code-only and pending independent
DB/security/owner review; no tests/fixtures/runtime/SQL execution. Baseline and
mixed routed rules as current Goal metadata/Work Log lifecycle task.

Source: FX16-S03 history + target rows; FX16-BR001/003; immutable-kind target
definition editing/removal contract and retained evidence. Exact SELF actions
goals.target.create/update/remove, goals.goal.history/read and goals.target.read.
NXG-FX16-G01/G02, NXG-P02-01, NXG-SYS02/03/05/08/09/11/13/14. Task targets
and their AC001/003, Goal Trash/providers remain separate.

Plan: additive0085 immutable target definition versions, owner/Goal/target
composite FK (add supporting unique parent key), actor FK, original target binary8
revision unique per source, captured/source-update UTC datetime2(7), kind/title,
exact Numeric initial/target decimal28,8 or Boolean null numeric fields, removed
flag. Snapshot Created on both Goal-create-with-Numeric and later Numeric/Boolean
target create; HistoryStarted actual pre-command baseline on first later edit/
remove of a legacy target without history, then Updated/Removed. Atomic source/
Goal revision/audit/receipt transaction; replay/rollback/denied append none.
Current progress values are not copied or recomputed, and existing GoalProgress
evidence/history remains independent. No retroactive source states invented.

GET /api/v1/goals/{id}/target-history?cursor: current own nonTrash Goal plus
history/Goal-read/target-read at start and before release in Serializable TX.
Includes retained removed targets with original snapshot title/definition (not
current title inferred).25-row timestamp/GUID descending seek with dedicated
purpose-keyed AES-GCM actor/owner/Goal cursor. Invalid cursor422, missing404,
revoked403. Numeric exact strings use nonexponential decimal formatting; Boolean
numeric values null. No cross-owner/shared/support/emergency/generic projection.

Native detail explicit-load readonly expandable versions, exact source values,
kind/removal label, honest baseline/empty/loading/error, profile-timezone capture/
source instants, unmount abort/generation/context/revision fences. No history
rewrite/restore/delete API. Existing mutation/dialogs unchanged.

Data impact/recovery: empty additive table plus parent unique index takes schema
locks/index validation, no data/backfill/grants/deletion. NOT EXECUTED. Preserve
captured versions on app rollback; no destructive down migration. Future Goal/
target purge must order dependent versions explicitly. Mixed old/new writers do
not establish complete capture. Acceptance NOT RUN: exact Numeric/Boolean fields,
immutable kind, original key replay no duplicate, legacy baseline honest, removed
evidence still readable, foreign/revoked/Trash/cursor ties and rollback semantics.
Compile builds only detect implementation errors.
