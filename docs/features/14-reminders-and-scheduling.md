# Reminders và Due Scheduling

FX-14 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Một reminder cho Task/Personal Event và scheduler dùng chung cho reminder nguồn khác.

[TickTick](https://help.ticktick.com/): Task, Calendar, Habit và Focus có các khu vực thao tác chuyên biệt.

**Áp dụng cho Nexora:** Tham chiếu TickTick reminder; giữ preset15phút và all 3 channels đã confirmed.

**Màn hình:** `Task/Event reminder field, nguồn expiry reminders`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Task/Event form chọn None,15phút trước Start hoặc exact datetime; preview theo User timezone rồi Save cùng resource.
2. Đến hạn, worker kiểm tra source revision/lifecycle/permission rồi tạo Notification.
3. Thay thời gian/reminder, Complete/Cancel/Trash hoặc disable module vô hiệu pending intent cũ.

## Dữ liệu và validation

- ReminderId, sourceRef, sourceRevision, dueInstant, timezone, configType, dispatchState; unique source+revision.
- Giới hạn một Reminder áp dụng Task và Personal Event. Expiry reminders của module khác theo đặc tả riêng.

## Hành vi và lifecycle

- **FX-14-BR-001:** Exact datetime phải ở tương lai khi tạo hoặc đổi một pending reminder. Preset đã qua thì yêu cầu chọn exact hoặc bỏ reminder. Sửa field khác của Task, hoặc restore version chứa reminder đã qua, được giữ historical config với trạng thái Expired; không enqueue alert cũ hoặc chặn toàn bộ Save chỉ vì reminder lịch sử đã hết hạn.
- **FX-14-BR-002:** All-day preset là15phút trước00:00 ngày bắt đầu, tức23:45 ngày trước; preview phải thể hiện rõ.
- **FX-14-BR-003:** Không đổi status Task tại Start. Terminal Task/Project hoặc Personal Event terminal hủy pending reminder.
- **FX-14-BR-004:** Missed-run default delegated: trễ tối đa15phút thì gửi có nhãn trễ; quá15phút ghi Missed trong source activity, không gửi backlog email hàng loạt.
- **FX-14-BR-005:** Dedupe theo source/revision/due time. Luôn tạo cả ba channel attempts độc lập; đổi timezone không dời exact instant của timed reminder.
- **FX-14-BR-006:** Independent Reminder, snooze và recurrence thuộc Q-10; không coi chúng đã duyệt từ hành vi TickTick.

## Quyền, API và tích hợp

- ScheduleReminder, InvalidateReminder, DispatchDueReminder; recheck source revision ngay trước dispatch.
- Notification đã gửi giữ trong Inbox tới khi User xóa, dù reminder/source được sửa.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-14-AC-001:** Reschedule sát giờ cũ không gửi stale reminder.
- **FX-14-AC-002:** Worker restart không tạo hai logical notifications.
- **FX-14-AC-003:** Browser denied không chặn Email/In-app.
- **FX-14-AC-004:** Project đóng khi còn Task dở không gửi reminder cũ.

AC nguồn và common acceptance gates vẫn bắt buộc.

### Scenario khôi phục reminder lịch sử

Khôi phục Task version có exact reminder ở quá khứ vẫn giữ toàn bộ editable snapshot hợp lệ; config reminder được đánh Expired và không phát ba kênh lần nữa. Missed-run catch-up15phút chỉ áp dụng intent đã đến hạn trong quá trình vận hành, không áp dụng việc owner khôi phục một version cũ.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `JOB-001`, `JOB-002`, `JOB-003`, `JOB-004`
- [phase-02-productivity.md](../requirements/phases/phase-02-productivity.md): `P02-RMD-001`, `P02-RMD-002`, `P02-RMD-003`, `P02-RMD-004`

Quyết định lớn cần PO: [Q-10](90-open-decisions.md#q-10). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
