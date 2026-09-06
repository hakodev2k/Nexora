# Time Tracking

FX-18 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Timer và manual entries cá nhân, summary theoProject/Task/category.

[Toggl Track](https://support.toggl.com/en-us/article/creating-a-time-entry-wg8nug/): Time entry tạo bằng timer hoặc nhập thủ công.

**Áp dụng cho Nexora:** Toggl tham chiếu timer/manual entry; không payroll/invoice hay tính công tự động.

**Màn hình:** `/time`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Start timer với description và Project/Task optional.
2. Stop tạo entry; chỉnh bằng form hoặc tạo manual entry.
3. Filter date/project/tag, tổng duration và drill-down. Resume entry tạo entry mới.

## Dữ liệu và validation

- Start/End UTC, End > Start, duration derived; description≤2.000 optional, category/tags optional.
- Project/Task refs cùng owner và nhất quán; một RunningTimer/User.

## Hành vi và lifecycle

- **FX-18-BR-001:** Start khi có timer đang chạy yêu cầu stop timer hiện tại trước; không hai timer đồng thời.
- **FX-18-BR-002:** Stop idempotent. Reload/background tab không reset timer vì dùng server instant.
- **FX-18-BR-003:** Manual overlap cảnh báo và cho confirm; reports ghi gross duration, không giả net duration.
- **FX-18-BR-004:** Project terminal hoặc Task Trash dừng timer gắn nguồn tại transition instant; giữ evidence và không tiếp tục Task bị khóa.
- **FX-18-BR-005:** Entry edit có history, Trash tới manual purge. Không tự ghi Finance. Pomodoro conversion explicit và dedupe.

## Quyền, API và tích hợp

- StartTimer, StopTimer, CreateManualEntry, EditEntry, ReportTime; source permission checks.
- Timer không tự đổi Task status.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-18-AC-001:** Stop retry chỉ một entry; hai tab Start vẫn một running timer.
- **FX-18-AC-002:** Resume không rewrite entry cũ.
- **FX-18-AC-003:** Qua DST tính elapsed seconds đúng, không trừ giờ hiển thị đơn thuần.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

Nguồn phạm vi: [module catalog](../requirements/01-scope-and-module-catalog.md). Feature này bổ sung chi tiết cho capability trong catalog, không bịa requirement ID phase cũ.

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
