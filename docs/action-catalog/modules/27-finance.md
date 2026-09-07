# FX-27 — Finance: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/27-finance.md) — FX-27-BR-001, FX-27-BR-002, FX-27-BR-003, FX-27-BR-004, FX-27-BR-005, FX-27-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/27-finance.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `finance` là stable logical key, bind installed ModuleId trong manifest. **Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="finance-account-read"></a>`finance.account.read` — Xem Financial account | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S02 |
| <a id="finance-account-create"></a>`finance.account.create` — Tạo Financial account | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S03 |
| <a id="finance-account-update"></a>`finance.account.update` — Sửa Financial account | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S03 |
| <a id="finance-account-close"></a>`finance.account.close` — Đóng account | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S02 |
| <a id="finance-transaction-read"></a>`finance.transaction.read` — Xem ledger | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S04 |
| <a id="finance-transaction-post"></a>`finance.transaction.post` — Ghi transaction | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S05 |
| <a id="finance-transaction-correct"></a>`finance.transaction.correct` — Điều chỉnh transaction đã post | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S05 |
| <a id="finance-transaction-void"></a>`finance.transaction.void` — Void transaction | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S04 |
| <a id="finance-transaction-split"></a>`finance.transaction.split` — Chia transaction theo category | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S05 |
| <a id="finance-transfer-post"></a>`finance.transfer.post` — Ghi chuyển khoản nội bộ | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S06 |
| <a id="finance-category-read"></a>`finance.category.read` — Xem Category | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S05 |
| <a id="finance-category-create"></a>`finance.category.create` — Tạo Category | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S05 |
| <a id="finance-category-update"></a>`finance.category.update` — Sửa Category | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S05 |
| <a id="finance-bill-read"></a>`finance.bill.read` — Xem Bill | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S07 |
| <a id="finance-bill-create"></a>`finance.bill.create` — Tạo Bill | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S07 |
| <a id="finance-bill-update"></a>`finance.bill.update` — Sửa Bill | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S07 |
| <a id="finance-subscription-read"></a>`finance.subscription.read` — Xem Subscription | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S08 |
| <a id="finance-subscription-create"></a>`finance.subscription.create` — Tạo Subscription | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S08 |
| <a id="finance-subscription-update"></a>`finance.subscription.update` — Sửa Subscription | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S08 |
| <a id="finance-budget-read"></a>`finance.budget.read` — Xem Budget | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S09 |
| <a id="finance-budget-create"></a>`finance.budget.create` — Tạo Budget | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S09 |
| <a id="finance-budget-update"></a>`finance.budget.update` — Sửa Budget | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S09 |
| <a id="finance-savings-read"></a>`finance.savings.read` — Xem Savings target | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-savings-create"></a>`finance.savings.create` — Tạo Savings target | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-savings-update"></a>`finance.savings.update` — Sửa Savings target | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-debt-read"></a>`finance.debt.read` — Xem Debt | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-debt-create"></a>`finance.debt.create` — Tạo Debt | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-debt-update"></a>`finance.debt.update` — Sửa Debt | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-category-merge"></a>`finance.category.merge` — Gộp category | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S05 |
| <a id="finance-category-remove"></a>`finance.category.remove` — Xóa category không referenced | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S05 |
| <a id="finance-bill-cancel"></a>`finance.bill.cancel` — Hủy bill | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S07 |
| <a id="finance-bill-record-payment"></a>`finance.bill.record_payment` — Ghi nhận thanh toán | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S07 |
| <a id="finance-bill-void-payment"></a>`finance.bill.void_payment` — Void liên kết thanh toán | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S07 |
| <a id="finance-subscription-pause"></a>`finance.subscription.pause` — Tạm dừng subscription | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S08 |
| <a id="finance-subscription-cancel"></a>`finance.subscription.cancel` — Hủy subscription | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S08 |
| <a id="finance-subscription-record-price"></a>`finance.subscription.record_price` — Ghi lịch sử giá | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S08 |
| <a id="finance-debt-record-payment"></a>`finance.debt.record_payment` — Ghi trả nợ | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S10 |
| <a id="finance-debt-adjust-interest"></a>`finance.debt.adjust_interest` — Điều chỉnh lãi | COMMAND / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S10 |
| <a id="finance-savings-record-progress"></a>`finance.savings.record_progress` — Ghi tiến độ tiết kiệm | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S10 |
| <a id="finance-report-read"></a>`finance.report.read` — Xem financial reports | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 | FX27-S01, FX27-S11 |
| <a id="finance-csv-preview"></a>`finance.csv.preview` — Preview CSV | COMPOSITE / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S12 |
| <a id="finance-csv-import"></a>`finance.csv.import` — Import CSV | COMPOSITE / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S12 |
| <a id="finance-csv-export"></a>`finance.csv.export` — Export CSV | COMPOSITE / SELF | Yes, gated | Financial (Administrative) | Blocked Q-05 | FX27-S12 |
| <a id="finance-report-share"></a>`finance.report.share` — Quản lý link chỉ-đọc của report | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-03/Q-05 | FX27-S11 |
| <a id="finance-support-read"></a>`finance.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `finance.account.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.account.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.account.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.account.read` |
| `finance.account.close` | Balance/reconciliation invariants Q-05; không delete ledger; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.transaction.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.transaction.post` | Balanced ledger/currency/rounding/correction semantics Q-05; không direct balance patch; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.transaction.correct` | Balanced ledger/currency/rounding/correction semantics Q-05; không direct balance patch; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.transaction.void` | Balanced ledger/currency/rounding/correction semantics Q-05; không direct balance patch; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.transaction.split` | Balanced ledger/currency/rounding/correction semantics Q-05; không direct balance patch; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.transfer.post` | Atomic paired entries; không duplicate expense/income; Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.category.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.category.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.category.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.category.read` |
| `finance.bill.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.bill.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.bill.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.bill.read` |
| `finance.subscription.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.subscription.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.subscription.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.subscription.read` |
| `finance.budget.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.budget.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.budget.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.budget.read` |
| `finance.savings.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.savings.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.savings.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.savings.read` |
| `finance.debt.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.debt.create` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.debt.update` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `finance.debt.read` |
| `finance.category.merge` | Reference preview; không rewrite posted history tùy ý; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.category.remove` | Reference preview; không rewrite posted history tùy ý; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.bill.cancel` | Linked transaction allocation and balance Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.bill.record_payment` | Linked transaction allocation and balance Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.bill.void_payment` | Linked transaction allocation and balance Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.subscription.pause` | Metadata only; không tự hủy dịch vụ ngoài/charge card; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.subscription.cancel` | Metadata only; không tự hủy dịch vụ ngoài/charge card; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.subscription.record_price` | Metadata only; không tự hủy dịch vụ ngoài/charge card; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.debt.record_payment` | Ledger and debt allocation Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.debt.adjust_interest` | Ledger and debt allocation Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.savings.record_progress` | Theo formula Q-05; không direct bank movement; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.report.read` | Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.csv.preview` | Schema/currency/dedupe + actual transaction/account rights Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.csv.import` | Schema/currency/dedupe + actual transaction/account rights Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.csv.export` | Schema/currency/dedupe + actual transaction/account rights Q-05; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |
| `finance.report.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | `sharing.link.read` |
| `finance.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Blocked financial semantics Q-05; reserve contracts only, không cho write production trước duyệt | Common + dynamic source/provider guards |

