# FX-03 — Module Platform: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/03-module-platform.md) — FX-03-BR-001, FX-03-BR-002, FX-03-BR-003, FX-03-BR-004, FX-03-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/03-module-platform.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `modules` là stable logical key, bind installed ModuleId trong manifest. **Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="modules-catalog-read"></a>`modules.catalog.read` — Xem installed modules và diagnostics | QUERY / ADMIN | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX03-S01, FX03-S02 |
| <a id="modules-policy-enable"></a>`modules.policy.enable` — Bật module hệ thống | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX03-S02, FX03-S03 |
| <a id="modules-policy-disable"></a>`modules.policy.disable` — Tắt module hệ thống | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX03-S02, FX03-S03 |
| <a id="modules-policy-defaults"></a>`modules.policy.defaults` — Đổi module mặc định lúc verify | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX03-S04 |
| <a id="modules-policy-sharing"></a>`modules.policy.sharing` — Đổi sharing policy của module | COMMAND / SUPER | No | Security (Administrative) | Resolved action; existing-link disable effects remain Blocked Q-03 | FX03-S05 |
| <a id="modules-policy-settings"></a>`modules.policy.settings` — Sửa cấu hình hệ thống của module | COMMAND / SUPER | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX03-S05 |
| <a id="modules-upgrade-read"></a>`modules.upgrade.read` — Xem upgrade preflight | QUERY / ADMIN | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX03-S02 |
| <a id="modules-runtime-register"></a>`modules.runtime.register` — Đăng ký trusted manifest/action keys | WORKER / SYSTEM | No | Operational (Administrative) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="modules-runtime-migrate"></a>`modules.runtime.migrate` — Áp dụng migration đã được duyệt | WORKER / SYSTEM | No | Operational (Administrative) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |
| <a id="modules-runtime-health"></a>`modules.runtime.health` — Đánh giá readiness/dependency | WORKER / SYSTEM | No | Operational (Administrative) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `modules.catalog.read` | Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.policy.enable` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.policy.disable` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.policy.defaults` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.policy.sharing` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.policy.settings` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.upgrade.read` | Deployment metadata/compatibility/checksum; không chạy migration từ UI; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.runtime.register` | Trusted deployment/health handler only; không cấp cho Admin/User qua permission matrix; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.runtime.migrate` | Trusted deployment/health handler only; không cấp cho Admin/User qua permission matrix; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.runtime.health` | Trusted deployment/health handler only; không cấp cho Admin/User qua permission matrix; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |

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
