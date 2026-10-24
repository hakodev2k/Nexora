# Goal lifecycle history — bounded local continuation

Task owner: implementation agent / Technical Lead. Existing user instruction to
code remaining slices + DEC014; branch `impl/m01-s00-scaffold`, HEAD
`1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`. Same baseline/routed rules as
Work Log continuation; code-only, independent security/DB review Pending.

Source binding: FX16-BR003 explicit status/reopen with history; FX16-S03 history;
catalog `goals.goal.history` SELF only plus `goals.goal.read`. Writer actions
`goals.goal.start/complete/abandon/reopen/archive/unarchive` retain existing
approval, strict source revision, current lifecycle/owner/authority and receipt.
Goals NXG-FX16-G01/G02, NXG-P02-01 and NXG-SYS02/03/05/08/09/13/14.
Does not claim Task-linked targets, Trash, metadata/target-definition history or
providers. Existing progress-history endpoint remains compatible and separate.

Before-code plan: additive0082 owner-scoped append-only GoalStateHistory event
table with owner+Goal FK, actor FK, UTC datetime2(7), exact action and before/after
status, source resulting binary8 revision unique per owner/Goal. No snapshots of
description/progress, no backfill or inferred old states, no grants or execution.
Append once after successful source transition in the SAME source/audit/receipt
transaction; original completed receipt replay returns before append. Transaction
rollback leaves no event. No history edits/deletes/restores exposed.

GET `/api/v1/goals/{id}/state-history?cursor=`: current own nonTrash Goal in one
Serializable transaction; current history+Goal read at start and before response.
No target-read prerequisite because no target fields are returned.25-row descending
UTC timestamp/GUID SQL seek with purpose-separated AES-GCM cursor bound to
actor/owner/Goal. Invalid/foreign cursor422, denied403, missing/deleted/Trash404.
Return `{items,nextCursor}` with event id, exact action, from/to status and UTC
recordedAt only. No Support/Emergency/share/generic discovery projection.

Native detail explicit-load readonly table, profile timezone, safe empty wording
(no pre-install history claim), loading/error/paging, unmount abort and generation
fence. Goal identity/revision/context key clears history after changes. Rendering
is text only. Revocation clears this private panel;401 triggers existing auth loss.

Migration locks/backup/recovery: additive empty table, no domain rewrite, no SQL
execution. Roll application back first, retain captured rows; no destructive down
migration. Future source purge must explicitly handle FK/history retention.
Mixed old/new writers cannot claim full history capture; deploy new writer
exclusively. Runtime acceptance NOT RUN: allowed transitions record exact pair;
replay/conflict/denial/rollback append none; foreign owner/cursor/Trash/permission
revocation denied; equal-time events paginate without dropping ties. Compile/build
are implementation-error checks only, not these acceptance outcomes.
