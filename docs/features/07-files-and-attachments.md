# Files, Uploads và Attachments

FX-07 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

File Service dùng chung Documents cover, attachments, invoices/resumes/certificates.

[Google Drive](https://support.google.com/drive/answer/2494822?hl=en): Chia sẻ qua link và giới hạn người được truy cập.

[Google Drive](https://support.google.com/drive/answer/1716222?hl=en): Trash là bước riêng trước khi xóa vĩnh viễn.

**Áp dụng cho Nexora:** Tham chiếu Drive upload/access; không xây Drive replacement hoặc public bucket.

**Màn hình:** `/files, resource attachments, upload dialog`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Chọn file → validate → upload pending/quarantine → scan → Available hoặc Rejected.
2. Owner preview/download, rename display name; attach thông qua resource provider.
3. Delete unreferenced file vào Trash; file còn resource/version reference cần xử lý dependency trước purge.

## Dữ liệu và validation

- FileId owner/displayName/storageKey/mediaType/detectedType/bytes/checksum/state; resource/version references.
- Delegated general formats PDF/PNG/JPEG/WebP/TXT/MD/CSV/DOCX/XLSX ≤25MiB; cover JPG/PNG/WebP ≤5MiB và 25MP.
- Crop stores original ref+rectangle, không ghi đè binary/version cũ; quota toàn account Q-08.

## Hành vi và lifecycle

- **FX-07-BR-001:** Extension/MIME/content validation cùng nhau; quarantine chưa download/share/index; scan failure fail closed.
- **FX-07-BR-002:** Private storage, download authorization mỗi lần; short-lived signed URL không public forever.
- **FX-07-BR-003:** Replace file tạo binary mới; resource/history còn tham chiếu giữ binary cũ tới purge policy.
- **FX-07-BR-004:** Cover no SVG/script/externalURL; reject decompression/pixel bombs; safe filename không path traversal.
- **FX-07-BR-005:** Dangling staged upload cleanup24h delegated, không xóa file đã committed hoặc historyref.
- **FX-07-BR-006:** File sharing qua owning resource/policy, không bypass Documents Draft hoặc Trash.

## Quyền, API và tích hợp

- InitiateUpload/CompleteUpload/ScanResult/AttachFile/GetAuthorizedDownload/Trash/Purge; references atomic.
- File metadata search only authorized; no arbitrary content OCR/extraction ngoài provider được phép.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-07-AC-001:** Quarantine file link đoán được vẫn denied.
- **FX-07-AC-002:** Restore Document version còn hiển thị cover binary đúng trước crop.
- **FX-07-AC-003:** Delete source không purge file đang được source hợp lệ khác dùng.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `FIL-001`, `FIL-002`, `FIL-003`, `FIL-004`, `FIL-005`, `FIL-006`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PLT-003`
- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-FIL-001`, `P03-FIL-002`, `P03-FIL-003`, `P03-FIL-004`

Quyết định lớn cần PO: [Q-08](90-open-decisions.md#q-08). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
