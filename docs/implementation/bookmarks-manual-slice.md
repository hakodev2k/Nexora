# Bookmarks manual metadata slice

Status: `SLICE_IMPLEMENTED` on PR #4 under `DEC-20260909-014`. This is a code-only implementation record; runtime migration, functional tests and browser QA remain owner work.

## Boundary

This slice stores owner-scoped HTTP(S) bookmark metadata: URL, canonical URL, title, optional description, health/status projection and timestamps. It intentionally does not fetch or open URLs, embed remote content, resolve tags/collections, share records or place them in Trash. Duplicate URLs remain separate records; no automatic merge is performed.

## Traceability

| Contract | Implementation |
| --- | --- |
| FX21-S01/S02 browse and add/edit | `IBookmarkService`; `SqlBookmarkService`; `/api/v1/bookmarks` GET/POST/PUT |
| `bookmarks.bookmark.read/create/update` | SQL action/module checks through `SqlSelfCapability` and migration `0009` permission rows |
| `bookmarks.bookmark.archive/unarchive` | `/api/v1/bookmarks/{id}/transition`; Active/Archived lifecycle only |
| Owner isolation | Every query and mutation scopes `OwnerId` from the trusted `IdentityPrincipal`; request payload cannot select an owner |
| URL safety | Absolute HTTP(S) validation, canonicalization and SHA-256 digest; no outbound client, iframe or browser navigation |
| Concurrency/idempotency | SQL `rowversion` ETag with `If-Match`; durable `RequestReceipt` for mutations |
| Audit | Successful create/update/archive/unarchive writes a redacted `security.AuditEvent`; URL/description are not copied into audit text |
| Frontend | `/bookmarks` route and FX21 module entry with create/edit/search/archive/unarchive, validation, loading/empty/error/conflict states |

Migration `database/migrations/20260910_0009_bookmarks_manual.sql` adds `[knowledge].[Bookmark]`, owner/index constraints and the five canonical action permissions.

## Explicitly deferred

Metadata refresh, network health checks, external navigation, collections, tags, sharing/support projections, Trash/restore/purge, Read Later integration and provider adapters remain separate contracts. They must not be inferred from this slice or enabled by a grant alone.

## Verification boundary

The implementation run may use frontend build and structural checks to catch code errors. SQL execution, .NET compilation, API/functional tests, E2E/browser tests, fixtures, mock/demo data and manual QA are not claimed here.
