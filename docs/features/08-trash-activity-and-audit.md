# Trash, Activity và Audit

FX-08 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Shared lifecycle framework, lịch sử nghiệp vụ và security audit.

[Google Drive](https://support.google.com/drive/answer/1716222?hl=en): Trash là bước riêng trước khi xóa vĩnh viễn.

[Microsoft Customer Lockbox](https://learn.microsoft.com/en-us/purview/customer-lockbox-requests): Yêu cầu và phê duyệt quyền hỗ trợ có giới hạn.

**Áp dụng cho Nexora:** Drive chỉ tham chiếu Trash/restore; Nexora không tự áp dụng retention30days vì User yêu cầu giữ tới tự purge.

**Màn hình:** `/trash, resource history, /admin/audit`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Delete preview scope → Trash aggregate; owner xem Trash/search/filter theo module.
2. Restore kiểm tra parent/current policy → phục hồi đúng aggregate deletion batch.
3. Purge confirmation irreversible trong app → xóa content/version theo dependency; audit tombstone tối thiểu còn theo Q-08.

## Dữ liệu và validation

- DeletionBatchId/root/members/priorState/deletedAt/deletedBy, dependency refs; không suy cohort từ timestamps.
- Activity actor/action/resource/version/from/to/reason safe; Audit immutable event/action/outcome/correlation.

## Hành vi và lifecycle

- **FX-08-BR-001:** Projects/Tasks/Documents giữ Trash vô thời hạn tới owner purge; Calendar personal delete=Cancel không Trash.
- **FX-08-BR-002:** Restore batch chỉ members bị xóa trong batch; child đã Trash riêng trước đó không bị hồi sinh.
- **FX-08-BR-003:** Task không restore nếu Project Trash/terminal; child Document cần parent ngoài Trash/Archived; parentpurged không reparent.
- **FX-08-BR-004:** Activity không là Audit; mọi Task/Project changes history; Documents contentversion Save; Calendar no versionhistory.
- **FX-08-BR-005:** Admin audit permission không đọc payload riêng; audit appendonly/businessUser không sửa/xóa.
- **FX-08-BR-006:** Retention audit/backup và account deletion Q-01/Q-08; không đặt auto-purge User data ngầm.

## Quyền, API và tích hợp

- TrashProvider PlanDelete/CommitDelete/PlanRestore/Restore/Purge; atomic aggregate+concurrency.
- AuditWriter append-only; ActivityProvider safe fields; direct endpoints check current lifecycle.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-08-AC-001:** Restore parent không hồi sinh child xóa riêng trước batch.
- **FX-08-AC-002:** Taskrestore vào closed Project fail không partialmutation.
- **FX-08-AC-003:** Delete inbox hoặc resource không xóa emergency audit.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `ACT-001`, `ACT-002`, `ACT-003`, `AUD-001`, `AUD-002`, `AUD-003`, `AUD-004`, `AUD-005`, `AUD-006`, `TRS-001`, `TRS-002`, `TRS-003`, `TRS-004`, `TRS-005`, `TRS-006`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PLT-001`, `P01-PLT-002`

Quyết định lớn cần PO: [Q-01](90-open-decisions.md#q-01), [Q-08](90-open-decisions.md#q-08). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
