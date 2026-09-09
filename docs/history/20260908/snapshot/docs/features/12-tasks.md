# Tasks, Kanban và Table

FX-12 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Task cá nhân, status transitions, required times, priorities/tags, AC, history/versionrestore và Calendar projection.

[Microsoft To Do](https://support.microsoft.com/en-us/todo/): Lists và task cá nhân; chỉ dùng làm tham chiếu tổ chức công việc.

[TickTick](https://help.ticktick.com/): Task, Calendar, Habit và Focus có các khu vực thao tác chuyên biệt.

**Áp dụng cho Nexora:** Kanban và Task organization tham chiếu TickTick; mọi Task thuộc một Project không đổi, không assignment/team.

**Màn hình:** `/projects/:id/tasks, /tasks/:id`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Chỉ tạo Task tại cột NotStarted/InProgress. Quick Create luôn mở form đầy đủ với Project, Title, Start, End bắt buộc.
2. Kanban hỗ trợ kéo trạng thái, sắp xếp trong cột và mở detail; Table có Title, Status, Priority, Start, End.
3. Lọc theo Status/khoảng thời gian; tìm Title/Tag. Card hiển thị Title, Priority, Start, End, Overdue.
4. Sửa và xem history; restore toàn bộ phiên bản tạo revision mới. Trash/restore phải kiểm tra Project cha.

## Dữ liệu và validation

- ProjectId bất biến; Title1–200; Start/End bắt buộc, End > Start. Description, AC dạng text/checklist, Priority và nhiều Tag là optional.
- P0 cao nhất, P3 thấp nhất. Tag dùng chung với Projects. Checklist lưu ID từng mục để history/diff ổn định.
- Tối đa một Reminder: exact datetime hoặc15phút trước Start. Calendar projection duy nhất theo TaskId.
- Status: NotStarted, InProgress, Completed, Skipped. Rank theo Project+cột; history snapshot và lý do chuyển ngược.

## Hành vi và lifecycle

- **FX-12-BR-001:** NotStarted → InProgress/Completed và InProgress → Completed tự do. Skip từ NotStarted/InProgress. InProgress → NotStarted hoặc Completed/Skipped → InProgress/NotStarted cần lý do.
- **FX-12-BR-002:** Delegated: không chuyển trực tiếp Completed ↔ Skipped; quay về InProgress có lý do rồi chuyển. AC checklist chưa đạt không tự chặn Complete vì User chưa yêu cầu gate này.
- **FX-12-BR-003:** Task Completed/Skipped vẫn sửa được khi Project hoạt động. Project terminal chặn mọi mutation, create và restore. Không chuyển Task sang Project khác.
- **FX-12-BR-004:** Đến Start không tự đổi status. Qua End mà Task chưa Completed/Skipped thì đánh Overdue; không tự Complete/Skip. Đóng Project không sửa status các Task còn dở.
- **FX-12-BR-005:** Mọi thay đổi gồm fields/status/AC/rank/reminder tạo history. Restore toàn bộ editable snapshot nhưng không đổi owner/Project hoặc vượt lifecycle gate; restore làm status chuyển ngược vẫn cần lý do.
- **FX-12-BR-006:** Task điều khiển Calendar một chiều. Terminal giữ Event và status; Trash ẩn projection, vô hiệu share và reminder. Subtasks/recurrence/attachments còn Q-10.

## Quyền, API và tích hợp

- CreateTask, UpdateTask, TransitionTask, ReorderTask, RestoreTaskVersion, TrashTask, RestoreTask; kiểm tra Project state tại commit.
- TaskChanged/TaskTrashed cập nhật Calendar, Reminder và Search idempotently theo source revision.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-12-AC-001:** Drag ngược thiếu reason bị reject; hủy dialog trả card về trạng thái server.
- **FX-12-AC-002:** Retry Save không nhân version/Calendar Event; stale Save trả conflict.
- **FX-12-AC-003:** Restore không thể đổi Project hoặc sửa Task trong Project đã kết thúc.
- **FX-12-AC-004:** Priority trống hợp lệ; search nhiều Tag giữ owner isolation.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

- [06-decisions-and-traceability.md](../requirements/06-decisions-and-traceability.md): `DEC-TSK-001`, `DEC-TSK-002`, `DEC-TSK-003`, `DEC-TSK-004`, `DEC-TSK-005`, `DEC-TSK-006`, `DEC-TSK-007`, `DEC-TSK-008`, `DEC-TSK-009`, `DEC-TSK-010`, `DEC-TSK-011`
- [phase-02-productivity.md](../requirements/phases/phase-02-productivity.md): `P02-HIS-001`, `P02-HIS-002`, `P02-HIS-003`, `P02-HIS-004`, `P02-HIS-005`, `P02-HIS-006`, `P02-TSK-001`, `P02-TSK-002`, `P02-TSK-003`, `P02-TSK-004`, `P02-TSK-005`, `P02-TSK-006`, `P02-TSK-007`, `P02-TSK-010`, `P02-TSK-011`, `P02-TSK-012`, `P02-TSK-013`, `P02-TSK-014`, `P02-TSK-020`, `P02-TSK-021`, `P02-TSK-022`, `P02-TSK-023`, `P02-TSK-024`, `P02-TSK-025`, `P02-VIW-001`, `P02-VIW-002`, `P02-VIW-003`, `P02-VIW-004`, `P02-VIW-005`, `P02-VIW-006`, `P02-VIW-007`, `P02-VIW-008`, `P02-VIW-009`

Quyết định lớn cần PO: [Q-10](90-open-decisions.md#q-10). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
