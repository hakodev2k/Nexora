# Cross-cutting Platform Requirements

**Document ID:** `NX-PRD-002`  
**Status:** Working draft  
**Applies to:** Mọi phase và mọi module, trừ khi có ngoại lệ được ghi rõ.

## 1. Identity, ownership và privacy boundary

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `OWN-001` | P0 | Mỗi business record phù hợp MUST có một owner ổn định (`OwnerUserId` ở logical model). | Tạo record không có owner bị từ chối; owner được xác định từ authenticated context, không tin giá trị do client tự gửi. |
| `OWN-002` | P0 | Mọi read/list/count/search/export MUST lọc theo access scope. | Test User A không thấy record private của User B qua UI, API, search, export, direct ID hoặc aggregate count. |
| `OWN-003` | P0 | Resource mới MUST là private mặc định. | Không tạo share/public visibility ngầm; anonymous request nhận kết quả không tiết lộ sự tồn tại của resource. |
| `OWN-004` | P0 | Chuyển ownership chỉ được thực hiện bởi policy được duyệt và phải audit. | Không có generic update cho `OwnerUserId`; event ghi actor, old owner, new owner, reason. |
| `OWN-005` | P0 | Disabled/deleted user không được làm mất khả năng xử lý dữ liệu sở hữu. | Quy trình disable, retention, transfer/export/delete được xác định trước permanent deletion. |
| `OWN-006` | P1 | Resource type MUST đăng ký capability: shareable, searchable, trashable, exportable, attachable. | Capability registry hoặc equivalent được kiểm thử; UI không hiển thị action không hỗ trợ. |

## 2. Sharing Engine

### 2.1 Share model

Share hỗ trợ `Item` và `Collection`; mặc định read-only. Mỗi share tham chiếu resource bằng type + stable identifier và không sao chép business data.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `SHR-001` | P0 | Chỉ owner hoặc actor có `share.create` trên resource mới được tạo share. | Unauthorized caller bị từ chối; action thành công được audit. |
| `SHR-002` | P0 | Hỗ trợ đúng ba access mode: `AnyoneWithLink`, `RequiredLogin`, `PasswordProtected`. | Mỗi mode có automated test cho anonymous/authenticated/wrong password/valid password. |
| `SHR-003` | P0 | Share token MUST đủ ngẫu nhiên, không tuần tự và không xuất hiện trong application logs. | Token enumeration test thất bại; log scan không chứa full token. |
| `SHR-004` | P0 | Password share MUST được hash; không lưu plaintext hoặc reversible ciphertext. | Database inspection không khôi phục được password; comparison dùng password verification routine. |
| `SHR-005` | P0 | Share có `Active`, `Expired`, `Revoked`; owner có thể revoke ngay. | Link bị từ chối sau revoke/expiration, kể cả khi cached; transition được audit. |
| `SHR-006` | P0 | Expiration hỗ trợ không hết hạn hoặc timestamp cụ thể theo user timezone khi nhập. | Server lưu instant chuẩn; boundary ngay trước/sau expiry được test. |
| `SHR-007` | P0 | Xóa/trash resource nguồn MUST vô hiệu hóa truy cập qua share. | Link không bypass trash; restore không tự re-enable share đã revoke. |
| `SHR-008` | P0 | Required-login share chỉ cấp quyền cho session hợp lệ, không biến resource thành public. | Logout hoặc session expiry chặn access; không index bởi global/public search. |
| `SHR-009` | P1 | Owner xem được danh sách link, mode, created/expiry/status và last accessed (nếu retention cho phép). | Management view không hiển thị secret/token đầy đủ sau khi tạo. |
| `SHR-010` | P1 | Có rate limit và abuse control cho anonymous/password access. | Brute-force test kích hoạt throttle mà không khóa owner khỏi resource. |

### 2.2 Không thuộc sharing baseline

Edit-through-share, comment/collaboration, public discovery/indexing và link analytics nâng cao là `OUT` cho đến khi có requirement riêng.

