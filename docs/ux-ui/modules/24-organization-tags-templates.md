# FX-24 — Organization / Tags / Collections / Templates — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Scoped tags, nonnested reference collections and typed content templates, not user-created modules.

## 2. Requirement sources

- [FX-24 feature](../../features/24-organization-and-templates.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-24-BR-001`, `FX-24-BR-002`, `FX-24-BR-003`, `FX-24-BR-004`, `FX-24-BR-005`, `FX-24-AC-001`, `FX-24-AC-002`, `FX-24-AC-003`, `P03-ORG-001`, `P03-ORG-002`, `P03-ORG-003`, `P03-ORG-004`, `P03-ORG-005`, `P03-ORG-006`, `P03-ORG-007`, `P03-ORG-008`, `P03-ORG-009`

## 3. Reference products

- [Notion templates](https://www.notion.com/help/database-templates) — checked2026-09-07. Evidence limit: Official help text.
- [Raindrop.io](https://help.raindrop.io/quickstart) — checked2026-09-07. Evidence limit: Official help text; browser extension is not added to Nexora scope.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Notion templates](https://www.notion.com/help/database-templates) | Templates reuse page structure and preset properties during creation. | ADAPT | Typed seed without IDs/shares/secrets; still explicit DocumentType/EditorMode; reject automatic creation and no-code databases. |
| [Raindrop.io](https://help.raindrop.io/quickstart) | Saving a bookmark can assign a collection and tags. | ADAPT | Owner metadata overrides, collection refs and direct Add form; reject AI/library chat/team sharing. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX24-S01 — Tag management: BROWSE profile.
- FX24-S02 — Collections: BROWSE profile.
- FX24-S03 — Collection detail: DETAIL profile.
- FX24-S04 — Templates: BROWSE profile.
- FX24-S05 — Template preview / apply: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX24-S01 | Tag management | /organize/tags?namespace=:namespace | Tag label/color; namespace; current usage count | New Tag |
| FX24-S02 | Collections | /organize/collections | Name; module; member count; description | New Collection |
| FX24-S03 | Collection detail | /organize/collections/:collectionId | Ordered typed members; availability; container description | Add existing resources |
| FX24-S04 | Templates | /organize/templates | Name; target module/type; schema compatibility | New Template |
| FX24-S05 | Template preview / apply | /organize/templates/:templateId | Sanitized seed fields; target create requirements; warnings | Use Template |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Module tag/collection picker → manage namespace → create/rename → preview delete impact; template → preview seed → normal creation form.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX24-S01 — Tag management

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Tag label/color; namespace; current usage count |
| Entry / proposed route | /organize/tags?namespace=:namespace; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Tag management. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Tag; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Rename; Delete unused. Back/Cancel always has authorized fallback. |
| Content regions / fields | Tag label/color; namespace; current usage count |
| Search / filters / sorting / pagination | Name/namespace; Name ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Usage counts authorized owner only; delete denial lists current usage incl Trash without revealing history payload. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Tag management' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX24-S02 — Collections

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; module; member count; description |
| Entry / proposed route | /organize/collections; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Collections. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Collection; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; rename; remove collection. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; module; member count; description |
| Search / filters / sorting / pagination | Name/module; Name ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Delete container dialog explicitly says linked resources remain. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Collections' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX24-S03 — Collection detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Ordered typed members; availability; container description |
| Entry / proposed route | /organize/collections/:collectionId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Collection detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Add existing resources; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Remove selected refs; reorder; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Ordered typed members; availability; container description |
| Search / filters / sorting / pagination | Member Title search;25/page picker. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No nesting or automatic resource move. Unavailable source neutral placeholder and safe remove-link action. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Collection detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX24-S04 — Templates

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; target module/type; schema compatibility |
| Entry / proposed route | /organize/templates; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Templates. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Template; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Preview; Edit; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; target module/type; schema compatibility |
| Search / filters / sorting / pagination | Name/module/type; Name ASC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Missing module/incompatible version shows unavailable, not invalid user content silently changed. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Templates' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX24-S05 — Template preview / apply

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Sanitized seed fields; target create requirements; warnings |
| Entry / proposed route | /organize/templates/:templateId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Template preview / apply. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Use Template; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit seed; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Sanitized seed fields; target create requirements; warnings |
| Search / filters / sorting / pagination | No mutation until target form Save. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Documents requires explicit Type/Mode every time. Application opens normal form; template is not a new module or schema. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Template preview / apply' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

General Tag1..50 trimmed unique case-insensitive owner+namespace, optional color; Documents one Tag and dedicated catalog; Projects/Tasks share theirs. Collection name1..100, no nesting. Template name/module/type/schemaVersion and sanitized editable seed; no IDs/owner/shares/history/secrets.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Management search name; namespace/module filter; Name ASC25/page; collection members rank; template preview read-only before apply.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Management search name; namespace/module filter; Name ASC25/page; collection members rank; template preview read-only before apply.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Tag used by Documents active/Archived/Trash | Rename if allowed; inspect usage | Delete blocked until no current page uses |
| History-only tag label | Delete current unused tag | Restore previews recreate/rebind, no infinite history FK |
| Collection | Add/remove refs; rename; delete container | No source delete or ownership change |
| Template | Preview/use/edit own seed | No executable schema/code or bypass immutable creation choices |

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
| [platform.ResourceLink](../../design-database/02-core-identity-platform.md#platform-resourcelink) | Typed cross-module relationship guarded by providers; Technical decision |
| [organization.Tag](../../design-database/06-documents-knowledge-discovery.md#organization-tag) | Registered namespace-scoped tags outside dedicated catalogs; Technical decision |
| [organization.ResourceTag](../../design-database/06-documents-knowledge-discovery.md#organization-resourcetag) | Generic labels only for resource types declaring tag support; Technical decision |
| [organization.Collection](../../design-database/06-documents-knowledge-discovery.md#organization-collection) | Named same-owner grouping of references, not ownership container; Technical decision |
| [organization.CollectionMember](../../design-database/06-documents-knowledge-discovery.md#organization-collectionmember) | Ordered collection membership; Technical decision |
| [organization.Template](../../design-database/06-documents-knowledge-discovery.md#organization-template) | Typed seed content; not executable or no-code module builder; Technical decision |

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
