# Personal Finance

FX-27 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Accounts, ledger, income/expense/transfers/splits, bills/payments/subscriptions, budgets/savings/debts và reports.

[Actual Budget](https://actualbudget.org/docs/budgeting/): Accounts, transactions và budget là các phần liên hệ trong quản lý tài chính cá nhân.

**Áp dụng cho Nexora:** Actual tham chiếu personal ledger/budget. Không bank sync, payment execution hoặc tự đoán FX/interest.

**Màn hình:** `/finance/accounts, /transactions, /bills, /subscriptions, /budgets, /reports`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Tạo Account/currency/opening balance → nhập transaction.
2. Filter ledger account/date/category/type; split và receipt attachments.
3. Bill/subscription occurrence Pending → User Record Payment → ledger transaction atomic.
4. Budget/savings/debt và reports drill-down; CSV import preview/export scoped.

## Dữ liệu và validation

- Account name/type Cash/Bank/Wallet/Other/currency/opening balance/date; currency immutable sau posted entry.
- Amount>0 decimal, account/date/type/category/payee/notes; split sum=tổng; transfer from/to và amounts riêng nếu khác currency.
- Bill amount/currency/due/recurrence; subscription price-effective history; budget period/category/amount.
- Savings target/deadline; debt principal/payment/manual interest adjustment; Q-05 xác định công thức tài chính cuối cùng.

## Hành vi và lifecycle

- **FX-27-BR-001:** Balance=opening+posted ledger; không sửa balance trực tiếp. Transfer hai vế atomic không income/expense.
- **FX-27-BR-002:** Posted delete dùng void/reversal có audit; payment retry không double entry. Close Account giữ reports chặn ghi mới.
- **FX-27-BR-003:** Recurring tạo pending occurrences/nhắc, không tự payment; sửa lịch không rewrite paid occurrences.
- **FX-27-BR-004:** Reports tách currency tới Q-05; budget rollover/FX/debt interest là major proposal chưa Approved.
- **FX-27-BR-005:** CSV preview mapping/date/currency/duplicates, invalid row report; commit atomic theo batch được preview; export excludes secrets.
- **FX-27-BR-006:** Purchase/Asset/Order links không tự tạo transaction; receipts qua Files. Sensitive sharing Q-03.

## Quyền, API và tích hợp

- PostTransaction/Transfer/Void/RecordBillPayment atomic idempotent; Finance owns ledger.
- FinanceSummary currency-aware; due reminders revision-aware; export riêng có owner permission.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-27-AC-001:** Transfer retry tác động mỗi account một lần, income không tăng.
- **FX-27-AC-002:** Split lệch tổng reject; VND+USD không total giả.
- **FX-27-AC-003:** Void payment giữ link/balance nhất quán.
- **FX-27-AC-004:** CSV preview chưa mutation; owner mismatch account denied.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Domain detail bổ sung

Các lựa chọn tài chính dưới đây là **proposal thuộc Q-05**, cần duyệt trước implement. Những field/flow đã có trong requirement nguồn vẫn giữ.

| Capability | Hành vi cụ thể đề xuất |
|---|---|
| Category | Name1–100; applicability Income/Expense/Both. Rename giữ relation. Delete khi đang dùng yêu cầu chọn replacement cùng applicability hoặc Uncategorised; preview counts và không rewrite historical label snapshot. Hierarchy optional trong source: đề xuất tối đa2cấp, không cycle, report roll-up không đếm child hai lần |
| Bill | Upcoming/Due/Overdue được suy từ due date và outstanding; PartiallyPaid khi0 < paid < amount; Paid khi outstanding=0; Cancelled explicit. Due date là User-local date |
| Partial payment | Amount>0 và≤outstanding; create/link Transaction atomic. Hai request cạnh tranh không overpay; refund/void phải giữ ledger và bill allocation nhất quán |
| Subscription | Active/Paused/Cancelled/Expired; pause không xóa paid history. Billing cycle monthly/yearly/custom bounded; trialEnd/price effective date lưu history. Auto-renew chỉ thông tin, không thanh toán |
| Recurrence | Daily/weekly/monthly/yearly; ngày31 dùng ngày cuối tháng trong tháng ngắn và hiển thị next-run preview. Occurrence unique template+scheduled date; pending draft/bill cần owner confirm |
| Savings | Chọn mode Manual hoặc SelectedAccounts; không trộn currency; đổi mode sau có progress cần preview/history. Không tự chuyển tiền |
| Debt | Direction Borrowed/Lent, principal/currency/counterparty/date; payment và interest adjustment explicit; không tự suy APR, amortization hoặc compound interest |
| Reports | Account balance, monthly income/expense, category breakdown, cash flow; đều có table equivalent và drill-down. Transfer không income/expense; void entries bị loại khỏi posted totals nhưng còn history |
| CSV | UTF-8, header/mapping, date/currency/decimal preview; match external ID hoặc fingerprint gợi ý duplicate; owner xác nhận từng nhóm trước commit, không tự merge khác accounts |

Category/bill/subscription list có search name/payee và filters state/date/account/category theo provider. Amount/payee filters trong Transaction theo P04-TXN-006; không chỉ cung cấp bốn filter rút gọn. Currency rounding dùng minor-unit rule của currency do ADR chốt; không binary float.

## Traceability và phần còn mở

- [phase-04-finance-and-vault.md](../requirements/phases/phase-04-finance-and-vault.md): `P04-ACC-001`, `P04-ACC-002`, `P04-ACC-003`, `P04-ACC-004`, `P04-ACC-005`, `P04-ACC-006`, `P04-BIL-001`, `P04-BIL-002`, `P04-BIL-003`, `P04-BIL-004`, `P04-BUD-001`, `P04-BUD-002`, `P04-CAT-001`, `P04-CAT-002`, `P04-CAT-003`, `P04-DEB-001`, `P04-REC-001`, `P04-REC-002`, `P04-RPT-001`, `P04-RPT-002`, `P04-RPT-003`, `P04-SAV-001`, `P04-SUB-001`, `P04-SUB-002`, `P04-TXN-001`, `P04-TXN-002`, `P04-TXN-003`, `P04-TXN-004`, `P04-TXN-005`, `P04-TXN-006`, `P04-TXN-007`, `P04-TXN-008`, `P04-TXN-009`

Quyết định lớn cần PO: [Q-03](90-open-decisions.md#q-03), [Q-05](90-open-decisions.md#q-05). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.

