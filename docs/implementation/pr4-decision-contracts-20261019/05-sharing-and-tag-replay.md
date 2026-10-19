# D07=A / D08=A — Revoked ShareLink owner diagnostics and historical Tag-create replay

**Owner choice:** DEC-20261019-PR4 D07=A, D08=A. Existing policy remains source of truth: `DEC-20260907-Q03` and `DEC-20260909-005` (invalidated token never revives), current FX04 action guards and FX24/20 tag namespace, current owner/source authority and safe ACK convention. These decisions resolve **specific test-oracle ambiguity**, not blanket access. Source observation: `SqlSharingService.cs:88–178` and `:395–444`; `SqlTagService.cs:479–526` at review SHA `7a1f157`. Source must be rechecked on each implementation SHA.

## D07 — Owner diagnostic list retains own explicitly revoked row (NO viewer access)

### Current API/service binding

`GET /api/v1/sharing/links?state=Active|Expired|Revoked&mode=&resourceType=&resourceId=&cursor=&limit=25`; full nullable-filter shape is `ShareLinkListQuery`, and output `ShareLinkPage` has `ShareLinkRecord` with `RevokedAt`, `IsActive`, `ETag`, `AllowedUserIds` and optional `RawToken`. The installed SQL already includes owner-revoked rows where `IsDeleted=1` **only if** `InvalidationReason='OwnerRevoked'`, the sharing epoch matches, the current source is owner-readable and current FX04 action is allowed. Do **not** generalize this predicate to source-delete/admin invalidation, foreign records or stale epoch.

| Context | Contract |
|---|---|
| Owner authenticated list, `state=Active` | Only current active eligible links (no revoked/expired). UI default to Active; switching to Revoked/Expired is explicit, no token is recovered |
| Owner list, `state=Revoked` | Only own active-epoch, current source-authorized diagnostic rows with `RevokedAt != null`, `IsActive=false` and **`RawToken=null`. No source title/body/notes, or invalidated source metadata beyond already authorized fields |
| Owner list, omitted `state` | Existing API nullable filter can include current eligible Active/Expired/OwnerRevoked; **do not reinterpret server null as Active** without versioned client compatibility review. Default UI must send `state=Active` explicitly. Cursor binds exact filter |
| `GET /api/v1/sharing/links/{id}` | Current owner/source/projection and link-metadata read policy; revoked metadata is not equivalent to token/read access; determine current implementation specific detail behavior before changing it |
| `GET /api/v1/sharing/resolve/{token}` or `/sharing/{token}` | Expired, revoked, source-deleted, epoch-invalidated, audience-denied and wrong viewer all unavailable `404` without resource/owner existence disclosure. Revocation is irreversible for issued token |
| Revoke command `DELETE /api/v1/sharing/links/{id}` | Requires original own link, live owner, active FX04, source action, current quoted ETag and idempotency key; same-TX invalidation marker, audit and receipt; unrelated/foreign links untouched |

### Database + race safety

Current backend sets `IsDeleted=1`, `RevokedAt/InvalidatedAt` and `InvalidationReason='OwnerRevoked'`. Preserve these fields, `IssuedSharingEpoch` and hash-only issued token. Owner diagnostic row exposes safe metadata only. No token material can be reconstructed or copied after first create; ensure serialization explicitly `RawToken=null` on all list/detail/replay paths except authorized new-create result memory. Repeated revoke does not update token or revive link; source delete/sharing-off increments/deactivates epoch, and future restore/re-enable cannot match old issued epoch. Count/pagination must be scoped before disclosure and cursor signed/HMAC-bound to OwnerId, mode, state, resource and epoch; stable seek `CreatedAt DESC,Id DESC` (current SQL). A revoked row must not survive into a list after source access revoked; avoid stale advisory read racing with current policy.

### UI behavior

Owner Sharing screen opens Active tab. Dedicated Revoked/Expired filters show status and revocation date when permitted; revoked row has no Copy/Re-enable/Edit audience token control, may have safe metadata for audit/diagnosis; status explicit. Allow mode/resource filtering and paginate 25, restoring filter on back. Current source denied during browsing clears stale title/preview and shows Unavailable not “no records”. Destructive confirmation retains current original key on uncertain response. Viewer screen never advertises “revoked but accessible”.

### Acceptance

- Owner revokes link → after commit `resolve(old token)` returns uniform 404; `state=Active` excludes row; `state=Revoked` includes only safe owner metadata (inactive/null token) with current owner+source proof.
- B/foreign/disabled account/module/sharing-off/source deleted/epoch rotated → no metadata or title/count, regardless of `state`, filter/pagination.
- After folder cohort delete → restore and sharing on → old token still404; creates new link with new token when authorized.
- 26+ own links with exact state/mode/resource filters → no duplicate, skip, count leak, filter-tamper cursor reuse.
- Concurrent owner revoke and viewer resolve/download → linearizable refusal after revocation; no cached private body or file capability surviving source epoch changes.
- Reconcile older `SqlSharingRepairTests.cs:408` expectation “revoked owner list empty”: change only the *owner diagnostic* assertion after current contract review, retain independent revoked-token 404 and owner negative tests. Never change a test oracle solely to silence CI.

