# FX-03 — Module Platform — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Manage trusted installed modules, dependencies and per-user enablement; no install marketplace.

## 2. Requirement sources

- [FX-03 feature](../../features/03-module-platform.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-03-BR-001`, `FX-03-BR-002`, `FX-03-BR-003`, `FX-03-BR-004`, `FX-03-BR-005`, `FX-03-AC-001`, `FX-03-AC-002`, `FX-03-AC-003`, `P01-MOD-001`, `P01-MOD-002`, `P01-MOD-003`, `P01-MOD-004`, `P01-MOD-005`

## 3. Reference products

- [WordPress Plugins](https://wordpress.org/documentation/article/manage-plugins/) — checked2026-09-07. Evidence limit: Official help text; no plugin installed/tested.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [WordPress Plugins](https://wordpress.org/documentation/article/manage-plugins/) | Installed plugins have activation/deactivation management. | ADAPT | Installed/enabled/disabled/dependency states; reject executable upload and marketplace. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX03-S01 — Module catalog: BROWSE profile.
- FX03-S02 — Module detail: DETAIL profile.
- FX03-S03 — Enablement impact: DIALOG profile.
- FX03-S04 — Registration defaults: ADMIN profile.
- FX03-S05 — Module settings: FORM profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX03-S01 | Module catalog | /admin/modules | Name; installed version; Ready/disabled/failure label; dependency count | Open module |
| FX03-S02 | Module detail | /admin/modules/:moduleCode | Manifest summary; dependencies; capabilities; schema/migration state; user enablement summary | Enable / Disable |
| FX03-S03 | Enablement impact | /admin/modules/:moduleCode/enablement | Before/after state; affected users count; hard dependencies; queued work impact | Confirm enable / Disable module |
| FX03-S04 | Registration defaults | /admin/modules/registration-defaults | Current policy revision; default-enabled modules | Save defaults |
| FX03-S05 | Module settings | /admin/modules/:moduleCode/settings | Registered nonsecret settings and masked Vault references | Save settings |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Module catalog → module detail → inspect dependency impact → confirm enable/disable or registration defaults → see recorded policy revision.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX03-S01 — Module catalog

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; installed version; Ready/disabled/failure label; dependency count |
| Entry / proposed route | /admin/modules; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Module catalog. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open module; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Registration defaults. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; installed version; Ready/disabled/failure label; dependency count |
| Search / filters / sorting / pagination | Name/key search; state filter; name ASC. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Installed means deployed, not usable; state text and icon accompany color. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Module catalog' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX03-S02 — Module detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Manifest summary; dependencies; capabilities; schema/migration state; user enablement summary |
| Entry / proposed route | /admin/modules/:moduleCode; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Module detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Enable / Disable; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Settings; view diagnostics. Back/Cancel always has authorized fallback. |
| Content regions / fields | Manifest summary; dependencies; capabilities; schema/migration state; user enablement summary |
| Search / filters / sorting / pagination | Tabs Overview, Dependencies, Settings. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Show exact affected dependent modules and retained data; avoid secret settings values. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Module detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX03-S03 — Enablement impact

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Before/after state; affected users count; hard dependencies; queued work impact |
| Entry / proposed route | /admin/modules/:moduleCode/enablement; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Enablement impact. Named modal with preview/reason and footer actions. |
| Primary action | Confirm enable / Disable module; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Before/after state; affected users count; hard dependencies; queued work impact |
| Search / filters / sorting / pagination | Preview scroll, no search needed. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Do not auto-enable dependencies without explicit preview; error retains requested change, refreshes revision. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Enablement impact' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX03-S04 — Registration defaults

| Dimension | Specification |
| --- | --- |
| Purpose / profile | ADMIN — Current policy revision; default-enabled modules |
| Entry / proposed route | /admin/modules/registration-defaults; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Registration defaults. Separate Admin shell, grouped target metadata/matrix and before/after review. |
| Primary action | Save defaults; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current policy revision; default-enabled modules |
| Search / filters / sorting / pagination | Name search; changed-only. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Changes apply future verified registrations, not silently mutate existing user grants. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 admin profile. Keep 'Registration defaults' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX03-S05 — Module settings

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Registered nonsecret settings and masked Vault references |
| Entry / proposed route | /admin/modules/:moduleCode/settings; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Module settings. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save settings; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Registered nonsecret settings and masked Vault references |
| Search / filters / sorting / pagination | Schema-specific required fields. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unknown/deprecated settings fail with compatibility guidance; no generic JSON secret editor. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Module settings' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

System module settings and registration defaults are SuperAdmin controlled; schema-validated options only. All current R1 modules initially default on at verified registration. No executable file upload, arbitrary migration button or user-authored schema.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Catalog search name/key; state/dependency filter; name ASC;25/page; no price/popularity marketplace ranking.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Catalog search name/key; state/dependency filter; name ASC;25/page; no price/popularity marketplace ranking.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Ready enabled | Open settings; authorized disable | Required dependent impact preview |
| Disabled | Inspect retained-data explanation; authorized enable | No user resource read/action |
| DependencyFailed/MigrationFailed | Inspect redacted diagnostics | Enable blocked until deployment fixes dependency |
| UpgradePending | Show installed/required version | No user-triggered arbitrary code installation |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Enablement impact | Before/after state; affected users count; hard dependencies; queued work impact | Confirm enable / Disable module / Cancel | Do not auto-enable dependencies without explicit preview; error retains requested change, refreshes revision. |
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
| [platform.PersonalSpace](../../design-database/02-core-identity-platform.md#platform-personalspace) | Exactly one personal ownership boundary per verified User; Technical decision |
| [platform.Module](../../design-database/02-core-identity-platform.md#platform-module) | Trusted developer-installed module identity; Technical decision |
| [platform.ModuleRelease](../../design-database/02-core-identity-platform.md#platform-modulerelease) | Immutable trusted deployment metadata; Technical decision |
| [platform.ModuleDependency](../../design-database/02-core-identity-platform.md#platform-moduledependency) | Declared hard/optional dependency graph; Technical decision |
| [platform.ModuleMigration](../../design-database/02-core-identity-platform.md#platform-modulemigration) | Per-module migration journal design; Technical decision |
| [platform.UserModuleGrant](../../design-database/02-core-identity-platform.md#platform-usermodulegrant) | Per-user module enablement snapshot; Technical decision |
| [platform.Permission](../../design-database/02-core-identity-platform.md#platform-permission) | Developer-declared action catalog; Technical decision |
| [platform.ResourceType](../../design-database/02-core-identity-platform.md#platform-resourcetype) | Typed resource capability registration; Technical decision |
| [platform.Resource](../../design-database/02-core-identity-platform.md#platform-resource) | Identity/lifecycle directory, not generic content store; Technical decision |
| [platform.ResourceLink](../../design-database/02-core-identity-platform.md#platform-resourcelink) | Typed cross-module relationship guarded by providers; Technical decision |
| [operations.Idempotency](../../design-database/04-files-jobs-notifications.md#operations-idempotency) | Bounded command retry ledger; Technical decision |
| [operations.Outbox](../../design-database/04-files-jobs-notifications.md#operations-outbox) | Transactional event intents; Technical decision |
| [operations.InboxReceipt](../../design-database/04-files-jobs-notifications.md#operations-inboxreceipt) | Per-consumer deduplication; Technical decision |

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

[FX-03 action catalog](../../action-catalog/modules/03-modules.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
