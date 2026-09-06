# Vault

FX-28 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Password, SecureNote, APIKey, Token, SSHKey, DatabaseCredential, RecoveryCodes, LicenseSecret và GenericSecret.

[Bitwarden](https://bitwarden.com/help/managing-items/): Vault tổ chức thành các item, có thao tác xem và quản lý item.

**Áp dụng cho Nexora:** Bitwarden tham chiếu items/masking/reveal. Không autofill extension hoặc Admin tự xem secret.

**Màn hình:** `/vault`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Owner unlock/xác thực tăng cường theo Q-02/Q-04 → list masked.
2. Create type/name/protected fields → encrypted version.
3. Detail explicit reveal/copy; edit/restore version; Trash/purge; lock.

## Dữ liệu và validation

- Name1–200/type immutable; schema protected fields theo type; tags/URL classified metadata.
- Payload encrypted version+keyVersion; không plaintext trong generic registry.
- Credential reference từ module khác là VaultRef, không value.

## Hành vi và lifecycle

- **FX-28-BR-001:** Support/Emergency không reveal/copy/export; Q-04 chốt safe metadata hay cấm support toàn Vault.
- **FX-28-BR-002:** Search/dashboard/share/webhook không payload; no public/restricted secret links theo proposed deny Q-04.
- **FX-28-BR-003:** Reveal tự che30s delegated; clipboard explicit best-effort clear30s nếu chưa đổi, không hứa xóa clipboard hệ điều hành.
- **FX-28-BR-004:** No secrets URLs/logs/analytics/persistent browser cache; protected response no-store.
- **FX-28-BR-005:** History encrypted tới purge; crypto envelope/key rotation/backup/recovery phải ADR+Q-04 trước implement.
- **FX-28-BR-006:** Password generator local CSPRNG length 16 default,12–128 allowed, categories explicit; chỉ persist khi Save.

## Quyền, API và tích hợp

- CreateSecret/UpdateSecret/RevealSecret/CopySecret/RestoreSecretVersion permissions tách riêng.
- ResolveVaultRef theo owner-authorized service purpose; audit action/ID không secret; no bulk plaintext endpoint.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-28-AC-001:** Canary secret không trong metadata/search/log/error.
- **FX-28-AC-002:** Admin support vẫn deny reveal.
- **FX-28-AC-003:** Tampered ciphertext fail closed; key thiếu không giả restore success.
- **FX-28-AC-004:** Asset export không dereference secret.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Item field dictionary

Protected payload schema phải versioned; bảng này xác định UX fields, không quyết định key architecture Q-04.

| Type | Payload bắt buộc | Optional protected fields |
|---|---|---|
| Password | Password | Username, login URLs, notes |
| SecureNote | Note body | Classification label |
| APIKey | Key value | Provider, scope, expiry, endpoint |
| Token | Token value | Issuer, type, scope, expiry |
| SSHKey | Private key hoặc public key được owner chọn loại lưu | Public key, passphrase, fingerprint, host label |
| DatabaseCredential | Engine, host, database/account label, username, password hoặc connection secret | Port, TLS options, connection notes |
| RecoveryCodes | Ít nhất một code | Service label, used flag theo từng code |
| LicenseSecret | Activation/license key | Product, vendor, expiry |
| GenericSecret | Ít nhất một named secret field | Additional key/value fields, protected notes |

Name/type/tags/favorite/folder đều owner metadata nhưng có thể nhạy cảm; safe search classification phải Q-04 review. Folder/tag/favorite hỗ trợ theo P04-VLT-007, không phải permission boundary. Duplicate names allowed; one optional Folder, multiple Tags (delegated). Folder deletion không purge secret: yêu cầu chuyển items về Unfiled hoặc replacement sau preview. Reveal/copy từng protected field đều audited; RecoveryCode used flag là explicit owner mutation, không suy đã dùng khi Copy.

## Traceability và phần còn mở

- [phase-04-finance-and-vault.md](../requirements/phases/phase-04-finance-and-vault.md): `P04-CRY-001`, `P04-CRY-002`, `P04-CRY-003`, `P04-CRY-004`, `P04-CRY-005`, `P04-VAC-001`, `P04-VAC-002`, `P04-VAC-003`, `P04-VAC-004`, `P04-VAC-005`, `P04-VLT-001`, `P04-VLT-002`, `P04-VLT-003`, `P04-VLT-004`, `P04-VLT-005`, `P04-VLT-006`, `P04-VLT-007`, `P04-VLT-008`

Quyết định lớn cần PO: [Q-02](90-open-decisions.md#q-02), [Q-04](90-open-decisions.md#q-04). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.

