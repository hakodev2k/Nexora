# FX-18 — Time Tracking: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/18-time-tracking.md) — FX-18-BR-001, FX-18-BR-002, FX-18-BR-003, FX-18-BR-004, FX-18-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/18-time-tracking.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `time` là stable logical key, bind installed ModuleId trong manifest. **Owner scope; tối đa một timer đang chạy; không billable/team/payroll**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="time-timer-read"></a>`time.timer.read` — Xem timer đang chạy | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S01 |
| <a id="time-timer-start"></a>`time.timer.start` — Bắt đầu timer | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S01 |
| <a id="time-timer-stop"></a>`time.timer.stop` — Dừng timer | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S01 |
| <a id="time-timer-resume"></a>`time.timer.resume` — Tiếp tục bằng entry mới | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S01 |
| <a id="time-entry-read"></a>`time.entry.read` — Xem entries | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S02 |
| <a id="time-entry-create"></a>`time.entry.create` — Tạo manual entry | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S03 |
| <a id="time-entry-update"></a>`time.entry.update` — Sửa manual/time entry | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S03 |
| <a id="time-entry-trash"></a>`time.entry.trash` — Đưa entry vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S02 |
| <a id="time-entry-restore"></a>`time.entry.restore` — Khôi phục entry từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S02 |
| <a id="time-entry-purge"></a>`time.entry.purge` — Xóa vĩnh viễn entry | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX18-S02 |
| <a id="time-entry-history"></a>`time.entry.history` — Xem lịch sử entry | QUERY / SELF | Yes, gated | Sensitive (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX18-S04 |
| <a id="time-report-read"></a>`time.report.read` — Xem báo cáo thời gian | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX18-S04 |
| <a id="time-support-read"></a>`time.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `time.timer.read` | Owner scope; tối đa một timer đang chạy; không billable/team/payroll; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.timer.start` | Một owner timer; source Task/Project eligibility hiện tại; Resume không kéo dài entry cũ; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.timer.stop` | Một owner timer; source Task/Project eligibility hiện tại; Resume không kéo dài entry cũ; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.timer.resume` | Một owner timer; source Task/Project eligibility hiện tại; Resume không kéo dài entry cũ; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.read` | Owner scope; tối đa một timer đang chạy; không billable/team/payroll; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.create` | Start/end/duration hợp lệ, cùng owner; source link không grant Task edit; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.update` | Không đổi timer đang chạy qua edit; lưu correction lịch sử; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.trash` | Owner scope; tối đa một timer đang chạy; không billable/team/payroll; preview aggregate, không purge; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.restore` | Owner scope; tối đa một timer đang chạy; không billable/team/payroll; đúng deletion cohort, parent hợp lệ; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.purge` | Owner scope; tối đa một timer đang chạy; không billable/team/payroll; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.entry.history` | Owner-only history, cùng owner/module; không share/support; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.report.read` | Owner scope; tối đa một timer đang chạy; không billable/team/payroll; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |
| `time.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Owner scope; tối đa một timer đang chạy; không billable/team/payroll | Common + dynamic source/provider guards |

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
