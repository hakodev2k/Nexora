# FX-34 — Automation / Scheduler — UX/UI Specification

> **Current decision amendment — 2026-09-07:** Paused by Product Owner. Core task reminders, notification workers and platform jobs are separate and continue their approved scope. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Versioned personal trusted automations with trigger/actions/run history; Q-07 controls graph, catalog and egress.

## 2. Requirement sources

- [FX-34 feature](../../features/34-automation-and-scheduler.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-34-BR-001`, `FX-34-BR-002`, `FX-34-BR-003`, `FX-34-BR-004`, `FX-34-BR-005`, `FX-34-BR-006`, `FX-34-AC-001`, `FX-34-AC-002`, `FX-34-AC-003`, `FX-34-AC-004`, `P06-AUT-001`, `P06-AUT-002`, `P06-AUT-003`, `P06-AUT-004`, `P06-AUT-005`, `P06-AUT-006`, `P06-AUT-007`, `P06-AUT-008`, `P06-AUT-009`, `P06-AUT-010`, `P06-AUT-011`, `P06-AUT-012`, `P06-AUT-013`, `P06-AUT-014`

## 3. Reference products

- [n8n](https://n8n.io/features/) — checked2026-09-07. Evidence limit: Official product page; executions docs extraction failed, no live workflow tested.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [n8n](https://n8n.io/features/) | Workflows expose triggers, per-step outputs and execution debugging. | ADAPT | Versioned definition/run/step details; reject code/AI/loops/unbounded graph, keep Q-07 for allowed flow and egress. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX34-S01 — Automations: BROWSE profile.
- FX34-S02 — Definition editor: FORM profile.
- FX34-S03 — Definition detail / versions: DETAIL profile.
- FX34-S04 — Run history: BROWSE profile.
- FX34-S05 — Run / step detail: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX34-S01 | Automations | /automation | Name; trigger; enabled/state; latest version; last run outcome | New automation |
| FX34-S02 | Definition editor | /automation/new; /automation/:definitionId/edit | Name; trigger card; typed input config; ordered action cards; conditions; validation summary | Validate and Save version |
| FX34-S03 | Definition detail / versions | /automation/:definitionId | Exact saved version; compatibility; trigger/actions; enabled state; run summary | Enable / Run if approved Ready |
| FX34-S04 | Run history | /automation/:definitionId/runs | Run ID; definition version; trigger time; state; duration; safe error | Inspect run |
| FX34-S05 | Run / step detail | /automation/runs/:runId | Ordered steps; attempts; state/duration; redacted inputs/results; effect receipt/Unknown marker | Retry eligible failed work |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Automation list → definition draft → choose registered trigger/actions → validate → Save immutable version → enable → run history → inspect/retry safe failed work.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX34-S01 — Automations

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; trigger; enabled/state; latest version; last run outcome |
| Entry / proposed route | /automation; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Automations. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New automation; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; enable/disable; run if Ready. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; trigger; enabled/state; latest version; last run outcome |
| Search / filters / sorting / pagination | Name/state/trigger; Updated DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Show blocked definition separately from disabled user choice; no demo run labeled actual execution. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Automations' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX34-S02 — Definition editor

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Name; trigger card; typed input config; ordered action cards; conditions; validation summary |
| Entry / proposed route | /automation/new; /automation/:definitionId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Definition editor. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Validate and Save version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; Add registered action; preview schedule. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; trigger card; typed input config; ordered action cards; conditions; validation summary |
| Search / filters / sorting / pagination | Action catalog search; typed options only. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Step-list editor proposal, not graph canvas approval. Required credential references display masked name only; no raw output interpolation. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Definition editor' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX34-S03 — Definition detail / versions

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Exact saved version; compatibility; trigger/actions; enabled state; run summary |
| Entry / proposed route | /automation/:definitionId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Definition detail / versions. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Enable / Run if approved Ready; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Disable; Edit new version; History; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Exact saved version; compatibility; trigger/actions; enabled state; run summary |
| Search / filters / sorting / pagination | Version selector; no code editor. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Changing definition creates new version; queued/running execution stays pinned original, cannot silently adopt edits. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Definition detail / versions' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX34-S04 — Run history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Run ID; definition version; trigger time; state; duration; safe error |
| Entry / proposed route | /automation/:definitionId/runs; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Run history. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Inspect run; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Filter; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Run ID; definition version; trigger time; state; duration; safe error |
| Search / filters / sorting / pagination | State/date/version; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Canceled, skipped, timed-out and partial outcomes remain distinct, not all red Failed. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Run history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX34-S05 — Run / step detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Ordered steps; attempts; state/duration; redacted inputs/results; effect receipt/Unknown marker |
| Entry / proposed route | /automation/runs/:runId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Run / step detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Retry eligible failed work; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Request cancel; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Ordered steps; attempts; state/duration; redacted inputs/results; effect receipt/Unknown marker |
| Search / filters / sorting / pagination | Step navigation, attempt tabs. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Retry dialog lists which steps will execute and why safe; successful external effect never silently replayed. No all-step dry-run unless every action supports simulation. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Run / step detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Name, trigger key/version/config and actions key/version/bindings required. Schedule zone/start/end/frequency/cron preview. Bounded linear+conditions<=20step proposal Q-07; no code/loop/AI nodes or assumed graph editor. Vault references selected, never entered plaintext outputs.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Definition name/state/trigger filters, Updated DESC25/page. Runs state/date/definition-version filters, newest25/page. Step details own declared order; no search through raw secret outputs.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Definition name/state/trigger filters, Updated DESC25/page. Runs state/date/definition-version filters, newest25/page. Step details own declared order; no search through raw secret outputs.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Draft/Invalid | Edit and validate | Enable/run blocked on missing contracts/Q-07 |
| Ready Enabled | Manual run/schedule/event subject to authority | No action beyond registered capability |
| Running | Inspect; request cancel | Cancel not rollback of sent external effect |
| Failed/PartiallySucceeded/Unknown | Inspect per-step outcomes; retry only safe unresolved failure after reconcile | No replay succeeded non-idempotent steps |
| Disabled | Inspect versions/history; enable after checks | No future effects; source permission rechecked |

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
| [operations.Outbox](../../design-database/04-files-jobs-notifications.md#operations-outbox) | Transactional event intents; Technical decision |
| [operations.InboxReceipt](../../design-database/04-files-jobs-notifications.md#operations-inboxreceipt) | Per-consumer deduplication; Technical decision |
| [operations.Job](../../design-database/04-files-jobs-notifications.md#operations-job) | Durable registered work and lease; Technical decision |
| [automation.Definition](../../design-database/09-automation-monitoring.md#automation-definition) | Owner automation pointer to immutable approved definition version; Proposed: Q-07 |
| [automation.DefinitionVersion](../../design-database/09-automation-monitoring.md#automation-definitionversion) | Pinned trigger/condition/action contract graph; Proposed: Q-07 |
| [automation.Schedule](../../design-database/09-automation-monitoring.md#automation-schedule) | Durable automation schedule specification; Proposed: Q-07 |
| [automation.Run](../../design-database/09-automation-monitoring.md#automation-run) | One execution against exact immutable definition; Proposed: Q-07 |
| [automation.StepRun](../../design-database/09-automation-monitoring.md#automation-steprun) | Per-step outcome and stable side-effect identity; Proposed: Q-07 |

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

[FX-34 action catalog](../../action-catalog/modules/34-automation.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
