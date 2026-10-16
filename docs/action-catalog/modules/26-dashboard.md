# FX-26 — Dashboard: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/26-dashboard.md) — FX-26-BR-001, FX-26-BR-002, FX-26-BR-003, FX-26-BR-004, FX-26-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/26-dashboard.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `dashboard` là stable logical key, bind installed ModuleId trong manifest. **Attention summary; source read và module checks mỗi widget**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="dashboard-dashboard-read"></a>`dashboard.dashboard.read` — Xem Dashboard | QUERY / SELF | Yes, gated | Normal (Normal) | **SLICE_IMPLEMENTED (local)** — owner-scoped attention projection; runtime not run | FX26-S01 |
| <a id="dashboard-layout-update"></a>`dashboard.layout.update` — Lưu layout | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S02 |
| <a id="dashboard-layout-add-widget"></a>`dashboard.layout.add_widget` — Thêm widget | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S02 |
| <a id="dashboard-layout-configure-widget"></a>`dashboard.layout.configure_widget` — Cấu hình widget | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S02 |
| <a id="dashboard-layout-remove-widget"></a>`dashboard.layout.remove_widget` — Gỡ widget | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S02 |
| <a id="dashboard-layout-reorder-widget"></a>`dashboard.layout.reorder_widget` — Sắp xếp widget | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S02 |
| <a id="dashboard-widget-refresh"></a>`dashboard.widget.refresh` — Refresh widget | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S01 |
| <a id="dashboard-quick-create-open"></a>`dashboard.quick_create.open` — Mở create flow nguồn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX26-S01 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `dashboard.dashboard.read` | Attention summary; source read và module checks mỗi widget; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.layout.update` | Own layout/manifest declared widgets; config không tự cấp source data; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.layout.add_widget` | Own layout/manifest declared widgets; config không tự cấp source data; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.layout.configure_widget` | Own layout/manifest declared widgets; config không tự cấp source data; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.layout.remove_widget` | Own layout/manifest declared widgets; config không tự cấp source data; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.layout.reorder_widget` | Own layout/manifest declared widgets; config không tự cấp source data; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.widget.refresh` | Source query current permissions; redact/remove cached results khi revoked; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |
| `dashboard.quick_create.open` | Target module/create action required; Save vẫn source validation; Attention summary; source read và module checks mỗi widget | Common + dynamic source/provider guards |

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
