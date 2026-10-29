# FX40 Skill merge contract — 2026-10-24

Task owner: scoped Skill worker; integration and independent review: coordinator.
Source: `14c76bcb6960e4ce53369dce388a8480b1c0299a`, branch
`impl/m01-s00-scaffold`, existing PR #4. Session authorizes local code in the
assigned files, no commit/push, tests, fixtures or runtime execution. DEC014 and
current D01–D09 remain binding; D06 does not turn Skills into Plan sources.

Source binding: P07-SKL-001; FX-40-BR-003/006; FX-COM-001/002/006/007/010/013;
FX-COM-AC-001…006; FX40-S02. Goals NXG-FX40-G01, NXG-P07-01,
NXG-SYS-02/03/05/08/09/13/14. This is a bounded implementation contract under
DEC-GOV-001, not a new product approval or acceptance result.

## API, lifecycle and preservation

- POST `/api/v1/learning/skills/{sourceId}/preview-merge`: If-Match source,
  body `{targetId,targetETag}`. Both distinct, same-owner, Active, unmerged.
  Returns both current Skill snapshots, source/target evidence counts, overlapping
  Course count, new evidence count, expiry and signed preview token.
- POST `/{sourceId}/merge`: same If-Match and target fields plus
  `{confirm:true,expiresAt,previewToken}`; UUID Idempotency-Key mandatory.
  Preview expires after five minutes. Signature binds actor, owner, both IDs,
  exact revisions, counts and expiry. Altered/stale/expired previews fail closed.
- GET `/{id}/merge-history?cursor=`: skill.read, 25 rows, stable ID cursor;
  returns source/target pre-merge snapshots, counts, merge instant, nextCursor.
  Only owner SELF; no sharing/support/emergency projection.
- Dynamic prerequisites: skill.merge + skill.read, current account/module/owner
  authority, current Course source-read authority and source availability for
  every existing evidence reference on both Skills. No proficiency/evidence
  grant is inferred. Merge is its own explicit action.
- Preserve the source as an Archived merge tombstone with MergedIntoId. The
  source metadata, original evidence (including duplicate notes), previous audit
  history and unique normalized name remain retained. Every subsequent source
  mutation is locked; no unmerge/version restore is introduced. This implements
  the existing DB explicit-merge tombstone and archive-readonly contracts.
- Copy only Course refs absent on target, including original note/attribution;
  overlapping refs retain target interpretation and original source interpretation
  on source. Target metadata, lastUsed and self-assessed level stay explicit and
  unchanged. No source resource is deleted or rewritten; generic ResourceLinks
  stay attached to the retained source. Preview explains these effects.
- Append immutable merge history with both before snapshots and counts. All
  original source evidence remains inspectable. Target evidence copies are normal
  target-owned refs after merge; merge history preserves provenance. Retained
  source/target history pins payload purge; normal archive/trash still applies to
  a target that is not itself a merge tombstone.

## Atomicity, errors and receipts

One SQL serializable transaction: existing PersonalSpace update lock; revalidate
authority; claim typed receipt; validate both revisions and preview; inspect all
evidence source providers; copy absent refs; append history; archive source/set
pointer; touch target revision; update both Resource rows; audit; complete receipt;
recheck authority and source providers; commit. Failure rolls everything back.
No network/background/cache effects. Exact replay validates typed actor/owner/
source/target receipt plus current original action authority, returns only opaque
`{id,committed:true,detailAvailable:false}` and never historical content/ETags.

Errors: 403 current authority; safe 404 resource/source; 428 missing revision;
412 either revision stale; 409 lifecycle/preview expiry/change/idempotency conflict;
422 self merge, invalid IDs/confirmation/token/key; 503 unprovable receipt.

## Migration, rollout and recovery

0088 replaces only the NULL-only merge constraint with an Archived/nonself marker
constraint; adds same-owner FK history and indexes; installs exact merge permission
metadata without Allow grants. No data backfill or execution in this run. Coordinator
must register migration in M01MigrationManifest and merge action in ActionGrantPolicy.
Existing endpoint mapper/service registration is reused. Mixed old binaries are
unsafe after first merge because old Unarchive lacks tombstone guard: disable FX40
before rollback and retain schema/history; do not clear pointers or drop records.
Any data recovery/unmerge requires separately reviewed corrective contract.

## UX and evidence plan

Source card → explicit paged Active target picker → server preview of both names,
levels, exact revisions, counts and expiry → explicit confirm. Cancel writes nothing.
Stale response requires fresh selection/preview; uncertain retry freezes key/body;
success refreshes list, never claims inferred assessment. Archived source exposes
merge destination and read-only original evidence/history. Accessible shared dialogs.

Human QA assertions (NOT RUN): same/cross owner, self merge, either stale revision,
revoked action/module/source between preview/commit, unavailable Course, overlapping
notes preservation, exact replay/conflict/legacy receipt denial, rollback on audit
failure, concurrent merges/evidence/lifecycle, source locked after merge, purge pins,
keyboard/mobile/empty/error/cancel/uncertain retry. Compile/typecheck only this run.
Independent DB/security/source-contract review remains Pending; root schedules it.

Loaded rules: baseline per `.ai/routing.json`, Technical Lead core, architecture,
backend, frontend, security, database and owner-isolation routes (all listed files).
No agent configuration edits; baseline verifier not applicable.
