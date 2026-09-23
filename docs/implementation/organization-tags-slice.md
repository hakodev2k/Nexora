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
grant. SQL `rowversion` is the ETag authority. Audit rows contain action and
target metadata only. The `organization.ResourceTag` table is a future
provider-assignment boundary; this slice does not expose assignment writes.

## Explicit non-goals

`organization.tag.assign`, Documents tag delegation, Collections, Templates,
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
