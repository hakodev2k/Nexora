# Nexora AI Engineering Adoption

**Upstream:** `hakodev2k/AI-Engineering`  
**Revision:** `a9f31a7a6a2f65ddc77c3791fde303f990f72d52`  
**Adoption date:** 2026-09-07

## Decision

Nexora uses a **single-owner implementation agent**: the complete upstream **Technical Lead** role package. Specialist expertise is composed from standalone Rules and Skills instead of loading many autonomous roles simultaneously.

This matches Nexora's current source of truth: Public SaaS, personal-only Release 1, .NET 10/ASP.NET Core, ReactJS, SQL Server, Redis, modular-monolith direction, strict owner isolation, read-only sharing/support boundaries, sensitive Vault data, durable background work and production-readiness requirements.

## Baseline selected now

### Role
- `Daily AI Role/technical-lead/` → complete package.

### Rules
Selected files from:
- `software-architect`: module boundary, data ownership, transaction consistency.
- `dotnet-backend-developer`: authorization, background processing, caching, EF Core, migration safety, security, testing.
- `react-developer`: accessibility, browser security, design system, form validation, testing.
- `security-engineer`: IAM, data protection, secrets management, threat modeling.
- `privacy-engineer`: personal-data access control.

### Skills
Selected procedures for:
- modular monolith and system boundaries;
- ASP.NET authentication/authorization, REST, secure input, EF Core, idempotency, distributed caching and testing;
- React component design, frontend security, accessibility and testing;
- SQL transaction/concurrency and zero-downtime migrations;
- threat modeling, secret/key management and security code review;
- QA test strategy and contract testing.

### Engineering controls
Two complete packages are baseline controls:
1. `agent-multi-tenant-data-boundary-gate` — directly protects Nexora's highest-priority invariant: no cross-user data access or mutation.
2. `agent-ground-truth-completion-gate` — requires fresh evidence before the implementation agent can claim work is complete.

## Phase-on-demand controls

Migration rollback, cache invalidation, job idempotency, timezone/DST, secret exposure, webhook replay, SSRF, upload/archive safety, API contract regression, test-evidence freshness and rollback-readiness gates are identified now but intentionally **not vendored yet**. They should be copied as complete packages when their corresponding implementation phase/change exists.

This keeps the baseline small while preserving an explicit hardening path.

## MCP/API decision

No MCP connector is copied into Nexora in this baseline. GitHub is necessary for implementation workflow and can be provided by the agent host/runtime with least privilege. Vendoring `MCP-API/github`, `sql-server`, `n8n` or another provider now would introduce credential/configuration and side-effect surfaces before Nexora has approved an agent runtime, allowlist, human-approval policy, logging/redaction, revocation and operational owner.

## Explicit exclusions

Not selected because current Nexora requirements exclude or defer them:
- AI/LLM/RAG/ML/model roles and controls;
- Kubernetes/Azure/AWS/provider-specific deployment roles;
- billing/payment roles/connectors;
- native mobile roles;
- team/workspace collaboration roles;
- executable third-party plugin/marketplace tooling.

Product Manager/Product Owner authority also remains human-owned through Nexora requirements and decision records; the implementation agent must not promote `TBD` or `PROPOSED` behavior to approved scope.

## Runtime entry points

1. `/AGENTS.md`
2. `/.ai/README.md`
3. `/.ai/roles/technical-lead/README.md`
4. `/.ai/profiles/nexora-implementation-agent.md`
5. Exact requirement / feature / UX files for the task.

The baseline is governance and implementation guidance only; it does not claim application tests have run while Nexora remains documentation-first.
