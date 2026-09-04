# Role and Permission Matrix

**Document ID:** `NX-AUTHZ-001`  
**Version:** `1.2-draft`  
**Status:** Approved personal-only baseline + documented open items  
**Confirmed roles:** `SuperAdmin`, `Admin`, `User`

## 1. Authorization model

Quyền quản trị dùng format `module.action`. Quyết định truy cập business data phải kết hợp:

1. actor/account đang active;
2. System role và permission action;
3. module installed/system-enabled;
4. System/User module enablement;
5. Personal ownership hoặc explicit read-only share/support grant;
6. data-access context (`self`, `shared-link`, `support-grant` hoặc `emergency`);
7. resource state (active/trash/revoked/expired);
8. contextual security control (recent authentication, rate limit, expiry, allowlist...);
9. explicit deny/policy đặc biệt nếu có.

`Action permission + module enablement + owner/share/support scope` là baseline để tránh việc `tasks.view` vô tình cho Admin xem mọi Task của mọi User.

## 2. Role baseline

| Capability | User | Admin | SuperAdmin |
|---|---|---|---|
| Quản lý dữ liệu chính mình | Theo module được bật | Như User cho dữ liệu cá nhân của chính Admin | Như User cho dữ liệu cá nhân của chính SuperAdmin |
| Truy cập dữ liệu được share | Theo share policy | Theo share policy | Theo share policy trên normal path |
| Quản trị User | Không | Chỉ khi được cấp action/scope | Có |
| Cấp quyền Admin | Không | Không ở baseline | Có |
| Tạo/hạ/xóa SuperAdmin | Không | Không | Có, nhưng không được làm mất SuperAdmin cuối cùng |
| System settings | Không | Theo permission | Có |
| Audit Log | Dữ liệu/activity cá nhân nếu được thiết kế | Theo `audit.view` + scope | Toàn hệ thống |
| Dữ liệu User khác | Chỉ qua read-only share | Chỉ read-only support grant đúng module + permission | Chỉ support grant hoặc emergency path có reason/audit/notification |

## 2.1 Data-access contexts

Release 1 không có Workspace roles. Một request business-data chỉ có thể chạy trong một context rõ ràng:

| Context | Actor | Access |
|---|---|---|
| `Self` | User/Admin/SuperAdmin | CRUD dữ liệu cá nhân của chính actor theo module policy. |
| `SharedLink` | Anonymous hoặc authenticated viewer | Chỉ xem projection của resource theo mode/expiry/allowlist. |
| `Support` | Admin đủ permission | Chỉ xem dữ liệu của một User trong đúng một module và duration User đã cấp. |
| `Emergency` | SuperAdmin | Chỉ xem qua break-glass flow, bắt buộc reason, immutable audit và immediate User notification. |

## 3. Action vocabulary

| Action | Ý nghĩa |
|---|---|
| `view` | Xem list/metadata/detail không nhạy cảm. |
| `create` | Tạo resource. |
| `update` | Sửa resource/state không thuộc action nhạy cảm riêng. |
| `delete` | Đưa vào Trash. |
| `restore` | Khôi phục từ Trash. |
| `purge` | Permanent delete. |
| `share` | Tạo/revoke/manage share. |
| `export` / `import` | Xuất/nhập dữ liệu. |
| `execute` | Chạy tool/job/workflow. |
| `configure` | Thay đổi configuration/schedule/integration. |
| `reveal` | Giải mã và hiển thị Secret. |
| `copy` | Giải mã và gửi Secret tới clipboard response. |
| `publish` | Chuyển content sang trạng thái published nếu module có. |
| `support_access` | Mở read-only support session trong phạm vi active User grant. |
| `emergency_access` | SuperAdmin mở read-only break-glass session có reason/audit/notification. |

Không tạo action synonym tùy tiện (`read` và `view`, `remove` và `delete`) nếu không có semantic khác.

## 4. Permission catalog theo module

