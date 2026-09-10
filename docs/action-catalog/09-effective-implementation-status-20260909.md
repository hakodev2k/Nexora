# Effective action implementation status — 2026-09-09

> **Current local implementation approval:** [DEC-20260909-014](../requirements/12-owner-decisions-local-e2e-implementation.md) supersedes older M01-only/future-slice approval and local-code pause statements below. Full local E2E is approved with contracts first; real providers/production remain unapproved. Business rules and retired actions are unchanged.


Current implementation-status overlay for the action catalog. `DEC-20260909-014` supersedes the old M01-only/future-slice approval for local code; it does not authorize production/provider execution.

It authorizes no production/provider call, production deployment, production data access or secret access by itself. Local implementation still requires a sufficient API/DB/UX/acceptance/security/evidence contract. The current run is code-only and does not add test/mock/demo data.

Current PR #4 slice overlay: FX16 numeric Goals is now implemented locally for
`goals.goal.read`, `goals.goal.create`, `goals.goal.update`,
`goals.goal.start`, `goals.goal.complete`, `goals.goal.abandon`,
`goals.goal.reopen`, `goals.target.read`, `goals.target.create` and
`goals.target.record_progress`. These rows remain owner/module/action gated and
are not runtime-verified in this code-only environment. Boolean/Tasks targets,
archive/trash/history, reminders, support and provider rows remain gated.

The same PR also implements the bounded FX26-S01 read-only Dashboard attention
projection for `dashboard.dashboard.read`. Four source widgets perform current
module/action checks and return independent Ready/Empty/Unavailable/Degraded
states; layout writes, widget refresh mutation and quick-create remain gated.

## Authority

This file applies these current decisions:

- `DEC-20260909-001`: M01 S00-S11 + backend scaffold + React frontend scaffold + local scripts are approved for implementation.
- `DEC-20260909-002` through `DEC-20260909-010`: account deletion, TOTP recovery-code policy, Vault hybrid policy, sensitive projection policy, Finance initial scope, Task extension policy, outbound policy, paused modules and production sequencing.
- `DEC-20260909-011`/`012`: historical contract-first sequencing remains applicable; DEC-014 removes the need for repeated PO approval once the exact contract is sufficient.
- `DEC-20260909-013`: do not use “Full R1 implementation-ready” as a Go state. Use exact `SLICE_READY_TO_IMPLEMENT`, `SLICE_APPROVED_TO_IMPLEMENT`, `SLICE_IMPLEMENTED`, `SLICE_VERIFIED_LOCALLY` and later production Go/No-Go states.

If a module table row still says `Resolved delegated`, `Blocked Q-*`, `DEP-EXT-01`, `Paused`, or `Docs-only; no implementation approved`, the effective implementation status is determined by this file plus `docs/delivery/04-paused-blocked-gate-register.md`.

## Global row normalization rule

Every action row falls into exactly one effective implementation state:

