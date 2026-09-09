# FX-06 — Notifications: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/06-notification-center.md) — FX-06-BR-001, FX-06-BR-002, FX-06-BR-003, FX-06-BR-004, FX-06-BR-005, FX-06-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/06-notifications.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `notifications` là stable logical key, bind installed ModuleId trong manifest. **Chỉ own inbox; fixed all3channels, không mute/quiet hours**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="notifications-inbox-read"></a>`notifications.inbox.read` — Đọc inbox/detail/delivery state | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX06-S01, FX06-S02 |
| <a id="notifications-inbox-mark-read"></a>`notifications.inbox.mark_read` — Đánh dấu đã đọc | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX06-S01 |
| <a id="notifications-inbox-mark-unread"></a>`notifications.inbox.mark_unread` — Đánh dấu chưa đọc | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX06-S01 |
| <a id="notifications-inbox-mark-all-read"></a>`notifications.inbox.mark_all_read` — Đánh dấu tất cả đã đọc tại watermark | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX06-S01 |
| <a id="notifications-inbox-delete"></a>`notifications.inbox.delete` — Xóa inbox item/selection | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX06-S01 |
| <a id="notifications-source-open"></a>`notifications.source.open` — Mở trang nguồn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX06-S02 |
| <a id="notifications-push-subscribe"></a>`notifications.push.subscribe` — Đăng ký browser push endpoint | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX06-S03 |
| <a id="notifications-push-remove"></a>`notifications.push.remove` — Gỡ subscription thiết bị | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX06-S03 |
| <a id="notifications-dispatch-publish"></a>`notifications.dispatch.publish` — Tạo logical notification và3attempts | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="notifications-dispatch-deliver"></a>`notifications.dispatch.deliver` — Gửi channel attempt | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `notifications.inbox.read` | Chỉ own inbox; fixed all3channels, không mute/quiet hours; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.inbox.mark_read` | Owner; watermark không bao gồm notifications đến sau; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.inbox.mark_unread` | Owner; watermark không bao gồm notifications đến sau; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.inbox.mark_all_read` | Owner; watermark không bao gồm notifications đến sau; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.inbox.delete` | Owner explicit IDs; không xóa audit/unsend delivery; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.source.open` | Kèm action đọc của source; source revoked thì safe unavailable; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.push.subscribe` | Own device/browser permission; không phải tùy chọn bỏ kênh bắt buộc; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.push.remove` | Own device/browser permission; không phải tùy chọn bỏ kênh bắt buộc; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.dispatch.publish` | Trusted intent; safe payload; dedupe, independent failure; no permission to arbitrary-send User content; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |
| `notifications.dispatch.deliver` | Trusted intent; safe payload; dedupe, independent failure; no permission to arbitrary-send User content; Chỉ own inbox; fixed all3channels, không mute/quiet hours | Common + dynamic source/provider guards |

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
