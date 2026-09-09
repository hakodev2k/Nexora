# Nexora architecture design review

> Current specification · reconciled 2026-09-09. [Previous version](../history/20260908/snapshot/docs/architecture/README.md) is historical evidence, not implementation input.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation review only; no schema, migrations or application code were executed by that review. On 2026-09-09, Product Owner approved M01 + backend/frontend scaffold + local scripts for implementation through `DEC-20260909-001`; architecture conclusions outside that slice remain design authority, not runtime evidence.

Repository baseline was documentation-only. This review upgrades the **proposed architecture**, not existing application code. It keeps the chosen stack and personal-only model, and introduces enforceable module boundaries, owner-scoped contracts and transaction semantics derived from the database design.

| Document | Responsibility |
| --- | --- |
| [Review and ADRs](01-review-and-adrs.md) | Baseline gaps, retained decisions, technical resolutions and blocked product decisions |
| [Module boundaries](02-module-boundaries.md) | Assemblies, schemas, dependency direction, SDK/manifest contract |
| [Data transactions](03-data-transactions-and-consistency.md) | DbContexts, unit of work, projections, outbox, concurrency |
| [Authorization and secrets](04-authorization-and-sensitive-data.md) | Self/share/support/emergency, revocation, Vault/SSRF boundaries |
| [API and frontend contracts](05-api-and-frontend-contracts.md) | Routes versus endpoints, errors, idempotency, query state and modules |
| [Operations and verification](06-operations-and-verification.md) | Workers/cache/files/migrations/testing/readiness gates |

[Database](../design-database/README.md) · [UX/UI](../ux-ui/README.md) · [Cross-layer findings](../design-review/README.md)

Status: technical design reviewed at document level. Product gates are tracked in [Decision status](../features/90-open-decisions.md) and [PO decisions 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md). Runtime/library versions beyond User's chosen stack must be pinned at implementation approval/execution, not inferred from this document alone.

## Action catalog v1

[714 operation contracts /40 feature scopes](../action-catalog/README.md), [screen bindings](../action-catalog/06-screen-bindings.md), and [database binding](../design-database/16-action-catalog-binding.md). Docs/action contracts are design authority; implementation is currently approved only for the M01 package unless a later PO decision approves another slice.

## Current PO decision revision

[2026-09-09 implementation readiness](../requirements/11-owner-decisions-20260909-implementation-readiness.md) · [2026-09-07 decision source](../requirements/10-owner-decisions-20260907.md) · [Current Q status](../features/90-open-decisions.md) · [Physical delta:4new tables](../design-database/17-owner-decision-delta.md) · [Security/recovery ADR](07-owner-decisions-security-and-recovery.md) · [Capacity policy](08-capacity-and-verification-policy.md) · [Action catalog v1.1](../action-catalog/README.md). Historical181tables/197screens/714actions counts remain prior snapshots; current documented inventory includes185 table specs,202screens,733contracts with inactive scopes counted explicitly.

## Current milestone handoff

Start from [current delivery specification](../delivery/README.md). M01 scope, API/DB/UX/acceptance and evidence gates are linked there. A local/internal milestone is not Release1 completion. Scripts/migrations/runtime evidence are produced by implementation PRs, not by the architecture docs themselves.
