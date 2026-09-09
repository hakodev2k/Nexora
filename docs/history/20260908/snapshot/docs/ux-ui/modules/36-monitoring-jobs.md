# FX-36 — Monitoring / Jobs — UX/UI Specification

> **Current decision amendment — 2026-09-07:** Network probing paused/held; Admin operations for core jobs remain separately authorized. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

User monitors and incidents separate from privileged redacted job operations; network/capacity Q-07/Q-08.

## 2. Requirement sources

- [FX-36 feature](../../features/36-monitoring-and-job-operations.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-36-BR-001`, `FX-36-BR-002`, `FX-36-BR-003`, `FX-36-BR-004`, `FX-36-BR-005`, `FX-36-AC-001`, `FX-36-AC-002`, `FX-36-AC-003`, `P01-PLT-006`, `P07-CER-003`, `P07-INF-002`

## 3. Reference products

- [UptimeRobot](https://uptimerobot.com/) — checked2026-09-07. Evidence limit: Official product page, no uptime/SLA tested.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [UptimeRobot](https://uptimerobot.com/) | Monitoring product distinguishes endpoint checks, response time and SSL/cron capabilities. | ADAPT | Owner monitor/incident history separate from Admin jobs; scope/budget remain Q-07/Q-08. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX36-S01 — My monitors: BROWSE profile.
- FX36-S02 — Monitor form: FORM profile.
- FX36-S03 — Monitor detail / incidents: DETAIL profile.
- FX36-S04 — Admin jobs: ADMIN profile.
- FX36-S05 — Job attempt detail: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX36-S01 | My monitors | /monitoring | Name; type; safe target; current state; last observation; latency if known | New monitor |
| FX36-S02 | Monitor form | /monitoring/new; /monitoring/:monitorId/edit | Name; approved check type; target; interval; expected status/expiry threshold | Save monitor |
| FX36-S03 | Monitor detail / incidents | /monitoring/:monitorId | State; observations chart/table; incident timeline; freshness; safe failure code | Inspect incident |
| FX36-S04 | Admin jobs | /admin/jobs | Handler; opaque owner reference; due; state; lease expiry; attempts; correlation | Inspect job |
| FX36-S05 | Job attempt detail | /admin/jobs/:jobId | Lease/attempt timeline; safe errors; previous effect outcome; retry safety | Retry safe job |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Owner monitors → configure approved endpoint/check → observations → incident/recovery; Admin Jobs separately inspects lease/attempts and safe retry.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX36-S01 — My monitors

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; type; safe target; current state; last observation; latency if known |
| Entry / proposed route | /monitoring; module/source navigation or authorized deep link. |
| Header / layout | Screen title: My monitors. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New monitor; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; pause/resume. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; type; safe target; current state; last observation; latency if known |
| Search / filters / sorting / pagination | Name/state/type; name ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | User route never exposes system job inventory; empty not monitoring provider outage. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'My monitors' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX36-S02 — Monitor form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Name; approved check type; target; interval; expected status/expiry threshold |
| Entry / proposed route | /monitoring/new; /monitoring/:monitorId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Monitor form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save monitor; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Validate target; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; approved check type; target; interval; expected status/expiry threshold |
| Search / filters / sorting / pagination | Public-target validation and quota preview. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Private/internal addresses rejected by guard unless separately approved, no UI bypass. Costs/intervals stay proposal until Q. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Monitor form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX36-S03 — Monitor detail / incidents

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — State; observations chart/table; incident timeline; freshness; safe failure code |
| Entry / proposed route | /monitoring/:monitorId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Monitor detail / incidents. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Inspect incident; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Pause; Edit; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | State; observations chart/table; incident timeline; freshness; safe failure code |
| Search / filters / sorting / pagination | Date range; outcome; chronological chart/25row table. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | New incident after qualifying failures, recovery after approved successes; deduped all-channel notices. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Monitor detail / incidents' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX36-S04 — Admin jobs

| Dimension | Specification |
| --- | --- |
| Purpose / profile | ADMIN — Handler; opaque owner reference; due; state; lease expiry; attempts; correlation |
| Entry / proposed route | /admin/jobs; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Admin jobs. Separate Admin shell, grouped target metadata/matrix and before/after review. |
| Primary action | Inspect job; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Retry/cancel within operational permission. Back/Cancel always has authorized fallback. |
| Content regions / fields | Handler; opaque owner reference; due; state; lease expiry; attempts; correlation |
| Search / filters / sorting / pagination | State/handler/date/correlation; due DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Not User impersonation. Redacted arguments only; no secret/source content download. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 admin profile. Keep 'Admin jobs' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX36-S05 — Job attempt detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Lease/attempt timeline; safe errors; previous effect outcome; retry safety |
| Entry / proposed route | /admin/jobs/:jobId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Job attempt detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Retry safe job; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Request cancel; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Lease/attempt timeline; safe errors; previous effect outcome; retry safety |
| Search / filters / sorting / pagination | Attempt selector; chronological. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Preview impact and recheck authority. Unknown external effect blocks blind retry; cancel does not undo completed effects. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Job attempt detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Monitor name/type HTTP status or TLS expiry, target, interval and expected rule;5min/10s/3fail/2success proposals Q-08 not approved SLA. Cron heartbeat only if Q-07 catalog approves; token capability masked. Admin Job handler/owner reference immutable.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Monitor name/state/type filters, name ASC25/page; incidents time/state; observations newest25/page. Admin Jobs state/handler/time/correlation filters; due/start DESC25/page, no body search.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Monitor name/state/type filters, name ASC25/page; incidents time/state; observations newest25/page. Admin Jobs state/handler/time/correlation filters; due/start DESC25/page, no body search.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Monitor Up/Down | Inspect measurements/incidents; edit/pause | Do not conflate provider unreachable with target down |
| Unknown/Stale | Show last successful timestamp and diagnostic | No fabricated uptime percentage |
| Job Running/Leased | Inspect redacted lease/attempt; cooperative cancel if permitted | No arbitrary SQL/payload edit |
| Job failed/expired lease | Policy-safe retry/requeue with effect dedupe | No cross-user content preview |
| Module disabled | No new monitoring effects | Retain history under approved retention |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. External/staging/dispatch providers may fail independently: retain last successful authorized values with timestamp and error, offer safe retry-after, label partial results. Never show false price0, delivered notification, complete import or successful job.

## 18. Permissions / Read-only / Sensitive contexts

Owner isolation applies equally to list/count/picker/history/source links. Operational authority does not supply personal-data permission. 

Use [security UX](../global/12-security-sensitive-ux.md) and [explicit modes](../global/13-admin-support-emergency.md). Readonly banner is contextual and server enforced, not merely a disabled Save over full private DTO.

## 19. Responsive behavior

[UX-10](../global/10-responsive-design.md) plus per-screen overrides is mandatory: desktop appropriate split/table, tablet drawer, mobile stacked route/sheet with Back, all fields available. Do not reset approved default view/source state on resize. 

## 20. Accessibility

Keyboard-only primary/alternate/error/destructive flows, explicit labels and visible focus; semantics for tables/forms/statuses; chart/table equivalent; no drag-only; modal focus trap/return; touch/zoom/reduced-motion contracts [UX-11](../global/11-accessibility.md). Per-screen text is not certification; later assistive-tech tests must execute these paths.

## 21. Cross-module integration

Use registered source/command/projection contracts from [UX-14](../global/14-cross-module-interactions.md); links preserve owner/access mode and do not transfer ownership or mutate unrelated source.

Data design trace:

| Table / provider data | Purpose / dependency status |
| --- | --- |
| [operations.Job](../../design-database/04-files-jobs-notifications.md#operations-job) | Durable registered work and lease; Technical decision |
| [operations.JobAttempt](../../design-database/04-files-jobs-notifications.md#operations-jobattempt) | Run attempt diagnostics; Technical decision |
| [monitoring.Monitor](../../design-database/09-automation-monitoring.md#monitoring-monitor) | User monitoring of explicitly approved public target; Proposed: Q-07/Q-08 |
| [monitoring.Observation](../../design-database/09-automation-monitoring.md#monitoring-observation) | Immutable monitoring sample; Technical decision |
| [monitoring.Incident](../../design-database/09-automation-monitoring.md#monitoring-incident) | Deduplicated outage interval; Technical decision |
| [operations.SystemJob](../../design-database/04-files-jobs-notifications.md#operations-systemjob) | System-scoped public-cache/maintenance job, never fake owner; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-07](../../features/90-open-decisions.md#q-07) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-08](../../features/90-open-decisions.md#q-08) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

## 24. Acceptance checklist

- [ ] Execute primary journey for every Screen ID and authorized deep link; Back restores context.
- [ ] Required/optional/immutable fields match feature and data dictionary; no reference-product scope added.
- [ ] Every state/action row enforced both UI and server, including parent lifecycle and permission revoke race.
- [ ] Initial empty, filtered empty, loading, failed fetch, degraded/stale, disabled/read-only and conflict are distinct.
- [ ] Each destructive/reason dialog previews exact resources, cancels without mutation and handles stale revision.
- [ ] Keyboard-only and mobile flow reaches every action, detail field and safe exit; no drag/hover-only control.
- [ ] No secret or unauthorized payload in previews, error, URL, notification, logs or persistent browser state.
- [ ] Source BR/AC IDs and Q dependencies traced; Q-gated actions not treated as Approved.

**Five-question review:** User goal and simplest IA are sections1/6/9; mature reference evidence and adaptations/rejections sections3/4; important remaining choices are explicitly Q-gated in section23, not left for frontend to invent. This checklist is specification for later execution, not tests marked passed in a docs-only task.

## Canonical action binding — catalog v1

[FX-36 action catalog](../../action-catalog/modules/36-monitoring.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
