# Phase 1 — Core Platform and Application Shell

**Phase ID:** `NX-PH-01`  
**Version:** `1.2-draft`  
**Outcome:** Nexora có Public SaaS account lifecycle, personal-data isolation, developer-built Module Platform và các security/platform services đủ an toàn để nhận toàn bộ business modules Release 1.  
**Depends on:** Phase 0 exit criteria.

## 1. Scope

### P0

- Local development setup, Public SaaS deployment baseline, first-run bootstrap và responsive application shell.
- Authentication/session/logout/recovery strategy đã duyệt.
- Public self-registration, email verification và activation without Admin approval.
- User/Profile lifecycle cơ bản.
- Role/permission administration (`SuperAdmin`, `Admin`, `User`).
- Personal Space/internal owner boundary; không có Team Workspace hoặc membership.
- Module Registry/Manifest, System/User enablement, Registration Default và Admin grant contracts.
- Personal ownership/access-policy framework và cross-user/share/support/emergency test harness.
- Optimistic concurrency cho same-User multi-tab/session edits và notification intents.
- Audit Log baseline.
- Trash/soft-delete contract.
- File Storage contract + safe local implementation tối thiểu.
- Settings separation và secret-safe configuration.
- In-app, Email và Browser Push infrastructure cho mọi notification; cả ba kênh luôn được phát đồng thời.
- Notification Center: open source, read/unread, mark-all-read, single delete và bulk delete.
- Sharing Engine domain/service contract; UI chỉ cần đủ để verify bằng một test resource hoặc resource Phase 1 được duyệt.
- Background job abstraction/history tối thiểu nếu recovery/notifications/cleanup cần.
- Health/logging/migrations/error handling và security baseline.

### P1

- Activity History cơ bản và module/User preferences nâng cao.
- Admin audit filtering/export không nhạy cảm.
- Notification filtering/search nâng cao nếu được duyệt sau.

### Out of scope

Business modules; social login; billing/paid plans; self-hosted distribution; mobile native push; notification channel preference/mute/quiet hours; impersonation; AI/LLM; Team Workspace/collaboration; live presence/realtime co-editing; User/Admin-created modules hoặc executable plugin upload.

## 2. Actors và primary journeys

### 2.1 First-run operator

1. Khởi động dependencies và Nexora từ clean deployment environment.
2. Hệ thống nhận biết chưa có SuperAdmin và chỉ mở protected setup flow.
3. Operator tạo SuperAdmin đầu tiên bằng credential hợp lệ.
4. Setup token/flow bị vô hiệu hóa vĩnh viễn hoặc theo reset procedure đã duyệt.
5. SuperAdmin đăng nhập và thấy administration/dashboard shell.

### 2.2 Public User registration

1. Visitor nhập registration data và email.
2. Hệ thống tạo account ở trạng thái chờ xác minh và gửi verification email an toàn.
3. User mở token single-use còn hạn.
4. Account active, Personal Space được tạo idempotently và toàn bộ default modules được enable.
5. User đăng nhập/dùng ngay, không chờ Admin approval.

### 2.3 SuperAdmin quản lý User/Admin

1. Xem/quản lý User lifecycle theo permission và privacy policy.
2. Chuyển User thành Admin.
3. Gán module/action cụ thể; mặc định không có private User data scope.
4. Thu hồi permission/disable account và thấy audit outcome.

### 2.4 User self-service

User đăng nhập, xem/sửa profile/settings/timezone, quản lý sessions/recovery, notifications và logout. User chỉ thấy module SuperAdmin đã enable và dữ liệu của chính mình hoặc read-only share hợp lệ.

### 2.5 Admin support

Admin chỉ mở dữ liệu User khi có module/action permission và active support grant cho đúng một module. Support UI hiển thị User/module/expiry, chỉ read-only và ghi audit từng access.

### 2.6 SuperAdmin emergency access

