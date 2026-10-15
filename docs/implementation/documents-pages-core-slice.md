# Documents pages core local slice

Status: `SLICE_IMPLEMENTED` in PR #4; runtime acceptance remains owner
verification under `DEC-20260909-014`.

## Contract trace

- Goals: `NXG-FX20-G01…G03`, owner/lifecycle/concurrency goals.
- Sources: [FX-20 Documents](../features/20-documents.md), document payload
  contract and `FX-20-AC-001`, `FX-20-AC-004`.
- Operations: `listDocuments`, `getDocument`, `createDocument`,
  `saveDocument`, `transitionDocument`.

## Implemented boundary

- A verified account receives an owner-scoped Documents page projection from
  SQL through the authenticated `PersonalSpace.Id`; no owner or role is read
  from request data.
- Create requires an explicit `DocumentType` (`Document`, `Note` or
  `Knowledge`) and `EditorMode` (`Markdown` or `Block`). Pages start in
  `Draft` and store a bounded canonical body.
- Every successful create/save writes an immutable `documents.PageVersion`.
  Save checks the quoted SQL `ETag`/`If-Match`, keeps type/editor immutable and
  rejects obvious executable markup. Published pages can be archived; archive
  does not create a content version.
- List responses are summaries. List/Get now hold current session/action and owner/Trash/deleted lifecycle proof in the same Serializable SQL transaction as projection; default library excludes Archived, which uses an explicit filter/tab. UI clears denied projections and guards stale reads/reset. The subsequent mutation checkpoint uses current transactional action proof, structured digests and minimal durable receipts. Detail requires current page.read; otherwise API/UI use committed ACK without body. Original transition action is rechecked on replay. UI keeps exact uncertain command/key and protects dirty drafts before lifecycle. Legacy delimiter receipt keys fail409 safely rather than reproduce legacy responses. See full-product-audit-20261012/documents-mutation-contract-20261015.md; runtime acceptance remains not run. UI drafts remain in memory.

## Deliberately outside this slice

Folders/hierarchy, tags, covers/files, version-history browsing/restore,
sharing links, Trash cohorts and DOCX/Markdown import/export are not claimed.
They require their own contracts and migrations before implementation.

## Evidence

The code-only run may execute frontend build and structural static checks. The
agent does not add or run functional, SQL integration, E2E/browser tests,
fixtures or manual QA. .NET/SQL runtime verification remains `Not run` when
the required tooling is unavailable.

## Bounded library query continuation

GETdocuments also accepts cursor/query/documentType. Default page25 (maximum100), UpdatedAtDESC/IdDESC seek with encrypted authenticated owner/filter/limit-bound cursor and take+1. Query<=200 is a literal Title match, never Body; known typefilter only. UI uses25/page, LoadMore dedup, 300ms typingdebounce/Enter andabort/epoch invalidation, keeps editor draft acrosslistfilters andclearsdeniedprojections. Tag/location/folder semantics remain unimplemented requirements, not removed fromR1. Evidence: full-product-audit-20261012/documents-list-continuation-20261015.md.

GETdocuments now also accepts optional createdFrom/createdTo, inclusive profile-IANA local dates against CreatedAt using half-open UTC boundaries. UpdatedAt ordering remains unchanged. Shared SqlOwnerLocalDateRange resolves current owner profile in the caller transaction; missing/invalid zone503 and unrepresentable boundary422, no UTC fallback. Cursor dl2 binds dates/timezone/TZDB/resolved boundaries; dl1 requires a fresh query. UI combines date inputs with Title/Type/Archive and preserves pending/draft guards. See full-product-audit-20261012/documents-created-date-continuation-20261015.md. Source/build evidence only; human runtime acceptance remains not run.
