# FX-19 — Pomodoro / Focus: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/19-pomodoro.md) — FX-19-BR-001, FX-19-BR-002, FX-19-BR-003, FX-19-BR-004, FX-19-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/19-pomodoro-focus.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `focus` là stable logical key, bind installed ModuleId trong manifest. **Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="focus-session-read"></a>`focus.session.read` — Xem current/history Focus | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S02, FX19-S04 |
| <a id="focus-session-start"></a>`focus.session.start` — Bắt đầu work/break | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S01, FX19-S03 |
| <a id="focus-session-pause"></a>`focus.session.pause` — Tạm dừng | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S02 |
| <a id="focus-session-resume"></a>`focus.session.resume` — Tiếp tục | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S02 |
| <a id="focus-session-cancel"></a>`focus.session.cancel` — Hủy phiên | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S02 |
| <a id="focus-preference-update"></a>`focus.preference.update` — Lưu thời lượng Focus | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S01 |
| <a id="focus-session-finish-phase"></a>`focus.session.finish_phase` — Ghi nhận phase đến hạn | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="focus-session-record-time"></a>`focus.session.record_time` — Ghi work session thành Time Entry | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX19-S03 |
| <a id="focus-support-read"></a>`focus.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `focus.session.read` | Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.session.start` | Theo state machine; phase kế tiếp cần explicit start; không giả elapsed từ client; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.session.pause` | Theo state machine; phase kế tiếp cần explicit start; không giả elapsed từ client; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.session.resume` | Theo state machine; phase kế tiếp cần explicit start; không giả elapsed từ client; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.session.cancel` | Theo state machine; phase kế tiếp cần explicit start; không giả elapsed từ client; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.preference.update` | Defaults25/5/15 theo FX-19; không rewrite completed sessions; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.session.finish_phase` | Trusted clock validation; user polling không được submit elapsed giả; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |
| `focus.session.record_time` | Work only; source session immutable; dedupe session→entry; target module/action còn enabled; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | `time.entry.create` |
| `focus.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own focus session; elapsed từ clock/state hợp lệ; không tự tạo Calendar | Common + dynamic source/provider guards |

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