SuperAdmin dùng dedicated break-glass flow khi thực sự khẩn cấp, nhập reason trước access, chỉ read-only, ghi immutable audit và tạo immediate User notification. Normal SuperAdmin module route không browse private data toàn hệ thống.

## 3. Functional requirements — setup và shell

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-SHL-001` | P0 | Clean local-development setup có documented deterministic commands/config. | Reviewer mới khởi động được frontend, backend, SQL, Redis/storage từ clean state theo docs. |
| `P01-SHL-002` | P0 | First-run bootstrap không dùng default credential hard-coded. | Khi chưa bootstrap, normal login/business API bị chặn; sau bootstrap, setup credential không replay được. |
| `P01-SHL-003` | P0 | App shell có navigation theo module/permission và responsive desktop/mobile layout. | Route unauthorized bị server deny; mobile có thể truy cập profile/logout/admin được phép. |
| `P01-SHL-004` | P0 | Module chưa bật/không có quyền không xuất hiện như action khả dụng. | Direct URL vẫn trả denied/not found semantics an toàn. |
| `P01-SHL-005` | P0 | Global error boundary/response có safe error ID và recovery action. | Backend/frontend exception không lộ stack/secret; correlation tra được trong log. |
| `P01-SHL-006` | P1 | Application hiển thị version/build/environment indicator phù hợp cho operation. | Không lộ secret; operator đối chiếu đúng build khi report lỗi. |
| `P01-SHL-007` | P0 | App shell tạo navigation từ effective System/User module enablement. | Module bị disable biến mất khỏi navigation; direct route/API vẫn recheck server-side. |

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
| `P01-AUT-009` | P0 | Public visitor có thể self-register; registration không cần invitation hoặc Admin approval. | Valid submission tạo đúng một unverified account; duplicate/race/abuse handled safely. |
| `P01-AUT-010` | P0 | Email verification token single-use/time-bound kích hoạt account. | Unverified account không dùng business module; valid token active account; replay/expired token fail. |
| `P01-AUT-011` | P0 | Sau verification, Personal Space và default User module enablements được provision idempotently. | Retry/callback race không duplicate owner boundary hoặc grants. |
| `P01-AUT-012` | P0 | Registration/verification-resend/recovery endpoints chống enumeration và có rate/abuse controls. | Response semantics an toàn; không spam delivery provider. |

## 5. Functional requirements — users và profiles

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-USR-001` | P0 | SuperAdmin tạo, xem, cập nhật trạng thái User; unique identifier được enforce case/normalization rõ. | Duplicate/race condition không tạo hai identity mâu thuẫn. |
| `P01-USR-002` | P0 | User chỉ sửa profile/preference cho phép của chính mình. | Không sửa role/status/owner/security field qua tampered payload. |
| `P01-USR-003` | P0 | Disable, reactivate, soft-delete User có state transition và confirmation rõ. | State bất hợp lệ bị từ chối; Personal data, shares, support grants, reminders, sessions và jobs được reconciliation. |
| `P01-USR-004` | P0 | Không permanent-delete User khi còn unresolved Personal data hoặc dependency ngoài approved cascade/retention policy. | UI/API trả dependency summary; không silent partial purge. |
| `P01-USR-005` | P1 | User export/delete-account request chỉ bật sau khi data portability/retention policy được duyệt. | Không có partial silent deletion. |

### User state đề xuất

`PendingVerification` → `Active` ↔ `Disabled` → `Deleted/Retention` → `Purged`. Verification mới chuyển Pending sang Active; không có invite/approval state trong Release 1. `SuperAdmin` là role, không phải user state.

