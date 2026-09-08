# FX-14 — Reminders / Scheduling: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/14-reminders-and-scheduling.md) — FX-14-BR-001, FX-14-BR-002, FX-14-BR-003, FX-14-BR-004, FX-14-BR-005, FX-14-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/14-reminders.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `reminders` là stable logical key, bind installed ModuleId trong manifest. **Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="reminders-configuration-read"></a>`reminders.configuration.read` — Xem reminder trong form nguồn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX14-S01, FX14-S03 |
| <a id="reminders-configuration-set"></a>`reminders.configuration.set` — Đặt reminder tại nguồn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX14-S01, FX14-S02 |
| <a id="reminders-configuration-remove"></a>`reminders.configuration.remove` — Gỡ reminder tại nguồn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX14-S01 |
| <a id="reminders-schedule-reconcile"></a>`reminders.schedule.reconcile` — Đồng bộ lịch theo source revision | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="reminders-schedule-invalidate"></a>`reminders.schedule.invalidate` — Vô hiệu lịch cũ | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="reminders-schedule-dispatch"></a>`reminders.schedule.dispatch` — Dispatch reminder đến hạn | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `reminders.configuration.read` | Kèm source read; không đọc reminder config từ shared projection; Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh | Common + dynamic source/provider guards |
| `reminders.configuration.set` | Delegate source set_reminder/remove_reminder; không quyền tạo reminder bất kỳ resource; Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh | Common + dynamic source/provider guards |
| `reminders.configuration.remove` | Delegate source set_reminder/remove_reminder; không quyền tạo reminder bất kỳ resource; Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh | Common + dynamic source/provider guards |
| `reminders.schedule.reconcile` | Trusted scheduler; đúng owner/source/version; kiểm tra enabled/currentstate; idempotent occurrence; không làm Task tự đổi status; Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh | Common + dynamic source/provider guards |
| `reminders.schedule.invalidate` | Trusted scheduler; đúng owner/source/version; kiểm tra enabled/currentstate; idempotent occurrence; không làm Task tự đổi status; Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh | Common + dynamic source/provider guards |
| `reminders.schedule.dispatch` | Trusted scheduler; đúng owner/source/version; kiểm tra enabled/currentstate; idempotent occurrence; không làm Task tự đổi status; Một reminder/source; current source version/state; luôn tạo delivery intents cả ba kênh | Common + dynamic source/provider guards |

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
