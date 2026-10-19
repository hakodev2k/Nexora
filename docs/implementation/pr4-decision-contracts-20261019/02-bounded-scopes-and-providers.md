# D02=A / D03=A / D04=A — Local feature boundaries and provider guard contract

Decision source: [DEC-20261019-PR4](../../requirements/13-owner-decisions-20261019-pr4-blockers.md). Existing authority: DEC-20260909-006/007/008/009/010/014; feature FX27/FX11–14/FX29/30/33–36 and applicable shared security/UX/action catalogs. **These selected paths reaffirm existing scope, not license to remove remaining committed R1 requirements or call external providers.**

## D02: Finance = manually entered records only, advanced business scope gated

**Installed**: manual Category/Record CRUD/owner summary and protected paging in `FinanceEndpoints`, application and SQL service. Do not replace this with ledger transactions or derive income/debt/tax semantics.

| Layer | Contract (existing bounded path; check implementation before edits) |
|---|---|
| API | Category create/list/edit/delete with current usage guard; ManualRecord create/list/get/update/delete only where current approved action allows. Persist explicit `CurrencyCode` and `OccurredOn`; request DateOnly preserved, never reinterpret historical owner-local dates; actor/profile IANA timezone used for new-date suggestion **only**. Strict exact decimal amount from domain contract; no double conversion or language-derived currency. |
| Authority | Current SELF owner from session, module and exact action; absent/Deny/disabled before query and before commit. Current category source when referenced; list/count/summary and paging redact unavailable source and never infer zero as “source missing”. No reuse of old `finance.transaction.read` to authorize ManualRecord. |
| DB | Retain manual tables/migrations `0008` and subsequent security fixes. Composite OwnerId references, exact decimal scale, required nonempty ISO currency, status/ETag, ordered filtered cursor bound to owner/timezone/filters. Categories cannot be deleted when live references exist. No migration of manual values into an unapproved ledger. |
| UI | New/reset shows currency **unselected**; explicit user selection mandatory; date is owner-local IANA default for new only; existing record edit preserves stored date/currency; amount parsed as exact decimal, lost-response retry retains same key, failures preserve dirty draft and do not show fake success. |
| Acceptance | Empty currency rejected without API write; alternative timezone boundaries, decimal precision/roundtrip, stale ETag, duplicate key/collision, category in use, wrong owner/module off/read revoked, filter cursor replay and no total leak. Compare to current security repairs and exact-head CI. |

**Business gate:** accounts, two-sided ledger postings, transfers, budget allocations, debt interest/terms, FX rate source/quote/rounding, advanced deletion and export are `BUSINESS_GATED`. No HTTP endpoints/SQL tables/UI/grants/proxy data for these may be invented from the older action names. Keep a backlog row with requested PO contract and exit conditions; do not silently label these out of R1.

## D03: Tasks/Projects/Reminders = flat first slice

| Layer | Concrete contract |
|---|---|
| API/actions | One Task belongs to current owner Project; current `projects.*`, `tasks.*` and `reminders.*` installed action set, never expose `subtask/recurrence/snooze/standalone/attachment` based on unknown fields. Each protected update uses explicit status transition action vs metadata write, strict ETag and replay rules. |
| DB | Existing Project → Task owner-composite identity, status transition and Task history; at most one active approved reminder intent per supported source/revision (preserve scheduler rows/history). No speculative RecurrenceRule or parent subtask graph, no cascade to a non-authorized child. |
| UI | Project picker required; Task status not editable via generic metadata control; explicit transition and confirmation, start/end/source timezone correct. Reminder picker supports one current approved intent; when unavailable show disabled with reason rather than second fake reminder. Dirty-navigation and concurrency response preserve draft. |
| Security | Same transaction source owner/Project/Task/act permission and prior Task state before mutation; terminal Project/Task and Trash rules cannot be bypassed by Calendar projection, worker or “quick create”. Worker effect rechecks current source revision and module before dispatch. |
| Acceptance | Task cannot exist without Project; wrong owner, archived/trash/terminal source, concurrent reminder updates, duplicate scheduler executions, time zone/DST, exact revisions, module revoke, disabled extension menu and no hidden handler. Verify Calendar, Focus, Planner and Notification consumers do not gain parentless Task path. |

**Separate future business decision** required for subtask, recurring Task, snooze, standalone reminder, Task attachment, including UX, state machine, retention and migrations. Selected A does not cancel them from the broader committed catalog; maintain explicit gate classification.

