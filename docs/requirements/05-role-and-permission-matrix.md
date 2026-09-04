# Role and Permission Matrix

**Document ID:** `NX-AUTHZ-001`  
**Version:** `1.1-draft`  
**Status:** Baseline + proposed refinements  
**Confirmed roles:** `SuperAdmin`, `Admin`, `User`

## 1. Authorization model

Quyền quản trị dùng format `module.action`. Quyết định truy cập business data phải kết hợp:

1. actor/account đang active;
2. System role và permission action;
3. module installed/system-enabled;
4. current Personal/Workspace Space và module enablement;
5. Workspace membership/role hoặc Personal ownership;
6. data scope (`personal`, `workspace`, `restricted`, `shared-link` hoặc privileged `all`);
7. resource state (active/trash/revoked/expired);
8. contextual security control (recent auth, rate limit, share password...);
9. explicit deny/policy đặc biệt nếu có.

`Action permission + Module/Space enablement + membership + resource scope` là baseline để tránh việc `tasks.view` vô tình cho Admin/Member xem mọi Task.

## 2. Role baseline

| Capability | User | Admin | SuperAdmin |
|---|---|---|---|
| Quản lý dữ liệu chính mình | Theo module được bật | Như User | Có |
| Truy cập dữ liệu được share | Theo share policy | Theo share policy | Có global access nhưng privileged access phải audit |
| Quản trị User | Không | Chỉ khi được cấp action/scope | Có |
| Cấp quyền Admin | Không | Không ở baseline | Có |
| Tạo/hạ/xóa SuperAdmin | Không | Không | Có, nhưng không được làm mất SuperAdmin cuối cùng |
| System settings | Không | Theo permission | Có |
| Audit Log | Dữ liệu/activity cá nhân nếu được thiết kế | Theo `audit.view` + scope | Toàn hệ thống |
| Vault/Finance user khác | Không | Chỉ explicit privileged scope | Có, phải audit và áp dụng sensitive controls |

## 2.1 Workspace role baseline

Workspace roles là lớp khác với System roles:

| Capability | WorkspaceOwner | WorkspaceAdmin | Member | Guest |
|---|---|---|---|---|
| Quản lý Workspace lifecycle | Có, trừ system-only controls | Theo grant; không xóa/loại Owner cuối | Không | Không |
| Quản lý members/roles | Có | Theo grant và delegation limit | Không | Không |
| Enable Workspace module | Có nếu system cho phép | Theo `workspace_modules.manage` | Không | Không |
| Browse Workspace-visible resource | Theo module action | Theo module action | Theo module action | Không mặc định |
| Restricted resource | Theo policy/grant | Theo policy/grant | Explicit grant | Explicit grant |
| Create/edit/comment/assign | Theo module action | Theo module action | Theo module action | Chỉ explicit action |
| System administration | Không tự động | Không | Không | Không |

User có thể là WorkspaceOwner ở Workspace A, Member ở B và Guest ở C. Mỗi context phải được evaluate độc lập.

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
| `access_all` | Mở data scope toàn hệ thống cho module; đặc quyền cao, audit bắt buộc. |
| `comment` | Tạo/sửa/xóa comment theo ownership/moderation policy. |
| `assign` | Assign/unassign supported resource cho Workspace Member. |
| `manage_members` | Invite, suspend, remove hoặc đổi role trong delegation limit. |
| `manage_modules` | Enable/disable/configure module trong Workspace khi system policy cho phép. |

Không tạo action synonym tùy tiện (`read` và `view`, `remove` và `delete`) nếu không có semantic khác.

## 4. Permission catalog theo module

| Namespace | Actions baseline | Ghi chú |
|---|---|---|
| `users` | `view`, `create`, `update`, `disable`, `delete`, `restore`, `access_all` | Delete user phải theo retention workflow. |
| `workspaces` | `view`, `create`, `update`, `archive`, `delete`, `restore`, `access_all` | Last Workspace Owner và lifecycle guards. |
| `workspace_members` | `view`, `invite`, `update`, `suspend`, `remove`, `manage_members` | Role delegation và member-removal reconciliation. |
| `workspace_modules` | `view`, `enable`, `disable`, `configure`, `manage_modules` | Không vượt System Enablement/`supportedSpaces`. |
| `modules` | `view`, `enable`, `disable`, `configure` | System enable/disable chỉ SuperAdmin/authority được duyệt. |
| `roles` | `view`, `create`, `update`, `delete`, `assign` | Admin không quản lý SuperAdmin baseline. |
| `permissions` | `view`, `grant`, `revoke` | Chỉ SuperAdmin ở baseline. |
| `settings` | `view`, `update` | System/module scope tách biệt. |
| `audit` | `view`, `export`, `access_all` | Không có update/delete. |
| `sharing` | `view`, `create`, `revoke`, `access_all` | Còn phụ thuộc resource policy. |
| `files` | `view`, `create`, `delete`, `restore`, `purge`, `access_all` | Download đi cùng `view` + resource access. |
| `notifications` | `view`, `update`, `configure`, `access_all` | User quản lý notification của mình bằng owner policy. |
| `tasks` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `assign`, `comment`, `import`, `export`, `access_all` | Workspace support gồm assignment/comment. |
| `projects` | như Tasks | Archive có thể là `update` state hoặc action riêng sau decision. |
| `calendar` | `view`, `create`, `update`, `delete`, `restore`, `share`, `comment`, `import`, `export`, `access_all` | Workspace calendar async; external attendee invitation deferred. |
| `knowledge` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `publish`, `import`, `export`, `access_all` | Publish chỉ tồn tại nếu workflow được duyệt. |
| `documents` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `comment`, `export`, `access_all` | Async version/conflict; realtime co-editing out. |
| `comments` | `view`, `create`, `update`, `delete`, `moderate`, `access_all` | Luôn phụ thuộc parent resource access. |
| `search` | `view`, `configure`, `access_all` | Không được mở rộng data scope nguồn. |
| `finance` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `import`, `export`, `access_all` | Export/access_all audit bắt buộc. |
| `vault` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `reveal`, `copy`, `share`, `import`, `export`, `access_all` | `share/import/export` mặc định disabled cho đến decision. |
| `news` | `view`, `create`, `update`, `delete`, `configure`, `import`, `export`, `access_all` | `create/update` áp dụng source/category/saved data. |
| `shopping` | `view`, `create`, `update`, `delete`, `restore`, `configure`, `import`, `export`, `access_all` | Refresh có thể dùng `execute`. |
| `developer_tools` | `view`, `execute`, `configure` | Network/API tools có security policy riêng. |
| `github_discovery` | `view`, `execute`, `configure` | Read-only đối với GitHub; `execute` là refresh. |
| `automation` | `view`, `create`, `update`, `delete`, `execute`, `configure`, `access_all` | Execute/configure/audit tách riêng. |
| `personal_assets` | CRUD, restore/purge, share, import/export, `access_all` | Field classification theo asset. |
| `digital_assets` | CRUD, restore/purge, share, import/export, `access_all` | Secret field chuyển sang Vault reference. |
| `career` | CRUD, restore/purge, share, import/export, `access_all` | Resume/file access theo resource policy. |
| `backups` | `view`, `create`, `restore`, `delete`, `configure` | Restore là privileged + audit. |
| `integrations` | `view`, `create`, `update`, `delete`, `test`, `configure` | Credential value không được trả lại. |

