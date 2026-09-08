# FX-11 — Projects: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/11-projects.md) — FX-11-BR-001, FX-11-BR-002, FX-11-BR-003, FX-11-BR-004, FX-11-BR-005, FX-11-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/11-projects.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `projects` là stable logical key, bind installed ModuleId trong manifest. **Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="projects-project-read"></a>`projects.project.read` — Xem Project | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S01, FX11-S03 |
| <a id="projects-project-create"></a>`projects.project.create` — Tạo Project | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S01, FX11-S02 |
| <a id="projects-project-update"></a>`projects.project.update` — Sửa Project | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S02 |
| <a id="projects-project-start"></a>`projects.project.start` — Bắt đầu Project | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S03 |
| <a id="projects-project-revert"></a>`projects.project.revert` — Trở về Chưa làm | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S03 |
| <a id="projects-project-complete"></a>`projects.project.complete` — Hoàn thành Project | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S04 |
| <a id="projects-project-skip"></a>`projects.project.skip` — Bỏ qua Project | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S04 |
| <a id="projects-project-trash"></a>`projects.project.trash` — Đưa project vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S01 |
| <a id="projects-project-restore"></a>`projects.project.restore` — Khôi phục project từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX11-S01 |
| <a id="projects-project-purge"></a>`projects.project.purge` — Xóa vĩnh viễn project | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX11-S01 |
| <a id="projects-project-history"></a>`projects.project.history` — Xem lịch sử project | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX11-S05 |
| <a id="projects-project-share"></a>`projects.project.share` — Quản lý link chỉ-đọc của project | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX11-S03 |
| <a id="projects-support-read"></a>`projects.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `projects.project.read` | Create: Title/Description/Start/End, mặc định NotStarted; Update chỉ Project hoạt động; cảnh báo khoảng thời gian, không sửa status qua Update; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.create` | Create: Title/Description/Start/End, mặc định NotStarted; Update chỉ Project hoạt động; cảnh báo khoảng thời gian, không sửa status qua Update; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.update` | Create: Title/Description/Start/End, mặc định NotStarted; Update chỉ Project hoạt động; cảnh báo khoảng thời gian, không sửa status qua Update; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | `projects.project.read` |
| `projects.project.start` | Chỉ NotStarted ↔ InProgress; chuyển ngược cần reason; không terminal → active; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.revert` | Chỉ NotStarted ↔ InProgress; chuyển ngược cần reason; không terminal → active; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.complete` | Active; nếu còn Task chưa kết thúc: cảnh báo + reason; giữ Task states, khóa cả aggregate; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.skip` | Active; xác nhận; giữ Task states, khóa cả aggregate; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.trash` | Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc; preview aggregate, không purge; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.restore` | Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc; đúng deletion cohort, parent hợp lệ; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.purge` | Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.history` | Owner-only history, cùng owner/module; không share/support; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |
| `projects.project.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | `sharing.link.read` |
| `projects.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Cùng owner; Project terminal Completed/Skipped không mở lại; mọi Task bên trong chỉ-đọc | Common + dynamic source/provider guards |

## Deny và UX contract

- Module off, grant missing/deny, resource wrong owner, disallowed lifecycle, current Q gate hoặc source dependency fail: không side effect; không dùng hidden button thay authorization.
- Before/after field diff được kiểm tra cho Save, import, version restore, bulk, scheduler và automation. Form không được gửi status/reveal/export/owner trong generic Update.
- Safe capability reason: ModuleUnavailable, ActionDenied, LifecycleLocked, DependencyUnavailable, DecisionBlocked hoặc StepUpRequired; unknown/wrong-owner resource trả unavailable chung để không enumerate.
- Grant không thay đổi state graph. Chỉ quyền đã cấp và hợp lệ mới xuất hiện enabled; permission editor có thể hiển thị blocked row để giải thích, không cho bật.
- Revocation và support/share/system contexts áp toàn bộ [common contract](../00-authorization-contract.md). Readonly projections không reuse full owner DTO.

## Acceptance tối thiểu

1. Với mỗi row: positive case đúng context/current state; wrong-owner và wrong-context negative; absent/deny Admin grant; module off; stale revision; lifecycle/Q gate.
2. COMMAND/COMPOSITE: request replay/idempotency, before-commit recheck; affected fields cần đủ action. QUERY: owner-scoped filtering trước count/pagination/projection, cache không rò source revoked.
3. LOCAL: keyboard/menu và tool entry cùng capability gate; không network/persist ngầm. SYSTEM: trusted caller, original authority và no UI grant.
4. Row nhạy cảm: no secret in response preview, toast, logs, URL, search, browser persistent storage; current recent-auth gate nếu required.
5. Nếu handler/source projection chưa có approved contract, action phải báo Blocked/Unavailable, không tự thực thi fallback rộng hơn.
