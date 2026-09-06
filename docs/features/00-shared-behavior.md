# Hợp đồng hành vi chung

Ngày: 2026-09-06. Áp dụng cho mọi FX-01…FX-40. Các default nhỏ là **Resolved (delegated)** theo DEC-GOV-001; quyết định lớn ở [Decision queue](90-open-decisions.md). Đây là requirement/design, không phải implementation hoặc approval để code.

## Thứ tự áp dụng

1. Quyết định trực tiếp mới nhất của Product Owner và các requirement Approved.
2. Chi tiết Resolved (delegated) trong feature specs, trong phạm vi ủy quyền.
3. Proposal có Q-ID: chưa được implement như một quyết định đã duyệt.
4. Hành vi sản phẩm tham chiếu chỉ giải thích mẫu UX. Không nhập toàn bộ tính năng, pricing, quyền hoặc retention của sản phẩm đó vào Nexora.

Requirement nguồn vẫn giữ mã và acceptance criteria. Một link trace chỉ chứng minh có nơi xử lý, không chứng minh mọi scenario đã được test hoặc mọi business decision đã đóng. Nhãn P1 cũ không tự loại module Committed khỏi Release 1; những capability ghi “nếu được duyệt/if added” phải qua quyết định phạm vi, không tự trở thành cam kết.

## Access và ownership

| Context | Điều kiện | Được làm |
|---|---|---|
| Owner Self | Verified, Active, module installed/system-enabled/user-enabled, action permission, đúng owner | Chỉ action hợp lệ với lifecycle hiện tại |
| Share viewer | Link hợp lệ, mode/auth/allowlist, expiry/revoke, module/resource policy | Approved read-only projection, không owner API |
| Support | Admin có permission và User consent đúng một module, grant chưa hết hạn/revoke | Read-only trong scope, không export/reveal/copy secret/mutation |
| Emergency | SuperAdmin, reason, durable audit, module scope, immediate all-channel notification | Read-only theo emergency policy; không ambient Vault decryption |
| Job/Integration | Explicit owner authority và action grant | Recheck current access trước mỗi side effect |
| Platform operator | Operational permission | Health/metrics/redacted logs; không payload cá nhân |

**FX-COM-001:** OwnerUserId/internal PersonalSpaceId server-derived. Không Workspace, team ownership, assignment/comments/mentions hoặc collaboration được nhập lại từ sản phẩm tham chiếu.

**FX-COM-002:** Query, count, autocomplete, download, export, search index và caches đều owner-scoped. Unauthorized resource dùng phản hồi không tiết lộ sự tồn tại; validation cross-user reference không trả title của người khác. Client-hidden button không phải permission control.

**FX-COM-003:** Disable giữ dữ liệu, chặn new requests/contributions/queued side effects; running jobs dừng tại checkpoint an toàn. Re-enable không hồi sinh session/grant/share đã revoke. Ảnh hưởng của việc tắt sharing policy lên link cũ cần Q-03.

## Interaction mặc định

| Vấn đề | Default delegated |
|---|---|
| List | Page size25, tùy chọn25/50/100; stable tie-break ID, total khi có thể tính an toàn |
| Search | Trim query, case-insensitive theo collation được ADR chốt; literal text, không execute query syntax tùy ý |
| Date filter | Timed interval overlap: item.start < filter.end và item.end > filter.start; date-only chuyển User-local day thành half-open range |
| Ngoại lệ date filter | Calendar ICS export **fully contained**, không dùng overlap |
| Create/update | Explicit Save, field errors ngay cạnh field, giữ input khi request fail |
| Unsaved data | Leave/cancel warning; session expired cho login lại và retry an toàn, không lưu secret vào browser persistent storage |
| Loading | Skeleton/spinner có accessible label, không hiển thị stale private payload khi owner/context đổi |
| Empty | Phân biệt chưa có dữ liệu, filter không khớp, unavailable module và lỗi fetch |
| Error | Safe message + retry khi retriable + correlation ID; không SQL/stack/secret |
| Destructive action | Preview đúng aggregate/count, confirm rõ “Trash” hay “xóa vĩnh viễn”; cancel không mutation |
| Reordering | Keyboard alternative cho drag; chỉ thay rank trong cùng scope; drop lỗi trả vị trí server |
| External links | Validated http/https, new tab với opener isolation; không tự navigate QR/code/preview URLs |