| State | How to apply |
| --- | --- |
| `APPROVED_FOR_M01` | Exact action is listed in the M01 approved set below. It may be implemented now only inside the M01 package and must still produce runtime evidence. |
| `DESIGN_RESOLVED_NOT_APPROVED_NOW` | The design exists but its exact contract is insufficient for local implementation. Complete API/DB/UX/acceptance/security/evidence inputs first; DEC-014 then permits local code without another slice approval. |
| `POLICY_APPROVED_IMPLEMENTATION_GATED` | Product policy is decided, but implementation still needs a future slice/ADR/API/DB/UX/security/evidence contract. |
| `SENSITIVE_PROJECTION_GATED` | Sensitive share/support policy is approved, but field-level projection allowlists and tests do not exist yet for that resource. |
| `NETWORK_GUARD_GATED` | Named read-only outbound behavior is allowed only after a future slice defines network guards and evidence. |
| `PRODUCTION_OPS_GATED` | The row depends on provider/capacity/backup/restore/RPO/RTO/SLA or production Go/No-Go. |
| `PO_PAUSED` | Product Owner paused real/provider execution. Local/simulated/integration-safe code may exist under DEC-014, but no real provider call or default enablement is allowed. |
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
- M01 API operationIds `getCsrf` and `reauth` are approved M01 control endpoints even though catalog v1.1 has no standalone action keys for them. `getCsrf` grants no user authority; `reauth` refreshes recent-auth proof under the identity/session control boundary.
- M01 did not approve full Notification Center UI, full module settings, Files, Sharing, Support/Emergency, Vault, Finance, Projects, Tasks, Calendar, Documents, News/GitHub/Monitoring ingestion, Price Tracking, Automation or Integrations. This is the historical M01 baseline; DEC-014 and the current PR #4 slice overlays below supersede it for local code when a concrete contract is present.
- `identity.account.soft_delete`, `identity.profile.change_email`, `identity.profile.change_password`, `access.user.enable`, `access.user.revoke_sessions`, `modules.policy.sharing`, `modules.policy.settings`, `modules.runtime.register`, `modules.runtime.migrate`, `modules.runtime.health` and `settings.module.read`/`settings.module.update` remain contract-gated. `access.user.disable`, `notifications.inbox.*` and the implemented Projects/Tasks/Calendar/Documents/Finance-manual action subsets are permitted only within their documented local slices.

Implementation amendment: DEC-20260909-014 now permits the local-safe
`access.user.disable` operation and the initial owner-scoped Projects/Tasks/
Calendar slice when their concrete contracts are present. The runtime status is
still slice-scoped; this does not make the remaining advanced lifecycle,
history, ICS, sharing or provider actions implemented.

Current PR #4 implementation overlay: the same decision also permits the
owner-scoped Notification inbox, Trash lifecycle, Settings preferences and the
Documents/Notes/Knowledge page core when their contracts are present. The
implemented document action subset is `documents.library.read`,
`documents.page.read`, `documents.page.create`, `documents.page.save`,
`documents.page.publish`, `documents.page.unpublish`,
`documents.page.archive` and `documents.page.unarchive`. This overlay records
implementation authority only; each slice remains separately labelled
`SLICE_IMPLEMENTED` or `SLICE_VERIFIED_LOCALLY` by its evidence document.

The current PR #4 Finance overlay permits only the contracted manual-record
subset: `finance.manual_category.read`, `finance.manual_category.create`,
`finance.manual_category.update`, `finance.manual_category.remove`,
`finance.manual_record.read`, `finance.manual_record.create`,
`finance.manual_record.update` and `finance.manual_summary.read`. The
owner-scoped SQL schema, API and React flow are labelled `SLICE_IMPLEMENTED`
in `docs/implementation/finance-manual-records-slice.md`; advanced ledger,
account, transfer, bill, budget, report, CSV and sensitive share/support rows
remain gated below.

The current PR #4 Bookmarks overlay permits only the manual metadata subset:
`bookmarks.bookmark.read`, `bookmarks.bookmark.create`,
`bookmarks.bookmark.update`, `bookmarks.bookmark.archive` and
`bookmarks.bookmark.unarchive`. The owner-scoped SQL schema, inert URL boundary,
API and React flow are labelled `SLICE_IMPLEMENTED` in
`docs/implementation/bookmarks-manual-slice.md`; refresh, external navigation,
tags, collections, Trash, sharing/support and provider rows remain gated.

The current PR #4 Snippets overlay permits the text/version subset:
`snippets.snippet.read`, `snippets.snippet.create`, `snippets.snippet.save`,
`snippets.snippet.archive` and `snippets.snippet.unarchive`. The owner-scoped
SQL current/version tables, escaped source boundary, API and React flow are
labelled `SLICE_IMPLEMENTED` in `docs/implementation/snippets-text-slice.md`;
history/diff/restore, export, tags/templates, Trash and sharing/support rows
remain gated. Explicit local Copy does not execute or persist source.

