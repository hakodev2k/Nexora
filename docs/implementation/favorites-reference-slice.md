# FX25-S03 Favorites — local typed-reference slice

Implementation note for PR #4 (`impl/m01-s00-scaffold`). This is a local,
owner-scoped slice under `DEC-20260909-014`; it does not implement Recents,
Saved Search, Command Palette or a persisted search index.

## Goal → requirement → acceptance → operation → code

| Goal | Requirement / AC | Operation | Implementation evidence |
| --- | --- | --- | --- |
| `NXG-FX25-G01` — favorites never become an authority or access bypass | `FX-25-BR-001..003`; `FX-25-AC-001`; current source permission/lifecycle | `discovery.favorite.read/add/remove/reorder` | `discovery.Favorite` stores only `OwnerId`, typed `ResourceType/ResourceId` and rank; every projection rechecks FX25 and the source module/action. Deleted, trashed, archived or otherwise unavailable sources return a safe unavailable state with no stale title/status/time/route. |
| `NXG-FX25-G02` — source metadata remains current and bounded | `FX-25-BR-004`; `FX-25-AC-002` | `GET /api/v1/favorites` and `POST /api/v1/favorites` | Fixed parameterized source queries cover Project, Task, Event, Document, Bookmark, Snippet and Goal. Results are owner-filtered, cursor-paginated and capped at 100 per request; no generic SQL, index or snapshot payload is accepted. |
| `NXG-FX25-G03` — favorite mutations are explicit and recoverable | `FX-25-BR-005..006`; common owner/ETag/idempotency/audit gates | `POST`, `DELETE`, `PUT .../rank` | SQL `Serializable` transactions, unique owner/resource constraint, bounded rank, quoted `ETag`/`If-Match`, durable safe-response idempotency receipt and audit event. |

## Data and boundary decision

The design document describes a future `[platform].[Resource]` registry, but the
current migrations do not provide that table. This slice therefore uses an
additive typed reference with no cross-module foreign key. The service is the
explicit integration boundary: it allow-lists resource types, resolves the
source using the caller's trusted `OwnerId`, checks the source module/action and
active Trash rows, and projects only current safe metadata. A future registry
may replace this resolver through an additive migration without changing the
public favorite contract.

`discovery.Favorite` is owned by the Discovery module. It does not copy source
titles, descriptions, secrets, audit reasons or provider data. The unique key is
`(OwnerId, ResourceType, ResourceId)` and rank is bounded to
`0..1,000,000,000` with eight decimal places. Favorites survive a source
transition only as an unavailable reference so the owner can remove it; no
stale source projection is rendered.

## HTTP contract

- `GET /api/v1/favorites?resourceType=&limit=&cursor=` — authenticated SELF
  query; type is one of Project, Task, Event, Document, Bookmark, Snippet or
  Goal; limit is clamped to 1–100. Response is `{ items, nextCursor }`. The
  owner-scoped list and source projections run in one serializable SQL
  transaction, with a final FX25 capability check before commit.
- A missing/denied Favorites action returns `403 PermissionDenied`; an
  unavailable module or hard dependency returns `409 ModuleUnavailable`. Source
  read denial remains a redacted `Unavailable` projection so Favorites cannot
  disclose source existence or metadata.
- `POST /api/v1/favorites` — `{ resourceType, resourceId }`; the body cannot
  supply owner, title, status, route or rank. Returns `201` with the resolved
  safe projection. Duplicate owner/reference returns `409`.
- `DELETE /api/v1/favorites/{favoriteId}` — requires a quoted `If-Match`; a
  wrong owner or missing reference is an unavailable `404`; stale revision is
  `412`; success is `204`.
- `PUT /api/v1/favorites/{favoriteId}/rank` — `{ rank }` is required and must
  be a bounded decimal; quoted `If-Match`; returns the current safe projection
  and a fresh ETag.

Unsafe requests use the shared CSRF group. Mutations accept `Idempotency-Key`.
The receipt stores only the safe Favorite projection (or the 204 status). A
same-key/same-payload replay rechecks the current operation and source
authority, returns a current safe projection (redacted to `Unavailable` when
the source is no longer readable), and rejects a missing favorite as a safe
replay conflict; a changed payload is rejected. Audit rows record only
actor/owner/action/target/trace metadata.

## UI contract

The authenticated shell exposes `/favorites` only when FX25 is enabled. A direct
route while FX25 is disabled renders an unavailable state. The screen has
labeled type/UUID add controls, loading/empty/error states, safe available cards,
unavailable-source cards with removal, cursor-based loading, rank editing, ETag
conflict reload and internal-source navigation. It does not infer authority
from hidden controls, persist auth material in browser storage or render raw
HTML/source bodies.

## Evidence and limits

Code-only checks actually run for this revision: `python3 scripts/dev/verify-s00.py`,
`python3 .ai/scripts/verify-baseline.py`, `python3 -m py_compile
scripts/dev/verify-s00.py`, `bash -n scripts/dev/*.sh`, and `git diff --check`.
`npm run build --prefix web/Nexora.Web` was not run because this checkout has no
`node_modules`. .NET SDK, SQL Server, Docker, `sqlcmd`, browser tooling and
functional tests are unavailable in the execution environment; they are not
claimed as run. Recents, Saved Search, Command Palette and persisted
index/reindex remain gated.
