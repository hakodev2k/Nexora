# FX-01 — Identity / Profile: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/01-identity-and-profile.md) — FX-01-BR-001, FX-01-BR-002, FX-01-BR-003, FX-01-BR-004, FX-01-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/01-identity-profile.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `identity` là stable logical key, bind installed ModuleId trong manifest. **Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="identity-account-register"></a>`identity.account.register` — Đăng ký | COMMAND / PUBLIC | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S01 |
| <a id="identity-account-verify"></a>`identity.account.verify` — Xác minh email | COMMAND / PUBLIC | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S02 |
| <a id="identity-account-resend"></a>`identity.account.resend` — Gửi lại xác minh | COMMAND / PUBLIC | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S02 |
| <a id="identity-account-login"></a>`identity.account.login` — Đăng nhập | COMMAND / PUBLIC | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S03 |
| <a id="identity-account-reset-request"></a>`identity.account.reset_request` — Yêu cầu reset password | COMMAND / PUBLIC | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S04 |
| <a id="identity-account-reset-confirm"></a>`identity.account.reset_confirm` — Xác nhận reset password | COMMAND / PUBLIC | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S04 |
| <a id="identity-session-logout"></a>`identity.session.logout` — Đăng xuất hiện tại | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-session-revoke-session"></a>`identity.session.revoke_session` — Thu hồi một session của mình | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-session-revoke-all"></a>`identity.session.revoke_all` — Thu hồi mọi session của mình | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-session-read"></a>`identity.session.read` — Xem session của mình | QUERY / CONTROL | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-profile-read"></a>`identity.profile.read` — Xem profile của mình | QUERY / CONTROL | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX01-S05 |
| <a id="identity-profile-update"></a>`identity.profile.update` — Sửa tên/avatar/timezone | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S05 |
| <a id="identity-profile-change-email"></a>`identity.profile.change_email` — Yêu cầu xác minh email mới | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S05, FX01-S06 |
| <a id="identity-profile-change-password"></a>`identity.profile.change_password` — Đổi password | COMMAND / CONTROL | No | Security (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX01-S05, FX01-S06 |
| <a id="identity-mfa-enroll"></a>`identity.mfa.enroll` — Đăng ký MFA | COMMAND / CONTROL | No | Security (Administrative) | Blocked Q-02 | FX01-S06 |
| <a id="identity-mfa-remove"></a>`identity.mfa.remove` — Gỡ MFA | COMMAND / CONTROL | No | Security (Administrative) | Blocked Q-02 | FX01-S06 |
| <a id="identity-mfa-recover"></a>`identity.mfa.recover` — Dùng recovery proof | COMMAND / CONTROL | No | Security (Administrative) | Blocked Q-02 | FX01-S06 |
| <a id="identity-account-delete-request"></a>`identity.account.delete_request` — Yêu cầu xóa tài khoản | COMMAND / CONTROL | No | Destructive (Administrative) | Blocked Q-01 | FX01-S06 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `identity.account.register` | Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request; generic response, throttle; verify24h/reset30min theo security gate; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.account.verify` | Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request; generic response, throttle; verify24h/reset30min theo security gate; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.account.resend` | Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request; generic response, throttle; verify24h/reset30min theo security gate; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.account.login` | Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request; generic response, throttle; verify24h/reset30min theo security gate; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.account.reset_request` | Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request; generic response, throttle; verify24h/reset30min theo security gate; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.account.reset_confirm` | Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request; generic response, throttle; verify24h/reset30min theo security gate; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.session.logout` | Current principal/session proof; không ảnh hưởng session tài khoản khác; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.session.revoke_session` | Current principal/session proof; không ảnh hưởng session tài khoản khác; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.session.revoke_all` | Current principal/session proof; không ảnh hưởng session tài khoản khác; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.session.read` | Current principal; safe device/time metadata; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.profile.read` | Current principal; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.profile.update` | Giữ email cũ đến khi verify; đổi timezone giữ instants/all-day; thay credential revoke theo security policy; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.profile.change_email` | Giữ email cũ đến khi verify; đổi timezone giữ instants/all-day; thay credential revoke theo security policy; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.profile.change_password` | Giữ email cũ đến khi verify; đổi timezone giữ instants/all-day; thay credential revoke theo security policy; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.mfa.enroll` | Step-up, single-use proof và recovery policy sau duyệt; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.mfa.remove` | Step-up, single-use proof và recovery policy sau duyệt; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.mfa.recover` | Step-up, single-use proof và recovery policy sau duyệt; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |
| `identity.account.delete_request` | Purge/grace/backup residual theo policy chưa chốt; Đúng principal hoặc token một lần; không chấp nhận Role/OwnerId từ request | Common + dynamic source/provider guards |

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
