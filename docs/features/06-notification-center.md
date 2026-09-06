# Notification Center và Delivery

FX-06 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Task/Calendar reminders, Support/Emergency, Security/Account và Module/System notifications.

[GitHub Notifications](https://docs.github.com/en/subscriptions-and-notifications/how-tos/viewing-and-triaging-notifications/managing-notifications-from-your-inbox): Inbox có thao tác đọc và xử lý thông báo.

**Áp dụng cho Nexora:** GitHub tham chiếu inbox actions; Nexora gửi đồng thời cả In-app, Email, Browser Push theo yêu cầu, không channel preference/mute.

**Màn hình:** `/notifications`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Domain event tạo logical Notification và ba delivery attempts độc lập.
2. Inbox unread/all, newest first → mở source hoặc mark read/unread/all read.
3. Select từng/multiple để delete; notification giữ tới User chủ động xóa.

## Dữ liệu và validation

- Notification ID/type/sourceRef/title/safe body/createdAt/readAt/deletedAt; delivery per-channel state/attempt/lastError.
- Push subscription theo device, secret endpoint không lộ Admin; logical dedupe key.

## Hành vi và lifecycle

- **FX-06-BR-001:** Cả ba kênh luôn được lên lịch đồng thời; thiếu push permission/endpoint ghi Unavailable, không chặn In-app/Email.
- **FX-06-BR-002:** Retry transient độc lập, không phát ba logical notifications; no guarantee simultaneous receipt.
- **FX-06-BR-003:** MarkAllRead áp dụng watermark hiện tại; notification đến sau vẫn unread.
- **FX-06-BR-004:** Delete inbox không xóa security audit, không unsend email/push; đã committed delivery tiếp tục theo contract.
- **FX-06-BR-005:** Open source recheck permission; deleted/disabled/unavailable hiển thị thông báo an toàn không expose cached body.
- **FX-06-BR-006:** No secret payload, reset tokens trong general notifications; security email dedicated token flow riêng.

## Quyền, API và tích hợp

- PublishNotification/DeliverChannel/MarkRead/MarkAllRead/DeleteNotifications/OpenSource.
- Source provider định nghĩa safe template và route; failed finaldelivery operational monitoring.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-06-AC-001:** Browser denied vẫn tạo In-app và gửi Email; UI không báo push delivered giả.
- **FX-06-AC-002:** Retry cùng event chỉ một inbox item.
- **FX-06-AC-003:** Bulk delete retry không xóa notification mới ngoài selection.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `NTF-001`, `NTF-002`, `NTF-003`, `NTF-004`, `NTF-005`, `NTF-006`, `NTF-007`, `NTF-008`, `NTF-009`, `NTF-010`
- [06-decisions-and-traceability.md](../requirements/06-decisions-and-traceability.md): `DEC-NTF-001`, `DEC-NTF-002`, `DEC-NTF-003`, `DEC-NTF-004`, `DEC-NTF-005`, `DEC-NTF-006`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PLT-004`, `P01-PLT-007`, `P01-PLT-008`
- [phase-02-productivity.md](../requirements/phases/phase-02-productivity.md): `P02-NTF-001`, `P02-NTF-002`, `P02-NTF-003`, `P02-NTF-004`, `P02-NTF-005`, `P02-NTF-006`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
