# Habit Tracker

FX-17 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Thói quen cá nhân theo ngày/ngày trong tuần, check-in, streak và lịch sử.

[TickTick](https://help.ticktick.com/): Task, Calendar, Habit và Focus có các khu vực thao tác chuyên biệt.

**Áp dụng cho Nexora:** TickTick tham chiếu habit/check-in; khôngmedical advice hoặc social leaderboard.

**Màn hình:** `/habits`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Tạo Habit với Boolean/Count target và lịch daily/selected weekdays; reminder optional.
2. Today checklist hoặc month grid; check-in/nhập count.
3. Pause/resume/archive; sửa lịch theo ngày hiệu lực; xem streak/history.

## Dữ liệu và validation

- Title1–100; mode Boolean/Count bất biến; count target >0/unit optional; weekday set không trống.
- CheckIn unique Habit+local date, count≥0 hoặc boolean; comment và changedAt.
- Active/Paused/Archived/Trash; một reminder tùy chọn theo giờ trong ngày.

## Hành vi và lifecycle

- **FX-17-BR-001:** Không check-in tương lai. Sửa ngày quá khứ có Activity; đổi timezone không rewrite ngày lịch sử.
- **FX-17-BR-002:** Streak chỉ xét scheduled days đạt target. Ngày không scheduled hoặc paused bỏ qua; bỏ lỡ scheduled day ngắt streak.
- **FX-17-BR-003:** Đổi target/schedule chỉ có hiệu lực từ ngày đã chọn trở đi, không chấm lại lịch sử trước đó.
- **FX-17-BR-004:** Archive chỉ-đọc có Unarchive; Trash/manual purge; không xóa Tasks/Goals liên kết.
- **FX-17-BR-005:** Reminder dùng ba kênh; check-in đạt target trước dispatch hủy pending alert ngày đó.

## Quyền, API và tích hợp

- CreateHabit, SetSchedule, CheckIn, Pause, Archive; reminder revision; GetStreak deterministic.
- Không tạo Calendar Event từ Habit nếu chưa có scope được duyệt.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-17-AC-001:** Ngày không scheduled không ngắt streak.
- **FX-17-AC-002:** Đổi target không sửa achievements trước ngày hiệu lực.
- **FX-17-AC-003:** Retry cùng CheckIn command không cộng count lần nữa.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

Nguồn phạm vi: [module catalog](../requirements/01-scope-and-module-catalog.md). Feature này bổ sung chi tiết cho capability trong catalog, không bịa requirement ID phase cũ.

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
