# Paused / blocked / gated register — pre-implementation

2026-09-09 · Docs-only clarification layer. This document authorizes no application code, migration, runtime test, provider call, production deployment, production data access or secret access by itself.

This register clarifies the effective meaning of `Paused`, `Blocked`, `Gated`, `Resolved delegated`, and `Approved for M01` after `DEC-20260909-001` through `DEC-20260909-010`. If an older catalog row still says `Blocked Q-*`, `DEP-EXT-01 boundary needs clarification`, or `no implementation approved`, read that row through this register until the catalog is regenerated. This file may narrow availability; it never expands the approved implementation boundary.

## Status vocabulary

| Status | Meaning | Implementation rule |
| --- | --- | --- |
| `APPROVED_FOR_M01` | Current PO approval covers the exact M01 story/action/acceptance set plus the supporting scaffold/scripts. | May be implemented now only when the change maps to `DEC-20260909-001` and M01 S00-S11. |
| `DESIGN_RESOLVED_NOT_APPROVED_NOW` | The requirement/action contract is designed, but the current approval does not cover implementing it. | Keep docs as design authority; future code needs a bounded implementation approval. |
| `POLICY_APPROVED_IMPLEMENTATION_GATED` | Product policy is decided, but exact API/DB/UX/security/evidence contract or slice approval is still missing. | Do not code until the named implementation gate is closed. |
| `PO_PAUSED` | Product Owner explicitly paused the capability. | No active UI, handler, worker, provider call, default enablement, alert or support surface. Resume requires explicit PO decision. |
| `NETWORK_GUARD_GATED` | Read-only outbound policy exists only for named capabilities and only under strict guard evidence. | Do not run outbound code until a future slice defines allowlist, SSRF, redirect/payload limits, timeouts, retries, rate limits and degraded states. |
| `SENSITIVE_PROJECTION_GATED` | General sensitive-share/support policy is approved, but concrete field projections are not defined for the resource. | Do not expose share/support payloads until field-level allowlists and tests exist. |
| `PRODUCTION_OPS_GATED` | The capability depends on production provider, capacity, RPO/RTO/SLA, backup/restore or operational approval. | Local docs/scaffold do not satisfy this; close Local Stable first, then run production Go/No-Go. |
| `SUPERSEDED` | Historical or retired action kept for traceability. | No UI, handler or migration target should be created for it. |

## Absolute implementation boundary before first code

Only `APPROVED_FOR_M01` is implementable now.

Allowed now:

- M01 stories S00-S11.
- Backend scaffold required for M01.
- React frontend scaffold required for M01.
- Local scripts/runbook artifacts required for M01 evidence.
- Synthetic local fixtures, local SQL Server, optional Redis cache and captured/simulated notification transports.

Denied now:

- Full Phase 1 or full Release 1.
- Production deployment, public launch, provider spend, domain purchase, production secrets or production data.
- Business modules outside M01.
- Paused modules, paused workers and paused provider surfaces.
- Real OAuth, provider writes, payments, executable third-party integrations, webhook runtime or n8n runtime.

## Effective PO-paused capabilities

These are not blocked because of missing technical detail; they are intentionally paused by Product Owner.

| Capability | Catalog rows | Effective state | Exact effect |
| --- | ---: | --- | --- |
| FX30 Price Tracking | 18 | `PO_PAUSED` | No tracker UI/handler, product fetch, price refresh, alert evaluation, worker or support projection. |
| FX34 Automation / Scheduler / Workflows | 20 | `PO_PAUSED` | No user workflow definitions, runs, schedules, dry-runs, imports/exports, dispatch workers or support projection. |
| FX35 Integrations / Webhooks / n8n | 21 | `PO_PAUSED` | No owner connections, system connections, credentials, inbound/outbound webhooks, delivery retry, n8n or workers. |

Effective PO-paused rows: **59**.

Additional inactive network rows still appearing under paused/blocked wording in catalog v1.1:

| Capability | Catalog rows | Effective state | Exact effect |
| --- | ---: | --- | --- |
| FX32 Developer Toolbox network HTTP/DNS | 2 | `PO_PAUSED` under integration/network boundary | Keep local pure tools only. Do not provide arbitrary HTTP request test or DNS lookup execution. |
| FX36 Monitoring HTTP check/probe | 2 | `NETWORK_GUARD_GATED` | Monitoring probes are allowed only in a future Monitoring slice after owner-configured target and network guard evidence. Not M01. |

Catalog v1.1 may still count **63 Paused** rows. Effective interpretation after 2026-09-09 is: 59 Product-Owner-paused rows, 2 Toolbox network rows held by the integration/network pause, and 2 Monitoring rows moved semantically to guarded future outbound work.

## Effective blocked / gated capabilities

Catalog v1.1 records **75 Blocked** rows. After 2026-09-09, these are not all the same kind of blocker. Treat them as the following effective gates.

