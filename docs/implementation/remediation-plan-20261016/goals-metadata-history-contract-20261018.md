# Goal metadata snapshots

Task owner implementation agent / Technical Lead; continuing all local slices per
user and DEC014 on impl/m01-s00-scaffold, HEAD1cd27a46a2ba1f24ab2b6f32f84771230f4b1761.
Same required baseline/routed rules and code-only boundaries as Work Log Trash
continuation. Independent DB/security/owner review Pending. No test/data/runtime.

Sources: Goal title/description/period fields and common concurrency/history,
FX16-S02/S03 history, exact goals.goal.create/update/history/read SELF actions;
NXG-P02-01, NXG-FX16-G02, NXG-SYS02/03/05/08/09/11/13/14.
Existing lifecycle history covers BR003. Task-target AC001/003 and target definition
history remain separate, not satisfied by metadata snapshots.

Before-code plan: additive0084 GoalMetadataVersion, owner+Goal composite FK,
actor FK, unique original source binary8 revision, captured/source-updated UTC
timestamps, bounded title/description and original nullable date-only period.
Created/Updated snapshots in same source/audit/receipt transaction. On first
later edit of a legacy Goal without metadata history, capture actual current
fields as HistoryStarted before edit, not a fabricated historical create event.
Strict current Goal ETag/lifecycle/nonTrash and current actions remain required;
receipt replay returns before capture. No backfill, grants, history rewrite,
restore-version command, source progress/state/target snapshots or purge execution.

GET /api/v1/goals/{id}/metadata-history?cursor: current own nonTrash Goal and
history+Goal read in Serializable TX, before query and before response.25-row
timestamp/GUID descending seek, separately derived AES-GCM cursor bound to
actor/owner/Goal. Invalid/foreign cursor422, missing404, revoked403. Return id,
event, title/description/startDate/endDate/capturedAt/sourceUpdatedAt only.
No target-read dependency, Support/Emergency/share/generic search or note logging.

Native explicit-load25-row expandable readonly snapshots in detail, private
context/source-revision key, unmount abort and generation guard. Escaped text;
date-only fields unchanged, capture/source instants in profile timezone. Honest
empty/error/loading/baseline wording. New writes never imply pre-install coverage.

Migration is code only: empty additive table/CHECK/FK/index may take schema locks;
no existing row changes. Roll app back first and preserve captured data, no
destructive down migration. Future source purge must explicitly handle these
dependent versions. Mixed old/new writers cannot claim complete capture.
Acceptance intent NOT RUN: exact field snapshots, create/edit atomically captured,
legacy baseline honest, original-key replay/conflict/denial/rollback append none,
foreign owner/cursor/Trash/revoked denied, date-only/timezone/cursor ties correct,
stale UI cannot replace newly selected source. Only build checks will be recorded.
