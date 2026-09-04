# Phase 1 — Core Platform and Application Shell

**Phase ID:** `NX-PH-01`  
**Version:** `1.1-draft`  
**Outcome:** Nexora chạy local với Personal Space, Team Workspace, membership, developer-built Module Platform và các security/platform services đủ an toàn để nhận business modules và asynchronous collaboration.  
**Depends on:** Phase 0 exit criteria.

## 1. Scope

### P0

- Local setup, first-run bootstrap và responsive application shell.
- Authentication/session/logout/recovery strategy đã duyệt.
- User/Profile lifecycle cơ bản.
- Role/permission administration (`SuperAdmin`, `Admin`, `User`).
- Personal Space, Team Workspace, membership và Workspace roles.
- Workspace invitation/add/remove/leave và last Workspace Owner invariant.
- Module Registry/Manifest, System/Workspace/Personal enablement và contribution contracts.
- Space ownership/access-policy framework và cross-user/cross-workspace test harness.
- Async collaboration primitives: assignment/comment/mention/activity contracts, optimistic concurrency và notification intents.
- Audit Log baseline.
- Trash/soft-delete contract.
- File Storage contract + safe local implementation tối thiểu.
- Settings separation và secret-safe configuration.
- In-app Notification infrastructure tối thiểu.
- Sharing Engine domain/service contract; UI chỉ cần đủ để verify bằng một test resource hoặc resource Phase 1 được duyệt.
- Background job abstraction/history tối thiểu nếu recovery/notifications/cleanup cần.
- Health/logging/migrations/error handling và security baseline.

### P1

- Activity History cơ bản, Workspace/module preferences nâng cao.
- Admin audit filtering/export không nhạy cảm.
- Browser notification nếu provider/permission UX được duyệt.

### Out of scope

Business modules; public self-registration nếu chưa duyệt; social login; commercial billing; production hosting; full email/mobile push; impersonation; AI/LLM; live presence/cursors; realtime co-editing; User/Admin-created modules hoặc executable plugin upload.

## 2. Actors và primary journeys

### 2.1 First-run operator

1. Khởi động dependencies và Nexora từ clean local environment.
2. Hệ thống nhận biết chưa có SuperAdmin và chỉ mở protected setup flow.
3. Operator tạo SuperAdmin đầu tiên bằng credential hợp lệ.
4. Setup token/flow bị vô hiệu hóa vĩnh viễn hoặc theo reset procedure đã duyệt.
5. SuperAdmin đăng nhập và thấy administration/dashboard shell.

### 2.2 SuperAdmin quản lý user/admin

1. Tạo hoặc kích hoạt User theo onboarding decision.
2. Chuyển User thành Admin.
3. Gán permission action cụ thể; mặc định không có global data scope.
4. Thu hồi permission/disable account và thấy audit outcome.

### 2.3 User self-service

User đăng nhập, chuyển rõ giữa Personal Space và các Workspace được phép, xem/sửa profile/settings, quản lý sessions/recovery, xem notifications và logout. User không thấy route/action ngoài effective module/Space permission.

### 2.4 Workspace Owner/Admin

Tạo Workspace, invite/add Member, gán Workspace role, enable module system cho phép, quản lý settings/resource access, suspend/remove Member và xem audit trong authority. Không được làm mất Workspace Owner cuối cùng hoặc nâng thành System Admin.

### 2.5 Workspace Member/Guest

Member truy cập module/resource theo Workspace enablement và action permission. Guest chỉ thấy explicit allowed resource/module projection. Member removal phải chặn access nhưng không xóa Workspace-owned data họ tạo.

