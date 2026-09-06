# Calendar, Personal Events và ICS

FX-13 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Day/Week/Month/Agenda, manual events và Task projections, import/export ICS.

[Google Calendar](https://support.google.com/calendar/answer/37118?hl=en): Import file lịch là thao tác riêng, không đồng nghĩa đồng bộ liên tục.

**Áp dụng cho Nexora:** Google Calendar tham chiếu calendar/file import; Nexora no external sync, recurrence import, drag/resize hoặc Event share.

**Màn hình:** `/calendar, /calendar/events/:id`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Mở Calendar ở Day; có Week/Month/Agenda. Lọc Status/thời gian; tìm Title hoặc tên Project của Task.
2. Tạo Personal Event bằng form với Title, Description, Start, End; optional All-day và Reminder. Không drag/resize để sửa.
3. Scheduled → Completed/Canceled; Delete chính là Cancel. Canceled vẫn hiện gạch ngang.
4. Import ICS có preview/report. Export chọn Manual Events, Task Events hoặc cả hai; chọn trạng thái và toàn bộ/custom time range.

## Dữ liệu và validation

- Personal Event: Title1–200, Description không trống≤20.000; timed Start/End với End > Start hoặc all-day startDate/endDateExclusive.
- Personal status Scheduled/Completed/Canceled; Task projection giữ status Task riêng, không ép thành Scheduled.
- Source UID của ICS thuộc owner; timezone/provenance; tối đa một Reminder exact hoặc15phút trước Start.

## Hành vi và lifecycle

- **FX-13-BR-001:** Trùng thời gian cho phép sau cảnh báo. Completed/Canceled chỉ-đọc, không mở lại. Scheduled qua End vẫn hiển thị bình thường, không Overdue hoặc tự đổi trạng thái.
- **FX-13-BR-002:** Timed values giữ instant khi đổi timezone. All-day giữ ngày; UI nhập ngày cuối inclusive, storage endDate exclusive; hỗ trợ nhiều ngày.
- **FX-13-BR-003:** Import valid non-recurring VEVENT thành Personal Event Scheduled. Skip bản thiếu Title/Description/Start/End/UID, duplicate UID hoặc recurrence RRULE/RDATE/RECURRENCE-ID; bỏ VALARM và báo lý do từng entry.
- **FX-13-BR-004:** Floating datetime dùng User timezone và preview. TZID không biết hoặc thời gian không thể resolve an toàn thì skip/report, không giả UTC. Parser/ICS serialization cần fixtures theo chuẩn.
- **FX-13-BR-005:** Export custom range chỉ lấy Event nằm hoàn toàn trong khoảng. Xuất business fields được hỗ trợ, trừ Reminder/history/audit/reasons/internal IDs/secrets.
- **FX-13-BR-006:** Task thay đổi/Trash cập nhật projection. Calendar ICS xuất Task projection là ngoại lệ đã duyệt, không mở module export Projects/Tasks.

## Quyền, API và tích hợp

- CreatePersonalEvent, UpdateScheduledEvent, CompleteEvent, CancelEvent; TaskEventProvider read-only.
- ImportIcs/ExportIcs kiểm tra owner, UID và source/status filters; no external calendar sync.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-13-AC-001:** ICS hỗn hợp chỉ import valid entries, report recurring/invalid/duplicate; không import VALARM.
- **FX-13-AC-002:** Scheduled quá End hiển thị bình thường; Canceled gạch ngang và API edit bị chặn.
- **FX-13-AC-003:** Event cắt ngang biên export bị loại; all-day qua DST không lệch ngày.
- **FX-13-AC-004:** Direct edit Task Event từ Calendar bị từ chối.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

- [06-decisions-and-traceability.md](../requirements/06-decisions-and-traceability.md): `DEC-CAL-001`, `DEC-CAL-002`, `DEC-CAL-003`, `DEC-CAL-004`, `DEC-CAL-005`, `DEC-CAL-006`, `DEC-CAL-007`, `DEC-CAL-008`, `DEC-CAL-009`, `DEC-CAL-010`, `DEC-CAL-011`, `DEC-CAL-012`
- [phase-02-productivity.md](../requirements/phases/phase-02-productivity.md): `P02-CAL-001`, `P02-CAL-002`, `P02-CAL-003`, `P02-CAL-004`, `P02-CAL-005`, `P02-CAL-006`, `P02-CAL-010`, `P02-CAL-011`, `P02-CAL-012`, `P02-EVT-001`, `P02-EVT-002`, `P02-EVT-003`, `P02-EVT-004`, `P02-EVT-005`, `P02-EVT-010`, `P02-EVT-011`, `P02-EVT-012`, `P02-EVT-013`, `P02-EVT-014`, `P02-EVT-015`, `P02-ICS-001`, `P02-ICS-002`, `P02-ICS-003`, `P02-ICS-004`, `P02-ICS-005`, `P02-ICS-006`, `P02-ICS-007`, `P02-ICS-008`, `P02-ICS-009`, `P02-ICS-020`, `P02-ICS-021`, `P02-ICS-022`, `P02-ICS-023`, `P02-ICS-024`, `P02-ICS-025`, `P02-ICS-026`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
