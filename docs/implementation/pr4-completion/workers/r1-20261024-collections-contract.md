# FX24 Collections delegated local contract — 2026-10-24

Owner: Collections implementation worker; integration and independent review: root coordinator.
Source: `impl/m01-s00-scaffold`, `14c76bcb6960e4ce53369dce388a8480b1c0299a`.
Authorization: current worker assignment and DEC-20260909-014; D01–D09=A, current AGENTS code-only amendment. No commit/push, tests, fixtures, runtime or central-file edits.

## Source and trace

FX24-S02/S03 implement `organization.collection.read/create/update/delete/add/remove/reorder` (each fully qualified with `organization.collection.`). Sources: complete FX24 feature/action/UX, shared behavior, P03-ORG-002, FX-24-BR-004/005, FX-24-AC-003. Goals NXG-FX24-G01/G03, NXG-P03-01, NXG-SYS-02/03/04/05/07/08/09/13/14. No template scope is claimed. D05/Q03 keeps collection sharing and Support/Emergency projection unavailable.

## API and authority

Owner SELF only; live verified identity/session, FX24 installed/Ready/system+user enabled and exact current action are checked under SQL Serializable transaction before and after operation. Update and all membership commands additionally require collection read. Admin uses existing explicit grants; no ambient cross-owner authority. Route/body never supplies OwnerId. Unknown request fields are rejected. CSRF applies to unsafe requests.

Base `/api/v1/organization/collections`: GET list (`query`, page default 1, fixed 25, Name/Id stable); POST create `{name,description}`. GET/PUT/DELETE `/{id}`. GET `/capabilities`. GET `/candidates?resourceType=&query=` returns bounded current readable source candidates. POST `/{id}/members` `{resourceType,resourceId}`; DELETE `/{id}/members/{memberId}`; PUT `/{id}/members/order` `{memberIds}`.

All mutations require UUID Idempotency-Key; all except create require exact quoted base64 8-byte rowversion If-Match (428 missing/malformed, 412 stale). Success uses metadata DTO or opaque committed ACK, never stored private DTO replay. Durable receipt binds owner, actor, exact action, target, revision and canonical body; same intent replay returns opaque ACK under current original action authority, changed intent 409. No duplicate business mutation/audit. Missing/inaccessible targets 404; denied current policy 403; validation 422; lifecycle/duplicate/source unavailable 409; persistence failure 503 with safe text.

## Sources and privacy

Version 1 explicit source participants: Project (FX11/projects.project.read), Document (FX20/documents.page.read), Bookmark (FX21/bookmarks.bookmark.read), Snippet (FX22/snippets.snippet.read). Participants own their SQL and return only id/type/title/status. Archived is eligible when source's own read contract allows it; Trash/purge/invisible parent denied. Documents inherit VisiblePageV1 and ancestor visibility. No body, URL, snippet text, version history or registry-derived authorization. Every add, candidate and member resolution rechecks current source authority in the same transaction. All other types are explicitly ineligible until a source participant is implemented.

Collection metadata lists carry no member count. Detail omits unavailable member refs entirely: no hidden IDs, titles, gaps, total or hidden count. Exposed ordering is a dense visible sequence. Reorder accepts exactly the currently visible member IDs once each, preserving hidden member slots internally; source revocation/lifecycle changes require reload rather than overwriting. Remove unlinks only the owner's membership. Delete removes the container and its references, never a source. No nesting or ownership transfer.

## SQL and concurrency

Migration `20261024_0090_organization_collections.sql` expands organization.Collection and CollectionMember only, plus exact permission status registration. Runtime refinement of the technical dictionary: collection's module is FX24, status Active; no archive/trash commands are invented. Typed refs are explicit (type,id), not arbitrary platform.Resource grants. No source FK: source purge must remain possible and stale refs resolve unavailable. Owner/collection composite FK, unique membership, nonnegative decimal rank, creator/updater attribution, rowversion, owner/Name/Id indexes. Container update lock serializes all membership changes; every successful member change updates parent revision. Serializable source reads hold lifecycle and authority locks until commit. Reorder does not publish physical rank.

Name trimmed 1–100; optional description trimmed <=4000; no duplicate-name prohibition. At most 500 members per collection and 25 candidate rows per request are delegated bounded operational limits, not unlimited capacity promises. No Redis, network, workers, source writes or projection caches. Audit contains action/actor/container ID only. Receipts contain only version/action/id.

## UX and verification plan

FX24-S02 browse: name search, 25/page, empty/no-match/loading/error, create/edit name+description. FX24-S03 detail: current readable members, typed candidate picker, Add/Remove, keyboard Move up/down with server persisted order, explicit delete-container confirmation, safe source-open callback, retry, stale-revision reload and retained draft. Dirty navigation warning. Capability-derived controls never replace server checks. No sensitive sharing controls. No browser persistence of private data.

Human acceptance scenarios (NOT RUN): cross-owner IDs and counts fail closed; current grant/session/source revoke denies; source Trash/ancestor hiding omits member; archived follows participant contract; duplicate add and same-key retry have one effect; stale ETag/reorder visible-set conflict preserves data; removing/deleting keeps source; keyboard/mobile/cancel/error flows; real SQL migration/constraints/concurrency. Build/typecheck only for implementation errors. Independent security/DB/cross-module review remains pending coordinator scheduling.

Rollback: stop mapping Collections endpoints/DI/navigation, retain new SQL tables and receipts. No automatic down migration or source changes; future removal requires owner-approved data preservation. Migration is not executed by this worker.

## Loaded instructions

AGENTS.md; .agents/skills/nexora-engineering/SKILL.md; implementation profile; Technical Lead README/core rules; .ai/routing.json baseline plus architecture/backend/frontend/security/database/owner-isolation route files; .ai/verification.md. Mandatory docs/README, delivery README/current scope, goals README/system/phase/module/template; DEC014 and current D01–D09 register. Contract sources listed above plus knowledge data dictionary. Missing phase-on-demand migration package is represented by explicit additive migration/rollback/review plan; no gate execution is claimed.
