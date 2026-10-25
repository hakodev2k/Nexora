# FX31 local records contract — 2026-10-25

Owner: `/root/shopping_completion`; root integrates/reviews. Source branch
`impl/m01-s00-scaffold`, starting `75b6d9f4f0ac247680e22cc07bf26a4890071ef2`.
Current explicit user instruction authorizes completing local implementation,
synthetic SQL/E2E and UIUX; supersedes earlier code-only execution restriction.
D01–D09 and DEC-014 security/provider boundaries remain unchanged.

Loaded mandatory Nexora startup, Technical Lead core, current delivery/PO decisions,
system/goals template and FX31 module goal; matching architecture/backend/database/
security/frontend/verification/owner-isolation routing paths. Rules guide execution,
not business decisions. Root owns central wiring and independent review.

## Bounded source binding

P05-SEL-001, P05-ORD-001, P05-WAR-001; FX-31-BR-001/003/005/006;
FX-31-AC-001/003; common FX-COM-006/007 and isolation/denial/retry AC;
NXG-FX31-G01/G02/G03, NXG-SYS-02/03/05/08/11/13/14.

Actions: `shopping.seller.read/create/update`, `shopping.order.read/create/update`,
`shopping.warranty.read/create/update`. Exact feature/UI/data sources:
`docs/features/31-shopping-records.md`, `docs/action-catalog/modules/31-shopping.md`,
`docs/ux-ui/modules/31-shopping-records.md` FX31-S03/04/05/06,
`docs/design-database/08-news-shopping-developer.md` Seller/PurchaseOrder/OrderLine/Warranty.

This slice supplies private SELF manual records only. No provider calls, payment,
Finance mutation/reference, Asset handoff, sensitive nonSELF projection, file evidence,
merge, Trash/purge or state-transition/return handlers. Those are retained backlog,
not silently removed. Full graph edges are not specified by current higher-priority
sources; ordinary update must not invent transitions. Orders created as Draft and
only Draft metadata/lines editable here; status is server-owned.

P05-SEL-001 says public shop reference/status only while feature/DB prose adds private
contact/notes. This subset accepts Name and public HTTP(S) URL only; no contact/notes
form or request fields. URL is inert text, never fetched/opened automatically. This
does not resolve the wider field-source discrepancy through implementation.

## API and data

Root `/api/v1/shopping/records`: GET capabilities; GET/POST `sellers`, `orders`,
`warranties`; GET/PUT `{family}/{id}`. GET lists accept `page` (1..1000), optional
query<=200; 25 rows/page with `hasMore`, no unbounded counts. Seller Name ASC/Id,
Order updated DESC/Id; Warranty expiry ASC with explicit Lifetime/Unknown labels.

Seller create/update `{title,url?}`; Title1..200, absolute HTTP(S) URL<=2048 without
userinfo/control characters. No uniqueness/automatic merge by name.

Order create/update `{sellerId?,orderNumber?,purchasedOn,currency,shippingAmount,
taxAmount,discountAmount,notes?,lines[],confirmDuplicateReference?}`. ISO date-only,
explicit registered uppercase currency. Lines1..100 with title1..200, positive
quantity and nonnegative unit amount as invariant decimal strings (20 integral/8
fractional maximum). Variant<=300. Compute exact decimal total; reject overflow,
precision loss or negative total. No float or implicit FX. Referenced seller must
be current, same owner, readable. Snapshot seller title at Save; no tracked-price
dependency. Duplicate nonempty order reference yields409 unless explicit confirmation.

Warranty `{orderLineId,title,coverageKind,startsOn?,endsOn?,provider?,terms?,claimContact?}`.
This subset requires a current same-owner readable Order line (no Asset-only source
yet). Fixed requires start/end and end>=start; Lifetime/Unknown must not carry dates.
Owner SELF private warranty metadata only; no sharing/support/export source.

Writes require UUID Idempotency-Key and updates strong SQL-rowversion If-Match;
428 missing/malformed,412 stale,422 invalid fields,404 unavailable reference/resource,
403 denied/module unavailable,409 duplicate/reference/lifecycle conflict,503 SQL outage.
Success returns opaque `{id,committed:true,detailAvailable:false}` acknowledgement;
UI separately reads detail only with current read permission. Replay proves original
owner/action/key/digest/current action authority, never stores/replays private DTO.

0094 creates typed owner-scoped Seller/PurchaseOrder/OrderLine/Warranty tables,
same-owner FKs to Resource and parents, nonnegative/coverage/state constraints,
indexes and contract markers. Existing unexpected tables fail closed. Inserts only
missing action definitions/resource types; no grants/status/module promotion.
Mutation owner-lock, current SQL authority, receipt, registry/domain rows and minimal
audit commit in one serializable transaction. SQL is authoritative; no Redis state.

## UX, verification and rollback

Native list/detail/form screen with Sellers/Orders/Warranties tabs, query/page, clear
empty/no-match/fetch-error, current capability checks, labeled native controls,
explicit currency/date, line-total preview, dirty dialog and pending request guard.
Unknown write outcome freezes body/key and offers exact retry; stale revision requires
reload/compare before resave. Denial/unmount invalidates private responses.

Tests: validation/decimal/date/coverage boundaries; SQL owner/ref isolation, stale and
replay/different-body; UI permitted/denied/error/unknown-write/keyboard cases. Real SQL
execution belongs to isolated root environment, never in-memory substitute. Build/type
checks are implementation checks only. No Release Ready until full AC/review/runtime.

Rollback: code revert while keeping populated new tables; migration is additive and
has no automatic down/drop. No destructive rollback, default grants, seed personal
records or external effects. Independent DB/security review remains mandatory.
