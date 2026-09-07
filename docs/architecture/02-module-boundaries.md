# Module boundaries and extension contract

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

## Proposed composition

| Area | Owns | May depend on | Must not depend on |
| --- | --- | --- | --- |
| Nexora.Host.Api / Host.Worker | Composition, transport/authentication, request/job dispatch | Platform contracts and module registration entry points | Business rules or direct domain SQL |
| Nexora.Platform.Contracts | Owner/access context, result/error, resource IDs, contribution contracts, event envelopes | Small stable primitives only | Any specific domain module |
| Nexora.Platform implementation | Identity, policy, files, notifications, lifecycle coordination and durable work | Platform contracts, infrastructure adapters | Task/Page/Finance internal entity classes |
| Nexora.Modules.<Name>.Contracts | Versioned command/query/projection DTOs and public interfaces | Platform contracts | EF entities/DbContext or another module implementation |
| Nexora.Modules.<Name> implementation | Domain invariants, handlers, own EF mappings/migrations | Own contracts and approved other-module Contracts | Other module DbContext/repository/tables |
| Frontend shell and shared UI | App shell, navigation registry, common UX patterns/access-mode banner | Versioned frontend contribution interfaces | Direct imports of another feature private store/components |
| Frontend feature package | Screens/forms/query keys/commands for module | Shared UI + generated approved contracts | Direct SQL, global personal-data bag, another feature internals |


Module names are organizational boundaries, not all40 FX IDs must each produce a DbContext: Projects/Tasks and tightly coupled productivity extensions can share Productivity boundary, Documents folders/pages/tags share Documents. Grouping must be documented in schema catalog; core never imports their entity classes. A logical FX can consume several platform providers without owning a table.

## Registration manifest — required fields

Stable moduleCode; display label/icon/group; version/platformCompatibility; hard/optional dependency contracts; supported resource types and field projection versions; declared action permission keys; frontend route/nav/quick-create contributors; query/search/favorite/dashboard contributors; notification/event/job/action contributors; file/reference/lifecycle/history handlers; settings schema; import/export capability and formats; ordered migration IDs/checksums; backup/account-deletion inventory handler; health checks and contract test suite.

Registration fails for duplicate keys/routes, missing required handler, invalid dependency range/cycle, incompatible schema or unknown access context. Disabled module returns ModuleUnavailable contribution result; no blank navigation element or shell crash. MigrationFailed module is not Ready even if deployed DLL/frontend bundle is present.

## Resource provider contracts

| Contract | Inputs / outputs | Mandatory guard |
| --- | --- | --- |
| ResolveSummary | AccessContext, typed ResourceRef → allowlisted summary + availability + sourceRevision | Owner/module/action/source lifecycle before any field/count |
| ResolveDetail | Context + ResourceRef + requested projection version → typed DTO | Sharing/support projections separate DTOs, never serialize full entity and hide fields client-side |
| ExecuteCommand | Self context, command DTO, ETag, idempotency key → resource/revision/outcome | Domain invariants and parent gate; no arbitrary method names |
| ValidateReference | Owner, target type/id/version, purpose → reference lease/guard or reason | Serialize against purge; exact version pin verified by provider |
| PreviewLifecycle / ApplyLifecycle | Typed root refs, expected revision → affected graph; confirmed operation → batch result | Preview not authorization; recheck under root lock; no cross-owner expansion |
| ProjectCalendar / Search / Dashboard | Owner-scoped query → source-authoritative or versioned safe read model | Staleness and unavailable state explicit; never mutate source |
| PrepareBackup / EnumerateOwnedData | Operator recovery context or approved deletion context → inventory references | No generic user export or secret data dump |
| MigrateContributionConfig | Old schemaVersion/config → validated new config or incompatibility | No arbitrary executable user migration payload |


## Extension compatibility

Contracts use stable semantic identifiers, not assembly-qualified runtime names stored as executable strings. Consumers support declared version ranges; incompatible providers fail at registration or bounded feature use. Additive optional fields can evolve within compatible version; removing/renaming field/action needs migration and major contract version. Core lifecycle/audit hooks stay platform-owned while domain supplies bounded preview/apply handlers.

## Enforcement tests specified

Compile-time references plus dependency tests prohibit forbidden assemblies, EF entity navigation across module internals, raw table name references and direct SQL from host/frontend. Each module's test kit must test owner isolation, permission/module disable, lifecycle, version/conflict, notification dedupe, migration/backfill and unavailable optional dependencies. These tests are not created/run yet.

## Normative action contract update — 2026-09-07

[Action Catalog](../action-catalog/README.md) supplies exact stable keys. [Context evaluation](../action-catalog/00-authorization-contract.md), [semantic diff/composition](../action-catalog/01-composition-and-field-guards.md), [revoke](../action-catalog/02-revocation-and-runtime.md) and [module contract](../action-catalog/04-module-action-contract.md) are required. Admin Self does not bypass an Admin Deny by retaining User role. SuperAdmin-only policy commands cannot be delegated via an Allow row. Every entry point, worker and field-diff wrapper revalidates the same source action. No reflection-based arbitrary handler dispatch, wildcard grant, automatic prerequisite grant or implementation is authorized by these documents.
