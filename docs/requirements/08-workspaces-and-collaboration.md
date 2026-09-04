# Workspaces and Asynchronous Collaboration Requirements

**Document ID:** `NX-COLLAB-001`  
**Version:** `1.1-draft`  
**Status:** Working draft  
**Confirmed direction:** Team Workspace và collaboration phải được thiết kế từ đầu. Collaboration v1 là bất đồng bộ.

## 1. Scope

Nexora có hai context dữ liệu:

```text
User Account
├── Personal Space
└── Team Workspaces
    ├── Members
    ├── Workspace Roles
    ├── Enabled Modules
    └── Workspace-owned Resources
```

### Collaboration v1 bao gồm

- Shared Workspace resources.
- Membership và Workspace roles.
- Task assignment.
- Comments, replies và mentions.
- Watch/follow resource.
- Activity feed và notifications.
- Version history và optimistic-concurrency conflict detection.
- Shared Projects, Documents, Files, Calendars và module khác khi manifest hỗ trợ Workspace.
- Read-only external Sharing Engine tách biệt.

### Không bao gồm trong v1

- Live presence/cursor.
- Character-by-character simultaneous co-editing.
- CRDT/Operational Transformation.
- Voice/video/chat realtime.
- Screen sharing.
- Realtime whiteboard.

UI có thể refresh/revalidate/poll và thông báo nội dung đã thay đổi; không được tuyên bố “realtime collaboration”.

## 2. Space và ownership model

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `SPC-001` | P0 | Mỗi active User có đúng một Personal Space baseline. | Personal resource không cần WorkspaceId và chỉ owner/privileged policy truy cập. |
| `SPC-002` | P0 | User có thể thuộc nhiều Team Workspace. | Membership/permissions được evaluate độc lập theo Workspace. |
| `SPC-003` | P0 | Mỗi business resource thuộc đúng một owning Space: Personal hoặc Workspace. | Không đồng thời thuộc hai Workspace/Personal; create xác định Space server-side từ route/context. |
| `SPC-004` | P0 | `CreatedByUserId`/`UpdatedByUserId` tách khỏi owning Space. | Member rời Workspace không làm resource mất owner; history vẫn trace actor theo retention policy. |
| `SPC-005` | P0 | Chuyển resource giữa Personal và Workspace hoặc hai Workspace là dedicated operation, mặc định disabled nếu chưa có policy. | Không đổi WorkspaceId qua generic update; relation/files/comments/shares được kiểm tra. |
| `SPC-006` | P0 | List/detail/count/search/export/job/cache luôn scope theo current Space và permission. | Cross-workspace negative matrix pass kể cả direct ID, aggregate, index và stale cache. |
| `SPC-007` | P0 | Space context luôn hiển thị rõ trong UI khi tạo/sửa/chia sẻ resource. | User không vô tình tạo personal secret/document vào Team Workspace do hidden context. |

Logical ownership không còn hard-code chỉ bằng `OwnerUserId`; architecture phải biểu diễn `PersonalSpace` hoặc `Workspace` owner mà vẫn enforce private-by-default.

## 3. Workspace lifecycle

Lifecycle đề xuất:

```text
Active ↔ Suspended
   ↓
Archived
   ↓
DeletionPending
   ↓
Purged
```

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `WSP-001` | P0 | Authorized User tạo Workspace với name, identifier/slug policy, default settings và first Workspace Owner. | Creation atomic; không có Workspace thiếu active Owner. |
| `WSP-002` | P0 | Rename không thay stable Workspace ID; URL/slug collision và redirect policy rõ. | Existing resource references không hỏng. |
| `WSP-003` | P0 | Suspend/Archive chặn writes/jobs/invites theo policy nhưng giữ data. | Search/widget/automation không bypass state; authorized restore hoạt động. |
| `WSP-004` | P0 | Delete Workspace là workflow có dependency/data/export/retention preview và confirmation mạnh. | Không generic one-click purge; jobs/shares/files/modules/members được reconciliation. |
| `WSP-005` | P0 | Hệ thống ngăn remove/downgrade/disable Workspace Owner cuối cùng. | Concurrent membership changes vẫn giữ ít nhất một active Owner. |
| `WSP-006` | P1 | Workspace transfer hoặc owner succession có audit và recent-auth control. | Old/new authority, actor, reason/outcome được ghi. |

## 4. System roles và Workspace roles

System roles vẫn là:

- `SuperAdmin`: quản trị toàn instance.
- `Admin`: quyền hệ thống theo `module.action` + scope.
- `User`: account bình thường.

Workspace roles `PROPOSED`:

| Workspace role | Baseline authority |
|---|---|
| `WorkspaceOwner` | Full workspace administration, owners, members, modules, settings và data theo policy. |
| `WorkspaceAdmin` | Quản lý members/module/settings được ủy quyền; không remove Owner cuối cùng hoặc vượt system policy. |
| `Member` | Sử dụng module và cộng tác theo assigned permissions. |
| `Guest` | Chỉ truy cập module/resource/collection được cấp hạn chế; không browse toàn Workspace mặc định. |

System Admin không mặc nhiên là Workspace Admin. Workspace Owner không có system administration authority.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `WROLE-001` | P0 | Membership role và module action permission được đánh giá cùng nhau; default deny. | Workspace role không tự cấp action chưa cho phép; direct API denied. |
| `WROLE-002` | P0 | Workspace role chỉ có hiệu lực trong đúng Workspace. | Same user/role in Workspace A không cấp access Workspace B. |
| `WROLE-003` | P0 | Sensitive actions có permission riêng: members, roles, modules, settings, export, purge, privileged data. | Member không thể grant chính mình hoặc người khác quyền cao hơn. |
| `WROLE-004` | P0 | Workspace permission revoke có bounded propagation tới sessions/cache/jobs/search. | Removed Member mất access sau bound; stale queued job bị skip/fail. |
| `WROLE-005` | P0 | Guest access không tự mở toàn bộ entity quan hệ/backlink/search result. | Chỉ explicit accessible projection/resource được trả. |

## 5. Invitations và membership

Membership lifecycle đề xuất:

```text
Invited → Active ↔ Suspended → Removed
       ↘ Expired/Revoked
```

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `MEM-001` | P0 | Workspace Owner/Admin được phép invite hoặc add member theo system/onboarding policy. | Unauthorized invite blocked; duplicate active membership xử lý idempotent. |
| `MEM-002` | P0 | Invitation token đủ entropy, single-use, time-bound, revocable và không log plaintext. | Wrong/expired/replayed/revoked token fail without account enumeration. |
| `MEM-003` | P0 | Nếu local deployment chưa có email provider, có admin-created invitation link/code flow an toàn. | UI không giả vờ gửi email; link chỉ hiển thị một lần theo policy. |
| `MEM-004` | P0 | Accept invite xác nhận authenticated identity và target Workspace/role. | Logged-in wrong account không nhận membership ngoài explicit safe switch. |
| `MEM-005` | P0 | Remove/suspend member chặn future access, mentions delivery và delegated jobs; không xóa Workspace-owned data họ tạo. | Resource vẫn thuộc Workspace; stale sessions/jobs fail closed. |
| `MEM-006` | P0 | Member exit/removal có reconciliation cho assignments, ownership-like responsibilities, pending approvals và private references. | Admin thấy unresolved items; policy reassign/unassign giữ data. |
| `MEM-007` | P1 | Bulk member/role management có preview, partial-failure report và audit. | Không silent partial grant/revoke. |

## 6. Workspace module enablement

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `WMOD-001` | P0 | Chỉ module manifest hỗ trợ `Workspace` mới được bật trong Workspace. | Personal-only module bị từ chối. |
| `WMOD-002` | P0 | Workspace enablement không vượt System Enablement/licensing/policy. | System disable immediately gates Workspace routes/jobs/API. |
| `WMOD-003` | P0 | Workspace Owner/Admin chỉ enable module nếu có permission và dependency đầy đủ. | Missing dependency/migration/status được giải thích. |
| `WMOD-004` | P0 | Disable Workspace module giữ data nhưng chặn route/API/search/widget/job/action. | Re-enable phục hồi dữ liệu theo compatible version. |
| `WMOD-005` | P0 | Module role/user assignment có thể thu hẹp người dùng module trong Workspace. | Module enabled không có nghĩa mọi Member có mọi action. |

## 7. Resource access trong Workspace

Module phải chọn một access model được khai báo:

- `WorkspaceVisible`: mọi Member phù hợp có thể browse.
- `RoleRestricted`: chỉ role/grant được chỉ định.
- `ResourceRestricted`: explicit members/groups/resource grants.
- `GuestExplicit`: Guest chỉ thấy resource được cấp.

