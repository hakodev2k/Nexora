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

`GET /api/v1/search?q=&resourceType=&from=&to=&includeArchived=&limit=&cursor=` requires
the authenticated owner session, `FX25` enablement and
`discovery.search.query`. Query is trimmed and bounded to 500 characters;
`resourceType` is one of `Project`, `Task`, `Event`, `Document`, `Bookmark`,
`Snippet` or `Goal`; dates are User-profile IANA local calendar days, resolved in the same SQL transaction to inclusive start/exclusive next-day UTC boundaries; `limit` is bounded to 25.

The service queries at most limit+1 (maximum26) rows per enabled source, then merges by exact
title match, title-prefix match, title/body match, updated time descending and
SQL uniqueidentifier ID ascending and resource-type ordinal tie. Each provider reports its full current pre-seek matched count (saturated at int.MaxValue) and `Ready`, `Empty`, `Unavailable` or
`Degraded` state. A blank query intentionally returns an empty page and does
not execute broad source scans.

Documents search Title and Body; Snippets search current-version Title,
Language, SourceText and Description but never return source text as preview.
An encrypted authenticated sg2 cursor binds owner, normalized query/type, dates, archive flag, effective limit, configured profile IANA zone, TZDB version and resolved UTC date boundaries. Old sg1 cursors are rejected422 after this semantics correction; a profile timezone/TZDB change also requires a fresh query. A changed context/tampered token is rejected422 InvalidCursor. Seek follows the same score/time/SQLGuid/type order as merge; the next cursor is emitted only with another visible hit. Pagination reads current data each request and promises stable traversal under a stable dataset, not a snapshot across later changes. The React Load More flow deduplicates typed identities and clears accumulated hits for sources currently unavailable/degraded/empty.

A missing/unknown configured IANA zone fails503 TimeZoneUnavailable without guessing UTC. Entire-skipped or unrepresentable day boundaries (including an inclusive To date whose next day is outside the supported calendar range) fail422 DateRangeUnavailable instead of silently clamping or returning a wrong range. Whole calendar days use earliest valid starts, preserving short/long DST days. Blank/no-date queries do not scan sources/look up profile timezone unnecessarily.

Archived rows are excluded unless `includeArchived=true`; Trash/deleted rows
are always excluded. Unsupported or disabled modules are omitted from result
payloads and reported as unavailable when requested.

## Security and boundaries

- Every source query filters by `IdentityPrincipal.OwnerId`; no owner or source
  scope is accepted from the client. Notification/Vault/audit/support data is
  not indexed or queried by this slice.
- Root/current session and source module/action checks run in the same Serializable SQL transaction as matches and counts, with final root proof before commit. Source rows are staged until the source reader succeeds; failed sources expose no partial rows. Active owner Trash references and deleted records are excluded before matching/counts, including linked Task lifecycle for Events. A search hit
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