## 3. Trash và lifecycle dữ liệu

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `TRS-001` | P0 | Delete business record mặc định chuyển sang trash/soft delete. | Record biến mất khỏi active views nhưng xuất hiện trong Trash của đúng owner. |
| `TRS-002` | P0 | Restore trả record và quan hệ hợp lệ về active state. | Restore giữ owner, dữ liệu, attachments hợp lệ; conflict được báo rõ và không tạo state nửa vời. |
| `TRS-003` | P0 | Permanent delete yêu cầu explicit confirmation và authorization riêng. | API không chấp nhận accidental/replayed generic delete; event được audit. |
| `TRS-004` | P0 | Audit records không đi theo generic Trash và không bị business user xóa. | User không thấy permanent-delete action cho audit; retention theo policy riêng. |
| `TRS-005` | P1 | Retention/purge job phải idempotent, có dry-run hoặc preview cho admin trước khi bật tự động. | Retry không xóa thêm ngoài tập đã chọn; job history nêu số record thành công/lỗi. |
| `TRS-006` | P0 | Cascading lifecycle phải được định nghĩa cho mỗi aggregate. | Ví dụ Project vào Trash không orphan Task; quyết định cascade/retain được test. |

## 4. Audit Log

### 4.1 Event bắt buộc

Authentication; user/role/permission administration; privileged data access; share create/access/revoke; Vault reveal/copy/delete/export; Finance export; permanent delete/restore; automation execution/configuration; backup/restore; integration credential change và security setting change.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `AUD-001` | P0 | Audit event MUST có event type, occurred-at, actor, action, target type/id, outcome và correlation ID khi có. | Schema validation từ chối event thiếu trường bắt buộc; failed action quan trọng vẫn tạo event. |
| `AUD-002` | P0 | Audit không lưu plaintext password, token, API key, Vault secret hoặc full share token. | Automated redaction tests và sample inspection không tìm thấy secret. |
| `AUD-003` | P0 | Business user không được sửa/xóa audit event. | Không có update/delete endpoint; database permission/implementation bảo vệ append-only logical behavior. |
| `AUD-004` | P0 | SuperAdmin/Admin chỉ xem audit trong scope được cấp. | Admin thiếu `audit.view` bị từ chối; truy cập audit cũng được ghi nhận nếu policy yêu cầu. |
| `AUD-005` | P1 | Audit có filter tối thiểu theo time range, actor, event type, target và outcome. | Filter kết hợp trả đúng tập, không bypass scope. |
| `AUD-006` | P1 | Retention và export policy phải được quyết định trước production. | Decision record có owner, duration, format, integrity và deletion rules. |

## 5. Notification Center

### 5.1 Baseline channel

`In-app` là P0 khi module đầu tiên cần notification. Browser, email, mobile push và webhook là `PROPOSED` và phải có provider/channel decision riêng.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `NTF-001` | P0 | Module nguồn phát notification intent có idempotency key; Notification Center quản lý delivery/read state. | Retry cùng key không tạo duplicate notification. |
| `NTF-002` | P0 | Notification chỉ được gửi tới user được phép nhận và không làm lộ dữ liệu resource. | Sau revoke/ownership change, action link kiểm tra authorization lại; preview không chứa secret. |
| `NTF-003` | P0 | User có thể xem, mark read/unread và mark all read trong scope của mình. | User A không thao tác notification User B bằng direct ID. |
| `NTF-004` | P0 | User có preference theo event category; security-critical event có thể là `ALWAYS`. | Tắt một category không ảnh hưởng category khác; policy `ALWAYS` được giải thích trong UI. |
| `NTF-005` | P1 | Delivery attempt có trạng thái `Pending`, `Sent`, `Failed`, `Suppressed` và reason an toàn. | Failure retry theo policy; UI/admin log không lộ credential provider. |
| `NTF-006` | P1 | Quiet hours/timezone behavior phải nhất quán với loại cảnh báo. | Non-critical event hoãn đúng; critical event theo override policy đã duyệt. |

## 6. File Storage và attachments

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `FIL-001` | P0 | Upload MUST kiểm tra authorization, size, allowed type và filename normalization. | File vượt limit/type cấm/path traversal bị từ chối. |
| `FIL-002` | P0 | Download MUST kiểm tra quyền resource tại thời điểm request; URL không vĩnh viễn bypass access. | Direct URL của User A không dùng được bởi User B. |
| `FIL-003` | P0 | Storage metadata có owner/uploader, size, content type, checksum, created-at và lifecycle state. | Corrupt/mismatched upload được phát hiện; metadata query giữ scope. |
| `FIL-004` | P0 | File được gọi bằng opaque ID; original filename không được dùng làm storage path tin cậy. | Duplicate/unicode/special filenames không overwrite nhau. |
| `FIL-005` | P1 | Orphan cleanup chỉ xóa object không còn reference sau grace period và phải idempotent. | Shared/referenced file không bị xóa; retry an toàn. |
| `FIL-006` | P1 | Malware scanning strategy và file quota là decision trước khi cho upload từ untrusted/public channel. | Decision được duyệt hoặc upload channel đó bị disable. |