**FX-COM-004:** Default view không nhớ đè những lựa chọn đã chốt: Projects Grid, Project Tasks Kanban, Documents Grid, Calendar Day. List/filter riêng đã Approved thắng common defaults.

**FX-COM-005:** Required strings trim và reject blank; Unicode lengths do server kiểm tra. Không auto-truncate mất dữ liệu. Duplicate policy theo feature; Documents Title được trùng mọi nơi.

## Command/API contract

Routes UI trong từng feature chưa phải API đã deploy. Solution design phải map từng command/query đã liệt kê vào API versioned theo quy ước sau, không tự bỏ command vì chưa có code:

| Loại | Contract đề xuất |
|---|---|
| List/query | GET /api/v1/{module}/{resources}?cursor=&limit=&filter= ; result items/nextCursor/asOf |
| Detail | GET /api/v1/{module}/{resources}/{id}; trả revision/ETag và allowedActions |
| Create | POST collection; Idempotency-Key; trả 201+resource ID+revision |
| Update | PATCH resource; If-Match/currentRevision; server validate immutable fields |
| Business transition | POST resource/{id}/actions/{command}; payload target/reason/expectedRevision/idempotencyKey |
| Aggregate preview | POST .../actions/preview-{command}; affected IDs/count/revision token; commit revalidate |
| Long operation | POST import/export/run; trả 202+operationId; status endpoint owner-scoped |
| Conflict | 409 business lifecycle/dependency; 412 stale revision; safe current revision, no silent overwrite |
| Other errors | 401 unauthenticated; 403 policy/action denied; 404 inaccessible resource; 422 field validation; 429 throttled + retry hint |

**FX-COM-006:** Mọi mutation retry cùng idempotency key và cùng payload trả cùng result, không thêm version/event/ledger effect. Key reuse khác payload reject409. TTL/idempotency storage strategy là ADR, không dùng cache volatile như nguồn bảo đảm duy nhất.

**FX-COM-007:** Save trong tab cũ không overwrite. UI giữ bản nháp hiện tại, cho reload/compare và User tự Save trên revision mới. Không CRDT/OT/realtime merge.

**FX-COM-008:** Aggregate state và outbox event được commit nhất quán. Derived Calendar/Search/Dashboard/Notifications có event ID, schemaVersion, owner, resource revision; consumer dedupe, reject stale revision. API Save thành công không được báo derived projection đã cập nhật nếu chưa cập nhật.

## Time, lifecycle và data boundaries

**FX-COM-009:** Timed values lưu instant, hiển thị User IANA zone. All-day lưu local dates với exclusive end. User đổi timezone không dời timed instant hoặc all-day dates. Schedule local time có preview DST; ambiguous/invalid input không silently guess.

**FX-COM-010:** Archive, terminal, Trash và purge là các trạng thái khác nhau. Chỉ feature cho phép mới có archive/unarchive/reopen. Project terminal và personal Calendar Event terminal không reopen; không dùng generic restore-version để bypass.

**FX-COM-011:** Aggregate delete có deletion batch/provenance. Khôi phục parent không hồi sinh child đã Trash riêng trước batch. Child bị xóa cùng parent phục hồi đúng state tại delete; điều kiện parent hiện tại kiểm tra tại commit. Purged child không tái tạo.

**FX-COM-012:** Retention User data không tự lấy 30 ngày từ Google Drive. Projects/Tasks/Documents/Notification giữ đến khi owner chủ động xóa theo scope đã chốt. Audit/backup/generated files/job logs có policy riêng Q-01/Q-08.

