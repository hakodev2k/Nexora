# Goals và Targets

FX-16 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Mục tiêu cá nhân với targets số, yes/no và linkedTasks.

[ClickUp Goals](https://clickup.com/features/goals): Goals có targets dạng số, đúng/sai hoặc task.

**Áp dụng cho Nexora:** ClickUp tham chiếu targettypes; không team ownership, automatic financial actions hoặc đổi Task state từ Goal.

**Màn hình:** `/goals`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Tạo Goal/title → targets → Activate; xem progress và drill-down.
2. Sửa numeric/boolean target hoặc chọn linked Tasks; progress Task đọc trực tiếp từ nguồn.
3. Complete/Abandon thủ công; Archive/Unarchive; không auto-complete từ progress.

## Dữ liệu và validation

- Goal title1–200, description/deadline optional; Draft/Active/Completed/Abandoned/Archived.
- Target type Numeric/Boolean/Tasks bất biến. Numeric có initial/current/target và target > initial. Boolean có giá trị; Tasks có same-owner refs.
- Goal progress là trung bình trọng số bằng nhau của targets. Không target thì0%, hiển thị No targets.

## Hành vi và lifecycle

- **FX-16-BR-001:** Numeric progress = clamp((current−initial)/(target−initial),0,1); hiển thị một số thập phân, không làm tròn dữ liệu nguồn.
- **FX-16-BR-002:** Task target = Completed/selected Tasks. Skipped không Completed; unavailable/deleted vẫn trong mẫu số tới owner bỏ link, không tăng progress giả.
- **FX-16-BR-003:** Status do owner đổi. Reopen Goal về Active có history; quy tắc này không áp lên Project terminal.
- **FX-16-BR-004:** Archive chỉ-đọc và Unarchive về trạng thái trước; Trash giữ tới manual purge; xóa Goal không xóa Tasks.
- **FX-16-BR-005:** Không cộng currency khác nhau; Savings Goal chịu Finance semantics. Không alert mỗi lần phần trăm thay đổi.

## Quyền, API và tích hợp

- CreateGoal, UpdateTarget, LinkTasks, TransitionGoal; provider giải thích numerator/denominator.
- Goal không được chỉnh Task state qua read projection.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-16-AC-001:** Skipped Task không làm Goal đạt100%.
- **FX-16-AC-002:** Target = initial bị reject, không chia cho0.
- **FX-16-AC-003:** Xóa Task không tự coi target hoàn thành.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

Nguồn phạm vi: [module catalog](../requirements/01-scope-and-module-catalog.md). Feature này bổ sung chi tiết cho capability trong catalog, không bịa requirement ID phase cũ.

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
