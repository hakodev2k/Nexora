# FX-09 — Settings / Shell: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/09-settings-and-app-shell.md) — FX-09-BR-001, FX-09-BR-002, FX-09-BR-003, FX-09-BR-004, FX-09-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/09-settings-app-shell.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `settings` là stable logical key, bind installed ModuleId trong manifest. **Own nonsensitive preferences; no arbitrary JSON business data**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="settings-preference-read"></a>`settings.preference.read` — Xem preferences/navigation | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX09-S01, FX09-S04 |
| <a id="settings-preference-update"></a>`settings.preference.update` — Lưu theme/nav/display preferences | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX09-S02 |
| <a id="settings-module-read"></a>`settings.module.read` — Xem module settings | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX09-S03 |
| <a id="settings-module-update"></a>`settings.module.update` — Lưu owner module settings | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX09-S03 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `settings.preference.read` | Own nonsensitive preferences; no arbitrary JSON business data; Own nonsensitive preferences; no arbitrary JSON business data | Common + dynamic source/provider guards |
| `settings.preference.update` | Không đổi quyền/module grants; approved default views giữ nguyên; Own nonsensitive preferences; no arbitrary JSON business data | Common + dynamic source/provider guards |
| `settings.module.read` | Kèm target module/action; không plaintext credentials; Own nonsensitive preferences; no arbitrary JSON business data | Common + dynamic source/provider guards |
| `settings.module.update` | Target declared setting action required; không system policy/self-grant; Own nonsensitive preferences; no arbitrary JSON business data | Common + dynamic source/provider guards |

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