The current PR #4 Read Later overlay permits the Bookmark-reference subset:
`reading.queue.read`, `reading.item.save`, `reading.item.remove`,
`reading.item.read`, `reading.item.unread` and `reading.item.position`. The
owner-scoped SQL queue, safe snapshot/source-availability boundary, API and
React flow are labelled `SLICE_IMPLEMENTED` in
`docs/implementation/read-later-bookmark-slice.md`; News/body reader,
cross-module News state, search/tags, sharing/support and advanced lifecycle
rows remain gated. Position is explicit metadata only and never an inferred
reading percentage.

The current PR #4 FX24 overlay permits the owner-scoped Tag catalog subset:
`organization.tag.read`, `organization.tag.create`, `organization.tag.rename`
and `organization.tag.remove`. The SQL/API/React flow is labelled
`SLICE_IMPLEMENTED (local)` in
`docs/implementation/organization-tags-slice.md`. Only the local provider
namespaces (`projects`, `documents`, `bookmarks`, `snippets`) are accepted.
Assignment, Collections, Templates and sharing remain gated; a tag never
grants access or ownership.

The local implementation evaluates the current `platform.Module`,
`platform.UserModuleGrant`, `platform.Permission` and `platform.AdminPermission`
rows for each protected SELF request. Admin SELF access is default-deny and
requires an explicit resolved `Allow`; a matching `Deny` wins. Canonical
resource-qualified keys for the implemented Projects, Tasks, Calendar,
Notifications and Trash operations are registered by migration `0007`.
Legacy compact keys remain accepted only as a compatibility bridge for existing
local grant rows and are not a new catalog authority.

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

The current PR #4 overlay implements the bounded local subset:
`toolbox.catalog.read`, `toolbox.base64.run`, `toolbox.url_codec.run`,
`toolbox.html_codec.run`, `toolbox.hash.run`, `toolbox.uuid.run`,
`toolbox.password.run`, `toolbox.json.run` and `toolbox.regex.run`. These
operations are memory-only, server-gated by FX32 and never execute input or
contact a provider. XML/YAML/CSV conversion, advanced formatters, QR,
certificates, history/favorites, Save-to-Snippet and network rows remain gated.

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

## Default for all remaining action rows

All other action rows, including rows whose module table says `Resolved delegated`, remain contract-gated until their exact API/DB/UX/acceptance/security/evidence package is complete. Once that package is sufficient, DEC-20260909-014 permits local implementation without another PO approval; production/provider execution remains separately gated.

This includes but is not limited to Reminders, Planner, advanced Goals targets/archive/trash/history, Habits, Time Tracking, Focus, Files, Sharing, Support/Emergency, Read Later News/body-reader/search/advanced rows, Snippet history/diff/restore/export/tags, Dashboard layout/widget mutation and quick-create, Shopping manual records, Developer Toolbox advanced/history/network rows, advanced Finance/Vault, Career, Learning and other non-M01 actions. Implemented Projects, Tasks, Calendar, Documents, Notifications, Trash, Settings, Finance-manual, Bookmarks-manual, Snippets-text, Read-Later Bookmark-reference, FX16 numeric Goals, FX26-S01 Dashboard attention and the FX32 pure-toolbox subset are governed by their slice evidence documents rather than this default.

## Full R1 readiness rule

Do not use `Full R1 implementation-ready` as an approval or Go state.

Valid states are slice-scoped:

1. `SLICE_SPECIFIED` — requirements/action/API/DB/UX/AC are written.
2. `SLICE_READY_TO_APPROVE` — open decisions are closed enough for a bounded slice.
3. `SLICE_APPROVED_TO_IMPLEMENT` — Product Owner explicitly approves the exact slice.
4. `SLICE_IMPLEMENTED` — code exists and is merged subject to process.
5. `SLICE_VERIFIED_LOCALLY` — runtime evidence actually passes for that slice.
6. `PRODUCTION_GO_APPROVED` — later provider/capacity/security/ops Go/No-Go approves public release.

A slice may be implemented only when it names exact modules/actions/stories and includes API, DB, UX, acceptance, security/privacy and evidence contracts. Agents must not invent missing business decisions or infer real provider permission from R1 catalog membership.
