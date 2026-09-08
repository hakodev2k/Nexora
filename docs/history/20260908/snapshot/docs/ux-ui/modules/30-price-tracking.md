# FX-30 — Shopee Price Tracking — UX/UI Specification

> **Current decision amendment — 2026-09-07:** Paused by Product Owner; retained specification is future resumption reference, not current implementation scope. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Exact-variant Shopee tracking with price history/freshness/alerts; provider and cost feasibility Q-06.

## 2. Requirement sources

- [FX-30 feature](../../features/30-shopee-price-tracking.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-30-BR-001`, `FX-30-BR-002`, `FX-30-BR-003`, `FX-30-BR-004`, `FX-30-BR-005`, `FX-30-BR-006`, `FX-30-AC-001`, `FX-30-AC-002`, `FX-30-AC-003`, `P05-ALT-001`, `P05-ALT-002`, `P05-ALT-003`, `P05-ALT-004`, `P05-ALT-005`, `P05-ALT-006`, `P05-PHS-001`, `P05-PHS-002`, `P05-PHS-003`, `P05-PHS-004`, `P05-PHS-005`, `P05-PRD-001`, `P05-PRD-002`, `P05-PRD-003`, `P05-PRD-004`, `P05-PRD-005`, `P05-PRD-006`, `P05-PRD-007`

## 3. Reference products

- [Keepa](https://keepa.com/) — checked2026-09-07. Evidence limit: Official search excerpt only; no authenticated chart tested. camelcamelcamel detailed help failed, not used as proof.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Keepa](https://keepa.com/) | Public product describes price history charts and price-drop alerts for Amazon products. | ADAPT | History/threshold/freshness visualization for approved Shopee adapter, not proof a Shopee provider exists. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX30-S01 — Tracked products: BROWSE profile.
- FX30-S02 — Tracker create / edit: FORM profile.
- FX30-S03 — Product price detail / chart: DETAIL profile.
- FX30-S04 — Alert rules: FORM profile.
- FX30-S05 — Provider / alert history: BROWSE profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX30-S01 | Tracked products | /shopping/prices | Title; variant; currency/current price; availability; observed time; stale state | Track product |
| FX30-S02 | Tracker create / edit | /shopping/prices/new; /shopping/prices/:trackerId/edit | Product URL; provider; item/shop; variant options; currency; price definition preview | Save tracker |
| FX30-S03 | Product price detail / chart | /shopping/prices/:trackerId | Current/list price; exact variant; observed time; history chart; availability; alert summary | Manage alerts |
| FX30-S04 | Alert rules | /shopping/prices/:trackerId/alerts | Rule type; threshold with unit; enable state; rearm/cooldown explanation | Save alert |
| FX30-S05 | Provider / alert history | /shopping/prices/:trackerId/history | Observation outcome; provider; timestamp; error; triggered rule/notification link | Inspect evidence |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** New tracker → validate approved provider/item/variant/currency → Save → observe price history → set alert threshold → inspect alert evidence.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX30-S01 — Tracked products

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Title; variant; currency/current price; availability; observed time; stale state |
| Entry / proposed route | /shopping/prices; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Tracked products. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Track product; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; pause; remove; refresh allowed. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; variant; currency/current price; availability; observed time; stale state |
| Search / filters / sorting / pagination | Title/item search; state/currency/market; Updated DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Do not say lowest-ever unless complete comparable observation window is proven. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Tracked products' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX30-S02 — Tracker create / edit

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Product URL; provider; item/shop; variant options; currency; price definition preview |
| Entry / proposed route | /shopping/prices/new; /shopping/prices/:trackerId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Tracker create / edit. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save tracker; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Validate source; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Product URL; provider; item/shop; variant options; currency; price definition preview |
| Search / filters / sorting / pagination | Variant picker with explicit no-selection until valid. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Q-06 prevents pretending provider exists. Changing variant creates distinct series/reset comparison preview, never splices prices. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Tracker create / edit' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX30-S03 — Product price detail / chart

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Current/list price; exact variant; observed time; history chart; availability; alert summary |
| Entry / proposed route | /shopping/prices/:trackerId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Product price detail / chart. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Manage alerts; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open store; refresh; history table; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current/list price; exact variant; observed time; history chart; availability; alert summary |
| Search / filters / sorting / pagination | History range; chronological points/table. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Chart breaks on missing observations; keyboard table equivalent price/currency/time/provider/outcome. External store link clear. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Product price detail / chart' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX30-S04 — Alert rules

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Rule type; threshold with unit; enable state; rearm/cooldown explanation |
| Entry / proposed route | /shopping/prices/:trackerId/alerts; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Alert rules. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save alert; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; view alert history. Back/Cancel always has authorized fallback. |
| Content regions / fields | Rule type; threshold with unit; enable state; rearm/cooldown explanation |
| Search / filters / sorting / pagination | Target/percent validation. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Crossing dedupe and24h cooldown displayed from delegated baseline; all3notification attempts. No current-user cost guarantee. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Alert rules' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX30-S05 — Provider / alert history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Observation outcome; provider; timestamp; error; triggered rule/notification link |
| Entry / proposed route | /shopping/prices/:trackerId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Provider / alert history. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Inspect evidence; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Retry when allowed; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Observation outcome; provider; timestamp; error; triggered rule/notification link |
| Search / filters / sorting / pagination | Outcome/date/rule; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Separate delivery failure from price-condition match; no bypass CAPTCHA/login when source blocked. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Provider / alert history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Approved marketplace/provider, external item/shop ID/URL, exact variant/options and currency. Alert Target>0 or Percent1..100 or NewLow/BackInStock; explicit enable. Observation price nullable on failure, list price optional; compare identical variant/currency/price definition.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Tracked list search Title/item; state/marketplace/currency filter; latest update DESC25/page; chart date presets/custom; history Observed ASC; current price always timestamp.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Tracked list search Title/item; state/marketplace/currency filter; latest update DESC25/page; chart date presets/custom; history Observed ASC; current price always timestamp.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Fresh observation | Show actual comparable price; threshold rules | No guaranteed checkout price |
| Stale / provider failure | Show last successful price labeled stale, gap/error | Never zero-price point or fake realtime |
| Variant unavailable | Unknown/out-of-stock label; inspect history | No silent cheapest-variant substitution |
| Provider Q-06 unresolved | Document workflow and blocked provider setup | Manual-only record is not completed automated tracking |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
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
| [shopping.TrackedProduct](../../design-database/08-news-shopping-developer.md#shopping-trackedproduct) | Exact marketplace item/variant price tracker; Proposed: Q-06 provider contract |
| [shopping.PriceObservation](../../design-database/08-news-shopping-developer.md#shopping-priceobservation) | Append-only comparable observed price or unavailable outcome; Proposed: Q-06 |
| [shopping.PriceAlertRule](../../design-database/08-news-shopping-developer.md#shopping-pricealertrule) | Explicit variant-aware threshold rule; Proposed: Q-06 |
| [shopping.PriceAlertEvent](../../design-database/08-news-shopping-developer.md#shopping-pricealertevent) | Historical alert evidence; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

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

[FX-30 action catalog](../../action-catalog/modules/30-prices.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
