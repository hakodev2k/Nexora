# Personal Data, Sharing and Support Access

**Document ID:** `NX-PRD-008`  
**Version:** `1.2-draft`  
**Status:** Approved baseline; detailed module projections continue refinement  
**Compatibility note:** Filename được giữ để không làm hỏng link lịch sử. Toàn bộ Workspace/collaboration requirement của phiên bản 1.1 đã bị `DEC-PRD-024` supersede.

## 1. Confirmed product model

Nexora Release 1 là Public SaaS personal-only:

- Mỗi account đã xác minh đại diện cho một User cá nhân.
- Mỗi User tự nhập, sở hữu và quản lý dữ liệu của mình.
- Hệ thống có thể tạo một Personal Space/internal owner boundary, nhưng UI không cần buộc User hiểu khái niệm Space.
- Không có Team Workspace, membership, Workspace role, team-owned resource, assignment cho người khác, comments/mentions giữa members hoặc collaborative editing.
- Chia sẻ ra ngoài chỉ là read-only access; không biến viewer thành owner/collaborator.
- Admin support access và SuperAdmin emergency access là privileged read-only paths riêng, không phải sharing.

## 2. Out of scope Release 1

- Workspace create/invite/join/leave/member management.
- Workspace Owner/Admin/Member/Guest.
- Shared/team-owned Project, Task, Calendar, Document hoặc File.
- Assign Task cho User khác.
- Comment/reply/mention/follow giữa nhiều User.
- Edit-through-share, live cursor, presence, realtime co-editing, CRDT/OT.
- Chuyển ownership resource sang User khác.
- Hidden impersonation hoặc “login as User”.

## 3. Personal ownership boundary

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-OWN-001` | P0 | Mỗi resource thuộc đúng một User/Personal Space được server gán từ authenticated context. | Client gửi owner khác bị ignore/reject; database không có active resource thiếu/đa owner. |
| `PDS-OWN-002` | P0 | User chỉ CRUD dữ liệu của chính mình trừ read-only share grant hợp lệ. | User A không list/read/count/search/export/update/delete resource User B bằng ID, filter hoặc relation. |
| `PDS-OWN-003` | P0 | Child, file, tag, history, reminder, calendar projection, notification và background job không được rộng scope hơn resource nguồn. | Cross-user reference bị chặn ở write; stale cache/job rechecks owner/access. |
| `PDS-OWN-004` | P0 | Release 1 không có generic ownership transfer. | Update API không chấp nhận OwnerUserId; import luôn target current User. |
| `PDS-OWN-005` | P0 | Resource mới private mặc định và không có share cho tới khi owner chủ động tạo. | Anonymous/User khác nhận safe not-found/denied response không enumerate resource. |
| `PDS-OWN-006` | P0 | Admin/SuperAdmin personal data của chính họ vẫn dùng Self context như User. | System role không làm mọi normal module query trở thành global. |

## 4. Registration và Personal Space

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-REG-001` | P0 | Bất kỳ ai đáp ứng validation/anti-abuse policy đều có thể self-register Public SaaS. | Không cần invitation hoặc Admin approval. |
| `PDS-REG-002` | P0 | Account chỉ chuyển active sau khi email verification thành công. | Unverified account không dùng business modules; token expired/replayed bị từ chối. |
| `PDS-REG-003` | P0 | Sau verification, User được dùng ngay và internal Personal Space được tạo idempotently. | Retry callback không tạo hai Personal Space; login đầu tiên có owner context hợp lệ. |
| `PDS-REG-004` | P0 | Mọi module Release 1 mặc định enabled cho User mới. | Verified User nhìn thấy đúng module catalog; SuperAdmin policy mới chỉ áp dụng theo rule/version đã audit. |
| `PDS-REG-005` | P0 | SuperAdmin có thể enable/disable module theo từng User. | Disabled module bị chặn ở navigation, direct API, search, widgets, jobs và new notifications nhưng data không bị xóa. |

## 5. External share model

### 5.1 Access modes

