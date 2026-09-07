# API and frontend implementation contracts — design only

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Route proposals versus contracts

UX routes in module specifications describe navigation, not existing backend endpoints. Backend uses versioned resource-specific REST/application commands with JSON error DTOs; approve OpenAPI when stories become Ready. API does not mirror every screen one-to-one. Query DTO includes data, safe capabilities, revision, pagination/freshness, not EF navigation graphs.

## Transport conventions

Authenticated scope is server derived. UTC timestamps ISO8601 Z, local dates unzoned, money/precise decimals strings plus explicit currency. Server validates required fields, lengths, enum codes, owner, immutable fields and status regardless of client. List requests specify bounded limit default25/max100, allowlisted filter/sort, stable cursor; client never requests unbounded personal-data dump. Bulk commands preview selected explicit IDs+revisions and return per-item outcome; only domain-defined aggregate operations are atomic.

| Outcome | Proposed status / code | Client behavior |
| --- | --- | --- |
| Authentication required | 401 AuthenticationRequired | Clear protected caches; preserve only authorized nonsensitive draft in current memory while reauth; no persistent secret draft. |
| Forbidden/module unavailable | 403 PermissionDenied or ModuleUnavailable | Clear affected data, show reason appropriate to owner; public share may use generic unavailable to avoid existence disclosure. |
| Unknown/hidden resource | 404 ResourceUnavailable | No private title/owner leak; Back to authorized origin. |
| Validation failure | 422 ValidationFailed + field errors | Keep values, summary links to fields, focus first invalid field after submit. |
| State/conflict | 409 InvalidTransition/ParentLocked; 412 RevisionConflict | Do not move card/overwrite form; show current safe state and compare/reload. |
| Missing concurrency proof | 428 PreconditionRequired | Reload revision before retry; never force-write fallback. |
| Idempotency mismatch | 409 IdempotencyConflict | Same key/different payload invalid; new edit requires new key. |
| Rate limit/provider unavailable | 429 with retryAfter; 503 ProviderUnavailable | Show when retry is allowed, retain labeled stale data only if still authorized. |
| Accepted asynchronous work | 202 OperationAccepted + operation reference | Show queued/pending, not completed; navigate operation status. |


## Frontend module architecture

React shell owns global navigation, command palette, access-mode banner, shared status/feedback/dialog primitives and capability registry. Feature owns its route tree/query keys/forms. Query keys include subject/Owner context, mode/grant ID, module/resource/filter/revision dimensions; never share owner cache with Support or link viewer. Logout/expiry/revoke purges protected memory cache. Persist only approved nonsensitive display preferences.

Use one query-state abstraction and one form-validation strategy chosen/pinned in implementation ADR. Do not add multiple competing libraries per module. Server responses are authoritative for lifecycle and transitions; UI operation metadata (canEdit/canShare/reasonRequired) supplements, not replaces, documented matrices. Register disabled module state without importing executable user bundles.

Explicit Save everywhere source requires it, especially Documents. Block editor and Markdown mode immutable. Unsaved indicator in title/header; navigation guard Save/Discard/Keep editing. Save success shows new version, handles unchanged distinct Save versus retry. Autosave, real-time coediting and local offline-secret persistence are excluded.

## Accessible interaction contracts

Shared dialog focus trap/return and combobox behavior follow [W3C modal dialog](https://www.w3.org/WAI/ARIA/apg/patterns/dialog-modal/) and [combobox](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) patterns; checked2026-09-07, documentation only. Drag has button/keyboard alternative. Mobile views use stack navigation, not hover-only actions; tables and charts expose full details/table equivalent. Concrete contracts are in [UX common screens](../ux-ui/global/15-screen-contracts.md).

## Frontend Definition of Ready

Screen ID, entry/back, fields/validation, source command/query, state/action matrix, error/read-only/disabled/conflict cases, mobile/keyboard behavior, source reference classification and relevant Q gates all traceable. A feature is not Ready merely because its Markdown has24 sections. Wireframe/mockup usability review and code approval remain separate steps.
