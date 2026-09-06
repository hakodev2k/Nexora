# Import, Export và Backup/Restore

FX-10 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Framework chuyển dữ liệu theo format từng module và phục hồi hệ thống.

[Google Calendar](https://support.google.com/calendar/answer/37118?hl=en): Import file lịch là thao tác riêng, không đồng nghĩa đồng bộ liên tục.

[GitLab](https://docs.gitlab.com/administration/backup_restore/): Backup và restore là quy trình vận hành có phạm vi, prerequisites và kiểm tra.

**Áp dụng cho Nexora:** Calendar tham chiếu file import; GitLab tham chiếu vận hành restore. Không đồng nhất export User với full system backup.

**Màn hình:** `module import/export dialogs, /admin/backups`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Chọn file → parse/validate preview → mapping/duplicate report → confirm import → result summary.
2. Export chọn scope/type/status/date → preview count → download authorized expiry.
3. Operator backup encrypted manifest → verify → restore rehearsal isolated → compare integrity.

## Dữ liệu và validation

- ImportJob owner/module/schemaVersion/fileRef/status/counts/row errors/idempotency; ExportJob parameters/snapshotAt/fileExpiry.
- Backup manifest DB/files/config key references/version/checksums/capturedAt; keys không plaintext trong manifest.

## Hành vi và lifecycle

- **FX-10-BR-001:** Projects/Tasks import/export deferred; Calendar ICS included; Documents formats Q-11; Vault portability Q-04.
- **FX-10-BR-002:** Preview không mutation; import row errors sanitized, stableIDs/idempotency và per-module duplicate policy.
- **FX-10-BR-003:** Export permission không có trong support/emergency; recheck quyền lúcdownload; Userexport không audit hoặc otherUserdata.
- **FX-10-BR-004:** Vault ciphertext backup thiếu key recovery không được nhận là restorable; retention/RPO/RTO Q-08.
- **FX-10-BR-005:** Restore không chạy vào live database chỉ để test; plan có validation/rollback và approval operational khi đến phase đó.
- **FX-10-BR-006:** Không viết appcode hoặc infrastructure trong giai đoạn docs hiện tại.

## Quyền, API và tích hợp

- ImportProvider Validate/Preview/Commit/Report; ExportProvider Project/Serialize; BackupProvider inventory/verify/restore.
- File scan/size/format limits perprovider; no generic import bypass immutable fields.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-10-AC-001:** Malformed mixed ICS theo feature13 chỉ skip invalid/recurring/duplicate và báo cáo.
- **FX-10-AC-002:** Export Project route không xuất dù có generic provider.
- **FX-10-AC-003:** Restore rehearsal phải xác minh DB-file-key consistency, không chỉ job Success.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `EXP-001`, `EXP-002`, `EXP-003`, `IMP-001`, `IMP-002`
- [04-non-functional-requirements.md](../requirements/04-non-functional-requirements.md): `BKP-001`, `BKP-002`, `BKP-003`, `BKP-004`
- [phase-08-hardening-and-deployment.md](../requirements/phases/phase-08-hardening-and-deployment.md): `P08-BKP-001`, `P08-BKP-002`, `P08-BKP-003`, `P08-BKP-004`, `P08-BKP-005`, `P08-BKP-006`, `P08-BKP-007`

Quyết định lớn cần PO: [Q-01](90-open-decisions.md#q-01), [Q-04](90-open-decisions.md#q-04), [Q-08](90-open-decisions.md#q-08), [Q-11](90-open-decisions.md#q-11). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
