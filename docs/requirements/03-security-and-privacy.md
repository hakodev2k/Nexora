# Security and Privacy Requirements

**Document ID:** `NX-SEC-001`  
**Version:** `1.2-draft`  
**Status:** Working draft  
**Principle:** Private by default, least privilege, defense in depth.

## 1. Data classification

| Class | Ví dụ | Controls tối thiểu |
|---|---|---|
| `Public` | Public GitHub metadata, public news URL | Integrity, source attribution, cache policy. |
| `Internal` | System configuration không chứa secret, job metadata | Authenticated access, role policy, audit khi quản trị. |
| `Private` | Tasks, Projects, notes, documents, calendar, files, assets, career data của User | Cross-user isolation, authorization, protected backups. |
| `Sensitive` | Finance, identity/profile details, invoices | Private controls + limited export/logging + stronger audit. |
| `Secret` | Passwords, API keys, tokens, recovery codes, DB/SSH credentials | Application-level encryption, reveal controls, no plaintext logs/search/index/cache. |

Module owner MUST khai báo classification cho từng field nhạy cảm. Nếu chưa phân loại, mặc định dùng mức bảo vệ cao hơn hợp lý.

## 2. Authentication

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `AUTH-001` | P0 | Login password MUST dùng password hashing có salt và work factor phù hợp; không reversible encryption. | Database không chứa plaintext/reversible password; verification và rehash-on-upgrade được test. |
| `AUTH-002` | P0 | Login trả lỗi không cho biết account có tồn tại hay không. | Unknown user/wrong password có response semantics tương đương. |
| `AUTH-003` | P0 | Brute force protection áp dụng theo account và nguồn request mà không tạo denial-of-service dễ dàng. | Repeated failure bị throttle; successful login/reset theo policy có thể giải phóng trạng thái. |
| `AUTH-004` | P0 | Session/token có expiration, secure revocation/logout và rotation phù hợp. | Logout/revoke làm credential cũ không dùng được theo documented bound. |
| `AUTH-005` | P0 | Cookie-based auth (nếu dùng) MUST cấu hình `HttpOnly`, `Secure` khi HTTPS và `SameSite` phù hợp; state-changing request chống CSRF. | Security header/cookie tests pass; cross-site forged action bị từ chối. |
| `AUTH-006` | P0 | Reset/recovery token là single-use, time-bound, đủ entropy và không lưu/log plaintext. | Replay/expired token thất bại; password change revoke sessions theo policy. |
| `AUTH-007` | P1 | MFA cho SuperAdmin/Admin là `PROPOSED` bắt buộc trước production exposure. | Decision `DEC-SEC-004` phải đóng trước Phase 8; nếu bật, recovery flow được test. |
| `AUTH-008` | P0 | Disabled account không tạo session mới; session hiện tại bị thu hồi trong giới hạn đã định. | Disable user chặn UI/API/background actions của user đó. |
| `AUTH-009` | P0 | Public registration chỉ kích hoạt account sau khi email được xác minh bằng token single-use, time-bound. | Unverified account không vào business modules; expired/replayed token bị từ chối; verified account dùng ngay không cần Admin approval. |
| `AUTH-010` | P0 | Registration, verification resend và recovery có anti-abuse/rate-limit và response chống account enumeration. | Automation không spam email vô hạn hoặc xác định email đã tồn tại qua response. |

## 3. Authorization và access evaluation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `AZ-001` | P0 | Authorization MUST được enforce server-side cho mọi endpoint/action. | Ẩn button không được coi là control; direct API tests bị từ chối. |
| `AZ-002` | P0 | Quyền Admin sử dụng `module.action`; default deny. | Permission thiếu/unknown/removed đều deny. |
| `AZ-003` | P0 | Quyền action, System/User module enablement, personal ownership và share/support grant được đánh giá độc lập. | Có `tasks.view` không tự cho Admin xem Task User khác hoặc module bị disable. |
| `AZ-004` | P0 | Role SuperAdmin không tự cấp quyền đọc business data của User khác; chỉ active support grant hoặc emergency break-glass flow được phép. | Normal route bị từ chối; emergency bắt buộc target User/module, reason, immutable audit và immediate User notification. |
| `AZ-005` | P0 | Client không thể tự chọn role, owner, permission, price result, audit actor hoặc security state. | Tampered payload bị ignore/reject; authoritative values lấy server-side. |
| `AZ-006` | P0 | Cache/search/read model không được trả dữ liệu vượt access policy hiện tại. | Permission/share revoke được phản ánh trong bounded time đã định; query-time check bảo vệ stale index. |
| `AZ-007` | P0 | Hệ thống ngăn xóa, disable hoặc hạ quyền SuperAdmin cuối cùng. | Concurrent attempts vẫn giữ ít nhất một active SuperAdmin. |
| `AZ-008` | P0 | Hệ thống ngăn cross-user access qua direct ID, child, file, search, dashboard, export, share metadata, cache và job. | Mandatory negative matrix đạt 100% cho ít nhất hai User và các role khác nhau. |
| `AZ-009` | P0 | Support grant/share grant không thay đổi owner và không mở action ngoài phạm vi đã cấp. | Read-only grant không gọi create/update/delete/export/reveal; hết hạn/revoke chặn request mới. |
| `AZ-010` | P0 | Admin permission và module enablement không tự cấp data access; Admin phải có action phù hợp và support grant hợp lệ cho User/module. | Admin có `tasks.view` nhưng không có active Task support grant bị từ chối. |

