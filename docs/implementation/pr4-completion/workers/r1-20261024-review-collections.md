# Independent Collections source review — 2026-10-24

Reviewer: Skill implementation worker (independent of Collections authorship).
Scope: Collections API/contracts/service, four source participants, migration0090,
CollectionsScreen/collectionApi and native navigation integration. No review of own
Skill changes is claimed. Root owns all product corrections.

Authorization: explicit coordinator review request; current AGENTS code-only run.
Read-only source inspection plus this report only; no product edits, tests, fixtures,
runtime, migration execution, compile or browser automation in this review.
Branch `impl/m01-s00-scaffold`; HEAD
`14c76bcb6960e4ce53369dce388a8480b1c0299a` plus current shared workspace.
The latest root candidate-generation and reloadRevision hardening was reread after
the coordinator's notification; the earlier bytes are not this review's basis.

## Finding and final disposition

### COL-R01 — P2, RESOLVED IN SOURCE: committed create/delete navigation

Original finding location (prior reviewed bytes): `web/Nexora.Web/src/CollectionsScreen.tsx:103–105`.
Supporting integration: `web/Nexora.Web/src/App.tsx:1495`; native collection
selection/close correctly uses the callback at screen lines184/195.

After a confirmed create, `persist` calls only `setSelected(ack.id)`; it never calls
`onSelectCollection(ack.id)`. The real host supplies that callback and keys the
component from `location.resourceId`. Consequently, creating a new collection while
at `/collections/oldId` shows newId detail while the address remains oldId. Reload,
bookmark or profile-driven remount selects oldId again. Creating from the list
likewise leaves `/collections` while showing the new detail.

After a confirmed container delete, the same branch calls only `setSelected(null)`.
Deleting from `/collections/deletedId` leaves the URL pointing at the deleted
resource; reload/deep-link restoration requests the deleted ID and returns404.
This is a reproducible source-level location/state inconsistency; it is not an
authorization bypass or a claim of executed browser reproduction.

Expected: use the supplied native selection callback for committed create and
container delete, with local-state fallback only when no callback is provided.
Account for the existing in-flight dirty-leave guard: a host navigation requested
while `inFlight.current` is true is intentionally blocked, so release/sequence that
guard safely before requesting the committed navigation. Do not weaken uncertain
request protection or perform a fresh detail GET without current read authority.
Source binding: FX24-S02/S03 native detail/return journey; NXG-SYS-13/14.
Final disposition: **resolved in source by root; focused independent re-review
complete**. No product edits were made by the reviewer. The paragraphs above
record the original defect, not the behavior of the final reviewed bytes.

Focused re-review inspected the full latest CollectionsScreen and current hashes.
At line23, a distinct nullable committedNavigation envelope distinguishes no
pending navigation from a committed delete destination `{}`. Lines109–113 queue
the host destination only after validated opaque ACK: create queues ack.id and
container delete queues the list. Without a host callback, existing local
selection behavior remains. Member DELETE does not enter the container branch.

Lines106–107 clear the original receipt intent, uncertainty and modal state;
line124 releases the in-flight ref and busy state. The effect at lines80–84 waits
for the committed render with busy/uncertain/editor/deleting/picker all clear,
then consumes the envelope and dispatches the host callback. Shared modal unmount
cleanup releases its navigation guard; the screen guard reads the now-cleared
inFlight/originalIntent refs. Navigation therefore uses the existing host route
instead of changing selected state underneath a stale location.

The new effect performs no GET. Host navigation remounts into the existing
capabilities-before-GET load path at lines53–69; opaque replay and unknown-response
paths remain separate. Lines139–159 retain current-capability/stale-response/404
reloadRevision hardening, and candidate generation checks remain at lines86–95.
The user-facing text at line211 now lists Project, Document, Bookmark and Snippet,
consistent with the four supported types, without implementation jargon.

**Final bounded disposition: no open confirmed findings from this review.**
Create/delete URL, reload/back/forward and modal-guard behavior still require human
runtime verification; source correction is not an executed browser acceptance.

## Bounded review results

No confirmed Critical/High ownership, source-projection, replay or SQL-integrity
defect was found in the inspected bytes. This is a source-review result, not runtime
acceptance, a full security audit or a claim that SQL semantics were executed.