**Disposition:** specific D07 semantics **CONFIRMED**, but client default Active state, all source/revocation tests and independent security review still required. Current code appears to implement much of this already; do not claim completed until tests on the final revision.

## D08 — Original Tag-create replay after Tag removal = opaque committed ACK

### Entry boundary and exact original action

Existing `POST /api/v1/organization/tags` (`organization.tag.create`) dispatches documents namespace to the dedicated Document Tags service; current local namespace handling is in `SqlTagService`. The stable receipt namespace must be the **original producer/action** `organization.tag.create` (or the dedicated Documents equivalent), never treat receipt from a different action/namespace as valid. Client original request's UUID idempotency key and canonical digest must bind actor/owner, operation, normalized Tag namespace and title; current source implementation uses receipt claim and checks version 2 envelope, returned Id/action/namespace/ETag/status/result code. Do not infer the original resource from a matching *current* list entry or recreate it.

| Situation | Required response |
|---|---|
| First authorized create | One Tag registry/tag row/audit/receipt atomic. `201` with readable Tag DTO if `organization.tag.read` currently proves same returned revision; otherwise opaque `TagCommitted` acknowledgement (current API format, never arbitrary fallback DTO) |
| Same key+same original body after Tag was removed | Valid V2 receipt + current original write and namespace permission still valid ⇒ **opaque committed ACK** anchored to original TagId/status; no second Tag or audit and **no deleted Tag DTO** |
| Same key/body with Tag still present but unreadable or ETag changed | Opaque committed ACK, not latest value; current reads cannot substitute for original successful create |
| Same key+different canonical request, wrong action/namespace/actor, invalid key | `409` safe conflict or `422` validation as existing API contract; never accept as fresh or leak original ID |
| Legacy receipt absent/corrupt/ambiguous/unknown version, unresolved original create binding | `409` reconciliation-needed/common conflict; no second create or broad parsing |
| Current actor inactive, namespace disabled, action revoked/denied | Safe denial before receipt disclosure, even for an originally committed key; original write authority cannot be circumvented by historical success |
| Documents namespace | Route to `SqlDocumentTagService` with its own exact provenance; no cross-namespace receipt/ID alias or stealth fallback to generic Tags |

### SQL transaction and receipt grammar

Server-derived owner and current session, same Serializable transaction for module/action and namespace rules → receipt claim before fresh-create duplicate validation → verify stored original receipt version/action/namespace/Id/ETag/resultCode/status/digest → current original-write policy check → return exact committed ACK with **no write**. New create inserts owner-scoped Tag + registry + audit + complete receipt once. If digest replay matches but original action cannot be verified, deny; if same ID has since been deleted, don't query a new ID or recreate. If current read rights exist but original ETag no longer matches, return opaque rather than current Tag details. Replay after a 24h receipt-retention boundary can no longer guarantee dedupe; UI must reconcile explicitly and never blindly generate a second create.

Proposed reply shape should reuse installed `IdentityOperationResult<T>.Committed(originalTagId, status, "TagCommitted")`; do not add a new global response format unless the client and same-transaction contract demand it. Source-specific rules apply equally to Tag rename/remove/owner Namespace only where the original operation and actual receipt prove it; D08 is **not** blanket authorization for every historical delete replay.

### UI/UX contract

After save attempt with unknown response, freeze the submitted normalized title/namespace/request key. Button “Retry original request” reuses same key+body; no client reset/duplicate create until confirmation. On committed opaque ACK, show “saved previously; details may no longer be available”, no stale chip/tag name from deleted source; offer current authorized refresh and safe navigation, not optimistic Edit/Restore. When permission revoked, clear private list/selection and display actionable denial, not empty-success. Namespace choices must remain source-provided and Docs/Organization namespace isolated.

### Acceptance matrix

1. Create Tag + remove Tag, retry original create UUID/body: exactly one original Tag ID/one create audit/version, opaque TagCommitted, no Tag DTO/body, no recreated row.
2. Same body with new key is a **new intent**, subject to current duplicate/title/lifecycle contract; do not redefine semantics to “replay”.
3. Same key wrong title/namespace/actor; unknown receipt version/status/code; tampered row; historic read-only grant; module off ⇒ safe denial/409 as defined with no second mutation, no stale title.
4. Concurrent same-key initial creates serialize and only one commits. Fault-inject after first commit before HTTP response, subsequent retry identical.
5. Documents Tag namespace and Organization namespace cannot bind each other's receipts, especially after deletion or name reuse.
6. Owner B/nonowner cannot learn Tag existence via receipt or read/count; read grant revocation yields opaque ACK *only when current original write authority remains*.
7. Preserve exact 24h retention semantics, strict ETags and current SQL audits; actual real SQL/API/React tests on final SHA + independent security review.

**Disposition:** D08 A resolves safe replay semantics; old `NQA-013` test expecting ResourceUnavailable for a deleted Tag after a **verified** committed create is stale. No automatic green claim; update only the specific expected result after source and rollback/security tests confirm existing implementation.