Access model không thay thế action permission. Sensitive module như Vault/Finance có thể mặc định `ResourceRestricted` hoặc không hỗ trợ Workspace cho đến khi có security review.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `WACC-001` | P0 | Module khai báo default Workspace visibility và supported restriction levels. | Không có undefined “mọi người có thể xem?” behavior. |
| `WACC-002` | P0 | Child/attachment/comment/backlink không được rộng quyền hơn parent/resource policy. | Direct child ID/file/search path không bypass parent access. |
| `WACC-003` | P0 | Move/copy/template/import vào Workspace áp dụng current Workspace ownership và permission; không giữ share/grant nguồn ngầm. | Cross-space data leak tests pass. |
| `WACC-004` | P0 | Dashboard/count/facet/activity không làm lộ resource restricted. | Guest/restricted Member không suy ra title/count/actor của resource bị cấm. |

## 8. Assignment

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `ASN-001` | P0 | Supported resource có thể assign cho active Workspace Member có access phù hợp. | Không assign User ngoài Workspace/suspended/removed. |
| `ASN-002` | P0 | Assignee khác creator/editor và không thay owning Workspace. | Member removal không chuyển ownership sang Personal Space. |
| `ASN-003` | P0 | Assign/unassign tạo Activity và idempotent Notification intent. | Retry không duplicate notification; actor/resource/assignee đúng. |
| `ASN-004` | P0 | Remove Member áp dụng reassign/unassign policy và không để actionable item trỏ tới inaccessible identity. | Reconciliation report pass. |
| `ASN-005` | P1 | Multiple assignees, teams/groups hoặc watchers chỉ ship sau cardinality/notification decision. | UI/count/status semantics rõ. |

## 9. Comments, replies và mentions

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `COM-001` | P0 | Resource type phải đăng ký commentable capability; commenter cần resource access + `comment.create`. | Direct endpoint trên non-commentable/inaccessible resource bị từ chối. |
| `COM-002` | P0 | Comment body được sanitize, size-limited và version/concurrency-controlled. | XSS/oversized/tampered author tests pass. |
| `COM-003` | P0 | Reply depth baseline là một thread level hoặc giới hạn đã duyệt. | Cycle/deep nesting abuse bị chặn; mobile readable. |
| `COM-004` | P0 | Edit/delete comment giữ edited/deleted marker và policy/history phù hợp. | Không giả mạo author; reply handling predictable; privileged moderation audited. |
| `COM-005` | P0 | `@mention` chỉ resolve active Member actor được phép biết trong Workspace/resource context. | Không enumerate account ngoài Workspace hoặc mention người không có resource access. |
| `COM-006` | P0 | Mention/comment notification idempotent và không chứa private content vượt notification policy. | Retry không spam; revoked access làm deep link fail safely. |
| `COM-007` | P0 | Comment, attachment và mention thừa hưởng resource/Workspace lifecycle. | Resource trash/module disable/member removal không để bypass bằng comment URL. |
| `COM-008` | P1 | Reactions, resolved threads và moderation queue là optional capability, không P0. | Nếu bật, permissions/audit/state được định nghĩa riêng. |

## 10. Watchers, activity và notifications

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `COL-NTF-001` | P0 | User có thể follow/unfollow supported resource; assignment/mention có auto-follow policy rõ. | Follow state private, idempotent và access-scoped. |
| `COL-NTF-002` | P0 | Collaboration event types gồm assign, mention, comment/reply, status/change cần thiết, member/module changes. | Event schema versioned; no secret/sensitive body in generic payload. |
| `COL-NTF-003` | P0 | Notification preference theo Workspace/module/event, với mandatory security/admin exceptions. | Mute Workspace không tắt security-critical event trái policy. |
| `COL-NTF-004` | P0 | Activity feed chỉ hiển thị event/resource actor được phép xem. | Restricted resource title/count/actor không leak. |
| `COL-NTF-005` | P1 | Digest/batching có thể giảm noise nhưng không delay critical event. | Dedupe/batch golden tests. |

