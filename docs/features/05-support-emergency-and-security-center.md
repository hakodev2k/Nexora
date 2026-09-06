# Support, Emergency Access và Security Center

FX-05 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Consent đọc một module, truy cập khẩn cấp có audit và thông báo.

[Microsoft Customer Lockbox](https://learn.microsoft.com/en-us/purview/customer-lockbox-requests): Yêu cầu và phê duyệt quyền hỗ trợ có giới hạn.

[Bitwarden](https://bitwarden.com/help/managing-items/): Vault tổ chức thành các item, có thao tác xem và quản lý item.

**Áp dụng cho Nexora:** Lockbox tham chiếu approval; User Nexora cấp cho bất kỳ Admin đủ permission, không chỉ một người cụ thể.

**Màn hình:** `/settings/security/support, /admin/support, /admin/emergency`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. User chọn đúng một module → thời hạn24h(default)/custom/until revoke → consent rõ phạm vi read-only.
2. Admin có support permission chọn User+module → access qua grant, thấy timer/reason; User revoke bất kỳ lúc.
3. SuperAdmin emergency chọn module/reason → audit durable trước access → notify User ngay qua all 3.

## Dữ liệu và validation

- Grant owner/module/start/expiry/revokedAt/consentVersion; lựa chọn thời hạn được lưu explicit.
- Emergency reason nonblank20–1000 delegated, scope/module, actor/startedAt/endsAt; proposed session30min cần security review.

## Hành vi và lifecycle

- **FX-05-BR-001:** Support không edit/delete/export/impersonate/reveal/copy secrets; hết hạn hoặc revoke chặn request kế tiếp và pending downloads.
- **FX-05-BR-002:** Emergency cũng read-only, không bypass Vault decryption; safe metadata scope Q-04.
- **FX-05-BR-003:** Nếu không ghi audit bền vững thì không mở emergency access; notification queue failure phải báo operational error và retry, không nói User đã nhận.
- **FX-05-BR-004:** Security Center list sessions, grants/emergency access safe details, revoke controls; audit không User xóa.
- **FX-05-BR-005:** Thông báo ngay nghĩa enqueue đồng thời cả 3, không đảm bảo browser/SMTP thực nhận đúng thời điểm.

## Quyền, API và tích hợp

- GrantSupport/RevokeSupport/BeginEmergency/AuthorizeSupportRead; explicit grant context không ambient role.
- Audit event reason/access IDs; Notification outbox cùng transaction bắt đầu access.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-05-AC-001:** Any Admin đủ quyền dùng grant, Admin thiếu action không dùng được.
- **FX-05-AC-002:** Revoke lúc tab support mở làm refresh và file access thất bại.
- **FX-05-AC-003:** Audit storage lỗi chặn emergency; Vault plaintext vẫn denied.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [08-workspaces-and-collaboration.md](../requirements/08-workspaces-and-collaboration.md): `PDS-EMG-001`, `PDS-EMG-002`, `PDS-EMG-003`, `PDS-EMG-004`, `PDS-EMG-005`, `PDS-EMG-006`, `PDS-SUP-001`, `PDS-SUP-002`, `PDS-SUP-003`, `PDS-SUP-004`, `PDS-SUP-005`, `PDS-SUP-006`, `PDS-SUP-007`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PDS-003`, `P01-PDS-004`

Quyết định lớn cần PO: [Q-02](90-open-decisions.md#q-02), [Q-04](90-open-decisions.md#q-04). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
