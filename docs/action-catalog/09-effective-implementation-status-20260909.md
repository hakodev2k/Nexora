# Effective action implementation status — 2026-09-09

Docs-only normalization layer. This file is the current implementation-status overlay for the action catalog after the Product Owner delegated the recommended action decisions on 2026-09-09.

It authorizes no application code, migration, runtime test, provider call, production deployment, production data access or secret access by itself.

## Authority

This file applies these current decisions:

- `DEC-20260909-001`: M01 S00-S11 + backend scaffold + React frontend scaffold + local scripts are approved for implementation.
- `DEC-20260909-002` through `DEC-20260909-010`: account deletion, TOTP recovery-code policy, Vault hybrid policy, sensitive projection policy, Finance initial scope, Task extension policy, outbound policy, paused modules and production sequencing.
- `DEC-20260909-011`: after M01, implementation approval is by small vertical slice, not by full R1 or whole module batch.
- `DEC-20260909-012`: a future slice is implementable only when PO approval plus API, DB, UX, acceptance, security/privacy and evidence contracts exist.
- `DEC-20260909-013`: do not use “Full R1 implementation-ready” as a Go state. Use exact `SLICE_READY_TO_IMPLEMENT`, `SLICE_APPROVED_TO_IMPLEMENT`, `SLICE_IMPLEMENTED`, `SLICE_VERIFIED_LOCALLY` and later production Go/No-Go states.

If a module table row still says `Resolved delegated`, `Blocked Q-*`, `DEP-EXT-01`, `Paused`, or `Docs-only; no implementation approved`, the effective implementation status is determined by this file plus `docs/delivery/04-paused-blocked-gate-register.md`.

## Global row normalization rule

Every action row falls into exactly one effective implementation state:

| State | How to apply |
| --- | --- |
| `APPROVED_FOR_M01` | Exact action is listed in the M01 approved set below. It may be implemented now only inside the M01 package and must still produce runtime evidence. |
| `DESIGN_RESOLVED_NOT_APPROVED_NOW` | The action design exists, but current approval does not include implementation. This is the default for all non-M01 resolved rows not listed under another state. |
| `POLICY_APPROVED_IMPLEMENTATION_GATED` | Product policy is decided, but implementation still needs a future slice/ADR/API/DB/UX/security/evidence contract. |
| `SENSITIVE_PROJECTION_GATED` | Sensitive share/support policy is approved, but field-level projection allowlists and tests do not exist yet for that resource. |
| `NETWORK_GUARD_GATED` | Named read-only outbound behavior is allowed only after a future slice defines network guards and evidence. |
| `PRODUCTION_OPS_GATED` | The row depends on provider/capacity/backup/restore/RPO/RTO/SLA or production Go/No-Go. |
| `PO_PAUSED` | Product Owner intentionally paused the capability. No UI, handler, worker, provider call or default enablement. |
| `SUPERSEDED` | Historical key retained for traceability only. Do not create UI, handler, migration target or tests for the old key except denial/absence checks. |

Rows not named in this file inherit `DESIGN_RESOLVED_NOT_APPROVED_NOW`, unless they are part of the exact M01 approved action set.

## `APPROVED_FOR_M01` action set

Only these catalog actions are approved for implementation now, and only for M01 local-first scope.

| FX | Actions |
| --- | --- |
| FX01 Identity/Profile | `identity.account.register`, `identity.account.verify`, `identity.account.resend`, `identity.account.login`, `identity.account.reset_request`, `identity.account.reset_confirm`, `identity.session.logout`, `identity.session.read`, `identity.session.revoke_session`, `identity.session.revoke_all`, `identity.profile.read`, `identity.profile.update` |
| FX02 Users/Roles/Permissions | `access.user.read`, `access.permission.read`, `access.change.read`, `access.role.set`, `access.permission.set`, `access.entitlement.set` |
| FX03 Module Platform | `modules.catalog.read`, `modules.policy.enable`, `modules.policy.disable`, `modules.policy.defaults` |
| FX06 Notifications foundation | `notifications.dispatch.publish`, `notifications.dispatch.deliver` |
| FX09 Settings/Profile preference | `settings.preference.read`, `settings.preference.update` |