## 4. Encryption và key management outcomes

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `ENC-001` | P0 | Recoverable Secret MUST được mã hóa trước khi ghi persistent database/storage. | Database/backup snapshot không chứa plaintext secret. |
| `ENC-002` | P0 | Encryption MUST cung cấp confidentiality và integrity bằng approved authenticated encryption. | Ciphertext tamper làm decrypt thất bại an toàn; algorithm/version được lưu để migration. |
| `ENC-003` | P0 | Encryption key không được lưu cùng ciphertext như một plaintext application setting. | Deployment/setup inspection xác nhận separation và protected key access. |
| `ENC-004` | P0 | Key material không xuất hiện trong source, logs, audit, telemetry, exception hoặc client response. | Secret scanning/redaction tests pass. |
| `ENC-005` | P1 | Có key versioning và quy trình rotate/re-encrypt không làm mất dữ liệu. | Rotation rehearsal giải mã được dữ liệu cũ trong transition và hoàn thành migration có audit. |
| `ENC-006` | P0 | Backup chứa secret vẫn phải giữ mức bảo vệ tương đương dữ liệu nguồn. | Restore chỉ thành công khi có authorized key material; backup artifact không tự đủ để đọc secret. |
| `ENC-007` | P0 | Search/cache không lưu plaintext field thuộc class `Secret`. | Index/cache inspection và tests không tìm thấy secret. |

Lựa chọn thuật toán, KDF, key store và rotation interval là architecture decisions; không được hạ thấp các outcomes trên.

## 5. Vault-specific controls

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `VSEC-001` | P0 | List/search Vault chỉ trả masked metadata, không decrypt secret. | Network response list không chứa value; search không chạy trên plaintext secret. |
| `VSEC-002` | P0 | Reveal/Copy/Export là action riêng, được authorization và audit. | Quyền `vault.view` không đủ để reveal; event không chứa secret. |
| `VSEC-003` | P0 | Secret plaintext chỉ tồn tại ngắn nhất có thể ở server/client và không được persist trong browser storage. | Refresh/navigation clear reveal; local/session storage không chứa value. |
| `VSEC-004` | P0 | UI che secret mặc định và tự che lại sau interval/user action đã định. | Screenshot/manual test xác nhận default masked và auto-remask. |
| `VSEC-005` | P1 | Reveal/export/administrative Vault access SHOULD yêu cầu recent authentication hoặc step-up. | Expired recent-auth context buộc re-auth; decision đóng trước Phase 4 exit. |
| `VSEC-006` | P0 | Public/anonymous sharing Vault secret mặc định bị cấm. | Share engine không đăng ký Vault Secret là public-shareable; exception cần security decision riêng. |
| `VSEC-007` | P0 | Clipboard behavior phải được cảnh báo; clipboard auto-clear chỉ cam kết nếu platform support đáng tin cậy. | UI không tuyên bố đã clear nếu browser không xác nhận/không hỗ trợ. |

