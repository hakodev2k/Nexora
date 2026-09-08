# FX-03 — Module Platform: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/03-module-platform.md), [UX](../../ux-ui/modules/03-module-platform.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="modules-catalog-read"></a>`modules.catalog.read` — Xem installed modules và diagnostics | QUERY / ADMIN | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX03-S01, FX03-S02 |
| <a id="modules-policy-enable"></a>`modules.policy.enable` — Bật module hệ thống | COMMAND / SUPER | No | Resolved delegated | DEC-20260907-Q03/Q06/Q07; technical scope gate | FX03-S02, FX03-S03 |
| <a id="modules-policy-disable"></a>`modules.policy.disable` — Tắt module hệ thống | COMMAND / SUPER | No | Resolved delegated | DEC-20260907-Q03/Q06/Q07; technical scope gate | FX03-S02, FX03-S03 |
| <a id="modules-policy-defaults"></a>`modules.policy.defaults` — Đổi module mặc định lúc verify | COMMAND / SUPER | No | Resolved delegated | DEC-20260907-Q03/Q06/Q07; technical scope gate | FX03-S04 |
| <a id="modules-policy-sharing"></a>`modules.policy.sharing` — Đổi sharing policy của module | COMMAND / SUPER | No | Resolved delegated | DEC-20260907-Q03/Q06/Q07; technical scope gate | FX03-S05 |
| <a id="modules-policy-settings"></a>`modules.policy.settings` — Sửa cấu hình hệ thống của module | COMMAND / SUPER | No | Resolved delegated | DEC-20260907-Q03/Q06/Q07; technical scope gate | FX03-S05 |
| <a id="modules-upgrade-read"></a>`modules.upgrade.read` — Xem upgrade preflight | QUERY / ADMIN | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX03-S02 |
| <a id="modules-runtime-register"></a>`modules.runtime.register` — Đăng ký trusted manifest/action keys | WORKER / SYSTEM | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment only |
| <a id="modules-runtime-migrate"></a>`modules.runtime.migrate` — Áp dụng migration đã được duyệt | WORKER / SYSTEM | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment only |
| <a id="modules-runtime-health"></a>`modules.runtime.health` — Đánh giá readiness/dependency | WORKER / SYSTEM | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment only |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `modules.catalog.read` | Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.policy.enable` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; paused FX30/34/35 cannot be enabled by grant/defaults; sharing disable increments epoch and permanently invalidates old links | Common + dynamic source/provider guards |
| `modules.policy.disable` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; paused FX30/34/35 cannot be enabled by grant/defaults; sharing disable increments epoch and permanently invalidates old links | Common + dynamic source/provider guards |
| `modules.policy.defaults` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; paused FX30/34/35 cannot be enabled by grant/defaults; sharing disable increments epoch and permanently invalidates old links | Common + dynamic source/provider guards |
| `modules.policy.sharing` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; paused FX30/34/35 cannot be enabled by grant/defaults; sharing disable increments epoch and permanently invalidates old links | Common + dynamic source/provider guards |
| `modules.policy.settings` | SuperAdmin-only; preview dependency/affected users; không tự đổi existing grants khi sửa defaults; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge; paused FX30/34/35 cannot be enabled by grant/defaults; sharing disable increments epoch and permanently invalidates old links | Common + dynamic source/provider guards |
| `modules.upgrade.read` | Deployment metadata/compatibility/checksum; không chạy migration từ UI; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.runtime.register` | Trusted deployment/health handler only; không cấp cho Admin/User qua permission matrix; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.runtime.migrate` | Trusted deployment/health handler only; không cấp cho Admin/User qua permission matrix; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |
| `modules.runtime.health` | Trusted deployment/health handler only; không cấp cho Admin/User qua permission matrix; Installed manifest hợp lệ, dependency/version được kiểm tra; disable không purge | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