| Namespace | Actions baseline | Ghi chú |
|---|---|---|
| `users` | `view`, `create`, `update`, `disable`, `delete`, `restore` | Chỉ quản lý account/profile metadata theo scope; không mở business data của User. Delete user phải theo retention workflow. |
| `modules` | `view`, `enable`, `disable`, `configure` | System enable/disable chỉ SuperAdmin/authority được duyệt. |
| `roles` | `view`, `create`, `update`, `delete`, `assign` | Admin không quản lý SuperAdmin baseline. |
| `permissions` | `view`, `grant`, `revoke` | Chỉ SuperAdmin ở baseline. |
| `settings` | `view`, `update` | System/module scope tách biệt. |
| `audit` | `view`, `export` | System audit scope do role/policy quyết định; không có update/delete. |
| `sharing` | `view`, `create`, `revoke` | Còn phụ thuộc owner/resource/module policy. |
| `support` | `view`, `grant`, `revoke`, `support_access`, `emergency_access` | User chỉ grant/revoke của mình; emergency chỉ SuperAdmin. |
| `files` | `view`, `create`, `delete`, `restore`, `purge` | Download đi cùng `view` + valid data-access context. |
| `notifications` | `view`, `update`, `configure` | User quản lý notification của mình bằng owner policy. |
| `tasks` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share` | Task phải thuộc Project; import/export không có trong Release 1. |
| `projects` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share` | Import/export không có trong Release 1; terminal state không reopen. |
| `calendar` | `view`, `create`, `update`, `cancel`, `import`, `export` | Event không shareable; delete manual Event được biểu diễn bằng `cancel`. |
| `knowledge` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `publish`, `import`, `export` | Publish chỉ tồn tại nếu workflow được duyệt. |
| `documents` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `export` | Version/conflict cho nhiều tab/session; team comments/co-editing out. |
| `search` | `view`, `configure` | Không được mở rộng data scope của nguồn. |
| `finance` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `import`, `export` | Export audit bắt buộc; User khác chỉ qua valid support/emergency context. |
| `vault` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `reveal`, `copy`, `share`, `import`, `export` | `share/import/export` mặc định disabled cho đến decision; support/emergency không tự cấp `reveal`/`copy`. |
| `news` | `view`, `create`, `update`, `delete`, `configure`, `import`, `export` | `create/update` áp dụng source/category/saved data. |
| `shopping` | `view`, `create`, `update`, `delete`, `restore`, `configure`, `import`, `export` | Refresh có thể dùng `execute`. |
| `developer_tools` | `view`, `execute`, `configure` | Network/API tools có security policy riêng. |
| `github_discovery` | `view`, `execute`, `configure` | Read-only đối với GitHub; `execute` là refresh. |
| `automation` | `view`, `create`, `update`, `delete`, `execute`, `configure` | Execute/configure/audit tách riêng. |
| `personal_assets` | CRUD, restore/purge, share, import/export | Field classification theo asset. |
| `digital_assets` | CRUD, restore/purge, share, import/export | Secret field chuyển sang Vault reference. |
| `career` | CRUD, restore/purge, share, import/export | Resume/file access theo resource policy. |
| `backups` | `view`, `create`, `restore`, `delete`, `configure` | Restore là privileged + audit. |
| `integrations` | `view`, `create`, `update`, `delete`, `test`, `configure` | Credential value không được trả lại. |

## 5. Baseline permission rules

