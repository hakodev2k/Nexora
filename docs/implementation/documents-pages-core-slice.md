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
- List responses are summaries; page bodies are returned only by the owner
  detail endpoint. UI drafts remain in memory and all writes carry a durable
  idempotency key.

## Deliberately outside this slice

Folders/hierarchy, tags, covers/files, version-history browsing/restore,
sharing links, Trash cohorts and DOCX/Markdown import/export are not claimed.
They require their own contracts and migrations before implementation.

## Evidence

The code-only run may execute frontend build and structural static checks. The
agent does not add or run functional, SQL integration, E2E/browser tests,
fixtures or manual QA. .NET/SQL runtime verification remains `Not run` when
the required tooling is unavailable.
