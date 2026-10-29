# FX04 — Owner disclosure preview before link creation

Source authority: main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`; implementation parent PR #4 `840e086da55864a90e9a1e1038267c5be2e2e42f`. Evidence is bound to its containing implementation commit.

Scope: FX04-S01, `sharing.link.read/create` composition and approved Project/Published Document v1 projections. This completes the missing preview interaction only, not the whole Sharing module or create/lost-ACK workflow.

## Production behavior

- Authenticated GET `/api/v1/sharing/preview?resourceType=Project|Document&resourceId=UUID` checks current live actor/PersonalSpace, separate Sharing create AND read and source read AND share permissions, source/module state and current Sharing policy inside Serializable. Only own Project or Published Document is eligible. Unsupported resource classes, cross-owner or unavailable sources fail closed.
- Reuses the exact existing provider-owned public-link projection adapters; all nonTrash Project Tasks, no child Documents/history/audit/reminders/private notes. The query creates no link, token, receipt or audit mutation. Its OwnerPreview mode grants no audience authority.
- Sharing form loads actual SQL-backed projection before enabling creation. Identity/version mismatch or error does not enable creation. The server still rechecks all creation authority independently; frontend preview is not authorization or a content snapshot promise.
- Owner and viewer use the same text-safe renderer, including Project priority/tags/IDs, Task IDs and Document type/editor mode/ID alongside existing fields. Owner embedding uses coherent heading levels. Preview content stays in component memory; no browser persistence.
- Resource changes and late responses are fenced by generation and loaded identity. Reload or known parent401/403/404/ModuleUnavailable clears ready and remounts preview; stale responses cannot revive it.

Production: `src/Nexora.Application/Sharing/SharingServiceContracts.cs`, `src/Nexora.Infrastructure/Sharing/SqlSharingService.cs`, `src/Nexora.Api/Features/Sharing/SharingEndpoints.cs`, `web/Nexora.Web/src/ShareDisclosurePreview.tsx`, `web/Nexora.Web/src/SharedProjectionContent.tsx`, `web/Nexora.Web/src/App.tsx`, `web/Nexora.Web/src/api.ts`. No new permissions, SQL schema/migration or activation policy.

## Actual evidence and incomplete behavior

- PASS: Release backend build on isolated publication snapshot,0 warnings/errors; frontend TypeScript/Vite build. Existing bundle-size warning remains.
- PASS: independent source review after correcting parent-denial preview invalidation and omitted disclosure fields; no functional runtime acceptance is implied.
- NOT_RUN: preview unit, SQL/API and browser acceptance, full regression. Automatic approval review rejected new preview tests based on the older AGENTS code-only amendment; explicit clarification against the latest user STEP4 request is pending. No test pass is fabricated.
- Implementation: IMPLEMENTED_UNVERIFIED. Functional acceptance: NOT_RUN. Security: source reviewed, runtime verification pending. Provider activation: NOT_APPLICABLE. Production readiness: not established.

Required next proof: exact Project/Task and Document projections, Draft/Archived creation denial, deleted/cross-owner sources, current role/module/action/session revocation, no link/token writes, late response/resource change/deny reset, real browser preview/create integration and regression.

Sharing25-row/filter implementation remains uncommitted locally. Its proposed Revoked filter acceptance failed because manual Revoke sets IsDeleted=1 while the action contract excludes deleted links; UX requires Revoked management filtering. A specific PO decision on an owner-only OwnerRevoked metadata projection versus retaining hidden revoked links is pending. This preview commit does not change that historical lifecycle, resurrect tokens or publish the unresolved filter behavior. Creation unknown-outcome handling remains incomplete outside this preview slice.