| Mode | Ai có thể xem |
|---|---|
| `PublicLink` | Bất kỳ người nào có link, không cần đăng nhập. |
| `AuthenticatedLink` | Bất kỳ User Nexora đã đăng nhập và có link. |
| `RestrictedUsers` | Chỉ các account cụ thể được owner chọn, đã đăng nhập và có link. |

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-SHR-001` | P0 | Share grant chỉ cấp read-only projection của resource đã chọn. | Viewer không create/update/delete/comment/assign/export/reveal hoặc tạo share con. |
| `PDS-SHR-002` | P0 | Owner chọn đúng một access mode khi tạo link. | Anonymous/authenticated/listed matrix pass cho từng mode. |
| `PDS-SHR-003` | P0 | Share token opaque, đủ entropy, không tuần tự và không xuất hiện đầy đủ trong log/audit. | Enumeration/log-secret tests pass. |
| `PDS-SHR-004` | P0 | Owner có thể đặt expiration và revoke link. | Request ngay sau expiry/revoke bị chặn kể cả cache stale. |
| `PDS-SHR-005` | P0 | Link luôn đọc dữ liệu mới nhất của resource nguồn; không tạo snapshot/copy. | Owner update field được phép thì shared view phản ánh trên request sau. |
| `PDS-SHR-006` | P0 | Resource nguồn ở Trash hoặc đã permanent delete thì share không truy cập được. Behavior của link cũ sau restore còn Open. | Trash/purge chặn ngay cả khi cache stale; implementation không tự chọn restore policy trước decision. |
| `PDS-SHR-007` | P0 | Share không xuất hiện trong public/global search và không mở rộng quyền sang resource liên quan ngoài approved projection. | Search/index/count/relation tests không leak. |
| `PDS-SHR-008` | P0 | Owner quản lý danh sách link theo resource với mode, status, created time, expiry và revoke action. | Secret token chỉ hiển thị lúc tạo theo policy; management list không trả full token. |

### 5.2 Restricted Users

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-RST-001` | P0 | Restricted share chứa allowlist account IDs, không dựa vào display name. | Rename profile không mất access; spoofed email/display name không cấp access. |
| `PDS-RST-002` | P0 | Account lookup/add/remove chống enumeration và được owner-authorized. | User ngoài result policy không bị lộ; removed account mất access ngay trong propagation bound. |
| `PDS-RST-003` | P0 | Disable/delete listed account làm session/share access không còn hợp lệ. | Stale session bị chặn theo session-revocation bound. |

## 6. Module and resource sharing policy

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-POL-001` | P0 | SuperAdmin quyết định theo từng module/resource type có cho User tạo share hay không. | System-disabled sharing ẩn UI và chặn API; share hiện có xử lý theo explicit policy. |
| `PDS-POL-002` | P0 | Module manifest đăng ký resource projection thay vì serialize toàn entity. | History, audit, reason, reminder, secret và internal metadata không lộ mặc định. |
| `PDS-POL-003` | P0 | Admin có permission quản trị module không tự được tạo/revoke share của User. | Chỉ owner hoặc explicit security operation đã duyệt thay đổi share. |
| `PDS-POL-004` | P0 | Calendar Event cá nhân không shareable trong Release 1. | Calendar không hiển thị share action; generic Sharing API từ chối Event resource type. |

## 7. Task and Project sharing

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-TSH-001` | P0 | Task share hiển thị current Task detail theo approved fields nhưng không history, backward-transition reason, reminder hoặc audit. | Projection whitelist pass; viewer không mở Project/Task khác ngoài link scope. |
| `PDS-PSH-001` | P0 | Project share hiển thị Project và toàn bộ Tasks thuộc Project. | Không có option ẩn một Task; task count/detail khớp active current aggregate. |
| `PDS-PSH-002` | P0 | Viewer Project được thấy chi tiết Task: Title, Description, Acceptance Criteria, Priority, Tags, Start, End, Status và Overdue. | Reminder/history/reason/audit/internal IDs không lộ. |
| `PDS-PSH-003` | P0 | Project link là live composition; Task mới được thêm hoặc cập nhật xuất hiện tự động, Task vào Trash không còn hiện. | Request sau mutation phản ánh state mới mà không tạo link khác. |
| `PDS-PSH-004` | P0 | Project terminal vẫn xem được qua active link; read-only semantics không đổi. | Viewer không mutation; owner revoke/expiry/trash vẫn override access. |

Document/File sharing dùng cùng access modes nhưng field/preview/download projection phải được làm rõ tại Phase 3 trước implementation.

## 8. User-granted Admin support access

