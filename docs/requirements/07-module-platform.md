# Module Platform Requirements

**Document ID:** `NX-MOD-001`  
**Version:** `1.2-draft`  
**Status:** Working draft  
**Confirmed direction:** Module mới chỉ do trusted Nexora developers phát triển bằng code. Admin/User không tạo module, không upload executable code và không thay đổi schema module.

## 1. Mục tiêu

Module Platform cho phép Nexora bổ sung domain mới mà không phải sửa lại Platform Kernel hoặc tự tích hợp riêng với navigation, permissions, personal ownership, sharing, search, dashboard, notifications, audit, files, trash và automation.

Module đầu tiên được triển khai trong cùng codebase theo mô hình modular application. Khả năng cài executable plugin bên thứ ba hoặc no-code module builder không thuộc baseline.

## 2. Khái niệm

| Khái niệm | Ý nghĩa |
|---|---|
| Module Package | Frontend, backend, manifest, migrations, permissions và assets do developer phát triển/ship. |
| Module Definition | Metadata/version/capabilities được Nexora đăng ký từ package. |
| Module Installation | Package có mặt trong bản build/deployment và được Platform Kernel nhận diện. |
| System Enablement | SuperAdmin cho phép module hoạt động trong instance Nexora. |
| User Enablement | SuperAdmin bật/tắt module cho một User cụ thể. |
| Registration Default | Policy xác định module nào tự bật cho User mới; Release 1 mặc định là toàn bộ module. |
| Admin Module Grant | SuperAdmin cho phép Admin nhìn/quản trị module theo action cụ thể. |
| Module Permission | Quyền chi tiết `module.action` bên trong module. |

## 3. Module development boundary

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-001` | P0 | Chỉ trusted developer/build pipeline được thêm hoặc nâng cấp Module Package. | Không có UI/API cho User/Admin upload DLL, JavaScript bundle, migration hoặc executable package. |
| `MOD-002` | P0 | SuperAdmin quản lý enablement; Admin/User chỉ configure hoặc sử dụng module trong authority được cấp. | Request chứa custom code/schema/manifest hoặc unauthorized enablement từ client bị từ chối. |
| `MOD-003` | P0 | Module phải tuân thủ contract và test kit trước khi được đưa vào build. | Build/release chặn module thiếu manifest, permission declaration, migrations hoặc required tests. |
| `MOD-004` | P1 | Third-party executable marketplace và no-code module builder được đánh dấu `Deferred`. | Không tạo extension surface công khai hoặc security promise chưa được thiết kế. |

## 4. Module Manifest

Mỗi module phải khai báo machine-readable manifest tương đương:

```yaml
id: productivity.tasks
name: Tasks
version: 1.0.0
category: work
ownershipScope: personal
dependencies:
  - platform.identity
  - platform.notifications
permissions:
  - tasks.view
  - tasks.create
  - tasks.update
  - tasks.delete
contributions:
  routes: []
  navigation: []
  widgets: []
  entityTypes: []
  searchProviders: []
  events: []
  automationTriggers: []
  automationActions: []
  jobs: []
  settings: []
  migrations: []
```

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-MAN-001` | P0 | `ModuleId` là duy nhất, ổn định và không tái sử dụng sau khi module bị bỏ. | Duplicate ID chặn startup/build; rename phải có migration/alias decision. |
| `MOD-MAN-002` | P0 | Version dùng convention đã duyệt và khai báo compatibility với Platform Contract. | Incompatible module không được enable; lỗi nêu dependency/version thiếu. |
| `MOD-MAN-003` | P0 | Manifest Release 1 khai báo `ownershipScope: personal`; Workspace không phải supported scope. | Module không tạo group-owned resource hoặc nhận Workspace ID từ client. |
| `MOD-MAN-004` | P0 | Manifest khai báo permissions, dependencies, routes và contributions trước runtime use. | Unknown permission/route/entity contribution bị từ chối hoặc fail-fast an toàn. |
| `MOD-MAN-005` | P0 | Manifest không chứa secret hoặc environment credential. | Repository/build/registry scan không tìm thấy credential. |
| `MOD-MAN-006` | P1 | Manifest có display metadata, documentation, owner/team và deprecation notice. | Admin xác định được module version, purpose, support owner và status. |

## 5. Lifecycle

Lifecycle chuẩn:

