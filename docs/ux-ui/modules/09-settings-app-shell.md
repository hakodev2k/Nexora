# FX-09 — Settings / App Shell — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

One personal app shell and consistent Settings IA, no Workspace navigation.

## 2. Requirement sources

- [FX-09 feature](../../features/09-settings-and-app-shell.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-09-BR-001`, `FX-09-BR-002`, `FX-09-BR-003`, `FX-09-BR-004`, `FX-09-BR-005`, `FX-09-AC-001`, `FX-09-AC-002`, `FX-09-AC-003`, `P01-PLT-005`, `P01-SHL-001`, `P01-SHL-002`, `P01-SHL-003`, `P01-SHL-004`, `P01-SHL-005`, `P01-SHL-006`, `P01-SHL-007`, `P02-TZ-001`, `P02-TZ-002`, `P02-TZ-003`

## 3. Reference products

- [Notion](https://www.notion.com/help/navigate-with-the-sidebar) — checked2026-09-07. Evidence limit: Official help text only.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Notion](https://www.notion.com/help/navigate-with-the-sidebar) | Sidebar separates navigation and Trash; trashed pages are not editable before restore. | ADAPT | Collapsible module groups and module-local tree; reject workspace/teamspace switch and retention policy. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX09-S01 — Settings home: BROWSE profile.
- FX09-S02 — Preferences: FORM profile.
- FX09-S03 — My modules: BROWSE profile.
- FX09-S04 — Unavailable module route: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX09-S01 | Settings home | /settings | Sections Profile, Security & Sessions, Preferences, Modules, Sharing, Notification delivery, Data operations | Open section |
| FX09-S02 | Preferences | /settings/preferences | Theme; density; navigation collapse/pins; current timezone link | Save preferences |
| FX09-S03 | My modules | /settings/modules | Allowed installed modules; effective state; configured settings entry | Open module settings |
| FX09-S04 | Unavailable module route | /modules/:moduleCode/unavailable | Module name if known/allowed; unavailable state; retained-data explanation | Go Home |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Home → module group → feature → breadcrumb back; Settings → Profile/Security/Preferences/Modules/Sharing → explicit Save.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX09-S01 — Settings home

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Sections Profile, Security & Sessions, Preferences, Modules, Sharing, Notification delivery, Data operations |
| Entry / proposed route | /settings; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Settings home. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open section; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back to prior module. Back/Cancel always has authorized fallback. |
| Content regions / fields | Sections Profile, Security & Sessions, Preferences, Modules, Sharing, Notification delivery, Data operations |
| Search / filters / sorting / pagination | Search setting label; grouped sections; no pagination. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Do not add Workspace, Billing plans or integrations not approved. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Settings home' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX09-S02 — Preferences

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Theme; density; navigation collapse/pins; current timezone link |
| Entry / proposed route | /settings/preferences; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Preferences. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save preferences; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; Reset display defaults. Back/Cancel always has authorized fallback. |
| Content regions / fields | Theme; density; navigation collapse/pins; current timezone link |
| Search / filters / sorting / pagination | No notification channel preferences. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Reset only display preferences, not data, grants or module lifecycle. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Preferences' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX09-S03 — My modules

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Allowed installed modules; effective state; configured settings entry |
| Entry / proposed route | /settings/modules; module/source navigation or authorized deep link. |
| Header / layout | Screen title: My modules. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open module settings; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open module; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Allowed installed modules; effective state; configured settings entry |
| Search / filters / sorting / pagination | Name/group search; group/name ordering. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Cannot self-grant disabled modules; guidance names administrative control without exposing Admin private info. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'My modules' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX09-S04 — Unavailable module route

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Module name if known/allowed; unavailable state; retained-data explanation |
| Entry / proposed route | /modules/:moduleCode/unavailable; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Unavailable module route. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Go Home; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back; Settings modules. Back/Cancel always has authorized fallback. |
| Content regions / fields | Module name if known/allowed; unavailable state; retained-data explanation |
| Search / filters / sorting / pagination | No content filters. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Deep links/bookmarks do not bypass gate; global navigation retains ability to leave on mobile. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Unavailable module route' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Preference fields limited to theme, density, navigation/pinned modules, view choices and confirmed timezone; UI-language/default-currency scope Q-09. Notification channels fixed, not toggleable. User module settings cannot override SuperAdmin grant.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Settings search labels/section names only; no personal content or secret value search. Menu groups collapse, module names discoverable in More/Search.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Settings search labels/section names only; no personal content or secret value search. Menu groups collapse, module names discoverable in More/Search.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Enabled module | Navigate and show active route | No duplicate module-specific shell |
| Disabled module | Retained link shows unavailable state with Home/Back | No stale content or blank route |
| Admin own account | Same personal shell | Admin area separate; no target-user switcher |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
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
| [platform.Preference](../../design-database/02-core-identity-platform.md#platform-preference) | Schema-validated non-secret preferences; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-09](../../features/90-open-decisions.md#q-09) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-09 action catalog](../../action-catalog/modules/09-settings.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