## 6. Privileged administration

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `ADMSEC-001` | P0 | Permission/role change phải audit old/new values và actor. | Failed/successful changes có event; không chứa unrelated sensitive fields. |
| `ADMSEC-002` | P0 | Admin không thể grant permission cao hơn authority được delegation policy cho phép. | Privilege escalation tests thất bại; chỉ SuperAdmin quản lý Admin authority baseline. |
| `ADMSEC-003` | P0 | Admin chỉ truy cập dữ liệu User qua explicit support path khi có active grant cho đúng User và đúng một module. | Ordinary Admin module route không nhận `UserId` tùy ý; UI/API hiển thị support context và expiry. |
| `ADMSEC-004` | P0 | SuperAdmin emergency/break-glass access khi chưa có User consent bắt buộc nhập lý do trước khi truy cập. | Blank/whitespace reason bị từ chối; mọi attempt success/failure được audit. |
| `ADMSEC-005` | P0 | Impersonation không thuộc baseline; nếu bổ sung phải có requirement/security review riêng. | Không có hidden “login as” behavior. |
| `ADMSEC-006` | P0 | Registration, restricted-share User lookup và Admin support lookup không được enumerate account ngoài authorized context. | Unknown/inaccessible identity có safe response; lookup result giữ scope. |
| `ADMSEC-007` | P0 | User cấp support grant read-only cho một module với một trong ba duration: `24Hours` (mặc định), `CustomExpiry`, `UntilRevoked`. | Grant không chứa nhiều module; expiry/revoke boundary được server enforce. |
| `ADMSEC-008` | P0 | Bất kỳ Admin nào có đủ permission của module và permission sử dụng support access có thể dùng grant; grant không gắn độc quyền với một Admin. | Admin thiếu một trong hai permission bị từ chối; mỗi lần dùng ghi đúng actor. |
| `ADMSEC-009` | P0 | Mỗi lần SuperAdmin bắt đầu emergency access phải thông báo ngay cho User đồng thời qua In-app, Email và Browser Push. | Ba delivery attempt tạo cùng security event, retry idempotent; reason không lộ dữ liệu nhạy cảm không cần thiết. |
| `ADMSEC-010` | P0 | Support và emergency access mặc định read-only; action nhạy cảm như export, reveal/copy Secret, purge hoặc impersonation không được suy ra từ quyền xem. | Server từ chối mutation/sensitive action nếu thiếu dedicated approved control. |

## 7. Web/API security

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `WEBSEC-001` | P0 | Validate input server-side; encode output theo context; rich text được sanitize theo allowlist. | XSS/HTML injection test corpus không thực thi script. |
| `WEBSEC-002` | P0 | SQL/database access phải parameterized/ORM-safe; không ghép untrusted input vào query. | Injection tests pass; code review không có unsafe path chưa justify. |
| `WEBSEC-003` | P0 | SSRF-sensitive tools/integrations phải chặn loopback, link-local, private network và unsafe schemes theo policy. | URL parser/HTTP Client/Webhook tests không truy cập target cấm. |
| `WEBSEC-004` | P0 | Upload/download bảo vệ path traversal, content sniffing và executable content risk. | Malicious filename/MIME test pass; safe download headers. |
| `WEBSEC-005` | P0 | API có request size/time/rate bounds phù hợp; expensive queries có pagination/limit. | Oversized/unbounded request bị từ chối có kiểm soát. |
| `WEBSEC-006` | P0 | Security headers và HTTPS policy phải được xác định trước network exposure. | Phase 8 scan không có missing critical headers; HTTP behavior đúng topology. |
| `WEBSEC-007` | P0 | Error response không lộ stack trace, connection string, key, query hoặc internal path cho user. | Production-mode fault tests trả safe error ID; server log có correlation nhưng đã redact. |
| `WEBSEC-008` | P0 | Dependency và source secret scanning nằm trong release gate. | Critical vulnerable dependency/committed secret chặn release trừ documented exception. |

## 8. Integration security

- OAuth/API credential nếu xuất hiện sau này phải least privilege, có expiration/revoke và encrypted storage.
- Webhook inbound phải authenticate/signature-verify nếu provider hỗ trợ, chống replay và có idempotency.
- Webhook outbound không được gửi Secret/Private field ngoài explicit mapping đã duyệt.
- External content (RSS, GitHub descriptions, Shopee content) luôn là untrusted input và phải sanitize/encode.
- n8n không được nhận master encryption key hoặc database credential của Nexora.

## 9. Logging, telemetry và incident readiness

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `LOGSEC-001` | P0 | Có centralized redaction rules cho password/token/key/secret/share token/cookie. | Unit/integration tests với marker secrets không xuất hiện trong log sink. |
| `LOGSEC-002` | P0 | Log phân biệt operational log và Audit Log; không dùng một loại thay thế loại kia. | Retention/access/configuration độc lập. |
| `LOGSEC-003` | P1 | Security event bất thường có detection/alert path trước production. | Failed-login spikes, permission changes, backup failure có observable signal. |
| `IR-001` | P1 | Có documented response cho suspected credential leak, key compromise và unauthorized access. | Tabletop exercise hoàn thành trước Phase 8 exit. |

## 10. Security release gates

Một phase không được nghiệm thu nếu:

- còn Critical/High vulnerability liên quan scope phase mà chưa có acceptance chính thức;
- ownership/authorization negative tests chưa pass;
- secret có thể xuất hiện plaintext trong database, backup, cache, search hoặc logs;
- migration/rollback làm mất security metadata;
- privileged action bắt buộc chưa audit;
- support/emergency access không enforce consent/scope/expiry/reason/immediate notification;
- security decisions chặn phase trong decision log vẫn `Open`.