**FX-COM-013:** Cross-module relation chỉ ResourceRef cùng owner. Xóa link không xóa nguồn. Search/widget/share/export không tự dereference Vault payload; Finance owns ledger; Calendar Task Event do Task điều khiển.

**FX-COM-014:** Notification là một logical intent, tạo ba channel attempts đồng thời và độc lập. Không mute/quiet hours/channel preferences. “Gửi” là enqueue/attempt, không bảo đảm User đã nhận email/push. Denied browser permission hoặc provider failure phải có trạng thái trung thực.

## Data egress và nguồn ngoài

**FX-COM-015:** External content untrusted; rich HTML/Markdown/feeds/snippets render sanitized hoặc escaped. No arbitrary executable code. URL fetch dùng shared egress policy: reject credentials/unsafe scheme/private/link-local/metadata targets, validate DNS/redirects, giới hạn ports/bytes/time. Network scope và provider costs Q-06/Q-07/Q-08.

**FX-COM-016:** Chỉ user-facing supported business fields đi vào share/export. No owner IDs nội bộ, token, audit reason, hidden history, secret, support data. Public page noindex không được coi là security boundary.

**FX-COM-017:** Numeric limits trong specs là delegated defaults phục vụ design; team cần capacity/security verification. Không quảng cáo quota unlimited. Financial calculation/key recovery/egress và irreversible data-policy proposal phải PO/security decision trước code.

## Capability boundaries

| Nhóm | Share | History/Trash | Calendar | Import/export |
|---|---|---|---|---|
| Projects/Tasks | Read-only, Project bao gồm Task details | Mọi thay đổi; Trash owner purge | Chỉ Task projection | Module import/export deferred; Calendar ICS Task projection là exception |
| Documents | Chỉ tạo khi Published; Archive giữ link hợp lệ | Save version; archive/tree/Trash rules | Không | Formats Q-11 |
| Personal Calendar Event | Không | Không version; Delete=Cancel terminal | Chính nó | ICS confirmed |
| Vault | Không public; authenticated/export chờ Q-04 | Encrypted versions/Trash | Không | Q-04 |
| Finance/Assets/Career nhạy cảm | Safe projection theo Q-03, Resume qua Sharing | Domain history/dependency rules | Chỉ nguồn được duyệt; Interview Q-12 | Per-provider, không universal export |
| Pure tools/operational views | Không tạo share mặc định | Không lưu sensitive input mặc định | Không | Explicit output download, operational export không private data |
| Other resources | Chỉ khi provider có safe projection được đặc tả và SuperAdmin cho phép | Theo feature, không ép lifecycle chung | Không tự thêm nguồn | Không có generic endpoint bypass schema |

## Common acceptance gates

- **FX-COM-AC-001:** Hai User có resource cùng title; mọi list/detail/count/search/file/share/support route giữ isolation, kể cả stale caches.
- **FX-COM-AC-002:** Hai tab cạnh tranh Save/Archive/Delete không gây silent overwrite hoặc aggregate bị xử lý dở.
- **FX-COM-AC-003:** Retry command/event/job không double version, ledger entry, Calendar projection hay notification.
- **FX-COM-AC-004:** Parent terminal/Trash hoặc permission revoke xảy ra giữa preview và commit thì commit revalidate và fail an toàn.
- **FX-COM-AC-005:** Canary secret không xuất trong logs/search/share/export/notification/error; operational Admin không đọc payload User.
- **FX-COM-AC-006:** Keyboard/mobile và loading/empty/error/permission-unavailable flows đầy đủ; drag có thao tác tương đương.

## Definition of Ready

Feature được tách story implement khi: source scope/AC được trace; Q ảnh hưởng story đã đóng; state/field/action matrix và API schema được solution design; permissions/concurrency/lifecycle tests xác định; dependencies/limits/threat model được review; PO **approve implementation riêng**. Không cần hỏi lại chi tiết UX delegated nếu không đổi hành vi đã Approved.

