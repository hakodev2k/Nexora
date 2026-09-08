# FX-37 — Personal Assets — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal physical asset inventory with purchase/warranty/repair/components/history and sensitive serial fields.

## 2. Requirement sources

- [FX-37 feature](../../features/37-personal-assets.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-37-BR-001`, `FX-37-BR-002`, `FX-37-BR-003`, `FX-37-BR-004`, `FX-37-BR-005`, `FX-37-BR-006`, `FX-37-AC-001`, `FX-37-AC-002`, `FX-37-AC-003`, `P07-AST-001`, `P07-AST-002`, `P07-AST-003`, `P07-AST-004`, `P07-AST-005`, `P07-AST-006`, `P07-BND-001`, `P07-BND-002`, `P07-BND-003`, `P07-BND-004`, `P07-BND-005`, `P07-PUR-001`, `P07-WAR-001`, `P07-WAR-002`, `P07-WAR-003`

## 3. Reference products

- [Snipe-IT](https://snipe-it.readme.io/docs/overview) — checked2026-09-07. Evidence limit: Official docs; borrower text is Nexora design, not membership.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Snipe-IT](https://snipe-it.readme.io/docs/overview) | Assets have status labels and check-in/check-out context. | ADAPT | Personal asset state/repair/loan metadata; reject team assignment and configurable enterprise deployment states. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX37-S01 — Asset inventory: BROWSE profile.
- FX37-S02 — Asset create / edit: FORM profile.
- FX37-S03 — Asset detail: DETAIL profile.
- FX37-S04 — Warranty / repair form: FORM profile.
- FX37-S05 — Components / accessories: BROWSE profile.
- FX37-S06 — Asset history / Trash: HISTORY profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX37-S01 | Asset inventory | /assets/personal | Name; type; model; state; purchase date; warranty expiry if configured | New Asset |
| FX37-S02 | Asset create / edit | /assets/personal/new; /assets/personal/:assetId/edit | Name/type; category/brand/model; masked serial; state; notes | Save Asset |
| FX37-S03 | Asset detail | /assets/personal/:assetId | Information; Purchase; Warranty; Components; Repairs; History tabs | Edit / Record state |
| FX37-S04 | Warranty / repair form | /assets/personal/:assetId/service | Coverage kind/provider/dates/terms; repair dates/provider/cost/result/files | Save service record |
| FX37-S05 | Components / accessories | /assets/personal/:assetId/components | Owned component rows versus independent linked Asset; type/model/status | Add component / Link Asset |
| FX37-S06 | Asset history / Trash | /assets/personal/:assetId/history; /trash?module=assets | State/edit/purchase/service evidence; masked history values; deletion batch | Inspect version / Preview restore |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Asset Table → create item → add purchase/warranty/files → record repair/loan/state → inspect history/expiry → archive/Trash explicitly.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX37-S01 — Asset inventory

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; type; model; state; purchase date; warranty expiry if configured |
| Entry / proposed route | /assets/personal; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Asset inventory. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Asset; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; filter; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; type; model; state; purchase date; warranty expiry if configured |
| Search / filters / sorting / pagination | Name/model; type/status/tag; Name ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Keep Device/Electronics as subtype of same record, not duplicate inventory module. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Asset inventory' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX37-S02 — Asset create / edit

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Name/type; category/brand/model; masked serial; state; notes |
| Entry / proposed route | /assets/personal/new; /assets/personal/:assetId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Asset create / edit. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Asset; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; add purchase info. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name/type; category/brand/model; masked serial; state; notes |
| Search / filters / sorting / pagination | Owner form validation; no shared assignee picker. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Sensitive fields recent-auth policy applies; serial never toast/URL/search preview. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Asset create / edit' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX37-S03 — Asset detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Information; Purchase; Warranty; Components; Repairs; History tabs |
| Entry / proposed route | /assets/personal/:assetId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Asset detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Edit / Record state; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Add repair; loan/return record; Archive; Trash; safe share Q-03. Back/Cancel always has authorized fallback. |
| Content regions / fields | Information; Purchase; Warranty; Components; Repairs; History tabs |
| Search / filters / sorting / pagination | Tab-specific histories newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Loan borrower plain text does not grant access. Sold/disposed retains ownership/evidence until explicit purge. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Asset detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX37-S04 — Warranty / repair form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Coverage kind/provider/dates/terms; repair dates/provider/cost/result/files |
| Entry / proposed route | /assets/personal/:assetId/service; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Warranty / repair form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save service record; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; view expiry reminder. Back/Cancel always has authorized fallback. |
| Content regions / fields | Coverage kind/provider/dates/terms; repair dates/provider/cost/result/files |
| Search / filters / sorting / pagination | Fixed dates vs Lifetime/Unknown; currency with cost. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Default warranty lead30days delegated; date change invalidates old reminder. No Finance transaction automatically. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Warranty / repair form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX37-S05 — Components / accessories

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Owned component rows versus independent linked Asset; type/model/status |
| Entry / proposed route | /assets/personal/:assetId/components; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Components / accessories. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Add component / Link Asset; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Remove link; Open independent Asset. Back/Cancel always has authorized fallback. |
| Content regions / fields | Owned component rows versus independent linked Asset; type/model/status |
| Search / filters / sorting / pagination | Name/type search; rank/name order. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Prevent cycle/self-link; removal dialog distinguishes owned child deletion from independent Asset retained. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Components / accessories' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX37-S06 — Asset history / Trash

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — State/edit/purchase/service evidence; masked history values; deletion batch |
| Entry / proposed route | /assets/personal/:assetId/history; /trash?module=assets; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Asset history / Trash. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Inspect version / Preview restore; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back; purge eligible. Back/Cancel always has authorized fallback. |
| Content regions / fields | State/edit/purchase/service evidence; masked history values; deletion batch |
| Search / filters / sorting / pagination | Version/date/action; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Sensitive historical fields stay protected; purge preview preserves independent Vault/Finance/Asset refs. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Asset history / Trash' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Name1..200/type required; category/manufacturer/model/state/notes optional per create defaults. Serial masked sensitive; purchase seller/date/amount/currency/invoice; warranty Fixed/Lifetime/Unknown; component/accessory refs acyclic same-owner. Borrower text private, no User permission.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Name/model search; type/status/tag filters; Name ASC25/page; expiry view due ASC. Table name,type,model,status,purchase date; sensitive serial absent until detail reveal policy.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Name/model search; type/status/tag filters; Name ASC25/page; expiry view due ASC. Table name,type,model,status,purchase date; sensitive serial absent until detail reveal policy.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Active/Stored/Loaned/Repair/Sold/Disposed/Lost | Explicit state/history, edit allowed per feature; archive/Trash | No ownership transfer or provider action |
| Archived | Readonly; Unarchive; Trash | No edit |
| Independent accessory linked | Open/remove link | Parent purge does not cascade independent Asset |
| Sensitive context | Mask serial/contact/invoice; Q-03 safe sharing proposal | No default support/export exposure |

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

Sensitive field projections are constrained by Q-03/Q-04 where relevant; do not infer permission from metadata labels. No secret/private salary/serial/contact/financial values in generic previews or diagnostics. 

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
| [assets.PersonalAsset](../../design-database/10-assets-career-learning.md#assets-personalasset) | Physical item owned by one User; Technical decision |
| [assets.AssetAccessory](../../design-database/10-assets-career-learning.md#assets-assetaccessory) | Acyclic physical accessory relationship; Technical decision |
| [assets.AssetWarranty](../../design-database/10-assets-career-learning.md#assets-assetwarranty) | Asset coverage record; Technical decision |
| [assets.Repair](../../design-database/10-assets-career-learning.md#assets-repair) | Manual repair/service history; Technical decision |
| [assets.AssetLoan](../../design-database/10-assets-career-learning.md#assets-assetloan) | Personal lending note, not team membership; Technical decision |
| [assets.AssetVersion](../../design-database/10-assets-career-learning.md#assets-assetversion) | Physical asset history; Technical decision |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |
| [assets.Component](../../design-database/10-assets-career-learning.md#assets-component) | Owned physical component within one Asset aggregate; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-03](../../features/90-open-decisions.md#q-03) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-37 action catalog](../../action-catalog/modules/37-assets.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
