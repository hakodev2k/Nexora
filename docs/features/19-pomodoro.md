# Pomodoro và Focus

FX-19 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Phiên tập trung/nghỉ có timer, optionalTasklink và history.

[TickTick Focus](https://help.ticktick.com/articles/7055781980423585792): Tham chiếu Focus/Pomodoro từ kết quả trợ giúp chính thức; nội dung toàn trang chưa trích xuất được.

[TickTick](https://help.ticktick.com/): Task, Calendar, Habit và Focus có các khu vực thao tác chuyên biệt.

**Áp dụng cho Nexora:** TickTick Focus tham chiếu Pomodoro; lengths và conversions dưới đây là defaults Nexora được ủy quyền.

**Màn hình:** `/focus`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Chọn Task optional → Focus25phút → Start/Pause/Resume/Cancel.
2. Kết thúc phase thì owner chọn bắt đầu break5phút; sau4 focus có long break15phút.
3. Xem Completed/Cancelled history; explicit SaveAsTimeEntry cho elapsed focus.

## Dữ liệu và validation

- Defaults: Focus25phút (1–180), short break5 (1–60), long break15 (1–120), cycle4 (1–12).
- Phase Focus/ShortBreak/LongBreak; state Ready/Running/Paused/Completed/Cancelled; timestamps/elapsed/duration/TaskRef.

## Hành vi và lifecycle

- **FX-19-BR-001:** Một active Focus session/User; tính server instant trừ pause duration, background tab không reset.
- **FX-19-BR-002:** Không tự nối nhiều cycles khi offline. Chỉ hoàn thành phase đang chạy, chờ User bắt đầu phase tiếp.
- **FX-19-BR-003:** Task/Project terminal dừng linked focus và ghi Cancelled/SourceClosed; không sửa Task.
- **FX-19-BR-004:** Phase complete tạo Module Notification cả ba kênh; local beep tùy chọn không thay delivery contract.
- **FX-19-BR-005:** SaveAsTimeEntry explicit một lần/session; không tính break thành work time hoặc auto nhân dữ liệu Time Tracking.

## Quyền, API và tích hợp

- StartFocus, PauseFocus, ResumeFocus, CompletePhase, CancelFocus, SaveAsTimeEntry.
- Không tự tạo scheduled Calendar Event từ Focus.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-19-AC-001:** Reload giữ remaining time đúng.
- **FX-19-AC-002:** Double conversion chỉ một Time Entry.
- **FX-19-AC-003:** Offline nhiều cycles không tạo các phiên tập trung giả.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

Nguồn phạm vi: [module catalog](../requirements/01-scope-and-module-catalog.md). Feature này bổ sung chi tiết cho capability trong catalog, không bịa requirement ID phase cũ.

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
