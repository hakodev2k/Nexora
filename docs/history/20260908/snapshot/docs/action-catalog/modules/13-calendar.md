# FX-13 — Calendar: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/13-calendar.md) — FX-13-BR-001, FX-13-BR-002, FX-13-BR-003, FX-13-BR-004, FX-13-BR-005, FX-13-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/13-calendar.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `calendar` là stable logical key, bind installed ModuleId trong manifest. **Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="calendar-calendar-read"></a>`calendar.calendar.read` — Xem Day/Week/Month/Agenda | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S01, FX13-S02, FX13-S03, FX13-S04 |
| <a id="calendar-event-read"></a>`calendar.event.read` — Xem Event cá nhân | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-event-create"></a>`calendar.event.create` — Tạo Event cá nhân | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-event-update"></a>`calendar.event.update` — Sửa Event cá nhân | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-event-complete"></a>`calendar.event.complete` — Scheduled → Completed | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-event-cancel"></a>`calendar.event.cancel` — Scheduled → Canceled | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-event-set-reminder"></a>`calendar.event.set_reminder` — Đặt/thay một reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-event-remove-reminder"></a>`calendar.event.remove_reminder` — Gỡ reminder | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S05 |
| <a id="calendar-projection-open"></a>`calendar.projection.open` — Mở Task nguồn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S06 |
| <a id="calendar-ics-preview"></a>`calendar.ics.preview` — Preview file ICS | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S07 |
| <a id="calendar-ics-import"></a>`calendar.ics.import` — Import Event cá nhân | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX13-S07 |
| <a id="calendar-ics-export"></a>`calendar.ics.export` — Xuất ICS theo loại/trạng thái/khoảng chọn | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX13-S07 |
| <a id="calendar-support-read"></a>`calendar.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `calendar.calendar.read` | Từng loại source phải được phép; không render dữ liệu module disabled; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.event.read` | Title/Description/Start/End required; create Scheduled; update Scheduled only; all-day dates giữ semantics; trùng giờ warn, vẫn cho save; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.event.create` | Title/Description/Start/End required; create Scheduled; update Scheduled only; all-day dates giữ semantics; trùng giờ warn, vẫn cho save; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.event.update` | Title/Description/Start/End required; create Scheduled; update Scheduled only; all-day dates giữ semantics; trùng giờ warn, vẫn cho save; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | `calendar.event.read` |
| `calendar.event.complete` | Chỉ Scheduled; terminal chỉ-đọc, không reopen/Trash; cancel là hành vi của nút hủy Event; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.event.cancel` | Chỉ Scheduled; terminal chỉ-đọc, không reopen/Trash; cancel là hành vi của nút hủy Event; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.event.set_reminder` | Scheduled only; exact time hoặc Start−15min; không VALARM import; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.event.remove_reminder` | Scheduled only; exact time hoặc Start−15min; không VALARM import; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.projection.open` | Kèm tasks.task.read; không Calendar permission nào cho sửa Task; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | `tasks.task.read` |
| `calendar.ics.preview` | Nonrecurring valid events only; UID dedupe; Scheduled; ignore VALARM; báo skipped/invalid; import không cập nhật Task; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | `calendar.event.create` |
| `calendar.ics.import` | Nonrecurring valid events only; UID dedupe; Scheduled; ignore VALARM; báo skipped/invalid; import không cập nhật Task; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | `calendar.event.create` |
| `calendar.ics.export` | Chỉ fully-contained events; source read tại request/worker/download; Tasks projection là ngoại lệ ICS đã duyệt, không Tasks importer/exporter chung; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |
| `calendar.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own Calendar; Task projections luôn cần source Tasks access; không sửa Task qua Calendar | Common + dynamic source/provider guards |

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
