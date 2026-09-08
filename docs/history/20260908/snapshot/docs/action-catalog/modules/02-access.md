# FX-02 — Users / Roles / Permissions: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/02-users-roles-and-permissions.md) — FX-02-BR-001, FX-02-BR-002, FX-02-BR-003, FX-02-BR-004, FX-02-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/02-users-roles-permissions.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `access` là stable logical key, bind installed ModuleId trong manifest. **SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="access-user-read"></a>`access.user.read` — Xem metadata danh sách/chi tiết User | QUERY / ADMIN | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX02-S01, FX02-S02 |
| <a id="access-permission-read"></a>`access.permission.read` — Xem catalog và effective permissions | QUERY / ADMIN | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX02-S04 |
| <a id="access-role-set"></a>`access.role.set` — Gán/gỡ role | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX02-S03 |
| <a id="access-permission-set"></a>`access.permission.set` — Allow/Deny action của Admin | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX02-S04 |
| <a id="access-entitlement-set"></a>`access.entitlement.set` — Bật/tắt module tài khoản | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX02-S05 |
| <a id="access-user-disable"></a>`access.user.disable` — Disable tài khoản | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX02-S02 |
| <a id="access-user-enable"></a>`access.user.enable` — Enable tài khoản | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX02-S02 |
| <a id="access-user-revoke-sessions"></a>`access.user.revoke_sessions` — Thu hồi session target | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX02-S02 |
| <a id="access-change-read"></a>`access.change.read` — Xem preview thay đổi quyền | QUERY / SUPER | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX02-S03, FX02-S04, FX02-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `access.user.read` | SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.permission.read` | SuperAdmin trong grant editor; Admin chỉ own effective rights hoặc target metadata đã có access.user.read; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.role.set` | SuperAdmin-only; preview diff; không mất last active SuperAdmin; audit và current revision; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.permission.set` | SuperAdmin-only; preview diff; không mất last active SuperAdmin; audit và current revision; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.entitlement.set` | SuperAdmin-only; preview diff; không mất last active SuperAdmin; audit và current revision; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.user.disable` | SuperAdmin-only; preview diff; không mất last active SuperAdmin; audit và current revision; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.user.enable` | SuperAdmin-only; preview diff; không mất last active SuperAdmin; audit và current revision; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.user.revoke_sessions` | SuperAdmin-only; target rõ ràng, audit; không impersonate; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |
| `access.change.read` | SuperAdmin-only; preview không mutation, commit phải check lại; SuperAdmin hoặc Admin với operational grant cụ thể; không private business payload | Common + dynamic source/provider guards |

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
