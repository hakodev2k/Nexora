# FX-23 — Read Later: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/23-read-later.md) — FX-23-BR-001, FX-23-BR-002, FX-23-BR-003, FX-23-BR-004, FX-23-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/23-read-later.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `reading` là stable logical key, bind installed ModuleId trong manifest. **Queue reference theo source; không copy body hay xóa source**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="reading-queue-read"></a>`reading.queue.read` — Xem reading queue | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX23-S01, FX23-S02, FX23-S03 |
| <a id="reading-item-save"></a>`reading.item.save` — Lưu source vào Read Later | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX23-S01 |
| <a id="reading-item-remove"></a>`reading.item.remove` — Gỡ khỏi queue | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX23-S01, FX23-S03 |
| <a id="reading-item-read"></a>`reading.item.read` — Đánh dấu đã đọc | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX23-S01, FX23-S02 |
| <a id="reading-item-unread"></a>`reading.item.unread` — Đánh dấu chưa đọc | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX23-S01, FX23-S02 |
| <a id="reading-item-position"></a>`reading.item.position` — Lưu reading position | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX23-S02 |
| <a id="reading-support-read"></a>`reading.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `reading.queue.read` | Source provider read từng item, không source disabled payload; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |
| `reading.item.save` | Own queue item; source read; News read/unread còn phải qua news.article.mark_read/mark_unread, không ghi vòng; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |
| `reading.item.remove` | Own queue item; source read; News read/unread còn phải qua news.article.mark_read/mark_unread, không ghi vòng; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |
| `reading.item.read` | Own queue item; source read; News read/unread còn phải qua news.article.mark_read/mark_unread, không ghi vòng; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |
| `reading.item.unread` | Own queue item; source read; News read/unread còn phải qua news.article.mark_read/mark_unread, không ghi vòng; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |
| `reading.item.position` | Own queue item; source read; News read/unread còn phải qua news.article.mark_read/mark_unread, không ghi vòng; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |
| `reading.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Queue reference theo source; không copy body hay xóa source | Common + dynamic source/provider guards |

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
