# FX-22 — Snippets — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal code/text snippets with escaped rendering, complete versions and Copy, never execution.

## 2. Requirement sources

- [FX-22 feature](../../features/22-snippets.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-22-BR-001`, `FX-22-BR-002`, `FX-22-BR-003`, `FX-22-BR-004`, `FX-22-BR-005`, `FX-22-AC-001`, `FX-22-AC-002`, `FX-22-AC-003`, `P03-SNP-001`, `P03-SNP-002`, `P03-SNP-003`, `P03-SNP-004`

## 3. Reference products

- [GitHub Gists](https://docs.github.com/en/rest/gists/gists) — checked2026-09-07. Evidence limit: Official API evidence supports data/history, not visual layout or keyboard claims.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [GitHub Gists](https://docs.github.com/en/rest/gists/gists) | Gist API represents text files and version history. | ADAPT | Escaped code/language/history/copy; no executable snippets, GitHub publishing or collaboration. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX22-S01 — Snippet list: BROWSE profile.
- FX22-S02 — Snippet editor: EDITOR profile.
- FX22-S03 — Snippet detail / history: HISTORY profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX22-S01 | Snippet list | /snippets | Title; language; tags; updated time; safe code excerpt without execution | New Snippet |
| FX22-S02 | Snippet editor | /snippets/new; /snippets/:snippetId/edit | Title; Language; source code; optional description/tags | Save |
| FX22-S03 | Snippet detail / history | /snippets/:snippetId; /snippets/:snippetId/history | Escaped monospace body; Copy control; version list/diff; language/title | Copy code / Restore selected version |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Snippet list → create Title/language/code → Save → detail Copy/download → versions → restore as new version.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX22-S01 — Snippet list

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; language; tags; updated time; safe code excerpt without execution |
| Entry / proposed route | /snippets; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Snippet list. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Snippet; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; filter; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; language; tags; updated time; safe code excerpt without execution |
| Search / filters / sorting / pagination | Title/code/tag search; language/tag/status; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Long lines truncated with open-detail access; no syntax color-only language label. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Snippet list' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX22-S02 — Snippet editor

| Dimension | Specification |
| --- | --- |
| Purpose / profile | EDITOR — Title; Language; source code; optional description/tags |
| Entry / proposed route | /snippets/new; /snippets/:snippetId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Snippet editor. Header with current lifecycle + unsaved badge + explicit Save; editor canvas and source navigation. |
| Primary action | Save; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; Preview escaped text. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; Language; source code; optional description/tags |
| Search / filters / sorting / pagination | Language select searchable. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Tab indentation only inside editor with documented escape-focus shortcut; never trap keyboard. Clipboard read only explicit paste. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 editor profile. Keep 'Snippet editor' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX22-S03 — Snippet detail / history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — Escaped monospace body; Copy control; version list/diff; language/title |
| Entry / proposed route | /snippets/:snippetId; /snippets/:snippetId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Snippet detail / history. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Copy code / Restore selected version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit; Download text; Archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Escaped monospace body; Copy control; version list/diff; language/title |
| Search / filters / sorting / pagination | Versions latest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Copy toast says Copied without echoing content. Diff accessible side-by-side or unified text, no executable rendering. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Snippet detail / history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Title1..200 and code nonempty<=1MiB required; Language registered enum with plain text; description/tags optional. Secret warning suggests Vault; cannot guarantee detection. Version includes Title/language/body; restore creates new version.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Search Title/code/tags within own permitted snippets; language/tag/status filters; Updated DESC25/page; no search in Vault through snippets.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Search Title/code/tags within own permitted snippets; language/tag/status filters; Updated DESC25/page; no search in Vault through snippets.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Active | Edit/Save; Copy; plaintext download; history; Archive/Trash | No Run or Evaluate action |
| Archived | Readonly view/Copy owner; Unarchive; Trash | No edit |
| History | Inspect diff; eligible restore as new | Never overwrite old version |
| Trash | Restore/purge if allowed | No public access |

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
| [knowledge.Snippet](../../design-database/06-documents-knowledge-discovery.md#knowledge-snippet) | Escaped source text that Nexora never executes; Technical decision |
| [knowledge.SnippetVersion](../../design-database/06-documents-knowledge-discovery.md#knowledge-snippetversion) | Immutable snippet source/history; Technical decision |

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

[FX-22 action catalog](../../action-catalog/modules/22-snippets.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
