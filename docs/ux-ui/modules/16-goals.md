# FX-16 — Goals — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal Goals with immutable target kind Numeric/Boolean/Tasks and source-derived progress.

## 2. Requirement sources

- [FX-16 feature](../../features/16-goals.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-16-BR-001`, `FX-16-BR-002`, `FX-16-BR-003`, `FX-16-BR-004`, `FX-16-BR-005`, `FX-16-AC-001`, `FX-16-AC-002`, `FX-16-AC-003`

## 3. Reference products

- [ClickUp Goals](https://clickup.com/features/goals) — checked2026-09-07. Evidence limit: Official product page, not authenticated workflow; exact formula is Nexora delegated rule.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [ClickUp Goals](https://clickup.com/features/goals) | Numeric, true/false and Task targets show goal progress. | ADAPT | Personal equal-weight targets; explicit Goal status and Nexora formula; reject teams/financial automation. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX16-S01 — Goal list: BROWSE profile.
- FX16-S02 — Goal create / edit: FORM profile.
- FX16-S03 — Goal detail: DETAIL profile.
- FX16-S04 — Progress update: DIALOG profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX16-S01 | Goal list | /goals | Title; status; deadline; progress number/bar; no-target label | New Goal |
| FX16-S02 | Goal create / edit | /goals/new; /goals/:goalId/edit | Title; Description; deadline; target definitions | Save Goal |
| FX16-S03 | Goal detail | /goals/:goalId | Goal state/period; target rows; progress; linked Task availability; history | Update progress |
| FX16-S04 | Progress update | /goals/:goalId/progress | Selected numeric/boolean target; old/new value; optional note | Record progress |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Goal list → create Goal → add targets → record numeric/boolean progress or select Tasks → review ratio → explicitly complete/abandon.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX16-S01 — Goal list

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; status; deadline; progress number/bar; no-target label |
| Entry / proposed route | /goals; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Goal list. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Goal; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; Archive; Move to Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; status; deadline; progress number/bar; no-target label |
| Search / filters / sorting / pagination | Title/status/deadline; deadline/title;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Bar accompanied number/text; completed is explicit state, not inferred bar color. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Goal list' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX16-S02 — Goal create / edit

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title; Description; deadline; target definitions |
| Entry / proposed route | /goals/new; /goals/:goalId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Goal create / edit. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Goal; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; add target. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; Description; deadline; target definitions |
| Search / filters / sorting / pagination | Target-kind select and kind-specific fields. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Target type immutable once created; changing progress does not quietly change target baseline. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Goal create / edit' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX16-S03 — Goal detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Goal state/period; target rows; progress; linked Task availability; history |
| Entry / proposed route | /goals/:goalId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Goal detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Update progress; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Complete; Abandon; Reopen; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Goal state/period; target rows; progress; linked Task availability; history |
| Search / filters / sorting / pagination | Targets ordered; no independent Task edit controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unavailable Task remains denominator with explicit remove-link action; Goal deletion never deletes Tasks. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Goal detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX16-S04 — Progress update

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Selected numeric/boolean target; old/new value; optional note |
| Entry / proposed route | /goals/:goalId/progress; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Progress update. Named modal with preview/reason and footer actions. |
| Primary action | Record progress; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Selected numeric/boolean target; old/new value; optional note |
| Search / filters / sorting / pagination | Numeric precision retained; target ratio preview. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Tasks-kind target is readonly source projection; open Task instead of marking via Goal. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Progress update' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Title1..200 required; Description/deadline optional. Target kind chosen once, Numeric initial/current/target with target>initial, Boolean checked, Tasks one or more same-owner refs. Numeric clamp((current-initial)/(target-initial),0,1); equal-weight targets; no targets0% not complete.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Goal Title search; Status/deadline filters; active deadlines ascending then title/id delegated; progress shown one decimal without rounding source values.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Goal Title search; Status/deadline filters; active deadlines ascending then title/id delegated; progress shown one decimal without rounding source values.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Draft/Active | Edit target/progress; complete/abandon; archive; Trash | No automatic status from100% |
| Completed/Abandoned | Explicit reopen to Active with history; archive | Never applies reopen rule to Project |
| Archived | Readonly; Unarchive prior state; Trash | No progress editing |
| Task target unavailable | Explain missing target; owner may remove link | Do not remove denominator or count Skipped as Completed |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Progress update | Selected numeric/boolean target; old/new value; optional note | Record progress / Cancel | Tasks-kind target is readonly source projection; open Task instead of marking via Goal. |
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
| [productivity.Goal](../../design-database/05-productivity-calendar.md#productivity-goal) | Personal goal with explicit status independent of computed progress; Technical decision |
| [productivity.GoalTarget](../../design-database/05-productivity-calendar.md#productivity-goaltarget) | Numeric, Boolean or Task-linked target; Technical decision |
| [productivity.GoalProgress](../../design-database/05-productivity-calendar.md#productivity-goalprogress) | Manual progress evidence; Technical decision |
| [productivity.GoalTargetTask](../../design-database/05-productivity-calendar.md#productivity-goaltargettask) | Multiple immutable-type target Task references; Technical decision |

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

[FX-16 action catalog](../../action-catalog/modules/16-goals.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
