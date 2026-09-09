# Product Owner decisions — implementation readiness 2026-09-09

> Current specification amendment. This document records Product Owner choices made during the 2026-09-09 implementation-readiness interview. It supersedes conflicting older proposal language only for the named capabilities below. Historical snapshots remain evidence, not implementation input.

## Source and authority

Product Owner response: agreed to the recommended options from the implementation-readiness interview covering approval scope, account deletion/recovery, MFA recovery, Vault, sensitive projections, Finance, Task/Reminder extensions, outbound behavior, paused modules and production sequencing.

These decisions are Product Owner decisions. They do not prove implementation, tests, runtime readiness, production suitability or security certification.

## Approved implementation boundary

`DEC-20260909-001` approves implementation of **M01 + backend/frontend scaffold + local development scripts** only.

Allowed within this approval:

- M01 stories S00–S11 under `docs/delivery/milestone-01/**`.
- Backend application scaffold for the approved slice.
- React frontend scaffold for the approved slice.
- Local runbook/script artifacts needed for doctor/configure/dependencies/migrate/seed/bootstrap/run/verify.
- Synthetic local data, local SQL Server, optional Redis cache and captured/simulated email/push adapters.
- Pull requests to `main` with traceability and evidence.

Not allowed by this approval:

- Production deployment, public endpoint launch, paid provider provisioning, domain purchase or production secrets.
- Full Release 1 implementation outside the approved slice.
- Files, Sharing, Support/Emergency, Vault, Finance, Productivity, Documents or other business modules unless a later bounded implementation approval names them.
- FX30 Price Tracking, FX34 Automation/Scheduler/Workflows or FX35 Integrations/Webhooks/n8n.
- Real outbound provider writes, OAuth, payments, user data import from third-party accounts or executable third-party modules.

## Decisions

| ID | Status | Decision | Implementation effect |
| --- | --- | --- | --- |
| DEC-20260909-001 | Approved | Start with M01 plus backend/frontend scaffold and local scripts. | Agents may implement the bounded M01 slice after reading current contracts; every PR must prove scope/evidence. |
| DEC-20260909-002 | Approved | Account deletion remains soft-delete. Deleted emails are not reused for a new owner. Restoration, when implemented, must restore the same account/UserId/PersonalSpace; password reset must not auto-reactivate deleted accounts. | Keep unfiltered normalized-email uniqueness including deleted rows. Do not create a new owner by reusing a deleted email. No purge path is approved. |
| DEC-20260909-003 | Approved | Google Authenticator/TOTP recovery uses one-time recovery codes generated at enrollment. Codes are shown once, stored only as hashes, and consumed with password proof for a limited recovery flow. Email + password alone must not reset MFA. If both TOTP device and recovery codes are lost, there is no self-service recovery path under this decision. | M01 remains password-only and must fail closed for MFA-enabled fixtures. Later MFA enrollment/recovery must implement recovery codes and session revocation. Manual support recovery would need a separate PO decision. |
| DEC-20260909-004 | Approved | Vault uses a hybrid recoverability model: owner-controlled encrypted portability plus server-assisted recovery workflow. Operators/SuperAdmin may authorize recovery but must not receive ambient plaintext or export another user's secrets. | Vault crypto/key design still requires technical ADR and security evidence. Support/Emergency must not decrypt or copy Vault plaintext. |
| DEC-20260909-005 | Approved | Sensitive share/support uses projection allowlists, not raw owner DTO serialization. Public sharing hides sensitive fields by default; owner must explicitly preview/select sensitive fields before any exposure. Support sees only safe metadata/error/debug state. Emergency remains read-only and cannot export/copy secrets. | Finance, Assets, Career, Learning and Vault must define safe projections before share/support release. Generic Admin access must not include personal domain payloads. |
| DEC-20260909-006 | Approved | Finance initial implementation scope is basic manual records: category, amount, explicit currency, date and optional note. Budget, debt, interest, FX, transfers, account ledger and other advanced finance semantics remain gated backlog requiring later approval. | Do not infer default currency from UI language. Do not implement advanced ledger/accounting behavior until separately approved. |
| DEC-20260909-007 | Approved | First Productivity implementation keeps flat Tasks: every Task belongs to one Project and may have one approved reminder. Subtasks, recurring tasks, snooze, standalone reminders and Task attachments are not part of the first Productivity slice. | Core Project/Task/Calendar rules remain valid. Extension behavior must not be added by speculation or hidden schema fields. |
| DEC-20260909-008 | Approved | News, GitHub Discovery and Monitoring may use backend read-only outbound access to public metadata/feeds after an approved implementation contract, with allowlist, SSRF protection, redirect limits, payload limits, timeouts, retry/rate limits and clear degraded states. Monitoring HTTP probing is allowed only after the owner configures a target. | This does not resume Price Tracking, Automation or Integrations. No OAuth/write/provider mutation/payment flow is approved. |
| DEC-20260909-009 | Approved | FX30 Price Tracking, FX34 Automation/Scheduler/Workflows and FX35 Integrations/Webhooks/n8n remain paused for M01/M02. They are not moved to R2 by this decision. | Allow/default-on/module catalog cannot override paused status. Resume requires a new explicit PO decision. |
| DEC-20260909-010 | Approved | Sequence production after Local Stable. No production provider, public launch, capacity guarantee, RPO/RTO or SLA is committed now. | Local evidence may inform later production planning, but cannot be reported as production readiness. |

## Updated gate effects

| Previous gate/proposal | Current effect after 2026-09-09 decision |
| --- | --- |
| P-H01 MFA lost-device recovery | Product policy resolved for recovery-code based recovery. Implementation remains outside M01 unless a later slice approves MFA enrollment/recovery. |
| P-H02 deleted account recovery/email reuse | Product policy resolved: same-account restore only, no email reuse, no purge. Self-service restore UI remains outside M01. |
| P-H03 sensitive share/support | Product policy resolved at allowlist/default-hidden level. Each sensitive module still needs concrete field projection contracts before implementation. |
| P-H04 Vault portability/recovery | Product policy resolved as hybrid/no-operator-plaintext. Crypto format, key wrapping and restore rehearsal are technical gates before Vault release. |
| P-H05 advanced Finance | Initial Finance scope resolved as basic manual records. Advanced finance remains gated and unapproved. |
| P-H06 Task extensions | Initial Productivity scope resolved as flat Tasks and one reminder. Extensions remain gated and unapproved. |
| P-H07 outbound ingestion | Resolved for read-only public News/GitHub/Monitoring access under strict network guards. Paused modules stay paused. |
| P-H08/Q-08 production capacity | Resolved for sequencing: Local Stable first; production capacity/SLA/RPO/RTO deferred until after Local Stable evidence. |

## Agent instructions

Implementation agents must bind every change to:

`DEC-20260909-001` → exact M01 story/action/acceptance → code path → test/evidence → PR revision.

Do not describe work as done unless code exists and the required tests/evidence actually ran on the reported revision. Missing application runtime remains `Not run`, not `Pass`.