## 11. Asynchronous editing và conflicts

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `ASYNC-001` | P0 | Collaborative resource dùng optimistic concurrency/version token hoặc equivalent. | Hai User sửa cùng version: một commit, request stale nhận conflict; không silent overwrite. |
| `ASYNC-002` | P0 | Conflict response cung cấp current version và safe resolution options phù hợp resource. | User có thể reload, compare hoặc manually merge; permission rechecked. |
| `ASYNC-003` | P0 | Documents/Knowledge có version history; restore tạo version mới, không rewrite history. | Actor/time/change traceable; share/permission không reset. |
| `ASYNC-004` | P0 | Task/status/reorder/comment actions có atomic domain transition và idempotency. | Duplicate/reordered requests không mất update hoặc nhân đôi item. |
| `ASYNC-005` | P0 | UI không hiển thị live cursor/presence hoặc đảm bảo simultaneous co-editing. | Product wording/docs phản ánh asynchronous model. |
| `ASYNC-006` | P1 | Optional “recently edited by”/last-updated indicator dựa trên persisted activity, không fake realtime presence. | Timestamp/actor access-scoped và accurate. |

## 12. Workspace Sharing versus external Sharing Engine

Hai cơ chế phải tách rõ:

- **Workspace access:** dựa trên membership, role, module enablement và resource permission; có thể cho phép edit/comment.
- **External share link:** token-based, mặc định read-only, có login/password/expiration/revoke.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `WSHR-001` | P0 | Không dùng public share link để thay thế Workspace membership/collaboration permission. | Share viewer không edit/comment/assign trong baseline. |
| `WSHR-002` | P0 | Tạo external share cho Workspace resource cần module/resource permission riêng. | Member có edit không mặc nhiên có `share.create`. |
| `WSHR-003` | P0 | Workspace/module/resource disable/trash/revoke chặn external link theo policy. | Cached/stale share không bypass current state. |
| `WSHR-004` | P0 | Workspace Owner/Admin xem và revoke shares trong authority; access audited. | Không cần biết plaintext share password/token. |

## 13. Files và collaboration

- Workspace file thuộc Workspace, uploader chỉ là actor.
- Attachment thừa hưởng resource policy.
- Version/replace không overwrite binary history âm thầm.
- Comment attachment không tạo public file URL.
- Member removal không xóa files họ upload.
- Download/list/preview/search phải pass Workspace membership và resource permission.

## 14. Audit events bắt buộc

- Workspace create/rename/suspend/archive/delete/restore.
- Owner/Admin/member/guest invite, accept, role change, suspend/remove/leave.
- Last Owner violation attempt.
- Workspace module enable/disable/configuration.
- Permission/grant/restriction change.
- Privileged Workspace data access/export/purge.
- External share create/revoke/access theo policy.
- Comment moderation/permanent delete.
- Cross-space move/copy.

Audit không lưu full comment/document body, invitation token hoặc secret.

## 15. Cross-workspace security tests

Mỗi Workspace-capable module phải test:

1. User chỉ có Personal Space.
2. Member Workspace A nhưng không thuộc B.
3. User thuộc A và B với role khác nhau.
4. Workspace Owner/Admin/Member/Guest.
5. Removed/Suspended Member với session/cache/job cũ.
6. Module enabled ở A nhưng disabled ở B.
7. Workspace-visible và restricted resource.
8. Direct ID, child, file, comment, search, dashboard, export, job và share access.
9. Concurrent permission/membership/resource update.
10. Workspace archived/deletion pending.

## 16. Phase placement

| Phase | Workspace/collaboration capability |
|---|---|
| Phase 0 | Khóa roles, invitation, default visibility, module scope và member-removal policies. |
| Phase 1 | Personal/Workspace model, membership, roles, module enablement, audit và isolation framework. |
| Phase 2 | Shared Tasks/Projects/Calendar, assignment, comments, mentions và notifications. |
| Phase 3 | Shared Knowledge/Documents/Files, versioning, conflict resolution và external sharing. |
| Phase 4+ | Mỗi Finance/Vault/Shopping/Developer/Asset module phải explicit opt-in Workspace support. |
| Phase 8 | Cross-workspace penetration tests, scale, backup/restore, deletion và incident runbooks. |

## 17. Exit criteria cho collaboration baseline

- Personal/Workspace ownership không còn hard-code User-only.
- Workspace roles/module enablement/access policies approved.
- Last Workspace Owner invariant concurrency-safe.
- Assignment/comment/mention/activity/notification flows pass.
- Optimistic-concurrency conflict không silent overwrite.
- Member removal/revocation áp dụng tới API/search/files/jobs/cache.
- External sharing và Workspace collaboration được tách rõ.
- Cross-user/cross-workspace negative matrix đạt 100%.
- Không có Critical/High security/privacy finding mở.

