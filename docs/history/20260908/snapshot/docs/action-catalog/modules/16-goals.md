# FX-16 — Goals: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/16-goals.md) — FX-16-BR-001, FX-16-BR-002, FX-16-BR-003, FX-16-BR-004, FX-16-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/16-goals.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `goals` là stable logical key, bind installed ModuleId trong manifest. **Own Goal; explicit status; progress calculation không tự complete Goal**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="goals-goal-read"></a>`goals.goal.read` — Xem Goal | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S01 |
| <a id="goals-goal-create"></a>`goals.goal.create` — Tạo Goal | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S01, FX16-S02 |
| <a id="goals-goal-update"></a>`goals.goal.update` — Sửa Goal | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S02 |
| <a id="goals-goal-start"></a>`goals.goal.start` — Bắt đầu | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-goal-complete"></a>`goals.goal.complete` — Hoàn thành | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-goal-abandon"></a>`goals.goal.abandon` — Dừng mục tiêu | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-goal-reopen"></a>`goals.goal.reopen` — Mở lại mục tiêu | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-read"></a>`goals.target.read` — Xem Goal target | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-create"></a>`goals.target.create` — Tạo Goal target | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-update"></a>`goals.target.update` — Sửa Goal target | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-remove"></a>`goals.target.remove` — Bỏ target | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-link-task"></a>`goals.target.link_task` — Liên kết Task | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-unlink-task"></a>`goals.target.unlink_task` — Gỡ liên kết Task | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-progress-read"></a>`goals.progress.read` — Xem tiến độ Goal | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-goal-archive"></a>`goals.goal.archive` — Archive goal | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S01 |
| <a id="goals-goal-unarchive"></a>`goals.goal.unarchive` — Unarchive goal | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S01 |
| <a id="goals-goal-trash"></a>`goals.goal.trash` — Đưa goal vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S01 |
| <a id="goals-goal-restore"></a>`goals.goal.restore` — Khôi phục goal từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S01 |
| <a id="goals-goal-purge"></a>`goals.goal.purge` — Xóa vĩnh viễn goal | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX16-S01 |
| <a id="goals-support-read"></a>`goals.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |
| <a id="goals-goal-history"></a>`goals.goal.history` — Xem Goal history | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03 |
| <a id="goals-target-record-progress"></a>`goals.target.record_progress` — Cập nhật numeric/boolean progress | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX16-S03, FX16-S04 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `goals.goal.read` | Own Goal; explicit status; progress calculation không tự complete Goal; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.create` | Own Goal; explicit status; progress calculation không tự complete Goal; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.update` | Own Goal; explicit status; progress calculation không tự complete Goal; Own Goal; explicit status; progress calculation không tự complete Goal | `goals.goal.read` |
| `goals.goal.start` | Theo Goal graph FX-16, không áp Project terminal lock sang Goal; giữ lịch sử tiến độ; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.complete` | Theo Goal graph FX-16, không áp Project terminal lock sang Goal; giữ lịch sử tiến độ; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.abandon` | Theo Goal graph FX-16, không áp Project terminal lock sang Goal; giữ lịch sử tiến độ; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.reopen` | Theo Goal graph FX-16, không áp Project terminal lock sang Goal; giữ lịch sử tiến độ; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.target.read` | Own editable Goal; target type immutable; numeric/boolean/Task-based constraints FX-16; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.target.create` | Own editable Goal; target type immutable; numeric/boolean/Task-based constraints FX-16; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.target.update` | Own editable Goal; target type immutable; numeric/boolean/Task-based constraints FX-16; Own Goal; explicit status; progress calculation không tự complete Goal | `goals.target.read` |
| `goals.target.remove` | Preview tác động mẫu số/progress; giữ audit lịch sử; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.target.link_task` | Cùng owner; tasks.task.read; không sửa Task; denominator theo FX-16; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.target.unlink_task` | Cùng owner; tasks.task.read; không sửa Task; denominator theo FX-16; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.progress.read` | Own Goal; explicit status; progress calculation không tự complete Goal; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.archive` | Own Goal; explicit status; progress calculation không tự complete Goal; ngoài Trash, chưa Archived; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.unarchive` | Own Goal; explicit status; progress calculation không tự complete Goal; Archived, khôi phục previous state; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.trash` | Own Goal; explicit status; progress calculation không tự complete Goal; preview aggregate, không purge; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.restore` | Own Goal; explicit status; progress calculation không tự complete Goal; đúng deletion cohort, parent hợp lệ; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.purge` | Own Goal; explicit status; progress calculation không tự complete Goal; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.goal.history` | Owner only, không Support/link; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |
| `goals.target.record_progress` | Editable Goal; Task-based target derives source read, không ghi manual denominator; Own Goal; explicit status; progress calculation không tự complete Goal | Common + dynamic source/provider guards |

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