Notes:

- M01 also includes operator/developer work that has no user action key: S00 toolchain/runbook, S01 bootstrap SuperAdmin, S10 audit/outbox/jobs foundation and S11 evidence/restore rehearsal.
- M01 does not approve full Notification Center UI, full module settings, Files, Sharing, Support/Emergency, Vault, Finance, Projects, Tasks, Calendar, Documents, News/GitHub/Monitoring ingestion, Price Tracking, Automation or Integrations.
- `identity.account.soft_delete`, `identity.profile.change_email`, `identity.profile.change_password`, `access.user.disable`, `access.user.enable`, `access.user.revoke_sessions`, `modules.policy.sharing`, `modules.policy.settings`, `modules.runtime.register`, `modules.runtime.migrate`, `modules.runtime.health`, `settings.module.read`, `settings.module.update`, and `notifications.inbox.*` remain `DESIGN_RESOLVED_NOT_APPROVED_NOW` unless a later slice approves them.

## `PO_PAUSED` rows

### FX30 Price Tracking — R1 committed but paused

`prices.tracker.read`, `prices.tracker.create`, `prices.tracker.update`, `prices.tracker.pause`, `prices.tracker.resume`, `prices.tracker.remove`, `prices.tracker.refresh`, `prices.observation.read`, `prices.alert.read`, `prices.alert.create`, `prices.alert.update`, `prices.alert.enable`, `prices.alert.disable`, `prices.alert.remove`, `prices.delivery.read`, `prices.worker.fetch`, `prices.worker.evaluate`, `prices.support.read`.

Decision: keep in R1 catalog, but no implementation, worker, provider call, support projection, alert evaluation or default enablement until explicit PO resume.

### FX34 Automation/Scheduler/Workflows — R1 committed but paused

`automation.definition.read`, `automation.definition.create`, `automation.definition.save`, `automation.definition.validate`, `automation.definition.enable`, `automation.definition.disable`, `automation.definition.trash`, `automation.definition.restore`, `automation.definition.purge`, `automation.definition.history`, `automation.run.read`, `automation.run.start`, `automation.run.cancel`, `automation.run.retry_step`, `automation.run.dry_run`, `automation.schedule.update`, `automation.definition.import`, `automation.definition.export`, `automation.step.dispatch`, `automation.support.read`.

Decision: keep in R1 catalog, but no implementation, runtime, scheduler, dry-run, dispatch worker, definition import/export, n8n bridge or support projection until explicit PO resume.

### FX35 Integrations/Webhooks/n8n — R1 committed but paused

`integrations.connection.read`, `integrations.connection.create`, `integrations.connection.update`, `integrations.connection.test`, `integrations.connection.disable`, `integrations.connection.scopes`, `integrations.connection.credential`, `integrations.webhook.read`, `integrations.webhook.create`, `integrations.webhook.update`, `integrations.webhook.enable`, `integrations.webhook.disable`, `integrations.webhook.test`, `integrations.webhook.rotate`, `integrations.delivery.read`, `integrations.delivery.retry`, `integrations.system_connection.read`, `integrations.system_connection.configure`, `integrations.worker.receive`, `integrations.worker.send`, `integrations.support.read`.

Decision: keep in R1 catalog, but no OAuth, connection, credential, inbound/outbound webhook, delivery retry, n8n, provider write or worker until explicit PO resume.

### FX32 Developer Toolbox network tools

`toolbox.network.http`, `toolbox.network.dns`.

Decision: keep inactive under the integration/network boundary. Local pure tools may remain designed, but arbitrary HTTP/DNS execution needs separate PO/network approval.

## `NETWORK_GUARD_GATED` rows

Read-only outbound policy is approved only for News, GitHub Discovery and Monitoring after a future slice defines network guards and evidence.