```text
Discovered
    ↓
Installed
    ↓
Enabled ↔ Disabled
    ↓
Upgrading
    ↓
Deprecated
    ↓
Uninstalled/Removed from build
```

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-LCY-001` | P0 | Installed không đồng nghĩa Enabled. | Module mới xuất hiện trong catalog nhưng route/job/API không hoạt động trước system enablement. |
| `MOD-LCY-002` | P0 | Disable không xóa data, files, audit hoặc configuration cần phục hồi. | Re-enable khôi phục trạng thái hợp lệ theo version; data vẫn inaccessible khi disabled. |
| `MOD-LCY-003` | P0 | Disable dừng scheduled jobs, event consumers, notifications và new writes theo bounded behavior. | Queued/running work có cancel/finish/skip policy; không tiếp tục side effect trái phép. |
| `MOD-LCY-004` | P0 | Module có dependency đang được module khác sử dụng không thể disable nếu chưa xử lý impact. | UI/API hiển thị dependent modules và block/plan disable. |
| `MOD-LCY-005` | P0 | Upgrade chạy compatibility check và database migration trước enable version mới. | Partial failure không để module ở trạng thái báo Enabled nhưng schema không tương thích. |
| `MOD-LCY-006` | P0 | Uninstall/removal khỏi build cần retention/export/migration decision; không implicit purge. | Deployment fail-fast hoặc safe-disabled nếu historical definition/data còn cần. |
| `MOD-LCY-007` | P1 | Deprecation có replacement/migration timeline và user/admin notice. | Không remove permission/entity/event type khi consumer/data chưa được xử lý. |

## 6. Enablement hierarchy

Một request chỉ được vào module khi tất cả gate liên quan đều pass:

```text
Package installed
    ↓
System enabled
    ↓
Personal ownership supported
    ↓
User enabled
    ↓
Role/User assigned
    ↓
module.action allowed
    ↓
Resource access allowed
```

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-ENA-001` | P0 | Chỉ SuperAdmin hoặc system authority được duyệt mới thay đổi System Enablement. | Admin/User không thể bật module bị tắt toàn hệ thống. |
| `MOD-ENA-002` | P0 | SuperAdmin có thể enable/disable module cho từng User; User/Admin không tự override. | Module chưa installed/system-enabled hoặc dependency chưa ready bị chặn; disabled User không mở route/API/job. |
| `MOD-ENA-003` | P0 | Tất cả module Release 1 mặc định bật khi User hoàn tất email verification, cho tới khi Registration Default được SuperAdmin thay đổi. | Fresh verified account có đúng effective module set; policy version được audit. |
| `MOD-ENA-004` | P0 | Admin Module Grant và action permission không thay thế User module enablement hoặc support grant. | Admin có action nhưng không active support grant vẫn không xem private User data. |
| `MOD-ENA-005` | P0 | Disable/permission revoke có bounded propagation tới routes, API, search, widgets, jobs và cache. | Sau bound, stale link/widget/job không truy cập hoặc tạo side effect. |
| `MOD-ENA-006` | P0 | Effective enablement có explainable result cho authorized admin. | UI cho biết gate nào đang chặn mà không lộ dữ liệu/permission không được xem. |

## 7. Contribution contracts

### 7.1 Routes và navigation

- Route phải gắn Module ID, required action và owner/access context.
- Navigation được tạo từ registry và effective enablement, không hard-code tất cả module trong shell.
- Direct URL luôn kiểm tra lại server-side.
- Module có thể đóng góp nhiều route nhưng không chiếm namespace module khác.

### 7.2 Entity/Resource types

- Mỗi entity type đăng ký stable `ResourceType`.
- Khai báo personal owner scope, lifecycle, share/search/file/trash/audit capabilities.
- Resource thuộc đúng một User/Personal Space.
- Entity ID không đủ để truy cập nếu thiếu scope/permission.

### 7.3 Dashboard widgets

- Widget khai báo module, permission, personal owner context, data source và degraded state.
- Disable module tự loại widget khỏi dashboard mà không phá layout.
- Widget chỉ đọc qua module contract, không query table module khác.

### 7.4 Global Search

- Module đăng ký search provider/projection và safe searchable fields.
- Search rechecks effective module enablement, owner và share/support access.
- Disable module làm kết quả không còn accessible dù index còn stale.

### 7.5 Events và Automation

- Event/trigger/action có schema version và owner scope.
- Automation không được gọi module disabled hoặc action ngoài permission.
- Secret dùng Vault reference; không nằm trong manifest/event/log.

### 7.6 Settings

