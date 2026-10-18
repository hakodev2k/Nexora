# Manual Work Log Trash / Restore / Purge

Task owner: implementation agent / Technical Lead. User requests all remaining
coding slices; DEC014 authorizes local code. Branch impl/m01-s00-scaffold, HEAD
1cd27a46a2ba1f24ab2b6f32f84771230f4b1761, uncommitted. Baseline227 source and
protected12 owner files each0 drift. Skill nexora-engineering; baseline plus
architecture/backend/frontend/security/database/owner-isolation/verification
routes retained, required startup and critical auth/migration/concurrency reread.
Independent security/DB/owner review Pending. No test/fixture/runtime/SQL execution.

Sources: P07-WRK-001; FX40-BR006; FX08-BR002/005 and Trash flow3; UX40-S06,
D-TRASH/RESTORE/PURGE; exact SELF learning.worklog.trash/restore/purge plus read.
NXG-P07-01, NXG-FX40-G03, NXG-SYS02/03/05/08/09/13/14. TimeEntry AC004,
source links and global Trash provider registration remain separate open scope.

Before-code bounded plan:

- Additive0083 expands own manual WorkLog status to Trash, adds actual
  PreTrashState Active/Archived and owner-composite TrashBatchId. Existing
  PreArchiveState is preserved through Trash/restore. CHECKs preserve pairing.
  Existing history expands with Trashed/Restored and Trash snapshots; no backfill.
  WorkLog registry still worklog-manual-v1; metadata trash capability becomes true
  only for that exact contract. Three absent-only permission keys, no Allow grant.
- POST /{id}/{trash|restore|purge}-preview with strict original If-Match and no
  mutation: current own source/read/action in Serializable TX, single root cohort
  (one WorkLog, dependent private history count), current retained ResourceLink
  count (incoming/outgoing, nonDetached, includes version refs). Restore/purge
  require exact existing root/member/prior-state batch, not timestamp inference.
  Return id/operation/etag/from/to/batch/history count/reference count plus
  AES-GCM purpose-separated5min token bound to actor/owner/source/action and
  source revision/cohort/counts. No note/body in token or generic audit.
- POST /{id}/{trash|restore|purge} body {confirm:true,previewToken}, exact UUID
  key + original If-Match. Existing owner update lock, current action/read, typed
  opaque receipt before fresh token expiry/source/lifecycle (replay appends none).
  Validate token scope/revision/cohort/counts/expiry, then recheck all source and
  reference guards under Serializable. Changed preview409; stale revision412;
  missing428; malformed422; revoked403; foreign source404. No automatic retry.
- Trash creates durable one-root operations.TrashBatch/TrashMember, prior state,
  updates registry/domain and captures Trashed snapshot atomically. Restore only
  that exact current batch, restores actual Active/Archived, clears Trash fields,
  marks batch Restored and captures Restored snapshot; Archived stays readonly.
- Purge only Trash, retained references0, deletes source private history then
  WorkLog, preserves minimal registry Purged tombstone and cohort/audit/receipt;
  no unrelated source deletion or restored content. Audit metadata only.
  Final proof: current registry correct exact contract and expected availability,
  own domain absent for purge/present for others, current authority before commit.
- Native Work Log Trash state filter, current detail readonly, preview dialog
  showing exact source/prior/next/cohort/dependencies/history scope. Purge warns
  loss of content AND all versions, initially focuses Cancel, disabled if refs>0.
  Explicit confirmation only; pending frozen body/key/etag/token with existing
  uncertainty recovery. Context changes/revocation clear private dialog data.
  Trash is excluded from reports; historical snapshots remain own-read only.

Impact/recovery: schema expansion needs schema locks/CHECK validation, no row
backfill/content deletion by migration. Runtime purge is explicitly irreversible
and user-confirmed; agent does not execute it. Before purge owner can Restore the
exact batch; after purge only an independently approved backup recovery may help,
not an in-app undo promise. No destructive down migration; rollback preserves
Trash-aware readers/schema; old code cannot write Trash rows or assume history
vocabulary unchanged. Migration/rollback/restore rehearsals NOT RUN.

Acceptance intent NOT RUN: Active/Archived exact restore; malformed/foreign/stale
preview; cohort member edits; grant revocation; new refs/versions after preview;
concurrent save/restore/purge; retry after commit/expiry/purge; rollback atomicity;
private history absent after purge; registry tombstone present; report excludes
Trash; dirty/uncertain/keyboard flows. Compile builds only for implementation errors.
