# FX25-S01 — Global Search source-query local slice

This document records the first local Search implementation on PR #4. It is a
bounded owner-scoped read projection over sources that already have SQL
contracts. It deliberately does not introduce a persisted search index or
change source-of-truth ownership.

## Goal → requirement → acceptance → operation → code

| Goal / requirement | Acceptance boundary | Operation | Implementation |
| --- | --- | --- | --- |
| `NXG-FX25-G01`, `P03-SRC-001`, `P03-SRC-002`, `P03-SRC-006` | Search title/body-safe fields across Projects, Tasks, Events, Documents, Bookmarks, Snippets and Goals with resource/date/archive filters, deterministic score/time/id ordering and a bounded page. | `search` | `ISearchService.Search`, `SqlSearchService`, `GET /api/v1/search` |
| `NXG-FX25-G02`, `P03-SRC-003`, `P03-SRC-004` | Current module/action checks and owner filters run per source; results contain only safe title/snippet/status/time/route fields. | `search` | Source-specific SQL predicates and safe snippet projection |
| `NXG-FX25-G02`, `P03-SRC-005` | A disabled or failed source is reported independently as `Unavailable`/`Degraded`; it does not turn another source's results into an empty success. | `search` | Per-provider status in `SearchPage` |

## API and query contract

`GET /api/v1/search?q=&resourceType=&from=&to=&includeArchived=&limit=` requires
the authenticated owner session, `FX25` enablement and
`discovery.search.query`. Query is trimmed and bounded to 500 characters;
`resourceType` is one of `Project`, `Task`, `Event`, `Document`, `Bookmark`,
`Snippet` or `Goal`; dates are UTC date boundaries; `limit` is bounded to 25.

The service queries at most 25 rows per enabled source, then merges by exact
title match, title-prefix match, title/body match, updated time descending and
stable ID. Each provider reports a count and `Ready`, `Empty`, `Unavailable` or
`Degraded` state. A blank query intentionally returns an empty page and does
not execute broad source scans.

Documents search Title and Body; Snippets search current-version Title,
Language, SourceText and Description but never return source text as preview.
Archived rows are excluded unless `includeArchived=true`; Trash/deleted rows
are always excluded. Unsupported or disabled modules are omitted from result
payloads and reported as unavailable when requested.

## Security and boundaries

- Every source query filters by `IdentityPrincipal.OwnerId`; no owner or source
  scope is accepted from the client. Notification/Vault/audit/support data is
  not indexed or queried by this slice.
- Source module/action checks run immediately before each query. A search hit
  is not an authorization grant and the returned route only points to an
  internal source screen.
- LIKE metacharacters are escaped. Snippets are whitespace-collapsed,
  angle-bracket-neutralized and truncated to a safe 240-character preview.
- No persisted search projection, Redis cache, saved query, favorite, recent
  item, external network call or provider execution is added.
- The React `/search` screen has explicit loading/empty/error/partial states;
  opening a result navigates only to an internal route and never auto-executes
  a command or mutation.

## Evidence status

This is a code-only update under the current execution amendment. No new
unit/integration/E2E tests, fixtures, mock/demo records or functional QA claims
were added. Static/build/syntax checks are reported only when actually run;
.NET/SQL/browser runtime checks remain owner/CI dependencies when unavailable.
