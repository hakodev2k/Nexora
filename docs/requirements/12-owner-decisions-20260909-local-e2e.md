# Product Owner amendment — full local Release 1 implementation

Date: 2026-09-09. Decision: **DEC-20260909-014 — Approved**.

Source: the Product Owner's explicit instruction to continue implementation on
`hakodev2k/Nexora` PR #4, branch `impl/m01-s00-scaffold`, toward the full documented
Release 1 local end-to-end application. Do not merge this PR.

## Approval and precedence

Full local E2E implementation is approved across all documented Release 1 phases,
slices and modules. Continue in dependency-ordered vertical slices beyond M01
without asking for approval again for each slice. This supersedes the M01-only
implementation boundary in DEC-20260909-001 and the separate-slice approval
restriction in DEC-20260909-011/012. Their traceability, contract and verification
requirements remain applicable.

`DESIGN_RESOLVED_NOT_APPROVED_NOW` actions may now be implemented locally when
their contracts are sufficient. Missing API, DB, UX, acceptance, security or
evidence contracts must be completed before the affected code. Technical choices
within approved behavior are delegated to engineering. Unresolved business
behavior must still be referred to the Product Owner; approval to implement does
not invent that behavior or revive retired actions.

DEC-20260909-009's pause is lifted for **local/simulated/integration-safe code** in
FX30 Price Tracking, FX34 Automation/Scheduler/Workflows and FX35
Integrations/Webhooks/n8n. The same local simulation boundary applies to other
provider-dependent capabilities. Real provider wiring stays disabled by default.
Simulated observations and deliveries must be visibly identified and cannot be
reported as real provider results. Local workers require durable SQL state,
bounded retries, leases, idempotency and current authorization checks.

The initial manual Finance and flat Task slices remain the first dependencies.
Documented extensions may follow under this approval after complete contracts;
unspecified accounting, recovery or lifecycle behavior is not implicitly decided.

## Explicit exclusions

Production deployment, public launch, real external-provider execution, real
secrets, paid services, real user data and external destructive actions remain
unapproved. Future real-provider execution requires explicit later approval and
approved configuration; merely finding a credential does not authorize execution.
Local synthetic SQL Server integration tests and local backup/restore workflows
are approved. Production capacity, provider, RPO/RTO/SLA and deployment gates remain.

Repository access for the explicitly requested commit/PR workflow is authorized;
this is distinct from Nexora application's provider execution.

## Delivery and acceptance

- Use feature-based ASP.NET Core Minimal API in a modular monolith. Milestone
  names are delivery/evidence labels, never runtime module folders.
- SQL Server is authoritative. Transitional memory stores cannot be final flows.
- Implement backend, application/domain logic, migration, usable React flow and
  meaningful tests for each slice, with goal → requirement → AC → action/operationId
  → code → test traceability.
- Preserve owner isolation, authorization, soft-delete/no deleted email reuse,
  CSRF, secure cookies, hashed one-time tokens, concurrency/idempotency,
  last-SuperAdmin protection, audit/outbox and Vault no-operator-plaintext rules.
- Keep PR #4 and implementation evidence accurate about implemented/unimplemented
  scope, commands run/not run, build/test/migration status, blockers and next dependency.
- Missing tools mean affected checks are `Not run`; code existence and approval
  do not establish local verification or Release 1 completion. Required independent
  review remains pending until a separate reviewer actually performs it.

## Effective status interpretation

| Earlier status | Current local effect |
| --- | --- |
| APPROVED_FOR_M01 | Existing approval retained; foundation contracts still apply. |
| DESIGN_RESOLVED_NOT_APPROVED_NOW | LOCAL_IMPLEMENTATION_APPROVED, subject to complete slice contracts. |
| PO_PAUSED | LOCAL_SIMULATION_APPROVED for documented R1 capabilities; real provider execution remains denied. |
| POLICY_APPROVED_IMPLEMENTATION_GATED | Approval gate satisfied locally; missing contracts/security evidence remain required. |
| SENSITIVE_PROJECTION_GATED | Field allowlists and tests must precede exposure. |
| NETWORK_GUARD_GATED | Local simulation approved; real outbound execution not approved. |
| PRODUCTION_OPS_GATED | Local synthetic workflows approved with contracts; production gate retained. |
| SUPERSEDED | Remains retired. |

This decision changes authority, not runtime readiness. Each slice retains its own
specified/implemented/verified status, and every committed capability requires
evidence before the full local objective can be declared complete.
