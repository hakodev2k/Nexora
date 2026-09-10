# FX-21 — Bookmarks — UX/UI Specification

> **Current decision amendment — 2026-09-10:** External URLs are inert metadata, no Open external. The local UI slice supports manual metadata CRUD and Active/Archived lifecycle; auto-fetch, tags, collections and sharing remain gated. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md) and [slice evidence](../../implementation/bookmarks-manual-slice.md) apply. Conflicting older proposal paragraphs below are historical.

Review 2026-09-10 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Manual metadata UI slice implemented locally on PR #4; runtime verification remains owner work.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Owner bookmarks with explicit metadata capture, collections/tags and source health.

## 2. Requirement sources

- [FX-21 feature](../../features/21-bookmarks.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-21-BR-001`, `FX-21-BR-002`, `FX-21-BR-003`, `FX-21-BR-004`, `FX-21-BR-005`, `FX-21-AC-001`, `FX-21-AC-002`, `FX-21-AC-003`, `P03-BMK-001`, `P03-BMK-002`, `P03-BMK-003`

## 3. Reference products

- [Raindrop.io](https://help.raindrop.io/quickstart) — checked2026-09-07. Evidence limit: Official help text; browser extension is not added to Nexora scope.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Raindrop.io](https://help.raindrop.io/quickstart) | Saving a bookmark can assign a collection and tags. | ADAPT | Owner metadata overrides, collection refs and direct Add form; reject AI/library chat/team sharing. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX21-S01 — Bookmark library / collection: BROWSE profile.
- FX21-S02 — Add / edit Bookmark: FORM profile.
- FX21-S03 — Bookmark detail: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX21-S01 | Bookmark library / collection | /bookmarks; /bookmarks/collections/:collectionId | Title; domain; safe description excerpt; tags; health/freshness | Add Bookmark |
| FX21-S02 | Add / edit Bookmark | /bookmarks/new; /bookmarks/:bookmarkId/edit | URL; Title; description; extracted metadata preview/source; tags/collections | Save Bookmark |
| FX21-S03 | Bookmark detail | /bookmarks/:bookmarkId | Saved metadata and provenance; original link; health; collections/tags | Open original |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Bookmark library → Add URL/title → metadata preview → Save with optional collection/tags → open/organize/read later.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX21-S01 — Bookmark library / collection

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; domain; safe description excerpt; tags; health/freshness |
| Entry / route | `/bookmarks` is implemented for the manual metadata slice; `/bookmarks/collections/:collectionId` remains a future contract; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Bookmark library / collection. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Add Bookmark; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Grid/List; Open; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; domain; safe description excerpt; tags; health/freshness |
| Search / filters / sorting / pagination | Title/URL/tag search; collection/tag/health/status; updated DESC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Missing favicon placeholder not provider-error-as-empty; external links indicate destination. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Bookmark library / collection' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX21-S02 — Add / edit Bookmark

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — URL; Title; description; extracted metadata preview/source; tags/collections |
| Entry / route | `/bookmarks` inline create/edit is implemented for the manual metadata slice; `/bookmarks/new` and `/bookmarks/:bookmarkId/edit` remain future route contracts. |
| Header / layout | Screen title: Add / edit Bookmark. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Bookmark; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Fetch metadata; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | URL; Title; description; extracted metadata preview/source; tags/collections |
| Search / filters / sorting / pagination | No automatic external fetch on every keystroke. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Explicit fetch with guarded URL; manual values retained on fetch retry. Duplicate warn offers existing record or deliberate separate save. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Add / edit Bookmark' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX21-S03 — Bookmark detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Saved metadata and provenance; original link; health; collections/tags |
| Entry / proposed route | /bookmarks/:bookmarkId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Bookmark detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Open original; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit; Save to Read Later; Archive; Share eligible; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Saved metadata and provenance; original link; health; collections/tags |
| Search / filters / sorting / pagination | No inline external authenticated web embedding. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Source unavailable still shows owner saved facts; share exposes only registered safe projection. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Bookmark detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

URL HTTP(S) and Title1..200 required; description<=20k optional; tags/collections optional. Preserve meaningful URL query. Duplicate suggestion explicit; never auto-merge. Provider metadata provenance and owner override separate.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Library Grid/List; Title/URL/tag search; collection/tag/status/source-health filters; Updated DESC25/page. Collection scopes references, not copies.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Library Grid/List; Title/URL/tag search; collection/tag/status/source-health filters; Updated DESC25/page. Collection scopes references, not copies.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Active | Edit; open original; add Read Later; collections; Archive/Trash | Metadata fetch cannot overwrite owner text |
| Archived | Readonly; Unarchive; Trash | No background edit of manual fields |
| Broken/Stale source | Show saved record and freshness/error | Not deleted or empty result |
| Trash | Restore/purge per reference guard | No active share resolve |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-ARCHIVE / D-UNARCHIVE | Source + previous state and affected references | Archive / Unarchive; Cancel | Only features with archive lifecycle; preserve source-specific prior state/cohort. |
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
| [knowledge.Bookmark](../../design-database/06-documents-knowledge-discovery.md#knowledge-bookmark) | Personal URL record and explicit metadata overrides; Technical decision |
| [organization.Collection](../../design-database/06-documents-knowledge-discovery.md#organization-collection) | Named same-owner grouping of references, not ownership container; Technical decision |

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

[FX-21 action catalog](../../action-catalog/modules/21-bookmarks.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
