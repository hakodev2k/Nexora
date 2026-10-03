# FX-27 — Finance: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/27-finance.md), [UX](../../ux-ui/modules/27-finance.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). The manual-record action subset is implemented locally on PR #4 under DEC-20260909-014; advanced and sensitive rows remain gated, and runtime verification is owner-owned.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="finance-account-read"></a>`finance.account.read` — Xem Financial account | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S02 |
| <a id="finance-account-create"></a>`finance.account.create` — Tạo Financial account | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S03 |
| <a id="finance-account-update"></a>`finance.account.update` — Sửa Financial account | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S03 |
| <a id="finance-account-close"></a>`finance.account.close` — Đóng account | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S02 |
| <a id="finance-transaction-read"></a>`finance.transaction.read` — Xem ledger | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S04 |
| <a id="finance-transaction-post"></a>`finance.transaction.post` — Ghi transaction | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-transaction-correct"></a>`finance.transaction.correct` — Điều chỉnh transaction đã post | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-transaction-void"></a>`finance.transaction.void` — Void transaction | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S04 |
| <a id="finance-transaction-split"></a>`finance.transaction.split` — Chia transaction theo category | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-transfer-post"></a>`finance.transfer.post` — Ghi chuyển khoản nội bộ | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S06 |
| <a id="finance-category-read"></a>`finance.category.read` — Xem Category | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-category-create"></a>`finance.category.create` — Tạo Category | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-category-update"></a>`finance.category.update` — Sửa Category | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-bill-read"></a>`finance.bill.read` — Xem Bill | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S07 |
| <a id="finance-bill-create"></a>`finance.bill.create` — Tạo Bill | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S07 |
| <a id="finance-bill-update"></a>`finance.bill.update` — Sửa Bill | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S07 |
| <a id="finance-subscription-read"></a>`finance.subscription.read` — Xem Subscription | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S08 |
| <a id="finance-subscription-create"></a>`finance.subscription.create` — Tạo Subscription | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S08 |
| <a id="finance-subscription-update"></a>`finance.subscription.update` — Sửa Subscription | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S08 |
| <a id="finance-budget-read"></a>`finance.budget.read` — Xem Budget | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S09 |
| <a id="finance-budget-create"></a>`finance.budget.create` — Tạo Budget | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S09 |
| <a id="finance-budget-update"></a>`finance.budget.update` — Sửa Budget | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S09 |
| <a id="finance-savings-read"></a>`finance.savings.read` — Xem Savings target | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-savings-create"></a>`finance.savings.create` — Tạo Savings target | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-savings-update"></a>`finance.savings.update` — Sửa Savings target | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-debt-read"></a>`finance.debt.read` — Xem Debt | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-debt-create"></a>`finance.debt.create` — Tạo Debt | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-debt-update"></a>`finance.debt.update` — Sửa Debt | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-category-merge"></a>`finance.category.merge` — Gộp category | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-category-remove"></a>`finance.category.remove` — Xóa category không referenced | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S05 |
| <a id="finance-bill-cancel"></a>`finance.bill.cancel` — Hủy bill | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S07 |
| <a id="finance-bill-record-payment"></a>`finance.bill.record_payment` — Ghi nhận thanh toán | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S07 |
| <a id="finance-bill-void-payment"></a>`finance.bill.void_payment` — Void liên kết thanh toán | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S07 |
| <a id="finance-subscription-pause"></a>`finance.subscription.pause` — Tạm dừng subscription | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S08 |
| <a id="finance-subscription-cancel"></a>`finance.subscription.cancel` — Hủy subscription | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S08 |
| <a id="finance-subscription-record-price"></a>`finance.subscription.record_price` — Ghi lịch sử giá | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S08 |
| <a id="finance-debt-record-payment"></a>`finance.debt.record_payment` — Ghi trả nợ | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-debt-adjust-interest"></a>`finance.debt.adjust_interest` — Điều chỉnh lãi | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-savings-record-progress"></a>`finance.savings.record_progress` — Ghi tiến độ tiết kiệm | COMMAND / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S10 |
| <a id="finance-report-read"></a>`finance.report.read` — Xem financial reports | QUERY / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S01, FX27-S11 |
| <a id="finance-csv-preview"></a>`finance.csv.preview` — Preview CSV | COMPOSITE / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S12 |
| <a id="finance-csv-import"></a>`finance.csv.import` — Import CSV | COMPOSITE / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S12 |
| <a id="finance-csv-export"></a>`finance.csv.export` — Export CSV | COMPOSITE / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S12 |
| <a id="finance-report-share"></a>`finance.report.share` — Quản lý link chỉ-đọc của report | COMPOSITE / SELF | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX27-S11 |
| <a id="finance-support-read"></a>`finance.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Blocked | Q-05-R: advanced ledger semantics not decided; use manual-record baseline | FX05-S03, FX05-S05 |
| <a id="finance-manual-category-read"></a>`finance.manual_category.read` — Xem danh mục nhập tay | QUERY / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S15 |
| <a id="finance-manual-category-create"></a>`finance.manual_category.create` — Tạo danh mục | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S15 |
| <a id="finance-manual-category-update"></a>`finance.manual_category.update` — Sửa danh mục | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S15 |
| <a id="finance-manual-category-remove"></a>`finance.manual_category.remove` — Xóa danh mục chưa dùng | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S15 |
| <a id="finance-manual-record-read"></a>`finance.manual_record.read` — Xem khoản tiền nhập tay | QUERY / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S13 |
| <a id="finance-manual-record-create"></a>`finance.manual_record.create` — Ghi danh mục và số tiền | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S14 |
| <a id="finance-manual-record-update"></a>`finance.manual_record.update` — Sửa khoản tiền nhập tay | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S14 |
| <a id="finance-manual-summary-read"></a>`finance.manual_summary.read` — Tổng hợp theo danh mục/đơn vị tiền | QUERY / SELF | Yes when active | SLICE_IMPLEMENTED (local) | DEC-014 + manual-record contract; SQL/API/UI present, runtime verification owner-owned | FX27-S13 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `finance.account.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.account.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.account.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.account.read` |
| `finance.account.close` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.transaction.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.transaction.post` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.transaction.correct` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.transaction.void` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.transaction.split` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.transfer.post` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.category.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.category.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.category.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.category.read` |
| `finance.bill.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.bill.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.bill.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.bill.read` |
| `finance.subscription.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.subscription.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.subscription.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.subscription.read` |
| `finance.budget.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.budget.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.budget.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.budget.read` |
| `finance.savings.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.savings.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.savings.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.savings.read` |
| `finance.debt.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.debt.create` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.debt.update` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `finance.debt.read` |
| `finance.category.merge` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.category.remove` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.bill.cancel` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.bill.record_payment` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.bill.void_payment` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.subscription.pause` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.subscription.cancel` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.subscription.record_price` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.debt.record_payment` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.debt.adjust_interest` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.savings.record_progress` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.report.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.csv.preview` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.csv.import` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.csv.export` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.report.share` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | `sharing.link.read` |
| `finance.support.read` | Historical ledger/account/budget/debt/FX contract remains proposal; category and amount manual flow has separate keys/tables | Common + dynamic source/provider guards |
| `finance.manual_category.read` | Own manual categories; no account ledger/balance classification | Common + dynamic source/provider guards |
| `finance.manual_category.create` | Owner scoped; title1–100; unique normalized owner/name; remove blocked while any ManualRecord references category | Common + dynamic source/provider guards |
| `finance.manual_category.update` | Owner scoped; title1–100; unique normalized owner/name; remove blocked while any ManualRecord references category | Common + dynamic source/provider guards |
| `finance.manual_category.remove` | Owner scoped; title1–100; unique normalized owner/name; remove blocked while any ManualRecord references category | Common + dynamic source/provider guards |
| `finance.manual_record.read` | Owner records; category/date/currency filters; no inferred income/expense/account balances | Common + dynamic source/provider guards |
| `finance.manual_record.create` | Own Category + nonnegative decimal Amount + explicit CurrencyCode; date default local today; concurrency/Activity for edits; no TransactionLeg/posted ledger/bank side effect; no delete policy inferred | Common + dynamic source/provider guards |
| `finance.manual_record.update` | Own Category + nonnegative decimal Amount + explicit CurrencyCode; date default local today; concurrency/Activity for edits; no TransactionLeg/posted ledger/bank side effect; no delete policy inferred | Common + dynamic source/provider guards |
| `finance.manual_summary.read` | Authorized manual records only; sum same currency separately, no FX conversion/income/expense/net-worth claim | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
