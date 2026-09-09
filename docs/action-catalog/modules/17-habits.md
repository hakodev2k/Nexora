# FX-17 — Habits: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/17-habits.md) — FX-17-BR-001, FX-17-BR-002, FX-17-BR-003, FX-17-BR-004, FX-17-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/17-habits.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `habits` là stable logical key, bind installed ModuleId trong manifest. **Own habit; history past check-ins không bị viết lại bởi đổi schedule**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="habits-habit-read"></a>`habits.habit.read` — Xem Habit | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S01, FX17-S02 |
| <a id="habits-habit-create"></a>`habits.habit.create` — Tạo Habit | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S03 |
| <a id="habits-habit-update"></a>`habits.habit.update` — Sửa Habit | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S03 |
| <a id="habits-habit-schedule"></a>`habits.habit.schedule` — Đổi lịch/target từ ngày hiệu lực | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S03 |
| <a id="habits-habit-pause"></a>`habits.habit.pause` — Tạm dừng | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-habit-resume"></a>`habits.habit.resume` — Tiếp tục | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-habit-set-reminder"></a>`habits.habit.set_reminder` — Đặt/thay reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S03 |
| <a id="habits-checkin-record"></a>`habits.checkin.record` — Ghi check-in | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S01 |
| <a id="habits-checkin-correct"></a>`habits.checkin.correct` — Sửa check-in đã ghi | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S01, FX17-S04 |
| <a id="habits-streak-read"></a>`habits.streak.read` — Xem streak/lịch sử check-in | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S04 |
| <a id="habits-habit-archive"></a>`habits.habit.archive` — Archive habit | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-habit-unarchive"></a>`habits.habit.unarchive` — Unarchive habit | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-habit-trash"></a>`habits.habit.trash` — Đưa habit vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-habit-restore"></a>`habits.habit.restore` — Khôi phục habit từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-habit-purge"></a>`habits.habit.purge` — Xóa vĩnh viễn habit | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX17-S02 |
| <a id="habits-support-read"></a>`habits.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `habits.habit.read` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.create` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.update` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; Own habit; history past check-ins không bị viết lại bởi đổi schedule | `habits.habit.read` |
| `habits.habit.schedule` | Theo FX-17; lịch mới không rewrite quá khứ; pause không làm mất streak lịch sử; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.pause` | Theo FX-17; lịch mới không rewrite quá khứ; pause không làm mất streak lịch sử; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.resume` | Theo FX-17; lịch mới không rewrite quá khứ; pause không làm mất streak lịch sử; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.set_reminder` | Theo FX-17; lịch mới không rewrite quá khứ; pause không làm mất streak lịch sử; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.checkin.record` | Own habit/day; không future day; boolean/count validation; ghi correction; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.checkin.correct` | Own habit/day; không future day; boolean/count validation; ghi correction; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.streak.read` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.archive` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; ngoài Trash, chưa Archived; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.unarchive` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; Archived, khôi phục previous state; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.trash` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; preview aggregate, không purge; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.restore` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; đúng deletion cohort, parent hợp lệ; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.habit.purge` | Own habit; history past check-ins không bị viết lại bởi đổi schedule; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |
| `habits.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own habit; history past check-ins không bị viết lại bởi đổi schedule | Common + dynamic source/provider guards |

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
