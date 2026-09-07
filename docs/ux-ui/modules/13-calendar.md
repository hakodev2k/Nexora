# FX-13 — Calendar / Personal Events / ICS — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal Manual Events plus readonly Task projections, Day default; ICS import/export without sync or recurring import.

## 2. Requirement sources

- [FX-13 feature](../../features/13-calendar.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-13-BR-001`, `FX-13-BR-002`, `FX-13-BR-003`, `FX-13-BR-004`, `FX-13-BR-005`, `FX-13-BR-006`, `FX-13-AC-001`, `FX-13-AC-002`, `FX-13-AC-003`, `FX-13-AC-004`, `DEC-CAL-001`, `DEC-CAL-002`, `DEC-CAL-003`, `DEC-CAL-004`, `DEC-CAL-005`, `DEC-CAL-006`, `DEC-CAL-007`, `DEC-CAL-008`, `DEC-CAL-009`, `DEC-CAL-010`, `DEC-CAL-011`, `DEC-CAL-012`, `P02-CAL-001`, `P02-CAL-002`, `P02-CAL-003`, `P02-CAL-004`, `P02-CAL-005`, `P02-CAL-006`, `P02-CAL-010`, `P02-CAL-011`, `P02-CAL-012`, `P02-EVT-001`, `P02-EVT-002`, `P02-EVT-003`, `P02-EVT-004`, `P02-EVT-005`, `P02-EVT-010`, `P02-EVT-011`, `P02-EVT-012`, `P02-EVT-013`, `P02-EVT-014`, `P02-EVT-015`, `P02-ICS-001`, `P02-ICS-002`, `P02-ICS-003`, `P02-ICS-004`, `P02-ICS-005`, `P02-ICS-006`, `P02-ICS-007`, `P02-ICS-008`, `P02-ICS-009`, `P02-ICS-020`, `P02-ICS-021`, `P02-ICS-022`, `P02-ICS-023`, `P02-ICS-024`, `P02-ICS-025`, `P02-ICS-026`

## 3. Reference products

- [Google Calendar](https://support.google.com/calendar/answer/37118?co=GENIE.Platform%3DDesktop&hl=en) — checked2026-09-07. Evidence limit: Official help text; Nexora validation rules come from PO/features.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Google Calendar](https://support.google.com/calendar/answer/37118?co=GENIE.Platform%3DDesktop&hl=en) | Import & Export is a settings entry with file selection and target calendar selection. | ADAPT | Nexora .ics preview/report, always ManualEvent; reject recurrence/reminder import and external sync. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX13-S01 — Day: CALENDAR profile.
- FX13-S02 — Week: CALENDAR profile.
- FX13-S03 — Month: CALENDAR profile.
- FX13-S04 — Agenda: BROWSE profile.
- FX13-S05 — Manual Event form / detail: FORM profile.
- FX13-S06 — Task projection detail: DETAIL profile.
- FX13-S07 — ICS Import / Export: FORM profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX13-S01 | Day | /calendar?view=day | Date header; all-day strip; timed lanes; source icon+status; overlap layout | New Event |
| FX13-S02 | Week | /calendar?view=week | Week dates; all-day strip; per-day timed events | New Event |
| FX13-S03 | Month | /calendar?view=month | Date cells; event summaries; explicit +N more | New Event |
| FX13-S04 | Agenda | /calendar?view=agenda | Date groups; Title; source; start/end; status; Task Project | New Event |
| FX13-S05 | Manual Event form / detail | /calendar/events/new; /calendar/events/:eventId | Title; Description; time/date range; all-day; single reminder; lifecycle banner | Save / Complete when Scheduled |
| FX13-S06 | Task projection detail | /calendar/tasks/:taskId | Task Title/status/priority/start/end/Project; readonly source banner | Open Task |
| FX13-S07 | ICS Import / Export | /calendar/data | Import file preview/report or export source/status/all/custom range | Validate import / Generate export |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Calendar Day → choose date → New Event full form → Save despite overlap warning if confirmed → inspect; Task event → Open Task to edit.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX13-S01 — Day

| Dimension | Specification |
| --- | --- |
| Purpose / profile | CALENDAR — Date header; all-day strip; timed lanes; source icon+status; overlap layout |
| Entry / proposed route | /calendar?view=day; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Day. Date/view toolbar, all-day strip and event region. |
| Primary action | New Event; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Today; previous/next day; view switch; ICS. Back/Cancel always has authorized fallback. |
| Content regions / fields | Date header; all-day strip; timed lanes; source icon+status; overlap layout |
| Search / filters / sorting / pagination | Title/Project search; status/range; chronological. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Default Day on first entry, no automatic change on resize. Current time line decorative+accessible label; overlapping events remain separately reachable. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 calendar profile. Keep 'Day' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX13-S02 — Week

| Dimension | Specification |
| --- | --- |
| Purpose / profile | CALENDAR — Week dates; all-day strip; per-day timed events |
| Entry / proposed route | /calendar?view=week; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Week. Date/view toolbar, all-day strip and event region. |
| Primary action | New Event; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Today; previous/next week; view switch. Back/Cancel always has authorized fallback. |
| Content regions / fields | Week dates; all-day strip; per-day timed events |
| Search / filters / sorting / pagination | Same Calendar query, bounded week. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Week start delegated Monday unless approved preference changes; mobile selected day drilldown retains Week context and Back. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 calendar profile. Keep 'Week' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX13-S03 — Month

| Dimension | Specification |
| --- | --- |
| Purpose / profile | CALENDAR — Date cells; event summaries; explicit +N more |
| Entry / proposed route | /calendar?view=month; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Month. Date/view toolbar, all-day strip and event region. |
| Primary action | New Event; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Today; previous/next month; view switch. Back/Cancel always has authorized fallback. |
| Content regions / fields | Date cells; event summaries; explicit +N more |
| Search / filters / sorting / pagination | Same query, bounded visible range. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Overflow opens selected-day list, not hidden inaccessible items. Keyboard day/event navigation with fallback Agenda. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 calendar profile. Keep 'Month' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX13-S04 — Agenda

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Date groups; Title; source; start/end; status; Task Project |
| Entry / proposed route | /calendar?view=agenda; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Agenda. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Event; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open Event; range/view switch. Back/Cancel always has authorized fallback. |
| Content regions / fields | Date groups; Title; source; start/end; status; Task Project |
| Search / filters / sorting / pagination | Same query; Start ASC+source UID;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Empty visible day differs no events at all; range preserved navigating detail/back. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Agenda' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX13-S05 — Manual Event form / detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title; Description; time/date range; all-day; single reminder; lifecycle banner |
| Entry / proposed route | /calendar/events/new; /calendar/events/:eventId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Manual Event form / detail. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save / Complete when Scheduled; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel Event; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; Description; time/date range; all-day; single reminder; lifecycle banner |
| Search / filters / sorting / pagination | No drag/resize; no sharing. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Overlap warning preview then confirm. Cancel confirmation explains visible strikethrough, no reopen; past Scheduled ordinary display. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Manual Event form / detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX13-S06 — Task projection detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Task Title/status/priority/start/end/Project; readonly source banner |
| Entry / proposed route | /calendar/tasks/:taskId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Task projection detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Open Task; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back to Calendar. Back/Cancel always has authorized fallback. |
| Content regions / fields | Task Title/status/priority/start/end/Project; readonly source banner |
| Search / filters / sorting / pagination | No event edit controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Task Completed/Skipped retained with status; trashed Task absent. Parent locked state explains why Task cannot be continued. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Task projection detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX13-S07 — ICS Import / Export

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Import file preview/report or export source/status/all/custom range |
| Entry / proposed route | /calendar/data; module/source navigation or authorized deep link. |
| Header / layout | Screen title: ICS Import / Export. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Validate import / Generate export; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; View operation. Back/Cancel always has authorized fallback. |
| Content regions / fields | Import file preview/report or export source/status/all/custom range |
| Search / filters / sorting / pagination | Import valid nonrecurring unique UID only; export containment. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Always Manual Scheduled on import, ignore VALARM/status; skip invalid/recurring/duplicate with counts; floating time interpreted User zone, unknown TZID skipped. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'ICS Import / Export' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Manual Event Title, Description, Start, End required. All-day switches to date range one/multiple days, UI inclusive last date. One Reminder None/15min/exact. Scheduled new/import; Completed/Canceled immutable. No attendees/location/recurrence/share; Delete label becomes Cancel Event.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Views Day(default),Week,Month,Agenda. Filter source status (manual and Task vocabularies labeled) and range; search Title and Task Project name. Visible calendar uses overlap; ICS export fully-contained range. Agenda chronological25/page; grid range bounded.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Views Day(default),Week,Month,Agenda. Filter source status (manual and Task vocabularies labeled) and range; search Title and Task Project name. Visible calendar uses overlap; ICS export fully-contained range. Agenda chronological25/page; grid range bounded.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. Canceled Event is retained readonly, not Trash.

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Manual Scheduled | Edit only in form; Complete; Cancel | No automatic state/overdue at End |
| Manual Completed/Canceled | Readonly inspect/export if selected | No edit/reopen; Canceled shown strikethrough plus label |
| Task projection | Inspect; Open Task; export if selected | No Calendar editing/drag/resize/status mutation |
| Overlapping events | Show warning; allow save confirmation | No scheduling prohibition or auto-adjustment |

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
| [calendar.ManualEvent](../../design-database/05-productivity-calendar.md#calendar-manualevent) | Independent personal Event; no Task copy; Technical decision |
| [calendar.ImportedUid](../../design-database/05-productivity-calendar.md#calendar-importeduid) | Cross-import duplicate Event protection; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-12](../../features/90-open-decisions.md#q-12) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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