## 3. Functional requirements — setup và shell

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-SHL-001` | P0 | Clean local setup có documented deterministic commands/config. | Reviewer mới khởi động được frontend, backend, SQL, Redis/storage từ clean state theo docs. |
| `P01-SHL-002` | P0 | First-run bootstrap không dùng default credential hard-coded. | Khi chưa bootstrap, normal login/business API bị chặn; sau bootstrap, setup credential không replay được. |
| `P01-SHL-003` | P0 | App shell có navigation theo module/permission và responsive desktop/mobile layout. | Route unauthorized bị server deny; mobile có thể truy cập profile/logout/admin được phép. |
| `P01-SHL-004` | P0 | Module chưa bật/không có quyền không xuất hiện như action khả dụng. | Direct URL vẫn trả denied/not found semantics an toàn. |
| `P01-SHL-005` | P0 | Global error boundary/response có safe error ID và recovery action. | Backend/frontend exception không lộ stack/secret; correlation tra được trong log. |
| `P01-SHL-006` | P1 | Application hiển thị version/build/environment indicator phù hợp cho local operation. | Không lộ secret; operator đối chiếu đúng build khi report lỗi. |
| `P01-SHL-007` | P0 | App shell có Space switcher hiển thị current Personal/Workspace context và effective module navigation. | Create/action không dùng hidden stale Workspace; direct route rechecks membership/module enablement. |

## 4. Functional requirements — authentication/session

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-AUT-001` | P0 | User đăng nhập bằng identifier được duyệt + password; lỗi credential không enumerate account. | Valid flow tạo session; invalid flows có generic error và audit/security event. |
| `P01-AUT-002` | P0 | Password policy có minimum rules, breach/common-password decision và clear validation UX. | Server thực thi cùng/stricter than client; password không log. |
| `P01-AUT-003` | P0 | Logout hiện tại và revoke-all-sessions behavior được hỗ trợ theo decision. | Revoked session không gọi API được; result idempotent. |
| `P01-AUT-004` | P0 | Session timeout/absolute lifetime/rotation được cấu hình và test. | Expired session về login mà không mất unsaved data âm thầm nếu UI có thể cảnh báo. |
| `P01-AUT-005` | P0 | Password change yêu cầu current/recent auth và revoke sessions theo policy. | Attacker có stale session không tiếp tục sau revocation bound. |
| `P01-AUT-006` | P0 | Recovery flow chỉ bật khi có trusted delivery/operator process. | Nếu chưa có email/provider, UI không giả vờ gửi reset; có documented admin-safe recovery. |
| `P01-AUT-007` | P0 | Disabled user không login hoặc tiếp tục privileged/background action. | Disable invalidates sessions and scheduled actor context trong bound đã định. |
| `P01-AUT-008` | P1 | User xem và revoke active sessions/device metadata an toàn. | Metadata không chứa token/IP quá mức policy; revoke target hoạt động. |

## 5. Functional requirements — users và profiles

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-USR-001` | P0 | SuperAdmin tạo, xem, cập nhật trạng thái User; unique identifier được enforce case/normalization rõ. | Duplicate/race condition không tạo hai identity mâu thuẫn. |
| `P01-USR-002` | P0 | User chỉ sửa profile/preference cho phép của chính mình. | Không sửa role/status/owner/security field qua tampered payload. |
| `P01-USR-003` | P0 | Disable, reactivate, soft-delete User có state transition và confirmation rõ. | State bất hợp lệ bị từ chối; Personal data, memberships, assignments, shares và jobs được reconciliation. |
| `P01-USR-004` | P0 | Không permanent-delete User khi còn unresolved Personal data, membership/last-owner hoặc dependency. | UI/API trả dependency summary và remediation; Workspace-owned data không bị xóa theo creator. |
| `P01-USR-005` | P1 | User export/delete-account request chỉ bật sau khi data portability/retention policy được duyệt. | Không có partial silent deletion. |

### User state đề xuất

`Pending` (nếu invite) → `Active` ↔ `Disabled` → `Deleted/Retention` → `Purged`; transition cụ thể phụ thuộc onboarding decision. `SuperAdmin` là role, không phải user state.

## 6. Functional requirements — roles và permissions

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-RBAC-001` | P0 | Hỗ trợ ba role confirmed và default deny cho Admin permissions. | Admin mới không có module administration ngoài grants explicit. |
| `P01-RBAC-002` | P0 | SuperAdmin grant/revoke theo `module.action`; change atomic và audit old/new. | Concurrent/request replay không tạo phantom grant. |
| `P01-RBAC-003` | P0 | Admin không tự gán role/quyền vượt authority baseline. | API escalation tests fail. |
| `P01-RBAC-004` | P0 | Last active SuperAdmin invariant áp dụng concurrency-safe. | Hai request downgrade đồng thời không đưa count về 0. |
| `P01-RBAC-005` | P0 | Permission revocation có bounded propagation tới request/cache/job. | Test chứng minh quyền cũ không dùng được sau bound. |
| `P01-RBAC-006` | P0 | UI permission matrix hiển thị module/action/scope và effective access trước confirm. | SuperAdmin hiểu thay đổi; hidden dependency/conflict được báo. |
| `P01-RBAC-007` | P0 | System roles và Workspace roles là hai lớp độc lập. | WorkspaceOwner không có system authority; Admin không tự có Workspace access. |
| `P01-RBAC-008` | P0 | Last Workspace Owner invariant concurrency-safe. | Hai request remove/downgrade/leave đồng thời không đưa owner count về 0. |

