# FX-12 — Tasks — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Project-bound Tasks with four statuses, mandatory scheduling, complete history and a single reminder.

## 2. Requirement sources

- [FX-12 feature](../../features/12-tasks.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-12-BR-001`, `FX-12-BR-002`, `FX-12-BR-003`, `FX-12-BR-004`, `FX-12-BR-005`, `FX-12-BR-006`, `FX-12-AC-001`, `FX-12-AC-002`, `FX-12-AC-003`, `FX-12-AC-004`, `DEC-TSK-001`, `DEC-TSK-002`, `DEC-TSK-003`, `DEC-TSK-004`, `DEC-TSK-005`, `DEC-TSK-006`, `DEC-TSK-007`, `DEC-TSK-008`, `DEC-TSK-009`, `DEC-TSK-010`, `DEC-TSK-011`, `P02-HIS-001`, `P02-HIS-002`, `P02-HIS-003`, `P02-HIS-004`, `P02-HIS-005`, `P02-HIS-006`, `P02-TSK-001`, `P02-TSK-002`, `P02-TSK-003`, `P02-TSK-004`, `P02-TSK-005`, `P02-TSK-006`, `P02-TSK-007`, `P02-TSK-010`, `P02-TSK-011`, `P02-TSK-012`, `P02-TSK-013`, `P02-TSK-014`, `P02-TSK-020`, `P02-TSK-021`, `P02-TSK-022`, `P02-TSK-023`, `P02-TSK-024`, `P02-TSK-025`, `P02-VIW-001`, `P02-VIW-002`, `P02-VIW-003`, `P02-VIW-004`, `P02-VIW-005`, `P02-VIW-006`, `P02-VIW-007`, `P02-VIW-008`, `P02-VIW-009`

## 3. Reference products

- [Linear](https://linear.app/docs/select-issues) — checked2026-09-07. Evidence limit: Official docs; no exact Linear keyboard map imposed on Nexora.
- [Microsoft To Do](https://support.microsoft.com/en-us/todo/welcome-to-microsoft-to-do) — checked2026-09-07. Evidence limit: Official help text, not task interaction recording.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Linear](https://linear.app/docs/select-issues) | Selection, context command bar and keyboard reordering provide alternatives to pointer actions. | ADAPT | Task Move/Reorder commands with same validation as drag; reason-required backward moves remain Nexora-specific. |
| [Microsoft To Do](https://support.microsoft.com/en-us/todo/welcome-to-microsoft-to-do) | Personal lists organize tasks and My Day focuses daily work. | ADAPT | Nexora Project contains every Task; keep four statuses and irreversible Project terminal lock. Reject shared lists/assignment. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX12-S01 — Task Kanban: BROWSE profile.
- FX12-S02 — Task Table: BROWSE profile.
- FX12-S03 — Task Create / Edit: FORM profile.
- FX12-S04 — Task detail: DETAIL profile.
- FX12-S05 — Backward status reason: DIALOG profile.
- FX12-S06 — Task history / restore: HISTORY profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX12-S01 | Task Kanban | /projects/:projectId/tasks?view=kanban | Four status columns/counts; cards Title/Priority/Start/End/Overdue | Add Task in NotStarted/InProgress |
| FX12-S02 | Task Table | /projects/:projectId/tasks?view=table | Title; Status; Priority; StartDateTime; EndDateTime | Add Task |
| FX12-S03 | Task Create / Edit | /projects/:projectId/tasks/new; /tasks/:taskId/edit | Project readonly; Title; Start; End; optional Description/AC/Priority/Tags/Reminder | Save Task |
| FX12-S04 | Task detail | /tasks/:taskId | Title/status/priority/time/Overdue; description; AC text/checks; tags; reminder state; parent link | Edit while parent active |
| FX12-S05 | Backward status reason | /tasks/:taskId/transition | From→to; Project status; required reason; affected reminder preview | Confirm transition |
| FX12-S06 | Task history / restore | /tasks/:taskId/history | All versions; full snapshot diff incl checklist/tags/rank/reminder; reasons owner-only | Restore selected version |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Active Project → Kanban → Add in NotStarted/InProgress → full form Save → drag/move with reason when backward → detail/history → eligible restore.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX12-S01 — Task Kanban

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Four status columns/counts; cards Title/Priority/Start/End/Overdue |
| Entry / proposed route | /projects/:projectId/tasks?view=kanban; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Task Kanban. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Add Task in NotStarted/InProgress; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Move; reorder within column; Open detail; Table. Back/Cancel always has authorized fallback. |
| Content regions / fields | Four status columns/counts; cards Title/Priority/Start/End/Overdue |
| Search / filters / sorting / pagination | Title/Tag search; Status/time; rank order; paged Load more per column. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Drag movement remains pending until server confirmation. Backward opens reason dialog before move. Keyboard Move to… and Move before/after… have identical validation; failed move returns original rank. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Task Kanban' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX12-S02 — Task Table

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; Status; Priority; StartDateTime; EndDateTime |
| Entry / proposed route | /projects/:projectId/tasks?view=table; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Task Table. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Add Task; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open detail; Kanban. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; Status; Priority; StartDateTime; EndDateTime |
| Search / filters / sorting / pagination | Same search/filters; sort permitted columns;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No drag-only state change; row More → Change status. Mobile prioritized Title/Status with time/priority in expandable detail, never discard fields. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Task Table' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX12-S03 — Task Create / Edit

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Project readonly; Title; Start; End; optional Description/AC/Priority/Tags/Reminder |
| Entry / proposed route | /projects/:projectId/tasks/new; /tasks/:taskId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Task Create / Edit. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Task; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Project readonly; Title; Start; End; optional Description/AC/Priority/Tags/Reminder |
| Search / filters / sorting / pagination | Full form even from quick-create; no implicit time defaults. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Show out-of-Project period warning and explicit Save anyway. Exact newly set reminder future; historical expired config can remain while editing other fields. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Task Create / Edit' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX12-S04 — Task detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Title/status/priority/time/Overdue; description; AC text/checks; tags; reminder state; parent link |
| Entry / proposed route | /tasks/:taskId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Task detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Edit while parent active; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Change status; History; Move to Trash; Share if allowed. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title/status/priority/time/Overdue; description; AC text/checks; tags; reminder state; parent link |
| Search / filters / sorting / pagination | No inline Calendar-owned reschedule. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Calendar projection link navigates to occurrence/source; Task owns all edits. Terminal Task alone remains editable, terminal Project does not. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Task detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX12-S05 — Backward status reason

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — From→to; Project status; required reason; affected reminder preview |
| Entry / proposed route | /tasks/:taskId/transition; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Backward status reason. Named modal with preview/reason and footer actions. |
| Primary action | Confirm transition; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | From→to; Project status; required reason; affected reminder preview |
| Search / filters / sorting / pagination | Reason nonblank, whitespace rejected. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Cancel retains original card/state. Server conflict keeps reason in authorized memory and asks refresh, never force transition. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Backward status reason' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX12-S06 — Task history / restore

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — All versions; full snapshot diff incl checklist/tags/rank/reminder; reasons owner-only |
| Entry / proposed route | /tasks/:taskId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Task history / restore. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Restore selected version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Preview; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | All versions; full snapshot diff incl checklist/tags/rank/reminder; reasons owner-only |
| Search / filters / sorting / pagination | Version newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Restore creates new version, immutable Project preserved; backward needs reason, parent locked rejects. Past reminder restored Expired, no replay alert. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Task history / restore' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Required immutable Project, Title1..200, Start, End (End>Start). Optional Description, AC prose and checkbox rows, PriorityP0..P3 (P0 highest), many Tags, one Reminder None/15min before Start/exact. Project field fixed from context. Create in NotStarted or InProgress only; no subtask/recurrence/import/export.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Kanban default with four columns NotStarted/InProgress/Completed/Skipped, server counts. Card Title, Priority, Start, End, Overdue. Table Title,Status,Priority,Start,End. Filter Status/time overlap; search Title/Tag. Rank within Kanban column; table Start ASC then Id default delegated.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Kanban cards Title/Priority/Start/End/Overdue; Table Title/Status/Priority/Start/End; no move to another Project.

## 13. Search / Filter / Sort

Kanban default with four columns NotStarted/InProgress/Completed/Skipped, server counts. Card Title, Priority, Start, End, Overdue. Table Title,Status,Priority,Start,End. Filter Status/time overlap; search Title/Tag. Rank within Kanban column; table Start ASC then Id default delegated.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| NotStarted | Edit; move forward to InProgress/Completed/Skipped; Trash | No time-triggered automatic progress |
| InProgress | Edit; Complete/Skip; backward NotStarted with reason | Out-of-project time needs confirmation |
| Completed/Skipped with active Project | Edit normal fields; reopen via allowed backward transition with reason; history restore | Completed↔Skipped direct rejected; return InProgress with reason first |
| Any with terminal Project | Readonly detail/history | All mutation/create/restore/reorder blocked |
| Trash | Restore only active nonTrash parent; purge | No restore if parent terminal or trashed |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Backward status reason | From→to; Project status; required reason; affected reminder preview | Confirm transition / Cancel | Cancel retains original card/state. Server conflict keeps reason in authorized memory and asks refresh, never force transition. |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. This module does not need a fabricated provider-specific error surface for its ordinary local data; its registered cross-module sources may still be unavailable and must be labeled.

## 18. Permissions / Read-only / Sensitive contexts

Owner isolation applies equally to list/count/picker/history/source links. Operational authority does not supply personal-data permission. 

Use [security UX](../global/12-security-sensitive-ux.md) and [explicit modes](../global/13-admin-support-emergency.md). Readonly banner is contextual and server enforced, not merely a disabled Save over full private DTO.

## 19. Responsive behavior

[UX-10](../global/10-responsive-design.md) plus per-screen overrides is mandatory: desktop appropriate split/table, tablet drawer, mobile stacked route/sheet with Back, all fields available. Do not reset approved default view/source state on resize. Mobile status selector and Move menu preserve Kanban data/order; never drag-only.

## 20. Accessibility

Keyboard-only primary/alternate/error/destructive flows, explicit labels and visible focus; semantics for tables/forms/statuses; chart/table equivalent; no drag-only; modal focus trap/return; touch/zoom/reduced-motion contracts [UX-11](../global/11-accessibility.md). Per-screen text is not certification; later assistive-tech tests must execute these paths.

## 21. Cross-module integration

Use registered source/command/projection contracts from [UX-14](../global/14-cross-module-interactions.md); links preserve owner/access mode and do not transfer ownership or mutate unrelated source.

Data design trace:

| Table / provider data | Purpose / dependency status |
| --- | --- |
| [productivity.Task](../../design-database/05-productivity-calendar.md#productivity-task) | Task permanently belonging to one personal Project; Technical decision |
| [productivity.TaskChecklistItem](../../design-database/05-productivity-calendar.md#productivity-taskchecklistitem) | Structured acceptance criteria with stable item identity; Technical decision |
| [productivity.Tag](../../design-database/05-productivity-calendar.md#productivity-tag) | User-created shared Project/Task tag namespace; Technical decision |
| [productivity.TaskTag](../../design-database/05-productivity-calendar.md#productivity-tasktag) | Many tags per Task; Technical decision |
| [productivity.TaskVersion](../../design-database/05-productivity-calendar.md#productivity-taskversion) | Full Task versions including checklist and reminder configuration; Technical decision |

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