## 7. Settings và preferences

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `SET-001` | P0 | Tách system settings, module settings và user preferences. | User không sửa system settings; module disabled không mất settings nếu policy không yêu cầu. |
| `SET-002` | P0 | Giá trị nhạy cảm không trả lại plaintext sau khi lưu; UI chỉ cho replace/revoke. | GET API trả masked/metadata; audit khi credential thay đổi. |
| `SET-003` | P0 | Setting có validation, default rõ ràng và behavior khi missing. | Fresh install chạy với documented defaults hoặc setup gate. |
| `SET-004` | P1 | Thay đổi setting ảnh hưởng scheduler/security phải atomic và auditable. | Invalid config không làm mất config tốt trước đó. |

## 8. Import, export và portability

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `IMP-001` | P1 | Mỗi format import phải có version/schema, validation, preview và báo lỗi theo record. | Invalid rows không âm thầm bị bỏ; user biết imported/skipped/failed counts. |
| `IMP-002` | P1 | Import retry không tạo duplicate khi cùng source/idempotency key. | Chạy lại file cho kết quả dự đoán được. |
| `EXP-001` | P0/P1 | Export chỉ gồm dữ liệu actor được phép truy cập và phải audit với Finance/Vault/admin data. | Cross-user leak tests pass; export nhạy cảm yêu cầu control riêng. |
| `EXP-002` | P1 | Export phải mô tả format/version/timezone/encoding và không ghi secret vào server log. | File round-trip hoặc schema validation pass. |
| `EXP-003` | P1 | Vault secret export mặc định disabled cho đến khi policy, re-authentication và encryption format được duyệt. | Không có plaintext bulk export ở baseline. |

## 9. Integrations và background processing

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `JOB-001` | P0 | Mỗi scheduled/background job có stable type, run ID, status, started/finished time và safe error summary. | Admin/user được phép xem history; secret redaction pass. |
| `JOB-002` | P0 | Job có idempotency/retry policy và không tạo duplicate business effects. | Fault-injection retry giữ đúng một logical outcome. |
| `JOB-003` | P0 | Job kiểm tra resource/owner state trước khi tạo side effect. | Disabled user, revoked integration hoặc deleted resource không tiếp tục phát action sai. |
| `JOB-004` | P1 | Provider rate limit/backoff/circuit behavior được xử lý và hiển thị degraded status. | 429/timeout không gây retry storm; manual refresh nhận feedback rõ. |
| `INT-001` | P0 | Integration credential dùng secret storage, least privilege và có revoke/replace flow. | Không lưu plaintext trong config/log/audit; connection test không lộ secret. |
| `INT-002` | P0 | Lỗi integration không làm hỏng core manual workflow. | Provider outage chỉ degrade module phụ thuộc; core CRUD vẫn hoạt động. |

## 10. Activity History

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `ACT-001` | P1 | Activity feed chỉ hiển thị activity actor được phép xem. | Sharing revoked làm feed link không còn mở resource. |
| `ACT-002` | P1 | Activity có thể aggregate user-friendly nhưng không thay thế Audit Log. | Xóa/retention activity không ảnh hưởng audit events. |
| `ACT-003` | P2 | Recent items/favorites có giới hạn và preference riêng theo user. | Không suy ra recent item của user khác; deleted item được dọn khỏi view. |

## 11. Contract checklist cho từng module

Trước khi module đạt Definition of Ready, phải trả lời:

- Resource nào có owner và aggregate boundary nào?
- CRUD/state transitions nào được phép, ai được phép?
- Có support share, file, tags, collections, trash, search, notification, import/export không?
- Event nào phải audit; event nào chỉ là activity?
- Background job/integration nào tồn tại; retry/idempotency/degraded behavior là gì?
- Data sensitivity/retention/backup/restore rules là gì?
- Mobile/empty/loading/error/offline-provider states được xử lý ra sao?