## Deny và UX contract

- Module off, grant missing/deny, resource wrong owner, disallowed lifecycle, current Q gate hoặc source dependency fail: không side effect; không dùng hidden button thay authorization.
- Before/after field diff được kiểm tra cho Save, import, version restore, bulk, scheduler và automation. Form không được gửi status/reveal/export/owner trong generic Update.
- Safe capability reason: ModuleUnavailable, ActionDenied, LifecycleLocked, DependencyUnavailable, DecisionBlocked hoặc StepUpRequired; unknown/wrong-owner resource trả unavailable chung để không enumerate.
- Grant không thay đổi state graph. Chỉ quyền đã cấp và hợp lệ mới xuất hiện enabled; permission editor có thể hiển thị blocked row để giải thích, không cho bật.
- Revocation và support/share/system contexts áp toàn bộ [common contract](../00-authorization-contract.md). Readonly projections không reuse full owner DTO.

## Acceptance tối thiểu

1. Với mỗi row: positive case đúng context/current state; wrong-owner và wrong-context negative; absent/deny Admin grant; module off; stale revision; lifecycle/Q gate.
2. COMMAND/COMPOSITE: request replay/idempotency, before-commit recheck; affected fields cần đủ action. QUERY: owner-scoped filtering trước count/pagination/projection, cache không rò source revoked.
3. LOCAL: keyboard/menu và tool entry cùng capability gate; không network/persist ngầm. SYSTEM: trusted caller, original authority và no UI grant.
4. Row nhạy cảm: no secret in response preview, toast, logs, URL, search, browser persistent storage; current recent-auth gate nếu required.
5. Nếu handler/source projection chưa có approved contract, action phải báo Blocked/Unavailable, không tự thực thi fallback rộng hơn.