| Area | Actions | Required before implementation |
| --- | --- | --- |
| FX29 News/Feeds | `news.source.refresh`, `news.worker.fetch` | Future News slice approval; allowlist; SSRF protection; redirect/payload limits; timeout/retry/rate-limit policy; parser/degraded-state evidence. |
| FX33 GitHub Discovery | `github.repository.search`, `github.repository.refresh`, `github.query.run`, `github.snapshot.capture`, `github.provider.fetch` | Future GitHub Discovery slice approval; public metadata only; no OAuth, star/fork/issues/write; allowlist/rate-limit/cache/freshness/degraded-state evidence. |
| FX36 Monitoring | `monitoring.monitor.check`, `monitoring.probe.run` | Future Monitoring slice approval; owner-configured target; allowlist or target validation; SSRF/redirect/payload/timeout/rate-limit guards; safe observation evidence. |

No News/GitHub/Monitoring outbound row is part of M01.

## Outbound rows not covered by the 2026-09-09 read-only outbound approval

| Actions | Effective state | Decision |
| --- | --- | --- |
| `bookmarks.bookmark.refresh` | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Bookmark URL metadata refresh needs a separate slice/PO decision. Do not fetch or overwrite URL metadata by default. |
| `digital.observation.inspect` | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Domain/TLS public observation needs separate slice/PO decision and network guard contract. |
| `toolbox.network.http`, `toolbox.network.dns` | `PO_PAUSED` | Arbitrary network tools remain inactive under the integration/network pause. |

## `SENSITIVE_PROJECTION_GATED` rows

Sensitive share/support policy is decided: default hidden, projection allowlist, explicit owner preview/selection, support metadata only, no operator plaintext/secrets/export. These rows still need concrete field-level projection contracts and tests before implementation.

| Area | Actions |
| --- | --- |
| FX24 Organization collections | `organization.collection.share` |
| FX27 Finance | `finance.report.share`, `finance.support.read` |
| FX28 Vault | `vault.support.read` |
| FX31 Shopping | `shopping.order.share`, `shopping.support.read` |
| FX37 Assets | `assets.asset.share`, `assets.support.read` |
| FX38 Digital Assets | `digital.asset.share`, `digital.support.read` |
| FX39 Career | `career.resume.share`, `career.support.read` |
| FX40 Learning | `learning.certification.share`, `learning.support.read` |

Any additional future share/support row for Finance, Vault, Assets, Career, Learning, Digital Assets or Shopping inherits `SENSITIVE_PROJECTION_GATED` until its field contract exists.

## `POLICY_APPROVED_IMPLEMENTATION_GATED` rows

### Identity/MFA

`identity.mfa.enroll`, `identity.mfa.remove`, `identity.mfa.recover`.

Decision: TOTP + one-time recovery-code policy is approved, but MFA implementation is outside M01. A future MFA slice must define DB/API/UX/recovery-code hashing/session-revocation/security evidence before implementation.

### Deleted account restore

No restore UI/API is approved in M01.

Decision: same-account restore, no email reuse and no purge are approved. A future restore slice must define the API/UX/security/evidence. Until then, deleted accounts stay denied and cannot be reset into active state.

### Advanced Finance

`finance.account.read`, `finance.account.create`, `finance.account.update`, `finance.account.close`, `finance.transaction.read`, `finance.transaction.post`, `finance.transaction.correct`, `finance.transaction.void`, `finance.transaction.split`, `finance.transfer.post`, `finance.category.read`, `finance.category.create`, `finance.category.update`, `finance.category.merge`, `finance.category.remove`, `finance.bill.read`, `finance.bill.create`, `finance.bill.update`, `finance.bill.cancel`, `finance.bill.record_payment`, `finance.bill.void_payment`, `finance.subscription.read`, `finance.subscription.create`, `finance.subscription.update`, `finance.subscription.pause`, `finance.subscription.cancel`, `finance.subscription.record_price`, `finance.budget.read`, `finance.budget.create`, `finance.budget.update`, `finance.savings.read`, `finance.savings.create`, `finance.savings.update`, `finance.savings.record_progress`, `finance.debt.read`, `finance.debt.create`, `finance.debt.update`, `finance.debt.record_payment`, `finance.debt.adjust_interest`, `finance.report.read`, `finance.csv.preview`, `finance.csv.import`, `finance.csv.export`.