| Area | Rows / examples | Effective state | What is still missing before implementation or release |
| --- | --- | --- | --- |
| M01 runtime evidence | G02-G06 in M01 readiness | `APPROVED_FOR_M01` but not verified | Package/toolchain lockfiles, auth/security tests, last-SuperAdmin concurrency tests, delivery/outbox tests, clean checkout and restore rehearsal. |
| Identity MFA enrollment/recovery | `identity.mfa.enroll`, `identity.mfa.remove`, `identity.mfa.recover` | `POLICY_APPROVED_IMPLEMENTATION_GATED` | TOTP + one-time recovery-code policy is approved, but MFA implementation is outside M01 and needs its own slice/API/DB/UX/AC/security evidence. |
| Deleted account restore | restore UI/API not in M01 | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Same-account restore/no-email-reuse/no-purge policy is approved; self-service restore workflow still needs a later slice. |
| Sensitive share/support projection | e.g. `organization.collection.share`, `vault.support.read`, `shopping.order.share`, `assets.asset.share`, `assets.support.read`, `digital.asset.share`, `digital.support.read`, `career.support.read`, `learning.certification.share`, `learning.support.read` | `SENSITIVE_PROJECTION_GATED` | General policy is approved: allowlist/default-hidden/support metadata only. Each resource still needs concrete field-level projection contracts and tests. |
| Vault crypto, portability and recovery implementation | Vault release, key wrapping, encrypted package, recovery execution | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Hybrid/no-operator-plaintext policy is approved. Vault still needs crypto/key/package ADR, threat review and restore/import/export rehearsal before release. |
| Advanced Finance | 45 FX27 rows covering financial accounts, ledger transactions, transfers, bills, subscriptions, budgets, savings, debt, reports, CSV, support/report sharing | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Initial Finance is only manual category/amount/explicit-currency/date/optional-note. Advanced Finance needs later PO approval and contracts. |
| Import/export operational backup/restore | `transfer.backup.read`, `transfer.backup.request`, `transfer.restore.preview`, `transfer.restore.request`, `transfer.worker.backup`, `transfer.worker.restore` | `PRODUCTION_OPS_GATED` | Local Stable first; then provider, storage, capacity, RPO/RTO/SLA, key-recovery and isolated restore approval/evidence. |
| News read-only outbound | `news.source.refresh`, `news.worker.fetch` | `NETWORK_GUARD_GATED` | 2026-09-09 policy allows read-only public outbound for News only after future slice contract and network guard evidence. |
| GitHub Discovery read-only outbound | `github.repository.search`, `github.repository.refresh`, `github.query.run`, `github.snapshot.capture`, `github.provider.fetch` | `NETWORK_GUARD_GATED` | 2026-09-09 policy allows public read-only GitHub metadata only after future slice contract and network guard evidence; no OAuth or GitHub writes. |
| Monitoring read-only probing | `monitoring.monitor.check`, `monitoring.probe.run` | `NETWORK_GUARD_GATED` | Allowed only after owner-configured target, future Monitoring slice contract and network guard evidence. |
| Outbound not covered by 2026-09-09 policy | `bookmarks.bookmark.refresh`, `digital.observation.inspect`, `toolbox.network.http`, `toolbox.network.dns` | `POLICY_APPROVED_IMPLEMENTATION_GATED` or `PO_PAUSED` depending on module | 2026-09-09 outbound approval is only for News/GitHub/Monitoring. Bookmark URL metadata, Digital domain/TLS observation and arbitrary toolbox network execution need separate approval or explicit extension. |
| Shopping ↔ Finance link | `shopping.order.link_finance` | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Finance reference semantics require the future Finance slice and must not auto-write ledger/accounting effects. |
| Digital credential reference | `digital.credential.reference` | `POLICY_APPROVED_IMPLEMENTATION_GATED` | Requires Vault slice and credential-reference contract; must not reveal Vault plaintext from Digital Assets. |
| Production/capacity | provider, public release, RPO/RTO/SLA, cost envelope | `PRODUCTION_OPS_GATED` | Deferred until Local Stable evidence and later PO Go/No-Go. |

## Effective rules for future slice preparation

A future slice may convert a gated row to implementable only when all of these exist in current docs:

1. Explicit PO/session approval naming the bounded slice.
2. Source requirements and acceptance criteria updated for the exact capability.
3. API contract, request/response shapes and error/permission behavior.
4. DB/migration/transaction plan with owner isolation and rollback/restore effect.
5. UX/screen flow and unavailable/denied/degraded states.
6. Security/privacy review where the slice touches auth, secrets, support/share, outbound network, finance or personal data.
7. Tests and evidence expectations before `Implemented`, `Verified locally` or `Production-ready` can be claimed.

## Agent pre-flight checklist

Before any implementation PR, the agent must record:

- Current branch/revision.
- Exact approved slice and decision ID.
- Whether every touched action is `APPROVED_FOR_M01`, `DESIGN_RESOLVED_NOT_APPROVED_NOW`, `PO_PAUSED`, `NETWORK_GUARD_GATED`, `SENSITIVE_PROJECTION_GATED`, `PRODUCTION_OPS_GATED` or `SUPERSEDED`.
- Why no paused/blocked/gated action is implemented accidentally.
- Runtime evidence that actually ran, or `Not run` with reason.

If a module file and this register appear to disagree, use the stricter rule and resolve the documentation conflict before coding that capability.

## Current go/no-go

| Scope | Status |
| --- | --- |
| M01 + backend/frontend scaffold + local scripts | Go for implementation after PR merge/branch usage, still requires runtime evidence. |
| Full Phase 1 | No-go until later bounded approval and slice contracts. |
| Full Release 1 | No-go until every committed capability has implementation approval, code and evidence or a PO scope revision. |
| Production/public launch | No-go until Local Stable evidence plus production provider/capacity/RPO/RTO/SLA approval. |
| FX30/FX34/FX35 | No-go; explicitly paused. |