## 6. Functional requirements — roles và permissions

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-RBAC-001` | P0 | Hỗ trợ ba role confirmed và default deny cho Admin permissions. | Admin mới không có module administration ngoài grants explicit. |
| `P01-RBAC-002` | P0 | SuperAdmin grant/revoke theo `module.action`; change atomic và audit old/new. | Concurrent/request replay không tạo phantom grant. |
| `P01-RBAC-003` | P0 | Admin không tự gán role/quyền vượt authority baseline. | API escalation tests fail. |
| `P01-RBAC-004` | P0 | Last active SuperAdmin invariant áp dụng concurrency-safe. | Hai request downgrade đồng thời không đưa count về 0. |
| `P01-RBAC-005` | P0 | Permission revocation có bounded propagation tới request/cache/job. | Test chứng minh quyền cũ không dùng được sau bound. |
| `P01-RBAC-006` | P0 | UI permission matrix hiển thị module/action/scope và effective access trước confirm. | SuperAdmin hiểu thay đổi; hidden dependency/conflict được báo. |
| `P01-RBAC-007` | P0 | Self, SharedLink, Support và Emergency là access contexts độc lập, không suy ra lẫn nhau. | Admin/SuperAdmin normal route không có private cross-user access. |
| `P01-RBAC-008` | P0 | SuperAdmin quản lý module theo User và module/action grant theo Admin. | User/Admin không tự enable module bị tắt hoặc nâng permission. |

## 7. Functional requirements — platform services

### 7.1 Ownership/access policy

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-OWN-001` | P0 | Có reusable personal ownership/access evaluator cho API/query/search/file/share/support/job. | Reference resource pass cross-user/share/support/emergency matrix. |
| `P01-OWN-002` | P0 | Owner User/Personal Space lấy từ authoritative server context; Release 1 không có ownership transfer. | Client spoofing UserId hoặc access context thất bại. |
| `P01-OWN-003` | P0 | Support/emergency path được phân biệt rõ và audit; không có request flag tự bật global scope. | Tampered query/header không mở cross-user data. |

### 7.2 Personal data, sharing và privileged support foundation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-PDS-001` | P0 | Mỗi verified User có đúng một internal Personal Space/owner boundary. | Provision retry idempotent; User A/B isolation pass. |
| `P01-PDS-002` | P0 | Sharing hỗ trợ PublicLink, AuthenticatedLink và RestrictedUsers, luôn read-only, có expiry/revoke. | Mode/auth/allowlist/lifecycle matrix pass. |
| `P01-PDS-003` | P0 | User support grant chỉ một module, read-only, duration 24h default/custom/until-revoke. | Scope/expiry/revoke tests pass; any sufficiently permissioned Admin use is audited. |
| `P01-PDS-004` | P0 | SuperAdmin emergency path cần reason, read-only, audit và immediate User notification. | Missing reason/mutation/impersonation fail; notification idempotent. |
| `P01-PDS-005` | P0 | Version/concurrency token chuẩn hóa cho same-User multi-tab/session writes. | Stale edit không silent overwrite. |

