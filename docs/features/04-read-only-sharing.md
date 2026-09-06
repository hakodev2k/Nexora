# Read-only Sharing Engine

FX-04 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Link live read-only, expiry/revoke và module-level sharing policy.

[Google Drive](https://support.google.com/drive/answer/2494822?hl=en): Chia sẻ qua link và giới hạn người được truy cập.

**Áp dụng cho Nexora:** Ba chế độ PublicLink, AuthenticatedLink và RestrictedUsers; không edit collaboration.

**Màn hình:** `resource Share dialog, /s/:token, /settings/shares`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Owner chọn resource được module cho share → preview fields → mode → expiry → Create.
2. Viewer mở link; login khi cần; RestrictedUsers kiểm tra account allowlist.
3. Owner list links, đổi expiry/allowlist hoặc revoke; viewer nhận unavailable an toàn khi invalid.

## Dữ liệu và validation

- Token cryptographic random, only hash persisted; mode/allowedUserIds/expiresAt nullable/revokedAt/resourceRef.
- Delegated default expiry7days, owner chọn custom hoặc no-expiry; allowlist bằng verified account identity, không email guess.

## Hành vi và lifecycle

- **FX-04-BR-001:** SuperAdmin per-module cho tạo share; current module/resource/expiry/grant kiểm tra mọi request kể cả file.
- **FX-04-BR-002:** Project share toàn bộ Task details còn hiện hữu live, không hide task. Documents link chỉ Published tạo mới; Draft suspend; Archived giữ link đã active.
- **FX-04-BR-003:** Share không tự bao gồm child Documents, history/reasons/audit/reminder configs/private notes hoặc các resource liên kết.
- **FX-04-BR-004:** Calendar Event không share; Vault payload không share. Sensitive Finance/Asset projections phải Q-03/Q-04.
- **FX-04-BR-005:** Trash deny ngay; link reuse sau restore và disable-policy effects còn Q-03, proposal suspended không tự hồi sinh.

## Quyền, API và tích hợp

- CreateShare/ResolveShare/UpdateShare/RevokeShare/ProjectionProvider; signed file URLs ngắn hạn recheck.
- Share token không log/referrer; public page noindex, no public discovery; download chỉ khi projection cho phép.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-04-AC-001:** Expired/revoked link và wrong account đều không lộ title tồn tại.
- **FX-04-AC-002:** Project Task update phản ánh live nhưng history/reason không lộ.
- **FX-04-AC-003:** Draft transition làm share file bị deny cùng page.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `SHR-001`, `SHR-002`, `SHR-003`, `SHR-004`, `SHR-005`, `SHR-006`, `SHR-007`, `SHR-008`, `SHR-009`, `SHR-010`, `SHR-011`, `SHR-012`, `SHR-013`, `SHR-014`
- [08-workspaces-and-collaboration.md](../requirements/08-workspaces-and-collaboration.md): `PDS-SHR-001`, `PDS-SHR-002`, `PDS-SHR-003`, `PDS-SHR-004`, `PDS-SHR-005`, `PDS-SHR-006`, `PDS-SHR-007`, `PDS-SHR-008`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PDS-002`, `P01-SHR-001`, `P01-SHR-002`, `P01-SHR-003`

Quyết định lớn cần PO: [Q-03](90-open-decisions.md#q-03), [Q-04](90-open-decisions.md#q-04). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
