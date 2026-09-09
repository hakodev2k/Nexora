# FX-17 — Habits — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Boolean/count personal habits with effective schedules and one optional local-time reminder.

## 2. Requirement sources

- [FX-17 feature](../../features/17-habits.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-17-BR-001`, `FX-17-BR-002`, `FX-17-BR-003`, `FX-17-BR-004`, `FX-17-BR-005`, `FX-17-AC-001`, `FX-17-AC-002`, `FX-17-AC-003`

## 3. Reference products

- [Habitify](https://intercom.help/habitify-app/en/articles/9387661-record-progress-on-a-good-habit) — checked2026-09-07. Evidence limit: Official help excerpt; mobile patterns adapted to accessible web.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Habitify](https://intercom.help/habitify-app/en/articles/9387661-record-progress-on-a-good-habit) | Daily journal offers direct complete or partial-progress logging. | ADAPT | Boolean/count check-ins with visible controls; no swipe-only action, bad-habit/challenge expansion or auto-grading scope. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX17-S01 — Today habits: BROWSE profile.
- FX17-S02 — Habit library: BROWSE profile.
- FX17-S03 — Habit form: FORM profile.
- FX17-S04 — Habit detail / history: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX17-S01 | Today habits | /habits/today | Habit title; today target/current count; explicit complete button; streak text | Record progress |
| FX17-S02 | Habit library | /habits | Title; mode; schedule; current state | New Habit |
| FX17-S03 | Habit form | /habits/new; /habits/:habitId/edit | Title; mode; target/unit; weekdays; effective date; timezone; optional reminder | Save Habit |
| FX17-S04 | Habit detail / history | /habits/:habitId | Schedule; streak formula summary; monthly day results; activity | Record selected-day progress |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Habits Today → check complete/add count → view day result → inspect streak/history; edit schedule effective date without regrading past.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX17-S01 — Today habits

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Habit title; today target/current count; explicit complete button; streak text |
| Entry / proposed route | /habits/today; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Today habits. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Record progress; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open habit; choose previous day. Back/Cancel always has authorized fallback. |
| Content regions / fields | Habit title; today target/current count; explicit complete button; streak text |
| Search / filters / sorting / pagination | Date; active scheduled list; title order. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Swipe optional enhancement only; accessible buttons for Complete/Add count. No hidden negative habits/challenges. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Today habits' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX17-S02 — Habit library

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; mode; schedule; current state |
| Entry / proposed route | /habits; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Habit library. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Habit; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Pause/Resume; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; mode; schedule; current state |
| Search / filters / sorting / pagination | Title/state; title ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unscheduled today is not missed habit; paused state textual. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Habit library' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX17-S03 — Habit form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title; mode; target/unit; weekdays; effective date; timezone; optional reminder |
| Entry / proposed route | /habits/new; /habits/:habitId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Habit form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Habit; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; mode; target/unit; weekdays; effective date; timezone; optional reminder |
| Search / filters / sorting / pagination | Count fields conditional; weekday button group. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Mode readonly after create. Target schedule change previews effective day and says prior history unchanged. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Habit form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX17-S04 — Habit detail / history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Schedule; streak formula summary; monthly day results; activity |
| Entry / proposed route | /habits/:habitId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Habit detail / history. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Record selected-day progress; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit; Pause; Archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Schedule; streak formula summary; monthly day results; activity |
| Search / filters / sorting / pagination | Month navigation; scheduled/completed day labels. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Heatmap includes text/table day equivalent; future days disabled with reason; old timezone does not relabel saved dates. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Habit detail / history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Title1..100; immutable Boolean/Count mode; Count target>0 and optional unit; nonempty weekday set; timezone; optional one daily Reminder time. Past check-in editable with Activity, future check-in denied. Schedule/target effective date explicit.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Today/All views; Title search; Active/Paused/Archived filter; scheduled habits first then title; calendar history by month; streak scheduled-success days only.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Today/All views; Title search; Active/Paused/Archived filter; scheduled habits first then title; calendar history by month; streak scheduled-success days only.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Active scheduled day | Record count/check; edit past day; pause/archive | No future check-in |
| Paused | Inspect past results; resume | No streak penalty on paused days |
| Archived | Readonly; Unarchive; Trash | No check-in |
| Target reached before reminder | Show achieved | Pending daily reminder canceled, not all notifications muted |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-ARCHIVE / D-UNARCHIVE | Source + previous state and affected references | Archive / Unarchive; Cancel | Only features with archive lifecycle; preserve source-specific prior state/cohort. |
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
| [productivity.Habit](../../design-database/05-productivity-calendar.md#productivity-habit) | Scheduled personal habit; Technical decision |
| [productivity.HabitSchedule](../../design-database/05-productivity-calendar.md#productivity-habitschedule) | Effective-dated schedule preserving history; Technical decision |
| [productivity.HabitCheckIn](../../design-database/05-productivity-calendar.md#productivity-habitcheckin) | At most one result per scheduled local day; Technical decision |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

No additional major product question identified for this module beyond shared security/capacity implementation gates. Do not reopen routine UX details already delegated.

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

[FX-17 action catalog](../../action-catalog/modules/17-habits.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