| Area | Inspected evidence and result |
| --- | --- |
| API / context | CollectionEndpoints group uses existing unsafe-method CSRF protection and server identity resolution; response is no-store. DTOs explicitly disallow unmapped fields. No caller OwnerId or public/support projection path |
| Current owner/action | SqlCollectionService:32–35 combines current live account/PersonalSpace/session/role with FX24 capability; update/add/remove/reorder also require collection.read. All SQL collection/member reads and writes carry actor.OwnerId; owner/collection composite FK in0090 prevents cross-owner membership |
| Current source authority | CollectionSourceParticipant:21–30 checks current per-source capability before/after SQL. Source implementations use owner predicates, finite type/module/action pairs, lifecycle/Trash filters; Document uses approved VisiblePageV1 parent/folder visibility; Snippet joins exact current same-owner version. Only ID/type/title/status projected, no private body/URL/history |
| Hidden-member privacy | Service:76–85 and235–243 resolve each source and revalidate at final projection checkpoint. Unavailable refs are omitted entirely. DTO exposes no physical rank, hidden ID/count, gap or aggregate member count; frontend ordered list enumerates visible refs only |
| Source write isolation | Add resolves source before write and again at209–210. Reorder derives/revalidates exact visible set at195–204 and211–212. Remove/container delete change only membership/container; no source writes or FK cascade into source. Cleanup of owned hidden refs does not disclose source content |
| Opaque receipts | Service:137–151 binds request digest to OwnerId/id/ETag/body, with actor.UserId subject and exact operation in SqlRequestReceiptStore. Typed version/owner/id/action/status/code checked; current original write authority rechecked; Add replay checks current source module read authority. Returns Committed ACK only, not cached summary/ETag. Opaque replay intentionally need not re-open a source that has since been removed |
| Atomicity | Service:37–45 uses one Serializable transaction; parent UPDLOCK/HOLDLOCK at246 serializes membership writes. Receipt/business mutation/audit/completion share transaction, commit only at220. Exceptions/uncommitted returns dispose transaction; safe persistence error. Real race/deadlock/fault behavior unexecuted |
| Ranks / ETags | Strict quoted canonical eight-byte rowversion at250–254; stale parent checked at165–167. Every successful nondelete write touches parent revision at207. Reorder validates exact current visible IDs once each and permutes their existing physical slots, preserving hidden slots. Schema does not impose a transient unique-rank constraint that would break sequential swaps |
| Migration0090 | Additive tables/indexes/FKs, bounded metadata/type/rank checks, same-owner parent FK and unique source membership; existing unknown schema contract fails closed. No source FK/cascade, Allow grants, mock/demo data or destructive source transformation. Existing0012 creates organization schema. DDL execution/rollback/locking remain human QA gates |
| Uncertain retries | Screen:97–124 freezes original method/path/body/ETag/key and refuses a different intent until resolved; unknown response/status retains the original, retry passes identical object; collectionApi:18–25 verifies opaque ACK and expected target. Navigation guard and beforeunload cover in-flight/uncertain state. Explicit abandon warns it cannot cancel/undo a possible commit; its instruction to reload before new work is advisory rather than an enforced refresh gate |
| Latest hardening | Screen:86–95 checks candidate abort, mounted state and current sequence. Screen:139–159 blocks reloadRevision with unresolved original/in-flight request; fetches current capabilities before GET; rejects stale results; clears private edit/detail/delete/picker on404; fail clears private projections on401/403. These fixes remain present after committed-navigation correction |
| Native source-open | Screen:219 only passes typed source ID to host; App:1495 admits finite RESOURCE_TYPES then uses existing internal resource route. All four installed participant types belong to that set. Host remount key includes profile/PersonalSpace/role/modules/resourceId. No external-open navigation or implicit source write introduced. COL-R01 is resolved in source as detailed above |
| Dirty / accessible forms | Shared ResourceFormDialog supplies dirty-discard navigation warning, beforeunload, modal isolation/focus behavior. Native labeled fields, buttons and keyboard up/down are present. Actual keyboard/mobile/screen-reader behavior not run |

## Sources, rules and goal trace

Current AGENTS/engineering skill were reloaded; baseline/profile/Technical Lead
core, verification, routing architecture/backend/frontend/security/database/
owner-isolation rules and mandatory docs/current PO/goals had been loaded in this
session. Review additionally read the exact
`r1-20261024-collections-contract.md`, feature24, action24, UX24, Phase03
P03-ORG-002, module24 goals, current actor/capability/receipt infrastructure,
shared navigation/form primitives, source DDL and Document visibility projection.

Trace: P03-ORG-002; FX-24-BR-004/005; FX-24-AC-003; FX-COM owner/current authority,
strict revisions/idempotency/atomicity/privacy; FX24-S02/S03;
NXG-FX24-G01/G03, NXG-P03-01, NXG-SYS-02/03/04/05/07/08/09/13/14.
No Template/sharing acceptance or additional source-type eligibility is inferred.

## Reviewed byte identifiers and remaining gates

SHA256 of primary files after latest root hardening:

```text
SqlCollectionService.cs CC40CB20B495EC35CD700BF7451D48A31CBE4605B58666B3A5E1A276B32DF768
CollectionsScreen.tsx   7C37D395C6B7519ED72C2B612DC9618212B5E745B033FCACA23B4DC8611D8EB5
collectionApi.ts        6D92CD8D9F0FB2C2FEF7F1C743CE14B4C593AE0F9BC97BEB52B1E0DA7A610F2F
0090 migration          CD34E044158A438A27C02A8C5B2EFF10A6320DF4377AF53A39DFB6F460ED9646
```

Commands: Get-Content/numbered source reads, rg searches, Get-FileHash, read-only
`git -c safe.directory=... rev-parse HEAD`. No product mutation or check execution.

Focused re-review replaces the earlier CollectionsScreen hash
`A912A143BE1AB6D1F089388046D8F1C730EEAD379B38009FA079451FB9801F39`.
Service/API-client/migration hashes are unchanged. Root owns fresh integrated
build/current-head CI evidence after its correction; no builds/tests/runtime were
executed by this reviewer during the focused re-review.
Human owner retains SQL migration/constraints/ranks/rollback/fault/concurrency,
cross-owner/revocation/replay, native navigation, all popup/viewport/accessibility
and uncertainty-path runtime verification. Current-head CI/PO acceptance remain
separate gates. No blanket approval, merge, provider or production approval.
