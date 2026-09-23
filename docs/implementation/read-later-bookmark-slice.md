# Read Later Bookmark-reference slice

Status: `SLICE_IMPLEMENTED` on PR #4 under `DEC-20260909-014`. This is a code-only implementation record; runtime migration, functional tests and browser QA remain owner work.

## Boundary

This slice implements the local Bookmark-backed part of FX-23. A ReadingItem stores an owner-scoped source type/id plus safe title and URL snapshots, state (`Unread`, `Reading`, `Read`) and an explicit 0–1 position metadata value. It never copies bookmark bodies, follows URLs, renders remote content or changes the source record. News sources, shared read-state coordination and reader/body extraction remain gated.

## Traceability

| Contract | Implementation |
| --- | --- |
| FX23-S01 queue and unavailable-source view | `IReadingService`; `SqlReadingService`; `GET /api/v1/read-later` |
| `reading.queue.read` | Owner-scoped SQL list with optional state filter and source-availability projection |
| `reading.item.save` | `POST /api/v1/read-later`; Bookmark-only source validation, one owner/source entry and safe snapshots |
| `reading.item.remove` | `DELETE /api/v1/read-later/{id}`; removes only the queue reference, including when the source is unavailable |
| `reading.item.read/unread/position` | `PATCH /api/v1/read-later/{id}`; explicit state action, 0–1 validation and source-read dependency |
| Owner isolation | Every query/mutation scopes `OwnerId` from trusted `IdentityPrincipal`; request payload cannot choose an owner |
| Concurrency/idempotency | SQL `rowversion` ETag with `If-Match`; durable `RequestReceipt` for mutations |
| Source safety | Only existing owner Bookmark metadata is read; no body copy, URL fetch, iframe, external navigation or provider adapter |
| Audit | Successful save/remove/state changes write redacted `security.AuditEvent` rows |
| Frontend | `/read-later` route and FX23 module entry with source picker, state filter, mark read/unread, position metadata, remove, and loading/empty/error/conflict/unavailable states |

Migration `database/migrations/20260910_0011_reading_queue_bookmarks.sql` adds `[knowledge].[ReadingItem]`, owner/source/state/position constraints and six canonical action permissions.

## Explicitly deferred

News sources, body extraction/reader rendering, cross-module News read-state coordination, source filters/tags, search, history, sharing/support, Trash/restore/purge and external-open behavior remain separate contracts. They must not be inferred from this slice or enabled by a grant alone.

## Verification boundary

The implementation run may use frontend build and structural checks to catch code errors. SQL execution, .NET compilation, API/functional tests, E2E/browser tests, fixtures, mock/demo data and manual QA are not claimed here.
