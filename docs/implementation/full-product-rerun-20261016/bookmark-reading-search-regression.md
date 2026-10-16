# Bookmarks / Reading / Search regression rerun — 2026-10-16

## Task brief

Current user requests rerun of the attached full implementation prompt with functional and UI smoke checks. That explicit authorization supersedes the older code-only execution amendment for this run. Source checkout: PR #4 branch `impl/m01-s00-scaffold`, `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`; requirements main remains `8782f46`. The prior worktree is preserved, and no earlier production patch was copied over latest code.

Author `/root/bookmark_reading_fix`; independent review requested `/root/auth_review`. Root owns integrated build/real SQL/CI execution, frontend and browser smoke. No schema, migration, production or real provider operations are included. The43 baseline/backend/database/security/owner-isolation/verification instruction paths loaded earlier were compared byte-for-byte with this checkout and are unchanged.

## Confirmed repairs at current code

| Finding | Source / goal | Repair / acceptance |
| --- | --- | --- |
| High: retained Active Bookmark bypassed active Trash in direct/query and Reading source paths | FX-21-BR-003; FX-23-BR-001; FX-COM-AC-001/004; NXG-SYS-02/03/05; NXG-FX21-G03; NXG-FX23-G03 | Bookmark List/Get/mutation resolution and Reading source loading/availability exclude active owner-scoped `platform.TrashItem`. Safe queue snapshots remain allowed; SourceAvailable becomes false; fresh source commands fail; Remove remains source-independent. A completed replay may return only its known committed opaque outcome, never a live Trash DTO. |
| Medium: successful original ACK upgraded to full DTO after a later read grant | Common idempotency/response projection; NXG-SYS-03/08; FX21-S02; FX23-S01 | Store original DetailAvailable with minimal receipt reference/status in the same transaction; originally opaque remains opaque. Originally detailed response is permitted only with current read, current lifecycle and matching ETag. Historical receipts without original projection proof conservatively return ACK. Preserve Reading RequiresPosition metadata and existing legacy no-payload behavior. |
| Medium: unquoted/noncanonical rowversion accepted | Shared If-Match/concurrency; FX-COM-AC-002/003; NXG-SYS-05 | Both services require exact quoted canonical base64 of8 rowversion bytes; malformed/unquoted values return428. Existing revision conflicts remain412. |

Reading's latest field-diff behavior is preserved: changing Progress while setting Read/Unread requires its state action plus `reading.item.position`; no obsolete blanket422 rule was restored. Current precision<=8 decimals, namespace/recovery behavior, SQL current authority, current receipt result codes and source-independent removal stay intact.

## Regression inventory and current-source adaptations

`SqlBookmarkReadingAuthorityTests.cs`:19 cases, including theory instances. Covers command-only Admin ACK/HTTP shape; newly granted read preserving original ACK; legacy original-projection-unknown receipts; current Update read prerequisite; stale/quoted ETag; Archived readonly/unarchive; forced post-write authority/source revocation with/without keys;27-row owner/filter/limit-bound paging and API-issued cursors; literal search; durable singleton save/replay/remove204; current precision and combined-action progress guards; missing/revoked/Trash source; bounded duplicate Save versus position Update concurrency.

`SqlSearchPaginationAuthorityTests.cs`:16 cases. Ported tests now use latest SQLGuid ascending then resource-type ordinal order,26-candidate maximum source paging, current full counts, current sg2 API-issued cursor and source revocation semantics. Coverage includes28 mixed-source equal-rank/time hits, a shared UUID across different source types, context/tamper bindings, owner isolation, literal queries, active Trash/deleted Document/body-only safe match, exact/prefix/body rank, provider degradation and max-date rejection.

Added local-date acceptance for UTC, Bangkok and New York's23-hour and25-hour DST days with exact inclusive start/exclusive end SQL timestamps. Dated cursors invalidate after profile timezone change. Unknown IANA zone returns503 `TimeZoneUnavailable`; an entirely skipped Apia day returns422 `DateRangeUnavailable`; inclusive To=DateOnly.MaxValue returns422 rather than overflow. No-date queries intentionally avoid profile timezone lookup. These tests use the current NodaTime `SqlOwnerLocalDateRange` contract; Search production/helper code was not changed.

## Evidence and rollback

Scoped `git diff --check` is the author-side structural check. New tests are authored evidence expectations, not executed SQL proof. Root records actual build/runtime outcomes and fresh independent review in the integrated rerun ledger. No pass or complete claim follows from test presence.

Code revert restores behavior without touching migration journal or retained data. New receipt metadata uses existing columns. Historical receipts are not rewritten in production; only synthetic fixture receipt payloads are changed to exercise legacy compatibility. User-facing UI smoke remains root-owned. This is a bounded audit repair and regression addition, not an FX21/23/25 or R1 completion certificate.


## Notification harness correction

Parent allocated `SqlApiIntegrationTests.cs`, `tests/Nexora.FunctionalTestOperator/Program.cs` and `web/Nexora.Web/functional/platform.spec.ts` after the production author removed ambient SuperAdmin HTTP notification publication. Notification publication is SYSTEM contract authority; a browser role is not that authority.

The existing inbox integration test now checks User and SuperAdmin attempts are forbidden/not found/method unavailable and leave the target inbox empty. Real inbox read/mark-read/stale-ETag/watermark/delete/foreign-owner checks use explicit disposable-fixture rows: one synthetic notification and exactly InApp Pending, Email Pending and BrowserPush PermissionUnavailable deliveries. No successful arbitrary HTTP publisher or sensitive-payload publisher behavior is asserted.

Functional FN-034 uses the new `seed-notification USER_UUID TITLE` operator command out of band. The executable retains the existing local SQL validation and exact Nexora_Test_<32hex> database gate, and additionally requires an active seeded qa-*@example.invalid recipient. Inserts are parameterized and atomic; body is fixed synthetic text, no credentials/provider effects/production endpoint are introduced. Browser login and all subsequent UI/API authorization paths remain real and unchanged.

These harness edits are structural/source changes pending parent-owned compile, SQL and browser execution; no runtime pass is claimed.
