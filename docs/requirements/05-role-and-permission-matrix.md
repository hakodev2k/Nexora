# Role and Permission Matrix

**Document ID:** `NX-AUTHZ-001`  
**Status:** Baseline + proposed refinements  
**Confirmed roles:** `SuperAdmin`, `Admin`, `User`

## 1. Authorization model

Quyền quản trị dùng format `module.action`. Quyết định truy cập business data phải kết hợp:

1. actor/account đang active;
2. role và permission action;
3. data scope (`own`, `shared`, hoặc privileged `all`);
4. resource state (active/trash/revoked/expired);
5. contextual security control (recent auth, rate limit, share password...);
6. explicit deny/policy đặc biệt nếu có.

`Action permission + data scope` là refinement `PROPOSED` để tránh việc `tasks.view` vô tình cho Admin xem mọi Task. Nếu Product Owner muốn permission action tự bao gồm global scope, phải ghi decision và cập nhật audit/privacy requirements.

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

Không tạo action synonym tùy tiện (`read` và `view`, `remove` và `delete`) nếu không có semantic khác.

## 4. Permission catalog theo module

| Namespace | Actions baseline | Ghi chú |
|---|---|---|
| `users` | `view`, `create`, `update`, `disable`, `delete`, `restore`, `access_all` | Delete user phải theo retention workflow. |
| `roles` | `view`, `create`, `update`, `delete`, `assign` | Admin không quản lý SuperAdmin baseline. |
| `permissions` | `view`, `grant`, `revoke` | Chỉ SuperAdmin ở baseline. |
| `settings` | `view`, `update` | System/module scope tách biệt. |
| `audit` | `view`, `export`, `access_all` | Không có update/delete. |
| `sharing` | `view`, `create`, `revoke`, `access_all` | Còn phụ thuộc resource policy. |
| `files` | `view`, `create`, `delete`, `restore`, `purge`, `access_all` | Download đi cùng `view` + resource access. |
| `notifications` | `view`, `update`, `configure`, `access_all` | User quản lý notification của mình bằng owner policy. |
| `tasks` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `import`, `export`, `access_all` | Subtask/checklist thuộc Task aggregate. |
| `projects` | như Tasks | Archive có thể là `update` state hoặc action riêng sau decision. |
| `calendar` | `view`, `create`, `update`, `delete`, `restore`, `share`, `import`, `export`, `access_all` | Attendee/invitation deferred. |
| `knowledge` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `publish`, `import`, `export`, `access_all` | Publish chỉ tồn tại nếu workflow được duyệt. |
| `documents` | `view`, `create`, `update`, `delete`, `restore`, `purge`, `share`, `export`, `access_all` | Version restore có thể cần action riêng. |
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

## 6. Access resolution cho resource

Một request được phép khi một trong các nhánh hợp lệ và không bị contextual control chặn:

- actor là owner và có user/module action;
- actor có authenticated share grant hợp lệ cho read action;
- anonymous actor có public/password share session hợp lệ cho read action;
- Admin có `module.action` **và** data scope phù hợp;
- SuperAdmin dùng privileged path.

Share không cấp `update/delete/export/reveal` trong baseline. Resource ở Trash, share expired/revoked, account disabled hoặc security lock có thể override allow.

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

## 8. Mandatory authorization test matrix

Mỗi module P0 phải test tối thiểu:

- anonymous, disabled User, User owner, User khác, authenticated shared viewer;
- Admin không permission, Admin có action nhưng không global scope, Admin đủ action+scope;
- SuperAdmin;
- active, trashed, revoked/expired share resource;
- list, detail-by-ID, create, update, delete, restore, export, search và background execution có liên quan.