### 8.1 Grant model

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-SUP-001` | P0 | User chỉ cấp support access cho một module cụ thể trong mỗi grant. | Payload nhiều module hoặc wildcard bị từ chối. |
| `PDS-SUP-002` | P0 | Grant luôn read-only và không chuyển ownership. | Admin không update/delete/purge/export/reveal/copy bằng support context. |
| `PDS-SUP-003` | P0 | Duration options: `24Hours` (selected default), `CustomExpiry`, `UntilRevoked`. | UI mặc định 24 giờ; server enforce exact instant; custom expiry invalid bị từ chối. |
| `PDS-SUP-004` | P0 | User có thể revoke active grant trước expiry. | Request mới bị chặn trong propagation bound; revoke idempotent và audited. |
| `PDS-SUP-005` | P0 | Bất kỳ Admin đủ module view permission và `support.support_access` có thể dùng active grant. | Grant không gắn Admin cụ thể; từng access ghi actor Admin thực tế. |
| `PDS-SUP-006` | P0 | Grant không tự cấp access cho module bị disabled đối với User hoặc Admin. | Effective-access evaluator trả rõ gate bị chặn cho authorized diagnostics. |
| `PDS-SUP-007` | P0 | User xem được active/expired/revoked grants và access history an toàn của mình. | Không lộ internal security detail hoặc dữ liệu Admin ngoài cần thiết. |

### 8.2 Support session

- Admin chọn User và module có active grant.
- Server kiểm tra account state, Admin permission, User/Admin module enablement, grant state/expiry và requested read action.
- UI hiển thị rõ đang ở support context, User mục tiêu, module và expiry.
- Mọi detail/list/search trong support context giữ đúng một User + một module.
- Kết thúc session không revoke grant; expiry/revoke mới chấm dứt grant.
- Support không phải impersonation và không được tạo action dưới danh nghĩa User.

## 9. SuperAdmin emergency access

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `PDS-EMG-001` | P0 | Emergency access chỉ dành cho active SuperAdmin qua dedicated break-glass path. | Admin/User hoặc SuperAdmin trên normal route không kích hoạt được. |
| `PDS-EMG-002` | P0 | SuperAdmin bắt buộc nhập reason có ý nghĩa trước khi access. | Blank/whitespace/over-limit reason bị từ chối; reason được bảo vệ theo audit policy. |
| `PDS-EMG-003` | P0 | Emergency context read-only; không tự cấp export, reveal/copy Secret, purge hoặc impersonation. | Sensitive/mutation endpoint deny dù actor là SuperAdmin nếu thiếu dedicated separately-approved flow. |
| `PDS-EMG-004` | P0 | User được thông báo ngay khi emergency access bắt đầu. | In-app security notification intent được tạo cùng security event; kênh ngoài In-app theo Notification decision. |
| `PDS-EMG-005` | P0 | Start/use/end/failure của emergency access được audit bất biến với actor, target User, module, reason reference, time và outcome. | Audit không chứa private record body/Secret; query/report scope được bảo vệ. |
| `PDS-EMG-006` | P0 | Emergency session có thời hạn ngắn do security design quyết định và không được reuse sau expiry/revoke/demotion. | Token replay hoặc role change bị chặn. |

## 10. Access evaluation order

Một private resource request chỉ được allow nếu một nhánh hợp lệ và không có deny:

1. Account/session active.
2. Module installed, system-enabled và enabled cho actor/owner theo context.
3. Resource active và thuộc đúng owner.
4. Một trong:
   - actor chính là owner trong `Self` context;
   - valid share token + mode/auth/allowlist/expiry trong `SharedLink` context;
   - Admin action + active one-module grant trong `Support` context;
   - SuperAdmin + valid break-glass session trong `Emergency` context.
5. Requested action nằm trong context (share/support/emergency chỉ read).
6. Field/resource projection cho phép.
7. Request được audit/notification nếu policy yêu cầu.

## 11. Audit events

Bắt buộc audit:

- share create/access-denied/revoke/expire/invalidate;
- restricted-user add/remove;
- support grant/create/use/expire/revoke;
- Admin support session start/end/denied;
- emergency access start/use/end/denied;
- module/share policy thay đổi;
- User/module/account disabled khi có active share/support;
- attempted mutation/export/reveal qua read-only context.

Audit không chứa full share token, private content body, password, API key, Vault Secret hoặc browser-push credential.

## 12. Mandatory security test matrix

Mỗi shareable/supportable module phải test:

1. Owner, User khác, disabled/unverified User.
2. Anonymous với Public/Authenticated/Restricted link.
3. Authenticated non-listed/listed User.
4. Expired, revoked, malformed, guessed và cached token.
5. Resource active/Trash/permanently deleted; module enabled/disabled.
6. Admin thiếu module action, thiếu support action, sai User, sai module, expired/revoked grant.
7. SuperAdmin normal route, emergency không reason, valid emergency, demoted/revoked session.
8. List/detail/direct child/file/search/count/export/job/notification deep link.
9. Mutation, purge, export, reveal/copy attempts từ mọi read-only context.
10. Concurrent expiry/revoke/access và module/account disable races.

## 13. Open decisions

- `DEC-SHR-001`: Default/max expiration presets cho external share link.
- `DEC-SHR-002`: Existing share behavior khi SuperAdmin tắt sharing policy của module: revoke ngay hay chỉ cấm tạo mới.
- `DEC-SHR-003`: Share link cũ có hoạt động lại khi resource được restore từ Trash hay không.
- `DEC-SUP-001`: Support access có được nhìn Trash/history hay chỉ active current data.
- `DEC-SUP-002`: Vault có cho support metadata-only hay cấm hoàn toàn support context.
- `DEC-NTF-004`: Notification channels ngoài In-app cho emergency/support/security events.

Các mục Open không được tự suy diễn trong implementation.