| ID | Rule |
|---|---|
| `PERM-001` | Default deny cho permission không tồn tại, chưa gán hoặc resource type chưa đăng ký. |
| `PERM-002` | User CRUD trên owned resource được cấp qua user policy/module enablement, không cần tạo hàng nghìn permission records riêng. |
| `PERM-003` | Admin chỉ có action được SuperAdmin gán; account Admin mới không có module permission ngoài self-service User baseline. |
| `PERM-004` | `purge`, `export`, `reveal`, `copy`, `restore backup`, `support_access`, `emergency_access` và permission grant/revoke là sensitive actions. |
| `PERM-005` | Sensitive action có audit; một số action yêu cầu recent authentication theo security policy. |
| `PERM-006` | Xóa permission có hiệu lực cho request mới ngay; cache invalidation nằm trong bounded interval được test. |
| `PERM-007` | Permission check dùng authoritative server identity; client claims không được tự quyết định nếu chưa verify. |
| `PERM-008` | Permission rename/removal cần migration để không tạo orphan grants hoặc accidental allow. |
| `PERM-009` | Không có wildcard hoặc `access_all` grant cho business data. Admin chỉ qua active support grant; SuperAdmin chỉ qua support grant hoặc emergency context ngoài normal route. |
| `PERM-010` | UI chỉ hiển thị action có thể dùng nhưng server luôn kiểm tra lại. |
| `PERM-011` | Module enablement không cấp Admin action permission; permission không override module bị system/User-disabled. |
| `PERM-012` | User không được tự bật module đã bị SuperAdmin tắt cho account của mình. |
| `PERM-013` | Share/support/emergency context luôn read-only và không thay đổi ownership. |
| `PERM-014` | Child, file, relation, count, search result, notification và job không được rộng quyền hơn owner/resource nguồn. |
| `PERM-015` | Mọi module Release 1 bật mặc định cho User mới; SuperAdmin có thể thay registration default về sau. |
| `PERM-016` | SuperAdmin quản lý module theo từng User và module/action grant của Admin; Admin không tự nâng grant hoặc bypass disabled module. |

## 6. Access resolution cho resource

Một request được phép khi một trong các nhánh hợp lệ và không bị contextual control chặn:

- actor là owner và module được bật cho account;
- actor có authenticated share grant hợp lệ cho read action;
- anonymous actor có public share token hợp lệ cho read action;
- Admin có `module.view` + `support_access` và active User grant cho đúng module;
- SuperAdmin dùng explicit support hoặc emergency path.

External share/support/emergency access không cấp `create/update/delete/purge/export/reveal/copy`. Resource ở Trash, module disabled, share/support grant expired/revoked, account disabled hoặc security lock có thể override allow.

## 7. Administration workflows

### 7.1 Tạo Admin

1. SuperAdmin chọn active User.
2. Hệ thống hiển thị warning và permission set rỗng/proposed template.
3. SuperAdmin xác nhận role change và gán action cụ thể.
4. Hệ thống commit atomically, invalidate authorization cache và ghi audit old/new state.

### 7.2 Thu hồi quyền

Revocation phải áp dụng cho request mới, background job chưa chạy và session/action context theo policy. Running export/automation cần cancellation hoặc documented completion behavior.

### 7.3 Bảo vệ SuperAdmin cuối cùng

Check phải transactional/concurrency-safe cho disable, delete và role downgrade. UI confirmation không đủ để bảo đảm invariant.

### 7.4 User support grant

User chọn đúng một module và một duration: `24Hours` mặc định, `CustomExpiry` hoặc `UntilRevoked`. Grant read-only có thể được bất kỳ Admin đủ module action + `support_access` sử dụng. Expire/revoke phải chặn request mới và được audit.

### 7.5 SuperAdmin emergency access

Chỉ SuperAdmin có `emergency_access` mới mở break-glass session. Reason bắt buộc trước access; User được thông báo ngay; mọi attempt/use/end được audit. Không impersonate User và không suy ra reveal/export/mutation.

### 7.6 User module enablement

Module chỉ truy cập được khi package installed, system enabled, enabled cho User và dependencies/migrations ready. Disable giữ data nhưng chặn route/API/search/widget/job/new notification intent. Module mặc định bật cho User mới cho tới khi SuperAdmin cấu hình registration-default policy khác.

## 8. Mandatory authorization test matrix

Mỗi module P0 phải test tối thiểu:

- anonymous, unverified/disabled User, owner, User khác, public/authenticated/restricted external-share viewer;
- module enabled cho User A nhưng disabled cho User B;
- Admin không permission, Admin có action nhưng không support grant, Admin đủ action + active support grant;
- Admin với support grant sai User/sai module/hết hạn và Admin đủ active grant;
- SuperAdmin normal path, support path và emergency path có/không có reason;
- active, restricted, trashed, module disabled, revoked/expired share/support resource;
- list, detail-by-ID, create, update, delete, restore, export, search và background execution có liên quan.
