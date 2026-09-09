# FX-27 — Personal Finance — UX/UI Specification

> **Current decision amendment — 2026-09-07:** Current basic scope is ManualCategory/ManualRecord category+amount entry. Advanced ledger/budget/debt/FX remains unconfirmed; new screens below. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal accounts/ledger/bills/subscriptions/budgets/savings/debt/reporting; financial semantics remain Q-05.

## 2. Requirement sources

- [FX-27 feature](../../features/27-finance.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-27-BR-001`, `FX-27-BR-002`, `FX-27-BR-003`, `FX-27-BR-004`, `FX-27-BR-005`, `FX-27-BR-006`, `FX-27-AC-001`, `FX-27-AC-002`, `FX-27-AC-003`, `FX-27-AC-004`, `P04-TXN-006`, `P04-ACC-001`, `P04-ACC-002`, `P04-ACC-003`, `P04-ACC-004`, `P04-ACC-005`, `P04-ACC-006`, `P04-BIL-001`, `P04-BIL-002`, `P04-BIL-003`, `P04-BIL-004`, `P04-BUD-001`, `P04-BUD-002`, `P04-CAT-001`, `P04-CAT-002`, `P04-CAT-003`, `P04-DEB-001`, `P04-REC-001`, `P04-REC-002`, `P04-RPT-001`, `P04-RPT-002`, `P04-RPT-003`, `P04-SAV-001`, `P04-SUB-001`, `P04-SUB-002`, `P04-TXN-001`, `P04-TXN-002`, `P04-TXN-003`, `P04-TXN-004`, `P04-TXN-005`, `P04-TXN-007`, `P04-TXN-008`, `P04-TXN-009`

## 3. Reference products

- [Actual Budget](https://actualbudget.org/docs/transactions/transfers/) — checked2026-09-07. Evidence limit: Official documentation; financial semantics remain Product Owner decision.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Actual Budget](https://actualbudget.org/docs/transactions/transfers/) | Linked transfer sides represent movement between accounts rather than unrelated income/expense. | ADAPT | Transfer form previews both sides atomically; do not import Actual budget or deletion semantics while Q-05 open. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep currency, actual versus proposed ledger effect and financial-policy gates visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX27-S01 — Finance overview: DASHBOARD profile.
- FX27-S02 — Accounts: BROWSE profile.
- FX27-S03 — Account form: FORM profile.
- FX27-S04 — Transactions ledger: BROWSE profile.
- FX27-S05 — Transaction / split form: FORM profile.
- FX27-S06 — Transfer form: FORM profile.
- FX27-S07 — Bills and payment allocation: DETAIL profile.
- FX27-S08 — Subscriptions / price history: DETAIL profile.
- FX27-S09 — Budgets: FORM profile.
- FX27-S10 — Savings and debts: DETAIL profile.
- FX27-S11 — Reports: DASHBOARD profile.
- FX27-S12 — Finance CSV import / corrections: DIALOG profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX27-S01 | Finance overview | /finance | Per-currency account totals; upcoming Bills; subscription renewals; budget summary policy status | Record transaction |
| FX27-S02 | Accounts | /finance/accounts | Name; type; currency; derived balance; status | New Account |
| FX27-S03 | Account form | /finance/accounts/new; /finance/accounts/:accountId/edit | Name; type; currency; opening amount/date; status policy | Save Account |
| FX27-S04 | Transactions ledger | /finance/transactions | Date; payee; type; category; account; inflow/outflow; currency; posted/draft state | New transaction |
| FX27-S05 | Transaction / split form | /finance/transactions/new; /finance/transactions/:transactionId | Income/Expense type; account/date/amount; payee; category or split rows; memo; before/after preview | Review and post — Q-05 |
| FX27-S06 | Transfer form | /finance/transfers/new | From account/amount/currency; To account/amount/currency; date; memo; both impacts | Confirm transfer — Q-05 |
| FX27-S07 | Bills and payment allocation | /finance/bills; /finance/bills/:billId | Bill title/amount/currency/due; paid/remaining; linked payment transactions | Record / link payment |
| FX27-S08 | Subscriptions / price history | /finance/subscriptions; /finance/subscriptions/:subscriptionId | Service; amount/currency/cycle; next renewal; status; price-effective history | Record price/renewal change |
| FX27-S09 | Budgets | /finance/budgets | Period/category/currency/limit rows and spent/remaining proposal | Save budget — Q-05 |
| FX27-S10 | Savings and debts | /finance/savings; /finance/debts | Savings target/mode/current/deadline; Debt direction/principal/currency/payments/manual interest | Record progress / payment — Q-05 |
| FX27-S11 | Reports | /finance/reports | Category/account/time aggregates; currency; methodology; table equivalent | Inspect contributing transactions |
| FX27-S12 | Finance CSV import / corrections | /finance/data; /finance/transactions/:transactionId/correction | Mapped rows or original journal plus proposed correction; exact total effect | Apply validated import / correction — Q-05 |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Accounts → record transaction → review category/splits or transfer both sides → confirm → ledger/report; Bill → record/link payment → allocation and state.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX27-S01 — Finance overview

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DASHBOARD — Per-currency account totals; upcoming Bills; subscription renewals; budget summary policy status |
| Entry / proposed route | /finance; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Finance overview. Shared dashboard profile; header → controls → declared content → feedback. |
| Primary action | Record transaction; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Accounts; Reports; Bills. Back/Cancel always has authorized fallback. |
| Content regions / fields | Per-currency account totals; upcoming Bills; subscription renewals; budget summary policy status |
| Search / filters / sorting / pagination | Period/account/currency filters explicit. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Mixed currencies displayed separately; amounts hidden in support/share unless approved projection; no advisory claim from dashboard. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dashboard profile. Keep 'Finance overview' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S02 — Accounts

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; type; currency; derived balance; status |
| Entry / proposed route | /finance/accounts; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Accounts. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Account; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open ledger; close account under Q-05. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; type; currency; derived balance; status |
| Search / filters / sorting / pagination | Name/type/status/currency; Name ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Balance read-only derivation. Empty first-account prompt does not create fake default bank account. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Accounts' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S03 — Account form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Name; type; currency; opening amount/date; status policy |
| Entry / proposed route | /finance/accounts/new; /finance/accounts/:accountId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Account form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Account; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; type; currency; opening amount/date; status policy |
| Search / filters / sorting / pagination | Currency searchable explicit choice. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Currency locks after posted entry. Opening-balance correction impact Q-05, never silently overwrite historic ledger. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Account form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S04 — Transactions ledger

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Date; payee; type; category; account; inflow/outflow; currency; posted/draft state |
| Entry / proposed route | /finance/transactions; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Transactions ledger. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New transaction; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Transfer; split; inspect; CSV import; allowed export. Back/Cancel always has authorized fallback. |
| Content regions / fields | Date; payee; type; category; account; inflow/outflow; currency; posted/draft state |
| Search / filters / sorting / pagination | Payee/memo search; date/account/category/type/currency; date DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Sticky semantic headers; numeric alignment; phone priority Date/Payee/Amount+currency with expandable remaining fields. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Transactions ledger' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S05 — Transaction / split form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Income/Expense type; account/date/amount; payee; category or split rows; memo; before/after preview |
| Entry / proposed route | /finance/transactions/new; /finance/transactions/:transactionId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Transaction / split form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Review and post — Q-05; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Save draft if approved; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Income/Expense type; account/date/amount; payee; category or split rows; memo; before/after preview |
| Search / filters / sorting / pagination | Split rows show running allocated and remaining exact amount. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No raw float; inline error for mismatched totals; server rechecks currency/allocations. Posted form readonly unless approved correction flow. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Transaction / split form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S06 — Transfer form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — From account/amount/currency; To account/amount/currency; date; memo; both impacts |
| Entry / proposed route | /finance/transfers/new; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Transfer form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Confirm transfer — Q-05; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | From account/amount/currency; To account/amount/currency; date; memo; both impacts |
| Search / filters / sorting / pagination | Same-currency matching amounts; explicit two amounts for FX proposal. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Show one linked transfer identity; no income/expense double count. Partial save failure rolls back both sides. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Transfer form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S07 — Bills and payment allocation

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Bill title/amount/currency/due; paid/remaining; linked payment transactions |
| Entry / proposed route | /finance/bills; /finance/bills/:billId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Bills and payment allocation. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Record / link payment; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit eligible bill; cancel under policy; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Bill title/amount/currency/due; paid/remaining; linked payment transactions |
| Search / filters / sorting / pagination | State/date search; due ASC;25/page bill list. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Payment confirmation previews exact allocation and existing eligible transactions; retry no duplicate payment. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Bills and payment allocation' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S08 — Subscriptions / price history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Service; amount/currency/cycle; next renewal; status; price-effective history |
| Entry / proposed route | /finance/subscriptions; /finance/subscriptions/:subscriptionId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Subscriptions / price history. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Record price/renewal change; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Pause/cancel metadata; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Service; amount/currency/cycle; next renewal; status; price-effective history |
| Search / filters / sorting / pagination | Service/status/renewal; due ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Action wording Record canceled, not Cancel at provider; past purchase values unchanged. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Subscriptions / price history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S09 — Budgets

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Period/category/currency/limit rows and spent/remaining proposal |
| Entry / proposed route | /finance/budgets; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Budgets. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save budget — Q-05; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | View proposal; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Period/category/currency/limit rows and spent/remaining proposal |
| Search / filters / sorting / pagination | Period and currency explicit. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Envelope versus spending-limit decision open; no design calls proposed no-rollover formula Approved. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Budgets' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S10 — Savings and debts

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Savings target/mode/current/deadline; Debt direction/principal/currency/payments/manual interest |
| Entry / proposed route | /finance/savings; /finance/debts; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Savings and debts. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Record progress / payment — Q-05; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Inspect history; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Savings target/mode/current/deadline; Debt direction/principal/currency/payments/manual interest |
| Search / filters / sorting / pagination | Status/currency/date; target/due ordering. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Manual vs selected-account savings mode and debt interest rules conditional; never invent payoff/FX calculation. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Savings and debts' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S11 — Reports

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DASHBOARD — Category/account/time aggregates; currency; methodology; table equivalent |
| Entry / proposed route | /finance/reports; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Reports. Shared dashboard profile; header → controls → declared content → feedback. |
| Primary action | Inspect contributing transactions; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Change range; approved export. Back/Cancel always has authorized fallback. |
| Content regions / fields | Category/account/time aggregates; currency; methodology; table equivalent |
| Search / filters / sorting / pagination | Date/account/category/currency; source date ordering. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Missing data distinct zero. Correction/reversal inclusion policy displayed after Q-05 closure. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dashboard profile. Keep 'Reports' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX27-S12 — Finance CSV import / corrections

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Mapped rows or original journal plus proposed correction; exact total effect |
| Entry / proposed route | /finance/data; /finance/transactions/:transactionId/correction; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Finance CSV import / corrections. Named modal with preview/reason and footer actions. |
| Primary action | Apply validated import / correction — Q-05; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Mapped rows or original journal plus proposed correction; exact total effect |
| Search / filters / sorting / pagination | Valid/invalid/duplicate row filter; correction reason. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No generic destructive Delete posted transaction. Preview never posts until policy approved and User confirms. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Finance CSV import / corrections' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Account name/type/currency/opening amount/date. Transaction amount>0 exact decimal, account/date/type; category/payee/memo per kind; splits must sum; transfer distinct same-owner accounts, explicit currencies/amounts. Currency immutable after posted entry. Q-05 blocks final posting/correction/budget/FX/debt semantics, not automatically resolved by reference Actual Budget.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Ledger dense Table date,payee,type,category,account,currency,inflow,outflow; search payee/memo; date/account/category/type/currency filters; date DESC+Id25/page. Totals separated currency; no silently converted grand total.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Ledger dense Table date,payee,type,category,account,currency,inflow,outflow; search payee/memo; date/account/category/type/currency filters; date DESC+Id25/page. Totals separated currency; no silently converted grand total.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Draft proposal | Edit/review/delete draft only if Q-05 approved | No posted balance effect before explicit posting |
| Posted proposal | Inspect journal/history; approved correction workflow | No generic edit balance/Trash or unapproved reversal |
| Bill Open/PartiallyPaid | Record/link payment with remaining amount preview | No automatic bank debit |
| Subscription Active | Record change/renewal/cancel metadata | No provider cancellation/payment |
| Financial policy pending | Readonly specification/wireframe proposal | Affected commands not implementation-ready |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Finance CSV import / corrections | Mapped rows or original journal plus proposed correction; exact total effect | Apply validated import / correction — Q-05 / Cancel | No generic destructive Delete posted transaction. Preview never posts until policy approved and User confirms. |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. External/staging/dispatch providers may fail independently: retain last successful authorized values with timestamp and error, offer safe retry-after, label partial results. Never show false price0, delivered notification, complete import or successful job.

## 18. Permissions / Read-only / Sensitive contexts

Sensitive field projections are constrained by Q-03/Q-04 where relevant; do not infer permission from metadata labels. No secret/private salary/serial/contact/financial values in generic previews or diagnostics. 

Use [security UX](../global/12-security-sensitive-ux.md) and [explicit modes](../global/13-admin-support-emergency.md). Readonly banner is contextual and server enforced, not merely a disabled Save over full private DTO.

## 19. Responsive behavior

[UX-10](../global/10-responsive-design.md) plus per-screen overrides is mandatory: desktop appropriate split/table, tablet drawer, mobile stacked route/sheet with Back, all fields available. Do not reset approved default view/source state on resize. Ledger mobile keeps date/payee/amount+currency, detail exposes all remaining columns; no fake compact total across currencies.

## 20. Accessibility

Keyboard-only primary/alternate/error/destructive flows, explicit labels and visible focus; semantics for tables/forms/statuses; chart/table equivalent; no drag-only; modal focus trap/return; touch/zoom/reduced-motion contracts [UX-11](../global/11-accessibility.md). Per-screen text is not certification; later assistive-tech tests must execute these paths.

## 21. Cross-module integration

Use registered source/command/projection contracts from [UX-14](../global/14-cross-module-interactions.md); links preserve owner/access mode and do not transfer ownership or mutate unrelated source.

Data design trace:

| Table / provider data | Purpose / dependency status |
| --- | --- |
| [finance.Account](../../design-database/07-finance-vault.md#finance-account) | Personal ledger account in one explicit currency; Proposed: Q-05 |
| [finance.Category](../../design-database/07-finance-vault.md#finance-category) | Transaction/budget category; Proposed: Q-05 |
| [finance.Transaction](../../design-database/07-finance-vault.md#finance-transaction) | Immutable-after-posting financial command envelope proposal; Proposed: Q-05 |
| [finance.TransactionLeg](../../design-database/07-finance-vault.md#finance-transactionleg) | Signed account impact; two legs for transfer; Proposed: Q-05 |
| [finance.TransactionSplit](../../design-database/07-finance-vault.md#finance-transactionsplit) | Category allocation of nontransfer transaction; Proposed: Q-05 |
| [finance.TransactionVersion](../../design-database/07-finance-vault.md#finance-transactionversion) | Auditable ledger edit/correction history; Proposed: Q-05 |
| [finance.Bill](../../design-database/07-finance-vault.md#finance-bill) | Due obligation with explicit transaction allocations; Proposed: Q-05 |
| [finance.PaymentAllocation](../../design-database/07-finance-vault.md#finance-paymentallocation) | Idempotent bill/debt allocation to real transaction; Proposed: Q-05 |
| [finance.RecurringRule](../../design-database/07-finance-vault.md#finance-recurringrule) | Versioned financial schedule creates pending occurrences only; Proposed: Q-05 |
| [finance.Subscription](../../design-database/07-finance-vault.md#finance-subscription) | Manual recurring service record; Proposed: Q-05 |
| [finance.SubscriptionPrice](../../design-database/07-finance-vault.md#finance-subscriptionprice) | Effective-dated subscription amount history; Proposed: Q-05 |
| [finance.Budget](../../design-database/07-finance-vault.md#finance-budget) | Conditional monthly/category spending limit model; Proposed: Q-05 |
| [finance.BudgetLine](../../design-database/07-finance-vault.md#finance-budgetline) | Category budget amount; Proposed: Q-05 |
| [finance.SavingsGoal](../../design-database/07-finance-vault.md#finance-savingsgoal) | Conditional savings progress mode; Proposed: Q-05 |
| [finance.SavingsAccount](../../design-database/07-finance-vault.md#finance-savingsaccount) | Selected accounts for account-based savings proposal; Proposed: Q-05 |
| [finance.Debt](../../design-database/07-finance-vault.md#finance-debt) | Manual debt/loan tracking, financial semantics blocked; Proposed: Q-05 |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-03](../../features/90-open-decisions.md#q-03) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-05](../../features/90-open-decisions.md#q-05) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-27 action catalog](../../action-catalog/modules/27-finance.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.

## Current basic Finance screens

FX27-S13 /finance/records: manual records Table, category/amount/currency/date, filter category/date/currency, date descending, bounded pagination; New record and Manage categories. Group summaries only same currency, no balance/debt/net-worth. FX27-S14 full create/edit form: Category and Amount, explicit Currency, date default user-local today and optional Note; no Account or Income/Expense mandatory field. Save validates/records revision and safe Activity, returns list/detail, Cancel dirty guard. FX27-S15 category list/form: name unique owner-normalized; New/Edit/Remove unused; referenced category removal blocked with safe usage count. Empty create prompt, no-filter-results reset, error retry, denied read no payload; row keyboard actions, mobile stack, shared validation/focus/conflict patterns. Prior ledger/budget/debt screens remain Blocked until their semantics are approved.
