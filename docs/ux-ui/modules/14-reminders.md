# FX-14 — Reminders / Scheduling — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287`.

Local implementation note — 2026-09-11: the bounded local slice is available
at `/modules/FX14`. It offers a server-authorized source picker for active
Tasks and manual Calendar Events, None/15-minute/exact configuration, source
and reminder ETag conflict handling, and the local delivery-state projection.
The picker is deliberately a module screen rather than an embedded source-form
field for this batch; no provider delivery, standalone reminder, snooze or
recurrence is implied. SQL/browser interaction evidence remains `Not run`.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Shared embedded Task/Event reminder field and registered source scheduling, not standalone reminder app.

## 2. Requirement sources

- [FX-14 feature](../../features/14-reminders-and-scheduling.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-14-BR-001`, `FX-14-BR-002`, `FX-14-BR-003`, `FX-14-BR-004`, `FX-14-BR-005`, `FX-14-BR-006`, `FX-14-AC-001`, `FX-14-AC-002`, `FX-14-AC-003`, `FX-14-AC-004`, `P02-RMD-001`, `P02-RMD-002`, `P02-RMD-003`, `P02-RMD-004`

## 3. Reference products

- [Todoist](https://www.todoist.com/help/todoist/features/introduction-to-reminders-9PezfU) — checked2026-09-07. Evidence limit: Official help search excerpt checked2026-09-07.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Todoist](https://www.todoist.com/help/todoist/features/introduction-to-reminders-9PezfU) | Custom reminders may specify exact time or relative time before a task. | ADAPT | Exactly one reminder and15min before Start preset; reject multiple/location reminders or subscription tiers. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX14-S01 — Embedded reminder field: FORM profile.
- FX14-S02 — Exact reminder picker: DIALOG profile.
- FX14-S03 — Reminder delivery state: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX14-S01 | Embedded reminder field | /tasks/:taskId/edit#reminder; /calendar/events/:eventId#reminder | None/preset/exact; resolved local timestamp and timezone; current state | Apply within source Save |
| FX14-S02 | Exact reminder picker | /reminders/picker?source=:opaqueId | Date/time; IANA timezone label; resolved instant; validation | Use this time |
| FX14-S03 | Reminder delivery state | /reminders/:reminderId | Due time; source revision/state; InApp/Email/BrowserPush attempts | Open source |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Task/Event form → None/15min/exact → preview local time and UTC instant → Save source → pending → channel outcomes.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX14-S01 — Embedded reminder field

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — None/preset/exact; resolved local timestamp and timezone; current state |
| Entry / proposed route | /tasks/:taskId/edit#reminder; /calendar/events/:eventId#reminder; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Embedded reminder field. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Apply within source Save; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Remove reminder; Cancel source form. Back/Cancel always has authorized fallback. |
| Content regions / fields | None/preset/exact; resolved local timestamp and timezone; current state |
| Search / filters / sorting / pagination | Radio choices, accessible datetime picker. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No separate Save that leaves Task version out of sync; source change invalidates old intent. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Embedded reminder field' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX14-S02 — Exact reminder picker

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Date/time; IANA timezone label; resolved instant; validation |
| Entry / proposed route | /reminders/picker?source=:opaqueId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Exact reminder picker. Named modal with preview/reason and footer actions. |
| Primary action | Use this time; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Date/time; IANA timezone label; resolved instant; validation |
| Search / filters / sorting / pagination | No presets beyond approved15minutes. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Cancel restores previous config; DST ambiguous/nonexistent time requires explicit validated resolution, not silent shift. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Exact reminder picker' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX14-S03 — Reminder delivery state

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Due time; source revision/state; InApp/Email/BrowserPush attempts |
| Entry / proposed route | /reminders/:reminderId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Reminder delivery state. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Open source; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Due time; source revision/state; InApp/Email/BrowserPush attempts |
| Search / filters / sorting / pagination | Chronological attempt list25/page if long. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Late<=15min sends marked late; older Missed. Browser denied not failure of other channels; dispatched not read. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Reminder delivery state' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Exactly one Task/Event reminder. Exact time future when newly set;15min before Start preset may fall prior day. All-day preset23:45 previous day. Historical past config restored Expired permitted; changing unrelated fields must not block Save.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Reminder picker no list search/filter/pagination. Delivery attempt readout chronological, channel labels; no unapproved snooze.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Reminder picker no list search/filter/pagination. Delivery attempt readout chronological, channel labels; no unapproved snooze.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| None | Choose preset/exact | No scheduled intent |
| Pending | Change/remove through source form | No independent duplicate reminder |
| Dispatched | Inspect channel attempt outcomes | Not proof User saw message |
| Expired/Missed/Canceled | Show reason; set new future reminder if source editable | No retrospective notification replay |
| Source terminal/Trash/module disabled | Inspect only if authorized | No pending dispatch |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Exact reminder picker | Date/time; IANA timezone label; resolved instant; validation | Use this time / Cancel | Cancel restores previous config; DST ambiguous/nonexistent time requires explicit validated resolution, not silent shift. |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. This module does not need a fabricated provider-specific error surface for its ordinary local data; its registered cross-module sources may still be unavailable and must be labeled.

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
| [notifications.Notification](../../design-database/04-files-jobs-notifications.md#notifications-notification) | One durable logical inbox notification; Technical decision |
| [notifications.Delivery](../../design-database/04-files-jobs-notifications.md#notifications-delivery) | Independent channel delivery work; Technical decision |
| [operations.Job](../../design-database/04-files-jobs-notifications.md#operations-job) | Durable registered work and lease; Technical decision |
| [calendar.Reminder](../../design-database/05-productivity-calendar.md#calendar-reminder) | Single current Task/Event reminder config and scheduling revision; Technical decision |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-10](../../features/90-open-decisions.md#q-10) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-14 action catalog](../../action-catalog/modules/14-reminders.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
