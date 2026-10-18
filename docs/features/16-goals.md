# Goals và Targets

> **Current decision amendment — 2026-09-10:** FX16-S01/S02/S03/S04 numeric
> Goal subset is implemented locally on PR #4 under `DEC-20260909-014`.
> Goal/target/progress data is SQL-backed and owner-isolated; task-linked,
> Trash, reminders and provider behavior remain separate.
> **2026-10-18 local continuation:** Boolean target create/read/progress is code-present
> under the [bounded contract](../implementation/remediation-plan-20261016/goals-boolean-contract-20261018.md).
> Compile evidence only; runtime acceptance and independent review remain open.
> Local progress-history query/UI is also code-present under its
> [bounded contract](../implementation/remediation-plan-20261016/goals-progress-history-contract-20261018.md).
> Goal metadata and lifecycle state history are additionally code-present under their 2026-10-18 bounded contracts. Numeric/Boolean target-definition versions are additionally code-present.
> Additional local code now implements [Numeric/Boolean target management](../implementation/remediation-plan-20261016/goals-target-management-contract-20261018.md)
> and [Archive/Unarchive](../implementation/remediation-plan-20261016/goals-archive-contract-20261018.md).
> Removed targets leave the active denominator but retain progress evidence;
> Unarchive restores the recorded prior status. Runtime acceptance/review Pending.
> See [numeric slice evidence](../implementation/goals-numeric-slice.md).
> Goal metadata versions: [bounded contract](../implementation/remediation-plan-20261016/goals-metadata-history-contract-20261018.md);
> immutable private snapshots, no pre-install backfill or restore-version command.
> Numeric/Boolean target definitions also retain original title/initial/target
> snapshots on create/update/remove, including removed targets, under the
> [target history contract](../implementation/remediation-plan-20261016/goals-target-history-contract-20261018.md).

FX-16 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; numeric Goal subset là
`SLICE_IMPLEMENTED (local)` theo DEC-20260909-014. Các capability còn lại
vẫn là contract-gated và không được suy ra từ subset này.

## Phạm vi và tham chiếu

Mục tiêu cá nhân với targets số, yes/no và linkedTasks.

[ClickUp Goals](https://clickup.com/features/goals): Goals có targets dạng số, đúng/sai hoặc task.

**Áp dụng cho Nexora:** ClickUp tham chiếu targettypes; không team ownership, automatic financial actions hoặc đổi Task state từ Goal.

**Màn hình:** `/goals` là route local đã nối cho numeric subset; các route
detail/progress riêng trong UX inventory vẫn là logical surfaces của cùng flow.

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
