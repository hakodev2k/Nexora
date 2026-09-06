# Projects

FX-11 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Project cá nhân bắt buộc làm container Task; trạng thái, thời gian, history, share và Trash.

[Microsoft To Do](https://support.microsoft.com/en-us/todo/): Lists và task cá nhân; chỉ dùng làm tham chiếu tổ chức công việc.

[TickTick](https://help.ticktick.com/): Task, Calendar, Habit và Focus có các khu vực thao tác chuyên biệt.

**Áp dụng cho Nexora:** Microsoft To Do/TickTick tham chiếu tổ chức công việc; trạng thái terminal vĩnh viễn của Nexora theo User, không sao chép hành vi reopen của sản phẩm khác.

**Màn hình:** `/projects, /projects/:id`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Trang Projects mặc định Grid; có Table. Mỗi mục hiển thị Title, Start, End. Tìm theo Title; lọc Tag, thời gian, Status; sắp xếp Title A–Z.
2. Tạo Project với Title, Description, Start và End bắt buộc; trạng thái ban đầu NotStarted.
3. Mở Project hiển thị Tasks bằng Kanban mặc định. Project đang hoạt động cho phép sửa thông tin.
4. Complete hoặc Skip cần xác nhận Project sẽ chuyển chỉ-đọc. Complete khi còn Task chưa kết thúc phải có lý do.
5. Xóa đưa Project và Tasks vào Trash cùng một đợt. Khôi phục đúng cấu trúc và trạng thái tại lúc xóa.

## Dữ liệu và validation

- Title 1–200 ký tự; Description không trống, tối đa20.000 ký tự; StartDateTime/EndDateTime bắt buộc và End > Start.
- Optional: Priority P0–P3, nhiều Tag dùng chung catalog với Task, màu sắc hoặc icon, ghi chú.
- Status: NotStarted, InProgress, Completed, Skipped. Actor và thời điểm thay đổi do server ghi.

## Hành vi và lifecycle

- **FX-11-BR-001:** NotStarted → InProgress tự do. InProgress → NotStarted được phép nhưng cần lý do (delegated). Completed/Skipped không bao giờ mở lại.
- **FX-11-BR-002:** Complete khi còn Task dở được phép sau cảnh báo và lý do; Skip giữ nguyên trạng thái Tasks. Cả hai khóa toàn bộ Task: không sửa, tạo thêm, khôi phục hoặc tiếp tục Task dở.
- **FX-11-BR-003:** Khi tất cả Tasks hiện hữu đã Completed/Skipped, hỏi owner có muốn Complete Project; không tự chuyển. Project rỗng không phát lời nhắc này.
- **FX-11-BR-004:** Task nằm ngoài thời gian Project chỉ gây cảnh báo; owner xác nhận thì lưu. Sửa thời gian Project làm Tasks vượt ngoài cũng phải preview/cảnh báo, không tự dời Tasks.
- **FX-11-BR-005:** Mọi thay đổi Project có history. Project share hiển thị live Project và toàn bộ Task details hiện hữu, không history/lý do/reminder config.
- **FX-11-BR-006:** Không tạo Calendar Event cho Project. Import/export Project và Task chưa hỗ trợ trong Release1; xóa Project không xóa Finance/Document liên kết.

## Quyền, API và tích hợp

- CreateProject, UpdateProject, TransitionProject, TrashProject, RestoreProject, PurgeProject; kiểm tra revision và aggregate tại commit.
- ProjectClosed khóa Task mutations, hủy pending reminders nhưng giữ Calendar projection lịch sử; cập nhật Search và Sharing qua provider.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-11-AC-001:** Complete Project có InProgress Task mà thiếu lý do bị từ chối. Khi Complete thành công, Task vẫn InProgress nhưng API chỉnh sửa bị chặn.
- **FX-11-AC-002:** Không thể mở lại Project bằng history restore hoặc payload sửa status.
- **FX-11-AC-003:** Restore Project không hồi sinh Task đã bị xóa riêng trước đợt xóa Project.
- **FX-11-AC-004:** Hủy cảnh báo thời gian thì không có mutation.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

- [06-decisions-and-traceability.md](../requirements/06-decisions-and-traceability.md): `DEC-PRJ-001`, `DEC-PRJ-002`, `DEC-PRJ-003`, `DEC-PRJ-004`, `DEC-PRJ-005`, `DEC-PRJ-006`, `DEC-PRJ-007`, `DEC-PRJ-008`
- [phase-02-productivity.md](../requirements/phases/phase-02-productivity.md): `P02-PRJ-001`, `P02-PRJ-002`, `P02-PRJ-003`, `P02-PRJ-004`, `P02-PRJ-005`, `P02-PRJ-010`, `P02-PRJ-011`, `P02-PRJ-012`, `P02-PRJ-013`, `P02-PRJ-014`, `P02-PRJ-015`, `P02-PRJ-016`, `P02-PRJ-020`, `P02-PRJ-021`, `P02-PRJ-022`, `P02-PRJ-023`, `P02-PRJ-024`, `P02-PRJ-025`, `P02-PRJ-030`, `P02-PRJ-031`, `P02-PRJ-032`, `P02-PRJ-033`, `P02-PRJ-034`, `P02-PRJ-035`, `P02-PRJ-040`, `P02-PRJ-041`, `P02-PRJ-042`, `P02-PRJ-043`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
