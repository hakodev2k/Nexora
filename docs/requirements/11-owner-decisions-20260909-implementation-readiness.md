# Product Owner decisions — implementation readiness 2026-09-09

> Interview decision record. [DEC-20260909-014](12-owner-decisions-20260909-local-e2e.md) supersedes the earlier M01-only approval, separate slice reapproval and prohibition on local implementation of paused R1 modules. The earlier decisions below retain their business/security policies except where explicitly amended; their former approval restrictions are historical, not current implementation instructions.

## Source and authority

Product Owner response batch 1: agreed to the recommended options covering approval scope, account deletion/recovery, MFA recovery, Vault, sensitive projections, Finance, Task/Reminder extensions, outbound behavior, paused modules and production sequencing.

Product Owner response batch 2: selected `Q1:D`, `Q2:A`, `Q3:A`, `Q4:A`, `Q5:A`, `Q6:A`, then delegated the remaining block/pause decisions to the reviewer using the same recommended safe-default policy. This produced `DEC-20260909-011` through `DEC-20260909-013` and the effective action-status overlay.

These decisions are Product Owner decisions or Product Owner-delegated implementation-readiness decisions. They do not prove implementation, tests, runtime readiness, production suitability or security certification.

## Original implementation boundary — superseded by DEC-20260909-014

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
| DEC-20260909-005 | Approved | Sensitive share/support uses projection allowlists, not raw owner DTO serialization. Public sharing hides sensitive fields by default; owner must explicitly preview/select sensitive fields before any exposure. Support sees only safe metadata/error/debug state. Emergency remains read-only and cannot export/copy secrets. | Finance, Vault, Assets, Career, Learning, Digital Assets and Shopping require field-level projection contracts before share/support release. Generic Admin access must not include personal domain payloads. |
| DEC-20260909-006 | Approved | Finance initial implementation scope is basic manual records: category, amount, explicit currency, date and optional note. Budget, debt, interest, FX, transfers, account ledger and other advanced finance semantics remain gated backlog requiring later approval. | Do not infer default currency from UI language. Do not implement advanced ledger/accounting behavior until separately approved. |
| DEC-20260909-007 | Approved | First Productivity implementation keeps flat Tasks: every Task belongs to one Project and may have one approved reminder. Subtasks, recurring tasks, snooze, standalone reminders and Task attachments are not part of the first Productivity slice. | Core Project/Task/Calendar rules remain valid. Extension behavior must not be added by speculation or hidden schema fields. |
| DEC-20260909-008 | Approved | News, GitHub Discovery and Monitoring may use backend read-only outbound access to public metadata/feeds after an approved implementation contract, with allowlist, SSRF protection, redirect limits, payload limits, timeouts, retry/rate limits and clear degraded states. Monitoring HTTP probing is allowed only after the owner configures a target. | This does not approve Bookmarks/Digital/Toolbox outbound rows and does not resume Price Tracking, Automation or Integrations. No OAuth/write/provider mutation/payment flow is approved. |
| DEC-20260909-009 | Approved | FX30 Price Tracking, FX34 Automation/Scheduler/Workflows and FX35 Integrations/Webhooks/n8n remain in the R1 catalog but Product Owner-paused. They are not moved to R2 by this decision. | Allow/default-on/module catalog cannot override paused status. Resume requires a new explicit PO decision. |
| DEC-20260909-010 | Approved | Sequence production after Local Stable. No production provider, public launch, capacity guarantee, RPO/RTO or SLA is committed now. | Local evidence may inform later production planning, but cannot be reported as production readiness. |
| DEC-20260909-011 | Approved | After M01, implementation approval must be by small vertical slice, not by full R1, whole phase or automatic module sequence. | Preferred next-slice unit is a coherent vertical slice such as Projects + Tasks + Calendar foundation, not a broad all-modules implementation. |
| DEC-20260909-012 | Approved | A future slice is implementable only when PO approval plus API, DB, UX, acceptance, security/privacy and evidence contracts exist for the exact actions/stories. | Feature specs and action catalog rows alone are not enough to start code. Agents must not self-select or infer approval from R1 catalog membership. |
| DEC-20260909-013 | Approved | Do not use “Full R1 implementation-ready” as a Go state. Use slice-scoped readiness states. | Valid states are `SLICE_SPECIFIED`, `SLICE_READY_TO_APPROVE`, `SLICE_APPROVED_TO_IMPLEMENT`, `SLICE_IMPLEMENTED`, `SLICE_VERIFIED_LOCALLY` and later production Go/No-Go. |

## Interview gate effects — apply DEC-20260909-014 for current local approval

| Previous gate/proposal | Current effect after 2026-09-09 decision |
| --- | --- |
| P-H01 MFA lost-device recovery | Product policy resolved for recovery-code based recovery. Implementation remains outside M01 unless a later slice approves MFA enrollment/recovery. |
| P-H02 deleted account recovery/email reuse | Product policy resolved: same-account restore only, no email reuse, no purge. Self-service restore UI remains outside M01. |
| P-H03 sensitive share/support | Product policy resolved at allowlist/default-hidden level. Each sensitive module still needs concrete field projection contracts before implementation. |
| P-H04 Vault portability/recovery | Product policy resolved as hybrid/no-operator-plaintext. Crypto format, key wrapping and restore rehearsal are technical gates before Vault release. |
| P-H05 advanced Finance | Initial Finance scope resolved as basic manual records. Advanced finance remains gated and unapproved. |
| P-H06 Task extensions | Initial Productivity scope resolved as flat Tasks and one reminder. Extensions remain gated and unapproved. |
| P-H07 outbound ingestion | Resolved for read-only public News/GitHub/Monitoring access under strict network guards. Bookmarks/Digital/Toolbox outbound needs separate approval. Paused modules stay paused. |
| P-H08/Q-08 production capacity | Resolved for sequencing: Local Stable first; production capacity/SLA/RPO/RTO deferred until after Local Stable evidence. |
| Full R1 readiness ambiguity | Resolved: no full-R1 Go state. Only exact vertical slices can become ready/approved/implemented/verified. |
| Action-catalog wording drift | Resolved through [effective implementation status overlay](../action-catalog/09-effective-implementation-status-20260909.md). Module table wording from catalog v1.1 must be interpreted through that overlay until catalog regeneration. |

## Agent instructions

Implementation agents must bind every change to:

`DEC-20260909-014` (retaining M01 approval from 001) → exact story/action/acceptance → code path → test/evidence → PR revision.

For any future slice, agents must bind every change to:

`DEC-20260909-014` → exact story/action/acceptance/API/DB/UX/security contract → code path → test/evidence → PR revision. Separate PO approval is required only for unresolved business decisions or excluded external/production execution.

Do not describe work as done unless code exists and the required tests/evidence actually ran on the reported revision. Missing application runtime remains `Not run`, not `Pass`.