- Tách System, Module và User settings.
- Settings schema có validation, defaults, sensitivity và migration.
- Secret setting chỉ replace/revoke, không đọc lại plaintext.

## 8. Module isolation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-ISO-001` | P0 | Module không đọc/ghi trực tiếp business table của module khác. | Cross-module dependency chỉ qua public application contract, shared kernel type tối thiểu hoặc event. |
| `MOD-ISO-002` | P0 | Module không tự triển khai identity, personal ownership, permission, sharing/support, audit, files, notifications hoặc secret store riêng. | Architecture/contract review không có duplicate security boundary. |
| `MOD-ISO-003` | P0 | Failure trong module phải được cô lập khỏi application shell và unrelated modules trong khả năng hợp lý. | Widget/route/job lỗi có degraded state; health báo đúng module. |
| `MOD-ISO-004` | P0 | Module query/write luôn nhận authoritative Actor + Owner/Access context từ Platform Kernel. | Client không spoof UserId, support/share context hoặc Module scope. |
| `MOD-ISO-005` | P0 | Cache key và background job payload phải bao gồm đủ Module/User identity. | Không cross-user cache hit hoặc job effect. |

## 9. Database migrations và compatibility

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-MIG-001` | P0 | Migration thuộc module, có ordered version và được ghi trạng thái. | Startup/release biết installed schema version từng module. |
| `MOD-MIG-002` | P0 | Migration phải idempotent hoặc có exact-once/locking control. | Hai instance không chạy destructive migration đồng thời. |
| `MOD-MIG-003` | P0 | Migration failure không mark module/version ready. | Module safe-disabled; prior usable version/restore path documented. |
| `MOD-MIG-004` | P0 | Breaking entity/event/API change cần compatibility window hoặc coordinated migration. | Consumer cũ không nhận schema không hiểu. |
| `MOD-MIG-005` | P0 | Data migration luôn giữ personal ownership, support/share boundary và audit requirements. | Rehearsal không đưa data sang sai User hoặc mở grant ngoài ý muốn. |

## 10. Module Catalog và administration

Admin surface phải hiển thị:

- Module ID, name, version và category.
- Developer/maintainer.
- Installed/system-enabled status.
- Ownership scope (`Personal` trong Release 1).
- Dependencies/dependents.
- User enablement và Admin grant summary.
- Permissions và contributions.
- Migration/schema status.
- Health, last job/error và diagnostics an toàn.
- Data retention/uninstall implications.

Admin không được xem secret, private module data hoặc raw stack trace chỉ vì được xem Module Catalog.

## 11. Security và supply chain

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MOD-SEC-001` | P0 | Module source/dependencies đi qua code review, build, tests, secret/vulnerability/license scan. | Release pipeline chặn finding theo policy. |
| `MOD-SEC-002` | P0 | Module không được tự nâng permission hoặc tự system-enable sau upgrade. | Manifest diff yêu cầu review; existing assignment không mở action mới ngầm. |
| `MOD-SEC-003` | P0 | New sensitive permission/contribution phải được admin thấy và approve theo policy. | Upgrade không tự grant `emergency_access`, `export`, `reveal`, network hoặc admin actions. |
| `MOD-SEC-004` | P0 | Module external HTTP/files/rich content tuân thủ shared SSRF/upload/sanitization policies. | Module không tạo bypass bằng custom client/parser. |
| `MOD-SEC-005` | P1 | Package signing/provenance/SBOM là release requirement trước third-party distribution. | Build artifact traceable tới source/dependency manifest. |

## 12. Contract test kit

Mỗi module phải pass:

1. Manifest/schema validation.
2. Install/enable/disable/re-enable/upgrade tests.
3. Personal owner-context tests.
4. Cross-user/share/support/emergency isolation tests.
5. Role/action/resource permission matrix.
6. Search/widget/navigation behavior khi enablement thay đổi.
7. Job/event retry, stale scope và disable behavior.
8. Audit/log/secret redaction.
9. Migration from oldest supported version.
10. Responsive/accessibility/error/degraded state cho P0 routes.

## 13. Definition of Done cho module mới

- Module manifest và owner approved.
- Scope, permissions, entity ownership, sharing và support behavior documented.
- Migrations và recovery path verified.
- Contributions đăng ký qua Platform Contract.
- Không direct DB access sang module khác.
- Contract/security/isolation tests pass.
- Module Catalog hiển thị đúng metadata/health.
- Disable không mất data và ngừng side effects.
- Documentation, configuration và known limitations hoàn thành.
