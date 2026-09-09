# FX-12 — Tasks: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/12-tasks.md) — FX-12-BR-001, FX-12-BR-002, FX-12-BR-003, FX-12-BR-004, FX-12-BR-005, FX-12-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/12-tasks.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `tasks` là stable logical key, bind installed ModuleId trong manifest. **Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="tasks-task-read"></a>`tasks.task.read` — Xem Task | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01, FX12-S02, FX12-S04 |
| <a id="tasks-task-create"></a>`tasks.task.create` — Tạo Task | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01, FX12-S03 |
| <a id="tasks-task-update"></a>`tasks.task.update` — Sửa Task | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S02, FX12-S03 |
| <a id="tasks-task-start"></a>`tasks.task.start` — Chưa làm → Đang làm | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01 |
| <a id="tasks-task-complete"></a>`tasks.task.complete` — Chưa làm/Đang làm → Hoàn thành | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01 |
| <a id="tasks-task-skip"></a>`tasks.task.skip` — Chưa làm/Đang làm → Bỏ qua | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01 |
| <a id="tasks-task-revert"></a>`tasks.task.revert` — Chuyển về trạng thái trước | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01, FX12-S05 |
| <a id="tasks-task-reorder"></a>`tasks.task.reorder` — Sắp xếp trong cột | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S01 |
| <a id="tasks-task-criteria"></a>`tasks.task.criteria` — Sửa văn bản/checklist Acceptance Criteria | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S03 |
| <a id="tasks-task-set-reminder"></a>`tasks.task.set_reminder` — Đặt/thay một reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S03 |
| <a id="tasks-task-remove-reminder"></a>`tasks.task.remove_reminder` — Gỡ reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S03 |
| <a id="tasks-task-trash"></a>`tasks.task.trash` — Đưa task vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S02, FX12-S04 |
| <a id="tasks-task-restore"></a>`tasks.task.restore` — Khôi phục task từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S04 |
| <a id="tasks-task-purge"></a>`tasks.task.purge` — Xóa vĩnh viễn task | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX12-S04 |
| <a id="tasks-task-history"></a>`tasks.task.history` — Xem lịch sử task | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX12-S06 |
| <a id="tasks-task-restore-version"></a>`tasks.task.restore_version` — Khôi phục version thành bản mới | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX12-S06 |
| <a id="tasks-task-share"></a>`tasks.task.share` — Quản lý link chỉ-đọc của task | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX12-S04 |
| <a id="tasks-support-read"></a>`tasks.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `tasks.task.read` | Own Task read trong Project active hoặc terminal; terminal không mất quyền xem; Trash chỉ source-approved preview, không live detail; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.create` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; Title/Project/Start/End required; create NotStarted/InProgress; Update không đổi status/project/reminder; ngoài khoảng Project cần confirm; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.update` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; Title/Project/Start/End required; create NotStarted/InProgress; Update không đổi status/project/reminder; ngoài khoảng Project cần confirm; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | `tasks.task.read` |
| `tasks.task.start` | Parent active; state graph FX-12; revert bắt buộc reason; Completed ↔ Skipped phải qua active state, không đi tắt; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.complete` | Parent active; state graph FX-12; revert bắt buộc reason; Completed ↔ Skipped phải qua active state, không đi tắt; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.skip` | Parent active; state graph FX-12; revert bắt buộc reason; Completed ↔ Skipped phải qua active state, không đi tắt; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.revert` | Parent active; state graph FX-12; revert bắt buộc reason; Completed ↔ Skipped phải qua active state, không đi tắt; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.reorder` | Parent active; giữ status, rank trong cùng Project/cột; đổi cột phải thêm transition action; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.criteria` | Parent active; lưu full version; không ngầm complete Task khi tick đủ; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.set_reminder` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; exact time hoặc Start−15min; revision invalidates old schedule; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.remove_reminder` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; exact time hoặc Start−15min; revision invalidates old schedule; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.trash` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; preview aggregate, không purge; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.restore` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; đúng deletion cohort, parent hợp lệ; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.purge` | Task ở Trash; individually deleted Task có thể purge dù Project đã terminal; nếu thuộc Project deletion cohort phải qua Project aggregate purge; preview pins/dependencies, xác nhận irreversible; không yêu cầu mở lại Project; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.history` | Owner-only history, cùng owner/module; không share/support; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.restore_version` | Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng; editable hiện tại, giữ immutable identity/topology, tái kiểm tra quyền trên field diff; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |
| `tasks.task.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | `sharing.link.read` |
| `tasks.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Cùng owner; ProjectId bất biến; Project terminal cho xem chỉ-đọc, không tiếp tục Task; mutation cần parent active trừ Trash purge theo provider/cohort riêng | Common + dynamic source/provider guards |

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