## 7. Functional requirements — platform services

### 7.1 Ownership/access policy

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-OWN-001` | P0 | Có reusable Space ownership/access evaluator cho API/query/search/file/comment/job. | Reference resource pass cross-user/cross-workspace matrix. |
| `P01-OWN-002` | P0 | Owning Personal/Workspace Space lấy từ authoritative server context; transfer dùng dedicated operation. | Client spoofing UserId/WorkspaceId thất bại. |
| `P01-OWN-003` | P0 | Global/access-all admin path được phân biệt và audit. | Normal user query không thể bật flag/scope bằng request parameter. |

### 7.2 Workspaces và asynchronous collaboration foundation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-WSP-001` | P0 | User có Personal Space và có thể thuộc nhiều Team Workspace với membership/role độc lập. | Create/switch/list isolation tests pass. |
| `P01-WSP-002` | P0 | Workspace lifecycle, invite/accept/suspend/remove/leave và last Owner rules đáp ứng `SPC/WSP/MEM` requirements. | Token, concurrency, revocation và data-retention tests pass. |
| `P01-WSP-003` | P0 | Assignment/comment/mention/follow contracts dùng shared resource access; UI đầy đủ phát hành cùng module Phase 2/3. | Reference resource rejects inaccessible Member/parent/module. |
| `P01-WSP-004` | P0 | Collaborative writes có version/concurrency token và standardized conflict response. | Two-member stale edit không silent overwrite. |
| `P01-WSP-005` | P0 | Workspace collaboration và external read-only Sharing Engine là hai access paths tách biệt. | Share-link viewer không edit/comment/assign. |

### 7.3 Module Platform

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-MOD-001` | P0 | Registry load/validate developer-built manifests và supported Space. | Duplicate/incompatible/invalid module fail-safe. |
| `P01-MOD-002` | P0 | System, Workspace, Personal và Role/User enablement gates được enforce cho route/API/search/widget/job. | Disable/revoke bound and direct-route tests pass. |
| `P01-MOD-003` | P0 | Dependencies, migrations, contributions và module health được quản lý theo Module Platform spec. | Enable/upgrade failure không mark module ready. |
| `P01-MOD-004` | P0 | Disable giữ data nhưng dừng new writes/jobs/events và ẩn contributions. | Re-enable compatible version khôi phục data; no orphan side effects. |
| `P01-MOD-005` | P0 | Không có User/Admin module authoring hoặc executable upload surface. | Security/API review confirms developer/build-only module delivery. |

### 7.4 Sharing Engine

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-SHR-001` | P0 | Domain model/service hỗ trợ item/collection, ba access mode, expiration, revoke và read-only grant. | Contract tests đạt `SHR-001..010` áp dụng được. |
| `P01-SHR-002` | P0 | Chỉ resource type đã đăng ký mới được share. | Unknown/non-shareable type bị từ chối không lộ data. |
| `P01-SHR-003` | P0 | Share access re-evaluate source state/authorization và không phụ thuộc cache stale. | Trash/revoke/expiry test chặn ngay theo bound. |

