# Daily và Weekly Planner

FX-15 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Lập kế hoạch ngày/tuần bằng cách chọn Task đã có; không resourceTask song song.

[Microsoft To Do](https://support.microsoft.com/en-us/todo/): Lists và task cá nhân; chỉ dùng làm tham chiếu tổ chức công việc.

[TickTick](https://help.ticktick.com/): Task, Calendar, Habit và Focus có các khu vực thao tác chuyên biệt.

**Áp dụng cho Nexora:** Tham chiếu daily planning của Microsoft To Do/TickTick, giữ sourceTask và Project ownership của Nexora.

**Màn hình:** `/planner`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Mở Today/Week theo User timezone; chọn Task từ Projects đang hoạt động và pin vào ngày.
2. Sắp xếp thủ công, xem due/overdue/Project; mở Task để chỉnh tại nguồn.
3. Unpin hoặc đổi ngày trong plan; Task hoàn thành vẫn nằm trong lịch sử plan.

## Dữ liệu và validation

- PlanDate local date, TaskRef cùng owner, rank; unique User+date+Task.
- Tuần bắt đầu Monday (delegated); ghi chú plan optional≤2.000 ký tự.

## Hành vi và lifecycle

- **FX-15-BR-001:** Pin/đổi ngày plan không đổi Task Start/End/Reminder. Reschedule phải qua Task form.
- **FX-15-BR-002:** Không copy Task. Task thuộc Project terminal chỉ đọc, không thêm vào plan mới; Trash source hiển thị unavailable, không cached private payload.
- **FX-15-BR-003:** Không auto-carryover. Task chưa xong giữ ở ngày cũ; Today có gợi ý backlog để owner tự pin.
- **FX-15-BR-004:** Plan không tạo Calendar Event mới; mỗi Task đã có projection riêng.
- **FX-15-BR-005:** Bỏ plan entry không xóa Task/history. Không share plan riêng vì chưa có approved projection.

## Quyền, API và tích hợp

- PinTask, UnpinTask, ReorderPlan, GetPlan; TaskQuery provider kiểm tra current access.
- Progress đếm Completed riêng, không cộng Skipped thành hoàn thành.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-15-AC-001:** Pin hai ngày vẫn chỉ một Task và một Calendar Event.
- **FX-15-AC-002:** Đổi ngày plan không đổi Reminder.
- **FX-15-AC-003:** Không pin Task của User khác hoặc đổi status Task trong closed Project.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

Nguồn phạm vi: [module catalog](../requirements/01-scope-and-module-catalog.md). Feature này bổ sung chi tiết cho capability trong catalog, không bịa requirement ID phase cũ.

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