### 7.3 Module Platform

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P01-MOD-001` | P0 | Registry load/validate developer-built manifests và personal ownership scope. | Duplicate/incompatible/invalid module fail-safe. |
| `P01-MOD-002` | P0 | System/User enablement, Registration Default và Admin module/action gates được enforce cho route/API/search/widget/job. | Disable/revoke bound and direct-route tests pass. |
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
| `P01-PLT-003` | P0 | File service đáp ứng upload/download metadata và personal owner controls. | Cross-user/path/type/size/checksum tests pass. |
| `P01-PLT-004` | P0 | Notification store/categories/retention, idempotent intents và independent In-app/Email/Browser-Push deliveries tồn tại; mọi intent tạo cả ba channel attempts. | Duplicate, cross-user access, provider failure và disabled User tests pass; lỗi một kênh không chặn kênh khác. |
| `P01-PLT-005` | P0 | System/module/user settings được tách; secret setting masked và secure. | User/Admin cannot edit unauthorized system setting; GET never returns secret. |
| `P01-PLT-006` | P0 | Job run model có status/history/error redaction nếu Phase 1 dùng background work. | Restart/retry/failure visible; no duplicate effects. |
| `P01-PLT-007` | P0 | Notification Center hỗ trợ open source, read/unread, mark-all-read, single delete và bulk delete. | Actions owner-scoped, bulk idempotent; deep link bị revoked/denied trả safe fallback. |
| `P01-PLT-008` | P0 | Không expose notification channel preference, mute hoặc quiet-hours trong Release 1. | UI/API không có suppression setting; delivery failure được ghi riêng. |

## 8. Data concepts tối thiểu

Không phải physical schema; architecture phải map mà không đổi semantics:

- `User`, `Profile`, `Role`, `PermissionDefinition`, `AdminGrant`, `Session/RefreshCredential` (theo auth decision).
- `PersonalSpace`/`PersonalOwnerReference`, `EmailVerificationToken`.
- `ModuleDefinition`, `ModuleVersion`, `SystemModuleEnablement`, `UserModuleEnablement`, `RegistrationModuleDefault`, `AdminModuleGrant`, `ModuleMigrationState`.
- `ResourceType`, `ResourceOwnerReference`, `ConcurrencyVersion`.
- `Share`, `ShareRestrictedUser`, `ShareAccessEvent`.
- `SupportGrant`, `SupportSession`, `EmergencyAccessSession`.
- `AuditEvent`, `ActivityEvent` (nếu P1).
- `FileObject`, `FileReference`.
- `Notification`, `DeliveryAttempt`.
- `SettingDefinition`, `SystemSetting`, `UserPreference`, `IntegrationSecretReference`.
- `JobDefinition`/internal schedule (nếu cần), `JobRun`.

Mọi entity có version/concurrency/lifecycle metadata phù hợp. Secret value không nằm trong audit/search/activity.

## 9. Required audit events

Bootstrap; registration/email verification/login/session/recovery; User lifecycle; System role/permission/User-module lifecycle; last SuperAdmin violation; support grant/session; emergency access; share lifecycle; privileged data access; file rejection; permanent delete/restore; setting/job/backup/restore events.

## 10. Error và edge cases bắt buộc

- Duplicate identifier và concurrent user creation.
- Permission revoked giữa lúc UI đang mở.
- User/module/permission/support grant disabled hoặc revoked trong khi job/request đang queued.
- Registration verification callback/retry race và duplicate Personal Space provisioning.
- Module upgrade/disable với dependent module, queued job và stale search/widget.
- Same-User two-tab edit conflict.
- Resource trash/revoke đúng lúc share được truy cập.
- Redis unavailable/cache flushed.
- SQL/file storage unavailable hoặc upload partial.
- Browser back/retry sau destructive form submit.
- Clock/timezone mismatch trong share/support expiration.
- Setup bị ngắt giữa chừng hoặc bootstrap request lặp.

## 11. Testing và verification

- Unit tests cho role/state/expiration/password policy/domain invariants.
- Integration tests với SQL, Redis, file, email và push adapters của test/production-like profile.
- E2E: register → verify email → Personal Space/default modules → login → self data isolation.
- E2E: bootstrap → promote Admin → grant/revoke module/action → disable → audit.
- Cross-user/API authorization; User module enablement; share mode/allowlist/expiry/revoke; support/emergency matrix.
- Module contract: discover/install/enable/disable/re-enable/upgrade/failure/dependency/migration tests.
- Security tests: credential enumeration, CSRF/XSS, session revoke, secret/log redaction, file traversal/type/size.
- Responsive + keyboard/accessibility test cho login, shell, profile, admin matrix.
- Migration from empty database và upgrade from previous Phase 1 build.

## 12. Exit criteria

- Tất cả P0 requirements và cross-cutting/security/NFR P0 liên quan pass.
- Không còn Critical/High security finding chưa xử lý.
- Cross-user isolation 100% pass ở UI/API/search/file/share/support/job/reference resource.
- Last SuperAdmin invariant, permission/User-module/support revocation propagation được chứng minh.
- Clean local-dev/bootstrap/migration/restart/Redis-loss và registration/email-provider failure flows pass.
- Audit redaction và privileged event coverage pass.
- Phase 2 có thể đăng ký personal Project/Task/Calendar resources, search/widget/job/share/support projections qua Module Contract mà không sửa identity/ownership model.
