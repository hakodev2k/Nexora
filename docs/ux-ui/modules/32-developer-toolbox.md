# FX-32 — Developer Toolbox — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Trusted developer utilities in a consistent input/output workbench; no executable plugins or user scripts.

## 2. Requirement sources

- [FX-32 feature](../../features/32-developer-toolbox.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-32-BR-001`, `FX-32-BR-002`, `FX-32-BR-003`, `FX-32-BR-004`, `FX-32-BR-005`, `FX-32-AC-001`, `FX-32-AC-002`, `FX-32-AC-003`, `P06-DAT-001`, `P06-DAT-002`, `P06-DAT-003`, `P06-DAT-004`, `P06-DEV-001`, `P06-DEV-002`, `P06-DEV-003`, `P06-DEV-004`, `P06-ENC-001`, `P06-ENC-002`, `P06-NET-001`, `P06-NET-002`, `P06-NET-003`, `P06-NET-004`, `P06-NET-005`, `P06-SEC-001`, `P06-SEC-002`, `P06-SEC-003`, `P06-SEC-004`, `P06-SEC-005`, `P06-TBX-001`, `P06-TBX-002`, `P06-TBX-003`, `P06-TBX-004`, `P06-TBX-005`, `P06-TBX-006`, `P06-TBX-007`, `P06-TBX-008`, `P06-TME-001`, `P06-TME-002`, `P06-TME-003`

## 3. Reference products

- [DevToys](https://devtoys.app/) — checked2026-09-07. Evidence limit: Official desktop product page, not evidence of web privacy or browser clipboard behavior.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [DevToys](https://devtoys.app/) | Offline utility catalog includes converters, formatters, text comparison and generators. | ADAPT | Web input/output workbench, memory-only default, explicit network warning; reject automatic clipboard reading/executable extensions. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX32-S01 — Tool catalog: BROWSE profile.
- FX32-S02 — Tool workbench: WORKBENCH profile.
- FX32-S03 — Tool history / favorites: BROWSE profile.
- FX32-S04 — Network request preview: DIALOG profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX32-S01 | Tool catalog | /developer/tools | Tool name; category; short purpose; Local/Server/Network badge | Open tool |
| FX32-S02 | Tool workbench | /developer/tools/:toolCode | Input editor; options; Run; output; validation/error location; privacy badge | Run tool |
| FX32-S03 | Tool history / favorites | /developer/tools/saved | Favorite names; explicitly opted-in safe history entries/time; privacy reminder | Open tool |
| FX32-S04 | Network request preview | /developer/tools/:toolCode/network-preview | Exact target; method; public address validation; data egress fields; permission state | Run approved request |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Tool catalog → choose utility → explicit paste/type → configure → Run → inspect/copy/download safe output → Clear; network tools require approved egress.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX32-S01 — Tool catalog

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Tool name; category; short purpose; Local/Server/Network badge |
| Entry / proposed route | /developer/tools; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Tool catalog. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open tool; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Favorite; filter. Back/Cancel always has authorized fallback. |
| Content regions / fields | Tool name; category; short purpose; Local/Server/Network badge |
| Search / filters / sorting / pagination | Name/category; favorites then name;25/page if needed. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Execution location is visible before pasting sensitive input, not buried after Run. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Tool catalog' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX32-S02 — Tool workbench

| Dimension | Specification |
| --- | --- |
| Purpose / profile | WORKBENCH — Input editor; options; Run; output; validation/error location; privacy badge |
| Entry / proposed route | /developer/tools/:toolCode; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Tool workbench. Shared workbench profile; header → controls → declared content → feedback. |
| Primary action | Run tool; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Copy input/output; Download; Clear; Favorite. Back/Cancel always has authorized fallback. |
| Content regions / fields | Input editor; options; Run; output; validation/error location; privacy badge |
| Search / filters / sorting / pagination | Options specific to trusted tool schema. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Desktop split input/output; mobile Input/Output tabs with result badge and Back; no Run action in Snippets just because same editor reused. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 workbench profile. Keep 'Tool workbench' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX32-S03 — Tool history / favorites

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Favorite names; explicitly opted-in safe history entries/time; privacy reminder |
| Entry / proposed route | /developer/tools/saved; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Tool history / favorites. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open tool; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Delete history; clear selected metadata. Back/Cancel always has authorized fallback. |
| Content regions / fields | Favorite names; explicitly opted-in safe history entries/time; privacy reminder |
| Search / filters / sorting / pagination | Tool/date; newest history25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Default no input history. Secret-capable tools cannot opt into unsafe persistence. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Tool history / favorites' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX32-S04 — Network request preview

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Exact target; method; public address validation; data egress fields; permission state |
| Entry / proposed route | /developer/tools/:toolCode/network-preview; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Network request preview. Named modal with preview/reason and footer actions. |
| Primary action | Run approved request; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Exact target; method; public address validation; data egress fields; permission state |
| Search / filters / sorting / pagination | No arbitrary auth headers or script payload. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Q-07 gate; redirect/DNS/response limits server-enforced; browser error never offers bypass. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Network request preview' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Pure input<=1MiB/output<=2MiB delegated. Validators show line/column/path where available. Inputs memory-only default, optional safe-history opt-in excludes secrets. JWT Decode explicitly not signature verification; MD5/SHA1 legacy checksum warning; random UUID/password uses CSPRNG.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Catalog search name/keyword; categories; favorites first then label. Workbench no list pagination; input/output each independently scrollable with error summary and labeled copy.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Catalog search name/keyword; categories; favorites first then label. Workbench no list pagination; input/output each independently scrollable with error summary and labeled copy.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Valid pure input | Run; copy/download result; Clear | No network without capability declaration |
| Invalid input | Edit with line/path error | No partial result labeled success |
| Network tool policy missing | Show blocked explanation and safe alternatives | No unapproved public/private target fetch |
| Potential secret input | Warn, remain memory-only | No automatic history/analytics/clipboard reading |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Network request preview | Exact target; method; public address validation; data egress fields; permission state | Run approved request / Cancel | Q-07 gate; redirect/DNS/response limits server-enforced; browser error never offers bypass. |
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

Workbench variants: JSON/XML/YAML/CSV input editor → format/convert options → syntax/path errors; Base64/URL/HTML/UTF8 text → encode/decode; Hash input+algorithm → digest with legacy warning; JWT token → decoded header/payload with Unverified label; Date ISO/epoch seconds/milliseconds → timezone-aware result; Cron5-field+zone → next10occurrences including DST explanation; Regex pattern+sample → bounded matches or timeout; Diff two inputs → unified accessible comparison; QR text → preview+download without automatic external navigation; ColorHEX/RGB/HSL/alpha → values and contrast preview; Certificate public text → parsed validity/issuer, private-key warning. None execute user code.

Data design trace:

| Table / provider data | Purpose / dependency status |
| --- | --- |
| [developer.ToolDefinition](../../design-database/08-news-shopping-developer.md#developer-tooldefinition) | Trusted utility discovery and privacy capabilities; Technical decision |
| [developer.ToolFavorite](../../design-database/08-news-shopping-developer.md#developer-toolfavorite) | Favorite tool only, no input data; Technical decision |
| [developer.ToolHistory](../../design-database/08-news-shopping-developer.md#developer-toolhistory) | Optional explicit opt-in safe tool history; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-07](../../features/90-open-decisions.md#q-07) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-32 action catalog](../../action-catalog/modules/32-toolbox.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
