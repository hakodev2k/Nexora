# Nexora Implementation Agent

This file is the repository entry point for AI-assisted implementation work.

## Authority and source precedence

1. `docs/requirements/**` is the product and security source of truth.
2. Approved decisions/ADRs and phase gates refine implementation choices.
3. `docs/features/**` and `docs/ux-ui/**` define approved behavior and UX detail.
4. `.ai/rules/**`, `.ai/skills/**`, `.ai/controls/**`, and `.ai/guards/**` guide engineering execution only.
5. The upstream AI role defaults are lowest priority when they conflict with Nexora documentation.

Never turn `TBD`, `PROPOSED`, or an open decision into product behavior without the approval required by Nexora docs. AI Engineering assets do not grant authority to change scope.

## Primary role

Load `.ai/roles/technical-lead/README.md` as the primary operating role. The Technical Lead owns decomposition, delegation, review, verification, and handoff. Specialist rules, skills, controls, and guards are activated by the task profile in `.ai/profiles/nexora-implementation-agent.md`.

## Nexora invariants

- Target stack: .NET 10 / ASP.NET Core, ReactJS, SQL Server, Redis.
- Architecture direction: modular monolith; do not introduce microservices, brokers, cloud-specific topology, Kubernetes, or a search cluster without an approved ADR and requirement.
- SQL Server is authoritative persistent state. Redis is rebuildable cache only; never use it as the source of truth for grants, sessions, balances, notification jobs, or secrets.
- Every personal business record is owner-scoped. Cross-user reads, counts, search results, exports, files, and indirect references must fail closed unless a valid explicit access context exists.
- UI visibility is not authorization. Server-side authorization is mandatory for every protected operation.
- Support access is module-scoped read-only. Emergency access is break-glass, reasoned, audited, and read-only. Neither path may reveal/copy/export another user's Vault secrets.
- Background effects require durable state, bounded retries, lease/recovery semantics, authority re-checks, and idempotency.
- External/provider content is untrusted. Enforce SSRF, redirect, payload, sanitization, timeout, retry, and rate-limit boundaries.
- Secrets and sensitive personal data must not leak to source, logs, URLs, analytics, search projections, generic errors, test fixtures, or agent output.
- Calendar/reminder/scheduler behavior must be timezone- and DST-correct and test deterministic clock boundaries.
- Destructive or irreversible changes require explicit impact analysis and rollback/restore evidence.

## Execution workflow

1. Read the exact requirement/feature/UX sources for the task and record relevant IDs plus open decisions.
2. Classify the task and load only the matching specialist rules/skills/controls.
3. Produce a bounded implementation plan with architecture, data, security, test, migration, and rollback impact.
4. Implement the smallest coherent vertical slice. Do not add speculative abstractions or unrelated refactors.
5. Run focused verification first, then affected regression gates. Evidence must correspond to the final commit state.
6. Perform independent review for security-sensitive, migration, authorization, background-job, caching, or cross-module changes.
7. Update docs/traceability when an implementation decision or approved ADR requires it.
8. Open a PR to `main`; never bypass required review by writing directly to `main`.

## Approval boundaries

Do not autonomously:
- change confirmed product requirements or acceptance criteria;
- approve an open product/security decision;
- deploy production, provision paid services, acquire domains, or publish public endpoints;
- use production data or secrets;
- perform destructive production SQL/file/key operations;
- weaken authentication, authorization, audit, backup, encryption, or release gates;
- accept Critical/High residual security risk.

## External capabilities

GitHub repository access is required for branch/commit/PR workflows and must use the host/runtime connector with least privilege. A vendored GitHub MCP server is intentionally not included until Nexora has an approved agent runtime, credential storage model, allowlist, logging, revocation, and approval policy.
