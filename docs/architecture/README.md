# Nexora architecture design review

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Repository baseline is documentation-only. This review upgrades the **proposed architecture**, not existing application code. It keeps the chosen stack and personal-only model, and introduces enforceable module boundaries, owner-scoped contracts and transaction semantics derived from the database design.

| Document | Responsibility |
| --- | --- |
| [Review and ADRs](01-review-and-adrs.md) | Baseline gaps, retained decisions, technical resolutions and blocked product decisions |
| [Module boundaries](02-module-boundaries.md) | Assemblies, schemas, dependency direction, SDK/manifest contract |
| [Data transactions](03-data-transactions-and-consistency.md) | DbContexts, unit of work, projections, outbox, concurrency |
| [Authorization and secrets](04-authorization-and-sensitive-data.md) | Self/share/support/emergency, revocation, Vault/SSRF boundaries |
| [API and frontend contracts](05-api-and-frontend-contracts.md) | Routes versus endpoints, errors, idempotency, query state and modules |
| [Operations and verification](06-operations-and-verification.md) | Workers/cache/files/migrations/testing/readiness gates |


[Database](../design-database/README.md) · [UX/UI](../ux-ui/README.md) · [Cross-layer findings](../design-review/README.md)

Status: technical design reviewed at document level. Product gates Q-01…Q-12 remain open where applicable. Runtime/library versions beyond User's chosen stack must be pinned at implementation approval, not installed in this phase.

## Action catalog v1

[714 operation contracts /40 feature scopes](../action-catalog/README.md), [screen bindings](../action-catalog/06-screen-bindings.md), and [database binding](../design-database/16-action-catalog-binding.md). Docs-only; separate PO approval required before implementation.
