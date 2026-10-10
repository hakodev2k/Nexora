# FX-15 — Planner: actions

Catalog v1 · 2026-09-07. The 2026-09-11 local slice binds `planner.plan.read`, `pin`, `unpin`, `reorder`, `reschedule` and `notes` to `SqlPlannerService` and `/api/v1/planner*`; the 2026-09-12 hardening adds owner-local default dates, source lifecycle locking, action-specific grants, actor attribution and same-owner SQL composite constraints. The support row remains unimplemented. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/15-planner.md) — FX-15-BR-001, FX-15-BR-002, FX-15-BR-003, FX-15-BR-004, FX-15-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/15-planner.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `planner` là stable logical key, bind installed ModuleId trong manifest. **Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="planner-plan-read"></a>`planner.plan.read` — Xem daily/weekly plan | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX15-S01, FX15-S02 |
| <a id="planner-plan-pin"></a>`planner.plan.pin` — Đưa Task vào kế hoạch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX15-S01, FX15-S03 |
| <a id="planner-plan-unpin"></a>`planner.plan.unpin` — Bỏ khỏi kế hoạch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX15-S01 |
| <a id="planner-plan-reorder"></a>`planner.plan.reorder` — Sắp xếp kế hoạch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX15-S01 |
| <a id="planner-plan-reschedule"></a>`planner.plan.reschedule` — Đổi ngày kế hoạch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX15-S02 |
| <a id="planner-plan-notes"></a>`planner.plan.notes` — Sửa planning notes | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX15-S01 |
| <a id="planner-support-read"></a>`planner.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `planner.plan.read` | Own plan + tasks.task.read trên từng Task; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |
| `planner.plan.pin` | Own planning metadata; source access hiện tại; không pin mới Task trong Project terminal; thay lịch Task cần action Tasks riêng; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |
| `planner.plan.unpin` | Own planning metadata; terminal/unavailable source remains removable from owner history. Không pin mới Task trong Project terminal; thay lịch Task cần action Tasks riêng; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |
| `planner.plan.reorder` | Own planning metadata; source access hiện tại; không pin mới Task trong Project terminal; thay lịch Task cần action Tasks riêng; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |
| `planner.plan.reschedule` | Own planning metadata; source access hiện tại; không pin mới Task trong Project terminal; thay lịch Task cần action Tasks riêng; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |
| `planner.plan.notes` | Own planning metadata; source access hiện tại; không pin mới Task trong Project terminal; thay lịch Task cần action Tasks riêng; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |
| `planner.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Lens trên Task đã có; không tạo bản sao, không thay Start/End hay status | Common + dynamic source/provider guards |

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
