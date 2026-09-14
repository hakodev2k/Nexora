# DEC-20260909-014 — Full local E2E implementation approval

Status: **Approved by Product Owner**, source: the current instruction to continue existing PR #4 (`impl/m01-s00-scaffold`) toward the full local end-to-end Nexora application. This amendment supersedes the implementation-approval limits of DEC-20260909-001/011/012 and the local-code pause of DEC-20260909-009. It does not supersede documented business/security invariants or prove any runtime result.

- Full local E2E implementation of documented Release 1 phases/slices/modules is approved. Continue slice-by-slice beyond M01 without requesting repeated technical or slice approval.
- `DESIGN_RESOLVED_NOT_APPROVED_NOW` means approved for local implementation **when the exact API/DB/UX/AC/security/evidence contract is sufficient**. Complete missing contracts before coding.
- Previously paused modules, including Price Tracking, Automation and Integrations, may be implemented as local/simulated/integration-safe code. Their real-provider execution remains disabled and unapproved.
- Real provider calls, production deployment/public exposure, real secrets, real user data, paid services and external destructive actions are not approved. Config/credentials alone do not bypass a future explicit execution approval.
- Local SQL Server, synthetic fixtures, migrations, bootstrap, local workers and simulated transports are permitted. SQL is authority; Redis is cache. Simulation must be labelled and never reported as actual delivery/provider evidence.
- Keep feature-based Minimal API + Application/Domain/Infrastructure modular monolith. Milestones are evidence labels, not runtime folders.
- Existing Product Owner business rules remain authoritative: owner isolation, same-account restore/no deleted-email reuse, no Account/Vault purge, no operator plaintext, current state machines and format boundaries. A genuinely missing business decision is not resolved by this approval; work on independent contracted capabilities while recording the affected blocker.
- Keep all commits and evidence in PR #4; do not merge it. Report implemented/unimplemented scope and actual/not-run verification explicitly.

## Execution amendment for the current implementation run

The current Product Owner instruction further narrows this run to **code-only implementation**. The human owner handles functional testing, manual QA, test-data/mock-data design, fixture preparation, user acceptance testing and runtime verification. Agents may run compile/build/typecheck/lint commands only to catch implementation errors; they must not add new unit, integration, E2E/browser test suites, mock/demo records or artificial fixtures, and must report those checks as not performed unless an existing command was actually run.

This execution amendment does not reduce the approved product scope or relax any security, privacy, owner-isolation, provider or production gate. A real local operator bootstrap is allowed; a demo seed is not.

## Effective status overlay

This amendment takes precedence over the earlier action overlay, delivery gate register, goals snapshots and readiness statements that require future slice approval or prohibit local paused-module code. They remain evidence of the previous boundary. `SUPERSEDED` action keys remain retired; this amendment does not revive them. Production/network gates continue to prohibit real external execution. Policy/security contracts remain mandatory before the corresponding local feature is enabled.

`Approved locally` is not `implemented`, `verified locally`, `R1 complete` or `production approved`. A simulated provider may satisfy explicitly simulated acceptance only.