## 5. Baseline permission rules

| ID | Rule |
|---|---|
| `PERM-001` | Default deny cho permission không tồn tại, chưa gán hoặc resource type chưa đăng ký. |
| `PERM-002` | User CRUD trên owned resource được cấp qua user policy/module enablement, không cần tạo hàng nghìn permission records riêng. |
| `PERM-003` | Admin chỉ có action được SuperAdmin gán; account Admin mới không có module permission ngoài self-service User baseline. |
| `PERM-004` | `access_all`, `purge`, `export`, `reveal`, `copy`, `restore backup`, permission grant/revoke là sensitive actions. |
| `PERM-005` | Sensitive action có audit; một số action yêu cầu recent authentication theo security policy. |
| `PERM-006` | Xóa permission có hiệu lực cho request mới ngay; cache invalidation nằm trong bounded interval được test. |
| `PERM-007` | Permission check dùng authoritative server identity; client claims không được tự quyết định nếu chưa verify. |
| `PERM-008` | Permission rename/removal cần migration để không tạo orphan grants hoặc accidental allow. |
| `PERM-009` | Không có wildcard grant cho Admin trong baseline; SuperAdmin bypass được code hóa rõ, không lưu như editable grant. |
| `PERM-010` | UI chỉ hiển thị action có thể dùng nhưng server luôn kiểm tra lại. |
| `PERM-011` | Module enablement không cấp permission; permission không override module/system/Workspace disabled state. |
| `PERM-012` | Workspace role chỉ có hiệu lực trong đúng Workspace; System role và Workspace role không tự ánh xạ sang nhau. |
| `PERM-013` | Workspace-owned resource không bị chuyển/xóa khi creator/member rời Workspace. |
| `PERM-014` | Child, comment, file, relation, count, search result và job không được rộng quyền hơn resource/Space nguồn. |

## 6. Access resolution cho resource

Một request được phép khi một trong các nhánh hợp lệ và không bị contextual control chặn:

- actor ở Personal Space là owner và có user/module action;
- actor là active Workspace Member, module được bật và có role/action/resource access;
- actor có authenticated share grant hợp lệ cho read action;
- anonymous actor có public/password share session hợp lệ cho read action;
- Admin có `module.action` **và** data scope phù hợp;
- SuperAdmin dùng privileged path.

External share không cấp `update/comment/assign/delete/export/reveal` trong baseline. Workspace collaboration có thể cấp edit/comment/assign qua membership. Resource ở Trash, module/Workspace disabled, membership removed, share expired/revoked, account disabled hoặc security lock có thể override allow.

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

### 7.4 Workspace membership và Owner cuối cùng

Workspace Owner/Admin grant/revoke chỉ trong delegation authority. Remove/leave/suspend/downgrade phải transactional và không được làm mất active Workspace Owner cuối cùng. Member removal thu hồi sessions/cache/jobs trong bounded time nhưng giữ Workspace-owned resources.

### 7.5 Workspace module enablement

Workspace module chỉ được bật nếu package installed, system enabled, manifest hỗ trợ Workspace và dependencies/migrations ready. Enablement không tự grant action permission; disable giữ data nhưng chặn route/API/search/widget/job.

## 8. Mandatory authorization test matrix

Mỗi module P0 phải test tối thiểu:

- anonymous, disabled User, Personal owner, User khác, authenticated external-share viewer;
- Workspace Owner/Admin/Member/Guest, removed/suspended Member;
- User thuộc nhiều Workspace với role khác nhau;
- module enabled ở Workspace A nhưng disabled ở B;
- Admin không permission, Admin có action nhưng không global scope, Admin đủ action+scope;
- SuperAdmin;
- active, restricted, trashed, Workspace/module disabled, revoked/expired share resource;
- list, detail-by-ID, create, update, delete, restore, export, search và background execution có liên quan.