## D04: local deterministic simulation; zero external effects

| Family | Allowed under selected A | Must remain OFF |
|---|---|---|
| FX29 News; FX33 GitHub; FX36 Monitoring | Stored local feed/repository/target metadata, source response contract, snapshot/UI, deterministic synthetic adapter **without outbound traffic** in this task. Config schema, partial/stale/degraded truth and source permissions can be implemented/tested locally. | Unreviewed live HTTP/DNS/redirects, private IP fetches, authenticated GitHub calls, target probes and background network execution. Existing DEC008 may allow **future** exact guarded read-only path after separate enablement review; chosen A keeps it OFF now. |
| FX30 Price | Clear synthetic price series/capacity/retry evidence in local safe mode under DEC014; no real purchase claim. | Real scraping/polling/fetch, repricing notifications sourced from real shops, scheduled provider worker |
| FX34 Automation | Local definitions, schedule/run history and deterministic in-process no-side-effect executions under contract | Real outbound/action dispatch, arbitrary code execution, external mutations and provider-triggered workflow |
| FX35 Integrations | Typed connection metadata and local simulated validation, no credentials needed in test | OAuth exchange, real webhooks/send/test requests, n8n/provider sync, stored real tokens |

### Candidate adapter boundary (not a live provider permission)

Define typed `ProviderMode = Disabled | SyntheticLocal | ReviewedReadOnly` as *internal non-user-authoritative config*; current A permits `Disabled` and explicit test-only `SyntheticLocal`. Do not create a generic module grant that enables `ReviewedReadOnly`. Requests should carry typed source, actor/owner, configured operation, deterministic clock/fixture revision and capped result shape; response identifies `mode`, `isSimulated`, `attemptedAt`, `lastSuccessfulAt` (null for no real read), source state `Unavailable/Stale/Degraded/Simulated` and safe diagnostic code. Never serialize “fresh” when only synthetic data exists.

#### API

Existing `/api/v1/automation/*`, `/api/v1/integrations/*`, `/api/v1/news/*` and `/api/v1/monitoring/*` routes should maintain effective runtime gate; unknown mode must fail closed. Candidate provider execution endpoint (only if authorized by source action and reviewed contract) must return an explicit policy-disabled result without outbound effects. UI must distinguish “configured”, “locally simulated” and “real provider available” and never show generic “Connected” or “Delivered” from a synthetic run.

#### SQL, workers and retry

Durable configuration and `Attempt`/`Effect` state records use owner/action/source revision, idempotency and lease fencing; SQL authoritative. Keep distinct attempt vs successful delivery times, bound retry and terminal failure; process restart can't duplicate provider effects. Scheduled jobs cannot promote configuration or simulation to real execution. No live credentials in fixtures/logs. **Do not unpause FX30/34/35 in module defaults, grant metadata, UI flags or worker registry.**

#### External security readiness checklist (future, not met here)

Explicit per-provider host/port/scheme allowlist; canonicalization, DNS rebinding protection on every redirect/hop; disallow private/link-local/loopback/cloud metadata targets; timeout, byte/redirect/rate limits, content-type/XXE controls, no credentials in URL, secure secret references, source revocation before and during execution, synthetic/production separation and observability without request body. Review cost/provider/legal constraints and written PO authorization for paused families. Meeting these technical checks does **not** silently authorize live FX30/34/35.

#### Local acceptance

- With network egress denied, all synthetic flows still operate with explicit `isSimulated=true`, never open socket or mark real delivery/freshness.
- Unknown mode or changed owner/module/grant cancels new attempts; old metadata read remains protected.
- Same key/body schedules one logical attempt, after crash respects lease/retry; changed body conflicts; private config/password never leaks to logs or shared projections.
- News/GitHub/Monitoring real adapters reject all calls under Option A; simulated adapter faults report Degraded/Unavailable, not zero successes.
- CI verifies no live jobs/credentials/default Allow were introduced; production deployment, capacity/SLA, real providers and cost approval remain gated.

## Delivery order and approval

1. Protect existing Finance/Task behavior and current-head failing tests. 2. Implement/document only contract-complete local feature subsets, with current action guards. 3. Retain the advanced business/provider gates, no automatic R1 rescope. 4. Independent reviewer signs off source-protection, SQL and worker logic. 5. User QA on exact final SHA with synthetic SQL and deterministic network-deny evidence. None of those checks are claimed executed by this documentation.