### 7.5 Audit/Trash/Files/Notifications/Settings

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-PLT-001` | P0 | Audit writer/query đáp ứng `AUD-001..004`; event schema versioned. | Auth/admin/reference-resource events kiểm chứng được và đã redact. |
| `P01-PLT-002` | P0 | Trash contract có soft-delete, restore, purge authorization và aggregate policy hook. | Reference resource lifecycle + race tests pass. |
| `P01-PLT-003` | P0 | Local file service đáp ứng upload/download metadata và Space ownership controls. | Cross-user/workspace/path/type/size/checksum tests pass. |
| `P01-PLT-004` | P0 | In-app notification store/read-state/Workspace-module preferences và idempotent intent contract tồn tại. | Mention/assignment duplicate, cross-workspace access, removed/disabled User tests pass. |
| `P01-PLT-005` | P0 | System/Workspace/module/user settings được tách; secret setting masked và secure. | Workspace role cannot edit system; GET never returns secret. |
| `P01-PLT-006` | P0 | Job run model có status/history/error redaction nếu Phase 1 dùng background work. | Restart/retry/failure visible; no duplicate effects. |

## 8. Data concepts tối thiểu

Không phải physical schema; architecture phải map mà không đổi semantics:

- `User`, `Profile`, `Role`, `PermissionDefinition`, `AdminGrant`, `Session/RefreshCredential` (theo auth decision).
- `PersonalSpace`, `Workspace`, `WorkspaceMembership`, `WorkspaceRole/Grant`, `Invitation`.
- `ModuleDefinition`, `ModuleVersion`, `ModuleEnablement`, `ModuleAssignment`, `ModuleMigrationState`.
- `ResourceType`, `ResourceSpaceReference`, `Assignment`, `Comment`, `Mention`, `Follow`, `ConcurrencyVersion`.
- `Share`, `ShareCredentialHash`, `ShareAccessEvent`.
- `AuditEvent`, `ActivityEvent` (nếu P1).
- `FileObject`, `FileReference`.
- `Notification`, `NotificationPreference`, `DeliveryAttempt`.
- `SettingDefinition`, `SystemSetting`, `UserPreference`, `IntegrationSecretReference`.
- `JobDefinition`/internal schedule (nếu cần), `JobRun`.

Mọi entity có version/concurrency/lifecycle metadata phù hợp. Secret value không nằm trong audit/search/activity.

## 9. Required audit events

Bootstrap completed; login/session/recovery; User lifecycle; System/Workspace role và permission; Workspace/member/invitation/module lifecycle; last SuperAdmin/Workspace Owner violation; privileged data access; share lifecycle; comment moderation; file rejection; permanent delete/restore; setting/job/backup/restore events.

## 10. Error và edge cases bắt buộc

- Duplicate identifier và concurrent user creation.
- Permission revoked giữa lúc UI đang mở.
- User/member/Workspace/module disabled trong khi job đang queued.
- Member thuộc nhiều Workspace và Space switch bị stale.
- Concurrent last-Owner removal/downgrade/leave.
- Module upgrade/disable với dependent module, queued job và stale search/widget.
- Two-member edit conflict; mention Guest/inaccessible Member.
- Resource trash/revoke đúng lúc share được truy cập.
- Redis unavailable/cache flushed.
- SQL/file storage unavailable hoặc upload partial.
- Browser back/retry sau destructive form submit.
- Clock/timezone mismatch trong expiration.
- Setup bị ngắt giữa chừng hoặc bootstrap request lặp.

## 11. Testing và verification

- Unit tests cho role/state/expiration/password policy/domain invariants.
- Integration tests với SQL, Redis và storage implementation thật của local profile.
- E2E: bootstrap → login → create user → promote Admin → grant/revoke → disable → audit.
- Cross-user/cross-workspace/API authorization matrix; membership/module enablement và share mode/expiry/revoke matrix.
- E2E: create Workspace → invite/join → enable module → assign permissions → revoke/remove → verify data retained/access blocked.
- Module contract: discover/install/enable/disable/re-enable/upgrade/failure/dependency/migration tests.
- Security tests: credential enumeration, CSRF/XSS, session revoke, secret/log redaction, file traversal/type/size.
- Responsive + keyboard/accessibility test cho login, shell, profile, admin matrix.
- Migration from empty database và upgrade from previous Phase 1 build.

## 12. Exit criteria

- Tất cả P0 requirements và cross-cutting/security/NFR P0 liên quan pass.
- Không còn Critical/High security finding chưa xử lý.
- Cross-user/cross-workspace isolation 100% pass ở UI/API/search/file/comment/job/reference resource.
- Last SuperAdmin và last Workspace Owner invariants, membership/module revocation propagation được chứng minh.
- Clean local setup/bootstrap/migration/restart/Redis-loss flows pass.
- Audit redaction và privileged event coverage pass.
- Phase 2 có thể đăng ký Personal/Workspace resource, assignment/comment/mention/search/widget/job qua Module Contract mà không sửa identity/ownership model.
