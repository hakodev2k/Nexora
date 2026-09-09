# FX-11 — Projects — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal Projects own every Task and become permanently readonly on Completed or Skipped.

## 2. Requirement sources

- [FX-11 feature](../../features/11-projects.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-11-BR-001`, `FX-11-BR-002`, `FX-11-BR-003`, `FX-11-BR-004`, `FX-11-BR-005`, `FX-11-BR-006`, `FX-11-AC-001`, `FX-11-AC-002`, `FX-11-AC-003`, `FX-11-AC-004`, `DEC-PRJ-001`, `DEC-PRJ-002`, `DEC-PRJ-003`, `DEC-PRJ-004`, `DEC-PRJ-005`, `DEC-PRJ-006`, `DEC-PRJ-007`, `DEC-PRJ-008`, `P02-PRJ-001`, `P02-PRJ-002`, `P02-PRJ-003`, `P02-PRJ-004`, `P02-PRJ-005`, `P02-PRJ-010`, `P02-PRJ-011`, `P02-PRJ-012`, `P02-PRJ-013`, `P02-PRJ-014`, `P02-PRJ-015`, `P02-PRJ-016`, `P02-PRJ-020`, `P02-PRJ-021`, `P02-PRJ-022`, `P02-PRJ-023`, `P02-PRJ-024`, `P02-PRJ-025`, `P02-PRJ-030`, `P02-PRJ-031`, `P02-PRJ-032`, `P02-PRJ-033`, `P02-PRJ-034`, `P02-PRJ-035`, `P02-PRJ-040`, `P02-PRJ-041`, `P02-PRJ-042`, `P02-PRJ-043`

## 3. Reference products

- [Microsoft To Do](https://support.microsoft.com/en-us/todo/welcome-to-microsoft-to-do) — checked2026-09-07. Evidence limit: Official help text, not task interaction recording.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Microsoft To Do](https://support.microsoft.com/en-us/todo/welcome-to-microsoft-to-do) | Personal lists organize tasks and My Day focuses daily work. | ADAPT | Nexora Project contains every Task; keep four statuses and irreversible Project terminal lock. Reject shared lists/assignment. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX11-S01 — Projects Grid / Table: BROWSE profile.
- FX11-S02 — Project Create / Edit: FORM profile.
- FX11-S03 — Project detail: DETAIL profile.
- FX11-S04 — Complete / Skip Project: DIALOG profile.
- FX11-S05 — Project history: HISTORY profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX11-S01 | Projects Grid / Table | /projects | Title; Start; End only in card/row | New Project |
| FX11-S02 | Project Create / Edit | /projects/new; /projects/:projectId/edit | Title; Description; Start; End; optional Priority/Tags/Color-or-Icon/Notes | Save Project |
| FX11-S03 | Project detail | /projects/:projectId | Title; status; period; description; Task Kanban default/Table toggle; metadata | Add Task while active |
| FX11-S04 | Complete / Skip Project | /projects/:projectId/close | Current state; unfinished Task count/list; irreversible lock warning | Complete Project / Skip Project |
| FX11-S05 | Project history | /projects/:projectId/history | Every Project change; version number; timestamp; changed fields; owner reason | Inspect version |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Projects Grid → New Project → Save → Project detail Kanban → create/work Tasks → Complete or Skip with required warning/reason → locked detail.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX11-S01 — Projects Grid / Table

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; Start; End only in card/row |
| Entry / proposed route | /projects; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Projects Grid / Table. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Project; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Grid/Table switch; item overflow. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; Start; End only in card/row |
| Search / filters / sorting / pagination | Title search; Tag/Status/time; Title ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Click title/card opens detail. Overflow Edit/Share/Move to Trash/History obey state. Default Grid, never Projects Kanban. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Projects Grid / Table' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX11-S02 — Project Create / Edit

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title; Description; Start; End; optional Priority/Tags/Color-or-Icon/Notes |
| Entry / proposed route | /projects/new; /projects/:projectId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Project Create / Edit. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Project; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; Description; Start; End; optional Priority/Tags/Color-or-Icon/Notes |
| Search / filters / sorting / pagination | Datetime zone visible; tags inline select/create. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Out-of-period Task warning only when editing range; confirmation preserves existing Task times. Terminal rejects stale form submit. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Project Create / Edit' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX11-S03 — Project detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Title; status; period; description; Task Kanban default/Table toggle; metadata |
| Entry / proposed route | /projects/:projectId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Project detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Add Task while active; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit; Share; Complete; Skip; History; Move to Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; status; period; description; Task Kanban default/Table toggle; metadata |
| Search / filters / sorting / pagination | Task controls from FX-12; no standalone Project Calendar. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Terminal banner states permanent readonly and unfinished Tasks cannot continue. All Tasks terminal prompts confirmation once per relevant revision, no auto-close/empty-project prompt. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Project detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX11-S04 — Complete / Skip Project

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Current state; unfinished Task count/list; irreversible lock warning |
| Entry / proposed route | /projects/:projectId/close; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Complete / Skip Project. Named modal with preview/reason and footer actions. |
| Primary action | Complete Project / Skip Project; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current state; unfinished Task count/list; irreversible lock warning |
| Search / filters / sorting / pagination | Unfinished preview pageable; completion reason required if any open. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Keep Task states unchanged. Skip does not silently skip all Tasks. Warn no reopen/no further Task work. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Complete / Skip Project' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX11-S05 — Project history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — Every Project change; version number; timestamp; changed fields; owner reason |
| Entry / proposed route | /projects/:projectId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Project history. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Inspect version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back to Project. Back/Cancel always has authorized fallback. |
| Content regions / fields | Every Project change; version number; timestamp; changed fields; owner reason |
| Search / filters / sorting / pagination | Latest first25/page; version preview readonly. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No generic Restore Project button: full Task version restore does not approve Project reopen/restore workflow. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Project history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand. Exact approved card/table fields in section12 override generic priority choices. |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Create/Edit required Title1..200, Description nonblank, Start and End datetime with End>Start. Optional PriorityP0..P3, many Project/Task-shared Tags, Color or Icon, Notes. Initial NotStarted. Editing Project times previews out-of-range Tasks but never reschedules them.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Projects default Grid, Table alternative. Visible card/row fields exactly Title, Start, End. Search Title; filters Tag, Status, time overlap; Title A–Z then Id;25/page. Status is filter/detail, not an added card column.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Projects listing fields remain Title/Start/End, default Grid.

## 13. Search / Filter / Sort

Projects default Grid, Table alternative. Visible card/row fields exactly Title, Start, End. Search Title; filters Tag, Status, time overlap; Title A–Z then Id;25/page. Status is filter/detail, not an added card column.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. Project terminal is irreversible even when unfinished Tasks remain.

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| NotStarted/InProgress | Edit; add Task; Share; History; Move to Trash; Complete/Skip | Backward Project InProgress→NotStarted reason required delegated |
| Completed/Skipped | Readonly detail/Tasks; Share if eligible; History; Trash | No reopen, edit, Task create/edit/restore/continue |
| Trash | Restore whole deletion cohort or purge after checks | No individual Task restore while parent trashed |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Complete / Skip Project | Current state; unfinished Task count/list; irreversible lock warning | Complete Project / Skip Project / Cancel | Keep Task states unchanged. Skip does not silently skip all Tasks. Warn no reopen/no further Task work. |
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
| [productivity.Project](../../design-database/05-productivity-calendar.md#productivity-project) | Personal project and terminal locking boundary; Technical decision |
| [productivity.Tag](../../design-database/05-productivity-calendar.md#productivity-tag) | User-created shared Project/Task tag namespace; Technical decision |
| [productivity.ProjectTag](../../design-database/05-productivity-calendar.md#productivity-projecttag) | Many tags per Project; Technical decision |
| [productivity.ProjectVersion](../../design-database/05-productivity-calendar.md#productivity-projectversion) | Immutable complete editable Project snapshot; Technical decision |

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

[FX-11 action catalog](../../action-catalog/modules/11-projects.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
