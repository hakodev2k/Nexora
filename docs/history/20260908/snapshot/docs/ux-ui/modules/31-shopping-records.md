# FX-31 — Shopping Records — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Manual wishlist/comparison/orders/sellers/warranties and explicit purchase-to-Asset handoff.

## 2. Requirement sources

- [FX-31 feature](../../features/31-shopping-records.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-31-BR-001`, `FX-31-BR-002`, `FX-31-BR-003`, `FX-31-BR-004`, `FX-31-BR-005`, `FX-31-BR-006`, `FX-31-AC-001`, `FX-31-AC-002`, `FX-31-AC-003`, `P05-CMP-001`, `P05-ORD-001`, `P05-SEL-001`, `P05-WAR-001`, `P05-WIS-001`

## 3. Reference products

- [AnyList](https://www.anylist.com/features) — checked2026-09-07. Evidence limit: Official feature comparison; no order-management behavior inferred.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [AnyList](https://www.anylist.com/features) | Shopping list features offer a focused item-list workflow. | ADAPT | Wishlist entry/list pattern only; Order/Seller/Warranty are Nexora requirements, not asserted AnyList capabilities. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX31-S01 — Wishlist: BROWSE profile.
- FX31-S02 — Comparison: DETAIL profile.
- FX31-S03 — Orders: BROWSE profile.
- FX31-S04 — Order form / detail: FORM profile.
- FX31-S05 — Sellers / merge: BROWSE profile.
- FX31-S06 — Warranty detail / claims: FORM profile.
- FX31-S07 — Create Asset handoff: DIALOG profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX31-S01 | Wishlist | /shopping/wishlist | Name; desired quantity/amount+currency; state; optional live tracker freshness | Add wishlist item |
| FX31-S02 | Comparison | /shopping/comparisons/:comparisonId | 2..4items; criterion rows; entered values; source freshness | Add/remove comparison item |
| FX31-S03 | Orders | /shopping/orders | Order number; seller; purchase date; status; total/currency | New Order |
| FX31-S04 | Order form / detail | /shopping/orders/new; /shopping/orders/:orderId | Seller/date/currency; line quantities/unit amounts/variants; shipping/tax/discount; notes; evidence | Save Order |
| FX31-S05 | Sellers / merge | /shopping/sellers; /shopping/sellers/:sellerId/merge | Name; URL; private contact; linked order count | New Seller / Review merge |
| FX31-S06 | Warranty detail / claims | /shopping/warranties/:warrantyId | Provider; Fixed/Lifetime/Unknown; dates; terms; claim contact; files | Save warranty |
| FX31-S07 | Create Asset handoff | /shopping/orders/:orderId/create-asset | Selected line; actual purchase snapshot; proposed Asset fields; existing Asset link | Open Asset draft |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Wishlist → compare2..4 → record Order/actual line price → delivered purchase → Create Asset draft → confirmed Asset link; no automatic ledger write.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX31-S01 — Wishlist

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; desired quantity/amount+currency; state; optional live tracker freshness |
| Entry / proposed route | /shopping/wishlist; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Wishlist. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Add wishlist item; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Compare selected2..4; Record purchase; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; desired quantity/amount+currency; state; optional live tracker freshness |
| Search / filters / sorting / pagination | Title/status/tag; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Desired/live/actual purchase prices distinct labels; stale tracker cannot overwrite actual order. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Wishlist' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX31-S02 — Comparison

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — 2..4items; criterion rows; entered values; source freshness |
| Entry / proposed route | /shopping/comparisons/:comparisonId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Comparison. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Add/remove comparison item; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open original; Record purchase; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | 2..4items; criterion rows; entered values; source freshness |
| Search / filters / sorting / pagination | Criterion list; no pagination across4items. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Mobile switches items with persistent criteria labels; chart/table not horizontal-only inaccessible layout. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Comparison' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX31-S03 — Orders

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Order number; seller; purchase date; status; total/currency |
| Entry / proposed route | /shopping/orders; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Orders. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Order; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; filter; Trash eligible. Back/Cancel always has authorized fallback. |
| Content regions / fields | Order number; seller; purchase date; status; total/currency |
| Search / filters / sorting / pagination | Number/seller search; date/status; purchase DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Total from actual line+shipping+tax-discount, not current tracked price. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Orders' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX31-S04 — Order form / detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Seller/date/currency; line quantities/unit amounts/variants; shipping/tax/discount; notes; evidence |
| Entry / proposed route | /shopping/orders/new; /shopping/orders/:orderId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Order form / detail. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Order; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Record status/return; Create Asset draft; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Seller/date/currency; line quantities/unit amounts/variants; shipping/tax/discount; notes; evidence |
| Search / filters / sorting / pagination | Line validation; totals preview; no implicit posting. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Delivered return checks remaining quantity. Confirm finance reference only links existing record, not payment. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Order form / detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX31-S05 — Sellers / merge

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; URL; private contact; linked order count |
| Entry / proposed route | /shopping/sellers; /shopping/sellers/:sellerId/merge; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Sellers / merge. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Seller / Review merge; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit; merge selected. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; URL; private contact; linked order count |
| Search / filters / sorting / pagination | Name/contact search owner only; Name ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Merge previews affected refs/labels and preserves history; same name does not trigger auto-merge. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Sellers / merge' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX31-S06 — Warranty detail / claims

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Provider; Fixed/Lifetime/Unknown; dates; terms; claim contact; files |
| Entry / proposed route | /shopping/warranties/:warrantyId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Warranty detail / claims. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save warranty; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Add evidence; Open source purchase; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Provider; Fixed/Lifetime/Unknown; dates; terms; claim contact; files |
| Search / filters / sorting / pagination | Coverage type determines required dates. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Sensitive contact/file fields excluded from share pending Q-03; no provider claim submission. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Warranty detail / claims' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX31-S07 — Create Asset handoff

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Selected line; actual purchase snapshot; proposed Asset fields; existing Asset link |
| Entry / proposed route | /shopping/orders/:orderId/create-asset; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Create Asset handoff. Named modal with preview/reason and footer actions. |
| Primary action | Open Asset draft; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Selected line; actual purchase snapshot; proposed Asset fields; existing Asset link |
| Search / filters / sorting / pagination | No Finance posting controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Source link idempotency after target Save; canceled draft leaves Order unchanged. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Create Asset handoff' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Wishlist Title, quantity>0; URL/desiredPrice optional with currency. Order date/seller/currency, lines qty>0 unitPrice>=0, shipping/tax/discount>=0; total computed and nonnegative. Warranty Fixed(start/end),Lifetime or Unknown distinct. Return quantity<=delivered. Seller merge explicit.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Wishlist Title/status search/filter; Orders number/seller/date/status; Warranty expiry/kind; defaults updated DESC/due ASC;25/page. Comparison at most4columns, mobile item tabs preserve criteria.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Wishlist Title/status search/filter; Orders number/seller/date/status; Warranty expiry/kind; defaults updated DESC/due ASC;25/page. Comparison at most4columns, mobile item tabs preserve criteria.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Wishlist Wanted | Edit; compare; record purchase; Archive/Trash | No checkout integration |
| Order Draft/Ordered/Shipped/Delivered | Record allowed manual progression/evidence/returns | No provider cancel/payment implied |
| Canceled/Returned | Inspect evidence; corrections per feature | No automatic Finance refund |
| Warranty Lifetime/Unknown | Display explicit non-date state | No fake distant expiry |
| Create Asset | Preview imported purchase fields and existing link | No duplicate Asset on retry |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Create Asset handoff | Selected line; actual purchase snapshot; proposed Asset fields; existing Asset link | Open Asset draft / Cancel | Source link idempotency after target Save; canceled draft leaves Order unchanged. |
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
| [shopping.WishlistItem](../../design-database/08-news-shopping-developer.md#shopping-wishlistitem) | Manual desired purchase, optionally linked to tracker; Technical decision |
| [shopping.Comparison](../../design-database/08-news-shopping-developer.md#shopping-comparison) | Saved personal comparison set; Technical decision |
| [shopping.ComparisonItem](../../design-database/08-news-shopping-developer.md#shopping-comparisonitem) | Wishlist member with explicit comparison notes; Technical decision |
| [shopping.Seller](../../design-database/08-news-shopping-developer.md#shopping-seller) | User-maintained seller contact metadata; Technical decision |
| [shopping.PurchaseOrder](../../design-database/08-news-shopping-developer.md#shopping-purchaseorder) | Manual purchase record, not marketplace control; Technical decision |
| [shopping.OrderLine](../../design-database/08-news-shopping-developer.md#shopping-orderline) | Actual purchased quantity and amount snapshot; Technical decision |
| [shopping.ReturnRecord](../../design-database/08-news-shopping-developer.md#shopping-returnrecord) | Manual return/refund evidence; Technical decision |
| [shopping.Warranty](../../design-database/08-news-shopping-developer.md#shopping-warranty) | Purchase-linked warranty metadata; Technical decision |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-03](../../features/90-open-decisions.md#q-03) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-06](../../features/90-open-decisions.md#q-06) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-31 action catalog](../../action-catalog/modules/31-shopping.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
