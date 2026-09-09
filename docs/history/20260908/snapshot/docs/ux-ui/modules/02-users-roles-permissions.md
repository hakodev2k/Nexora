# FX-02 — Users / Roles / Permissions — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Admin/SuperAdmin operational account and grant management, never ambient access to user content.

## 2. Requirement sources

- [FX-02 feature](../../features/02-users-roles-and-permissions.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-02-BR-001`, `FX-02-BR-002`, `FX-02-BR-003`, `FX-02-BR-004`, `FX-02-BR-005`, `FX-02-AC-001`, `FX-02-AC-002`, `FX-02-AC-003`, `P01-OWN-001`, `P01-OWN-002`, `P01-OWN-003`, `P01-RBAC-001`, `P01-RBAC-002`, `P01-RBAC-003`, `P01-RBAC-004`, `P01-RBAC-005`, `P01-RBAC-006`, `P01-RBAC-007`, `P01-RBAC-008`

## 3. Reference products

- [WordPress](https://wordpress.org/documentation/article/roles-and-capabilities/) — checked2026-09-07. Evidence limit: Official documentation, WordPress roles are not Nexora roles.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [WordPress](https://wordpress.org/documentation/article/roles-and-capabilities/) | Roles group capabilities governing administrative actions. | ADAPT | Use explicit Nexora system roles/action grants; reject access to others personal content from role alone. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX02-S01 — Users: BROWSE profile.
- FX02-S02 — User detail: DETAIL profile.
- FX02-S03 — Role assignment: FORM profile.
- FX02-S04 — Permission matrix: ADMIN profile.
- FX02-S05 — Module grants: ADMIN profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX02-S01 | Users | /admin/users | DisplayName; Email; account status; system role | Open User |
| FX02-S02 | User detail | /admin/users/:userId | Profile metadata; verification/account state; assigned roles; module summary | Manage grants |
| FX02-S03 | Role assignment | /admin/users/:userId/roles | Current roles and proposed role set; risk explanation | Review change |
| FX02-S04 | Permission matrix | /admin/users/:userId/permissions | Modules as groups; action label/key; explicit effect and effective result | Review permissions |
| FX02-S05 | Module grants | /admin/users/:userId/modules | Module; installed/system state; user enabled; blocked dependency | Review module changes |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Admin Users → select User → review role/module/action differences → confirm authorized change → audit result.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX02-S01 — Users

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — DisplayName; Email; account status; system role |
| Entry / proposed route | /admin/users; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Users. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open User; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Filter; select. Back/Cancel always has authorized fallback. |
| Content regions / fields | DisplayName; Email; account status; system role |
| Search / filters / sorting / pagination | Search name/email; state/role; name ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Rows navigate metadata detail, never personal Projects/Documents. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Users' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX02-S02 — User detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Profile metadata; verification/account state; assigned roles; module summary |
| Entry / proposed route | /admin/users/:userId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: User detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Manage grants; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Sessions administration if allowed; Audit. Back/Cancel always has authorized fallback. |
| Content regions / fields | Profile metadata; verification/account state; assigned roles; module summary |
| Search / filters / sorting / pagination | Tabs Account, Roles, Modules, Audit. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No impersonate button. Emergency/Support separate explicit flows with permission. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'User detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX02-S03 — Role assignment

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Current roles and proposed role set; risk explanation |
| Entry / proposed route | /admin/users/:userId/roles; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Role assignment. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Review change; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current roles and proposed role set; risk explanation |
| Search / filters / sorting / pagination | Fixed role choices; no custom Workspace role. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Show before/after, affected admin capabilities; last-admin check repeated server-side. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Role assignment' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX02-S04 — Permission matrix

| Dimension | Specification |
| --- | --- |
| Purpose / profile | ADMIN — Modules as groups; action label/key; explicit effect and effective result |
| Entry / proposed route | /admin/users/:userId/permissions; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Permission matrix. Separate Admin shell, grouped target metadata/matrix and before/after review. |
| Primary action | Review permissions; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Reset unsaved changes. Back/Cancel always has authorized fallback. |
| Content regions / fields | Modules as groups; action label/key; explicit effect and effective result |
| Search / filters / sorting / pagination | Module/action search; changed-only toggle. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No ambiguous unchecked=inherit without effective label; deny wins; matrix keyboard cell controls have labels. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 admin profile. Keep 'Permission matrix' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX02-S05 — Module grants

| Dimension | Specification |
| --- | --- |
| Purpose / profile | ADMIN — Module; installed/system state; user enabled; blocked dependency |
| Entry / proposed route | /admin/users/:userId/modules; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Module grants. Separate Admin shell, grouped target metadata/matrix and before/after review. |
| Primary action | Review module changes; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Module; installed/system state; user enabled; blocked dependency |
| Search / filters / sorting / pagination | Module search; enabled/disabled filter. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | User grant cannot override system disable; preview pending job/access impact and preserve data. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 admin profile. Keep 'Module grants' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

User search uses email/display name only with access.user.read permission. Role selection fixed User/Admin/SuperAdmin; grant editor explicit Allow/Deny per registered action. Only SuperAdmin commits role/module/Admin-action grants; Admin may inspect authorized metadata, never self-elevate; last active SuperAdmin protection.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

User list email/display-name search, account state/role filters, DisplayName ASC then Id,25/page. Matrix group by module; search action key/label; selected changes only preview.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

User list email/display-name search, account state/role filters, DisplayName ASC then Id,25/page. Matrix group by module; search action key/label; selected changes only preview.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Authorized operator | List account metadata; review grants within own permission | No business data tabs |
| Last active SuperAdmin | Inspect | Demote/disable blocked with explanation |
| Permission revoked | Return to authorized Admin landing | Clear target cache; no resubmit stale grant |

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
| [identity.User](../../design-database/02-core-identity-platform.md#identity-user) | Authentication principal; not a business-data owner; Technical decision |
| [identity.Role](../../design-database/02-core-identity-platform.md#identity-role) | Fixed system role catalog; Technical decision |
| [identity.UserRole](../../design-database/02-core-identity-platform.md#identity-userrole) | System role membership; Technical decision |
| [platform.SecurityInvariant](../../design-database/02-core-identity-platform.md#platform-securityinvariant) | Serialization point for security invariants; Technical decision |
| [platform.UserModuleGrant](../../design-database/02-core-identity-platform.md#platform-usermodulegrant) | Per-user module enablement snapshot; Technical decision |
| [platform.Permission](../../design-database/02-core-identity-platform.md#platform-permission) | Developer-declared action catalog; Technical decision |
| [platform.AdminPermission](../../design-database/02-core-identity-platform.md#platform-adminpermission) | Explicit Admin action allow/deny; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-02](../../features/90-open-decisions.md#q-02) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-02 action catalog](../../action-catalog/modules/02-access.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
