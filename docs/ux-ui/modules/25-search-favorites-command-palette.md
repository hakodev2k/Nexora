# FX-25 — Search / Favorites / Command Palette — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Unified authorized Search/Favorites/Recents and keyboard command palette, distinct from local feature search.

## 2. Requirement sources

- [FX-25 feature](../../features/25-search-favorites-and-command-palette.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-25-BR-001`, `FX-25-BR-002`, `FX-25-BR-003`, `FX-25-BR-004`, `FX-25-BR-005`, `FX-25-BR-006`, `FX-25-AC-001`, `FX-25-AC-002`, `FX-25-AC-003`, `P03-SRC-001`, `P03-SRC-002`, `P03-SRC-003`, `P03-SRC-004`, `P03-SRC-005`, `P03-SRC-006`, `P03-SRC-007`, `P03-SRC-008`, `P03-SRC-009`

## 3. Reference products

- [Linear Search](https://linear.app/docs/search) — checked2026-09-07. Evidence limit: Official docs; Nexora Cmd/Ctrl+K decision is delegated interaction, not claimed Linear Search shortcut.
- [Linear](https://linear.app/docs/select-issues) — checked2026-09-07. Evidence limit: Official docs; no exact Linear keyboard map imposed on Nexora.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Linear Search](https://linear.app/docs/search) | Global search and find-in-view are distinct scopes with keyboard entry points. | ADAPT | Global command palette versus local Documents direct-location search; no comments/team scope. |
| [Linear](https://linear.app/docs/select-issues) | Selection, context command bar and keyboard reordering provide alternatives to pointer actions. | ADAPT | Task Move/Reorder commands with same validation as drag; reason-required backward moves remain Nexora-specific. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX25-S01 — Global Search: BROWSE profile.
- FX25-S02 — Command palette: DIALOG profile.
- FX25-S03 — Favorites / recents: BROWSE profile.
- FX25-S04 — Saved searches: BROWSE profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX25-S01 | Global Search | /search | Query; permitted facets; grouped safe hits Title/type/path/snippet/updated; provider freshness | Open result |
| FX25-S02 | Command palette | /command-palette | Search input; recent sources; commands grouped Navigate/Create/Actions; shortcut hints | Run selected command / Open result |
| FX25-S03 | Favorites / recents | /favorites; /recent | Safe source title/type/path; favorite order or last opened | Open resource |
| FX25-S04 | Saved searches | /search/saved | Name; filter summary; schema compatibility | Run saved search |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Cmd/Ctrl+K or Search → type → results scoped to current allowed providers → open source; save search/favorite → revisit with fresh access check.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX25-S01 — Global Search

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Query; permitted facets; grouped safe hits Title/type/path/snippet/updated; provider freshness |
| Entry / proposed route | /search; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Global Search. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open result; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Save query; Favorite; Clear filters. Back/Cancel always has authorized fallback. |
| Content regions / fields | Query; permitted facets; grouped safe hits Title/type/path/snippet/updated; provider freshness |
| Search / filters / sorting / pagination | Query500; module/type/tag/date; relevance;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Press Enter in query executes; Escape clears suggestion overlay not committed filters. Source failure distinguish empty vs partial. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Global Search' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX25-S02 — Command palette

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Search input; recent sources; commands grouped Navigate/Create/Actions; shortcut hints |
| Entry / proposed route | /command-palette; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Command palette. Named modal with preview/reason and footer actions. |
| Primary action | Run selected command / Open result; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Close. Back/Cancel always has authorized fallback. |
| Content regions / fields | Search input; recent sources; commands grouped Navigate/Create/Actions; shortcut hints |
| Search / filters / sorting / pagination | Cmd/Ctrl+K global except reserved editor handling; arrows/Enter/Escape. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Modal combobox/listbox; no action on mere highlight. Destructive action closes palette into normal dialog with focus return chain. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Command palette' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX25-S03 — Favorites / recents

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Safe source title/type/path; favorite order or last opened |
| Entry / proposed route | /favorites; /recent; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Favorites / recents. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open resource; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Remove favorite; reorder; clear recent metadata. Back/Cancel always has authorized fallback. |
| Content regions / fields | Safe source title/type/path; favorite order or last opened |
| Search / filters / sorting / pagination | Type/module filter; favorite rank / recent DESC. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Favorites do not keep trashed resource visible; safe unavailable label allows removal. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Favorites / recents' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX25-S04 — Saved searches

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; filter summary; schema compatibility |
| Entry / proposed route | /search/saved; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Saved searches. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Run saved search; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Rename; delete definition. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; filter summary; schema compatibility |
| Search / filters / sorting / pagination | Name/module; Name ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Run validates current available fields/provider versions; incompatible filter asks edit, never executes unknown query. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Saved searches' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Query<=500; SavedSearch name<=100; validated filter/sort schema; no raw SQL. Recents max100 safe refs. Search excludes Vault payload/secret values and hides unavailable private content. Destructive palette actions launch normal confirmation.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Global query supports available module/type/tag/date facets; relevance then updated/id;25/page. Palette recent authorized items before typing; typed command/results grouped; local Documents search stays direct location.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Global query supports available module/type/tag/date facets; relevance then updated/id;25/page. Palette recent authorized items before typing; typed command/results grouped; local Documents search stays direct location.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| No query | Show authorized recent items/commands | No secret preview |
| Results | Keyboard select; open source; save search/favorite | Every hit rechecked current owner/module/lifecycle |
| Some provider failed | Partial-results banner per provider | Do not label complete/zero results |
| All providers unavailable | Retry/leave | Not No results |
| Revoked/disabled source | Clear stale hit and safe unavailable | No cached authorization |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Command palette | Search input; recent sources; commands grouped Navigate/Create/Actions; shortcut hints | Run selected command / Open result / Close | Modal combobox/listbox; no action on mere highlight. Destructive action closes palette into normal dialog with focus return chain. |
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
| [platform.ResourceType](../../design-database/02-core-identity-platform.md#platform-resourcetype) | Typed resource capability registration; Technical decision |
| [platform.Resource](../../design-database/02-core-identity-platform.md#platform-resource) | Identity/lifecycle directory, not generic content store; Technical decision |
| [discovery.SearchProjection](../../design-database/06-documents-knowledge-discovery.md#discovery-searchprojection) | Rebuildable safe search index projection; Technical decision |
| [discovery.SavedQuery](../../design-database/06-documents-knowledge-discovery.md#discovery-savedquery) | Named validated query, not raw SQL; Technical decision |
| [discovery.Favorite](../../design-database/06-documents-knowledge-discovery.md#discovery-favorite) | Owner favorite reference; Technical decision |
| [discovery.RecentItem](../../design-database/06-documents-knowledge-discovery.md#discovery-recentitem) | Bounded owner recents; Technical decision |

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

[FX-25 action catalog](../../action-catalog/modules/25-discovery.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
