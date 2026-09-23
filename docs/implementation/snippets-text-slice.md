# Snippets text/version slice

Status: `SLICE_IMPLEMENTED` on PR #4 under `DEC-20260909-014`. This is a code-only implementation record; runtime migration, functional tests and browser QA remain owner work.

## Boundary

This slice stores owner-scoped code/text snippets with title, language, description and an append-only current version. It supports list/search, create, save-as-new-version, archive/unarchive and explicit clipboard copy. Source is always escaped data: Nexora never compiles, evaluates, sends to AI/lint services or publishes it.

## Traceability

| Contract | Implementation |
| --- | --- |
| FX22-S01/S02 browse and editor | `ISnippetService`; `SqlSnippetService`; `/api/v1/snippets` GET/POST/PUT |
| `snippets.snippet.read/create/save` | SQL action/module checks through `SqlSelfCapability` and migration `0010` permission rows |
| `snippets.snippet.archive/unarchive` | `/api/v1/snippets/{id}/transition`; Active/Archived lifecycle only |
| Version integrity | `[knowledge].[SnippetVersion]` is append-only; save increments `CurrentVersion` transactionally |
| Owner isolation | Every query/mutation scopes `OwnerId` from trusted `IdentityPrincipal`; request payload cannot choose an owner |
| Concurrency/idempotency | SQL `rowversion` ETag with `If-Match`; durable `RequestReceipt` for mutations |
| Source safety | UTF-8 size limit (1 MiB), text-only rendering, no execution/provider/network path; copy never echoes or audits source |
| Audit | Successful create/save/archive/unarchive writes a redacted `security.AuditEvent` |
| Frontend | `/snippets` route and FX22 module entry with editor/list/search/archive/unarchive/copy and validation/loading/empty/error/conflict states |

Migration `database/migrations/20260910_0010_snippets_manual.sql` adds `[knowledge].[Snippet]`, append-only `[knowledge].[SnippetVersion]`, owner/version constraints and five canonical action permissions.

## Explicitly deferred

Version history/diff/restore UI, Trash/restore/purge, sharing/support projections, text export/download, tags/templates and secret detection remain separate contracts. They must not be inferred from this slice or enabled by a grant alone.

## Verification boundary

The implementation run may use frontend build and structural checks to catch code errors. SQL execution, .NET compilation, API/functional tests, E2E/browser tests, fixtures, mock/demo data and manual QA are not claimed here.
