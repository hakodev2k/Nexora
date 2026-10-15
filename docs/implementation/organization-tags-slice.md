# FX24-S01 organization tag catalog — local slice

Status: `SLICE_IMPLEMENTED (local)` on PR #4. This records implementation
authority and boundaries; it is not a runtime or functional-test claim.

## Contract used by the slice

- `GET /api/v1/organization/tags?namespace=&query=&limit=` returns only tags
  whose `OwnerId` is the authenticated PersonalSpace. The server sorts by name
  and returns a bounded page.
- `POST /api/v1/organization/tags` accepts a namespace (`projects`,
  `documents`, `bookmarks` or `snippets`), a trimmed 1–50 character name and
  an optional `#RRGGBB` color.
- `PUT /api/v1/organization/tags/{tagId}` renames the owner tag and changes its
  optional color. Namespace and ownership are immutable; `If-Match` is required.
- `DELETE /api/v1/organization/tags/{tagId}` requires `If-Match` and removes
  only an unused owner tag. A non-zero `ResourceTag` usage count returns
  `TagInUse` with no side effect.

All mutations require a UUID `Idempotency-Key`, CSRF and current FX24 action
grant plus current source namespace authority. Documents requires the exact FX20 documents.tag operation in addition to the generic endpoint wrapper; missing/unregistered source capability fails closed. SQL `rowversion` is the ETag authority. Audit rows contain action and
target metadata only. The `organization.ResourceTag` table is a future
provider-assignment boundary; this slice does not expose assignment writes.

## Explicit non-goals

`organization.tag.assign`, Documents page-tag assignment, Collections, Templates,
sharing/support projections, tag history, export/import and provider/network
work are not implemented here. Tags never grant permission or ownership.

## Code and schema

- Application contract: `src/Nexora.Application/Organization/TagServiceContracts.cs`
- Minimal API: `src/Nexora.Api/Features/Organization/OrganizationEndpoints.cs`
- SQL authority: `src/Nexora.Infrastructure/Organization/SqlTagService.cs`
- Migration: `database/migrations/20260910_0012_organization_tags.sql`
- React route: `/organize/tags` (`OrganizationTagsScreen`)

## Verification status

The code-only run ran frontend/static checks listed in
`docs/implementation/local-e2e-status.md`. .NET/SQL runtime, integration and
functional/browser verification were not run in this environment and are
owner-owned. No mock, fixture or demo data is introduced by this slice.

## Current namespace/receipt authority repair

List/Get now hold live actor/session/root proof with projection in Serializable SQL and recheck before response. Document namespace also requires documents.tag.read; all-namespace lists exclude unauthorized document tags before TOP and usage projection. Cursor tl2 binds source visibility; tl1 requires a fresh query. Mutations use the owned immutable namespace and exact documents.tag operation when appropriate, on keyless/fresh/replay/final commit paths. New receipts contain only Id/Namespace/Action/ETag, including remove204; structured JSONv2 digests bind owner and prevent old receipt replay without namespace proof. Old keys conflict409 safely, not compatibility-complete. Detail requires current read authority; otherwise committed ACK clears UI private state without automatic refresh. Page assignment remains unimplemented and must require page.save for field diff. See full-product-audit-20261012/tag-namespace-authority-continuation-20261015.md. Source/build evidence only, runtime not run; uncertain-command UI recovery is implemented with code-only source/build evidence, not runtime acceptance.

Tag commands now retain an immutable original payload/key/ETag under uncertainty. Inputs/new commands/query/close/navigation are guarded, with exact retry and an explicitly confirmed stop-reconciliation fallback that does not undo a server commit or send a replacement request. SDK rejects malformed ACKs/zero or mismatched IDs and unexpected Delete status. Revision denial releases its busy marker; known commits are separate from subsequent query failure. ResourceFormDialog optional closeBlocked keeps retry accessible while preventing accidental close. See full-product-audit-20261012/tag-uncertain-command-ui-20261015.md; human browser/runtime verification remains not run.

## Dedicated Documents catalog continuation

Documents catalog now owns documents.Tag and direct /api/v1/documents/tags handlers under documents.tag actions. Generic explicit namespace/owned-ID operations delegate with current FX24 wrapper plus FX20 proof in one transaction; all-namespace lists consume the provider-owned read-only TagCatalogProjection and exclude frozen legacy records. Additive0056 preserves legacy IDs/metadata/refs, rejects contradictory same-owner/cardinality mappings and adds nullable Page.TagId/same-owner FK as the usage boundary. No migration/journal/runtime run, grants or Admin eligibility changed. Quiesce old writers before local application; do not restart legacy writes after handoff without reconciliation. Safe namespace-aware v2 wrapper receipts keep original operation scope and return ACK when regenerated ETag differs. Page assignment/version/picker still need implementation. See full-product-audit-20261012/documents-tag-catalog-continuation-20261015.md.
