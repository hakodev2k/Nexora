# FX-31 — Shopping Records: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/31-shopping-records.md) — FX-31-BR-001, FX-31-BR-002, FX-31-BR-003, FX-31-BR-004, FX-31-BR-005, FX-31-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/31-shopping-records.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `shopping` là stable logical key, bind installed ModuleId trong manifest. **Owner manual records; không checkout/payment/provider write**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="shopping-wishlist-read"></a>`shopping.wishlist.read` — Xem Wishlist item | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-create"></a>`shopping.wishlist.create` — Tạo Wishlist item | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-update"></a>`shopping.wishlist.update` — Sửa Wishlist item | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-mark-purchased"></a>`shopping.wishlist.mark_purchased` — Đánh dấu đã mua | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-archive"></a>`shopping.wishlist.archive` — Archive wishlist | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-unarchive"></a>`shopping.wishlist.unarchive` — Unarchive wishlist | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-trash"></a>`shopping.wishlist.trash` — Đưa wishlist vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-restore"></a>`shopping.wishlist.restore` — Khôi phục wishlist từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-wishlist-purge"></a>`shopping.wishlist.purge` — Xóa vĩnh viễn wishlist | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX31-S01 |
| <a id="shopping-comparison-read"></a>`shopping.comparison.read` — Xem Comparison | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S02 |
| <a id="shopping-comparison-create"></a>`shopping.comparison.create` — Tạo Comparison | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S02 |
| <a id="shopping-comparison-update"></a>`shopping.comparison.update` — Sửa Comparison | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S02 |
| <a id="shopping-comparison-members"></a>`shopping.comparison.members` — Chọn sản phẩm so sánh | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S02 |
| <a id="shopping-comparison-criteria"></a>`shopping.comparison.criteria` — Sửa tiêu chí | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S02 |
| <a id="shopping-comparison-remove"></a>`shopping.comparison.remove` — Xóa comparison | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S02 |
| <a id="shopping-order-read"></a>`shopping.order.read` — Xem Order | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S03 |
| <a id="shopping-order-create"></a>`shopping.order.create` — Tạo Order | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S04 |
| <a id="shopping-order-update"></a>`shopping.order.update` — Sửa Order | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S04 |
| <a id="shopping-order-transition"></a>`shopping.order.transition` — Đổi trạng thái Order | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S04 |
| <a id="shopping-order-return"></a>`shopping.order.return` — Ghi nhận return/refund metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S04 |
| <a id="shopping-order-trash"></a>`shopping.order.trash` — Đưa order vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S03 |
| <a id="shopping-order-restore"></a>`shopping.order.restore` — Khôi phục order từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S03 |
| <a id="shopping-order-purge"></a>`shopping.order.purge` — Xóa vĩnh viễn order | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX31-S03 |
| <a id="shopping-order-history"></a>`shopping.order.history` — Xem lịch sử order | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX31-S04 |
| <a id="shopping-seller-read"></a>`shopping.seller.read` — Xem Seller | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S05 |
| <a id="shopping-seller-create"></a>`shopping.seller.create` — Tạo Seller | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S05 |
| <a id="shopping-seller-update"></a>`shopping.seller.update` — Sửa Seller | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S05 |
| <a id="shopping-seller-merge"></a>`shopping.seller.merge` — Gộp seller trùng | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S05 |
| <a id="shopping-warranty-read"></a>`shopping.warranty.read` — Xem Warranty record | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S06 |
| <a id="shopping-warranty-create"></a>`shopping.warranty.create` — Tạo Warranty record | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S06 |
| <a id="shopping-warranty-update"></a>`shopping.warranty.update` — Sửa Warranty record | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S06 |
| <a id="shopping-warranty-evidence"></a>`shopping.warranty.evidence` — Gắn/gỡ warranty evidence | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S06 |
| <a id="shopping-purchase-create-asset"></a>`shopping.purchase.create_asset` — Tạo Personal Asset từ purchase | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX31-S07 |
| <a id="shopping-support-read"></a>`shopping.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |
| <a id="shopping-order-link-finance"></a>`shopping.order.link_finance` — Gắn/gỡ Finance reference | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Blocked Q-05 for financial reference semantics | FX31-S04 |
| <a id="shopping-order-share"></a>`shopping.order.share` — Quản lý link chỉ-đọc của order | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Blocked Q-03 | FX31-S04 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `shopping.wishlist.read` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.create` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.update` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | `shopping.wishlist.read` |
| `shopping.wishlist.mark_purchased` | Explicit owner action, không auto post Finance; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.archive` | Owner manual records; không checkout/payment/provider write; ngoài Trash, chưa Archived; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.unarchive` | Owner manual records; không checkout/payment/provider write; Archived, khôi phục previous state; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.trash` | Owner manual records; không checkout/payment/provider write; preview aggregate, không purge; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.restore` | Owner manual records; không checkout/payment/provider write; đúng deletion cohort, parent hợp lệ; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.wishlist.purge` | Owner manual records; không checkout/payment/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.comparison.read` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.comparison.create` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.comparison.update` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | `shopping.comparison.read` |
| `shopping.comparison.members` | 2–4 products; same-unit comparison, unknown không0; xóa container không xóa product; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.comparison.criteria` | 2–4 products; same-unit comparison, unknown không0; xóa container không xóa product; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.comparison.remove` | 2–4 products; same-unit comparison, unknown không0; xóa container không xóa product; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.read` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.create` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.update` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | `shopping.order.read` |
| `shopping.order.transition` | Order graph FX-31, history; không tự hoàn tiền hoặc write Finance; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.return` | Order graph FX-31, history; không tự hoàn tiền hoặc write Finance; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.trash` | Owner manual records; không checkout/payment/provider write; preview aggregate, không purge; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.restore` | Owner manual records; không checkout/payment/provider write; đúng deletion cohort, parent hợp lệ; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.purge` | Owner manual records; không checkout/payment/provider write; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.history` | Owner-only history, cùng owner/module; không share/support; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.seller.read` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.seller.create` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.seller.update` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | `shopping.seller.read` |
| `shopping.seller.merge` | Explicit preview references; không tự merge theo tên; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.warranty.read` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.warranty.create` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.warranty.update` | Owner manual records; không checkout/payment/provider write; Owner manual records; không checkout/payment/provider write | `shopping.warranty.read` |
| `shopping.warranty.evidence` | files.reference.attach/detach + Clean + own record; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.purchase.create_asset` | Preview mapped fields; same owner; idempotent source link; không auto-create trước Save; Owner manual records; không checkout/payment/provider write | `assets.asset.create` |
| `shopping.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Owner manual records; không checkout/payment/provider write | Common + dynamic source/provider guards |
| `shopping.order.link_finance` | Same owner + finance.transaction.read; no transaction write/ledger side effect; Owner manual records; không checkout/payment/provider write | `finance.transaction.read` |
| `shopping.order.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Owner manual records; không checkout/payment/provider write | `sharing.link.read` |

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