Decision: initial Finance is only manual records with category, amount, explicit currency, date and optional note. Advanced ledger/account/transfer/bill/subscription/budget/savings/debt/interest/FX/report/CSV semantics need later PO approval and contracts.

### Cross-module dependency rows

| Actions | Gate |
| --- | --- |
| `shopping.order.link_finance` | Future Finance slice/reference semantics. Must not auto-write ledger/accounting. |
| `digital.credential.reference` | Future Vault credential-reference slice. Must not reveal Vault plaintext from Digital Assets. |
| `assets.serial.reveal`, `assets.serial.copy` | Future Assets sensitive owner reveal/copy slice. Requires recent-auth and no-search/no-share/no-log guarantees. |

### Vault crypto/recovery implementation

Vault hybrid/no-operator-plaintext policy is approved, but Vault implementation remains gated by crypto/key/package ADR, threat review, restore/import/export rehearsal and slice approval. Non-M01 Vault rows default to `DESIGN_RESOLVED_NOT_APPROVED_NOW` unless listed as `SENSITIVE_PROJECTION_GATED` or `SUPERSEDED`.

## `PRODUCTION_OPS_GATED` rows

`transfer.backup.read`, `transfer.backup.request`, `transfer.restore.preview`, `transfer.restore.request`, `transfer.worker.backup`, `transfer.worker.restore`.

Decision: operational backup/restore depends on Local Stable first, then provider/storage/capacity/RPO/RTO/SLA/key-recovery and isolated restore Go/No-Go. No production backup/restore row is implementable in M01.

## `SUPERSEDED` rows

| Area | Actions | Replacement / reason |
| --- | --- | --- |
| FX01 Account delete request | `identity.account.delete_request` | Replaced by `identity.account.soft_delete`; no delayed purge proposal. |
| FX21 External bookmark open | `bookmarks.bookmark.open_external` | External navigation excluded; URL remains inert metadata. |
| FX28 Vault owner restore/purge/version restore | `vault.item.restore`, `vault.item.purge`, `vault.item.restore_version` | Replaced by SuperAdmin-authorized Vault recovery model; no owner purge/restore path. |
| FX39 Standalone interview actions | `career.interview.read`, `career.interview.create`, `career.interview.update`, `career.interview.complete`, `career.interview.cancel`, `career.interview.link_calendar` | Replaced by `career.appointment.*` linked to internal Calendar Personal Event. |

## Default for all remaining non-M01 action rows

All other action rows, including rows whose module table says `Resolved delegated`, are `DESIGN_RESOLVED_NOT_APPROVED_NOW` until a future vertical slice explicitly approves them.

This includes but is not limited to Projects, Tasks, Calendar, Reminders, Planner, Goals, Habits, Time Tracking, Focus, Documents, Files, Sharing, Support/Emergency, Read Later, Snippets, Dashboard, Shopping manual records, Developer Toolbox local tools, Finance manual records, Career, Learning and other non-M01 actions.

## Full R1 readiness rule

Do not use `Full R1 implementation-ready` as an approval or Go state.

Valid states are slice-scoped:

1. `SLICE_SPECIFIED` — requirements/action/API/DB/UX/AC are written.
2. `SLICE_READY_TO_APPROVE` — open decisions are closed enough for a bounded slice.
3. `SLICE_APPROVED_TO_IMPLEMENT` — Product Owner explicitly approves the exact slice.
4. `SLICE_IMPLEMENTED` — code exists and is merged subject to process.
5. `SLICE_VERIFIED_LOCALLY` — runtime evidence actually passes for that slice.
6. `PRODUCTION_GO_APPROVED` — later provider/capacity/security/ops Go/No-Go approves public release.

A future slice may be approved only when it names exact modules/actions/stories and includes API, DB, UX, acceptance, security/privacy and evidence contracts. Agents must not self-select the next slice or infer approval from R1 catalog membership.
