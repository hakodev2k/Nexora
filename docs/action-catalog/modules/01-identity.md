# FX-01 — Identity / Profile: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/01-identity-and-profile.md), [UX](../../ux-ui/modules/01-identity-profile.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="identity-account-register"></a>`identity.account.register` — Đăng ký | COMMAND / PUBLIC | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S01 |
| <a id="identity-account-verify"></a>`identity.account.verify` — Xác minh email | COMMAND / PUBLIC | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S02 |
| <a id="identity-account-resend"></a>`identity.account.resend` — Gửi lại xác minh | COMMAND / PUBLIC | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S02 |
| <a id="identity-account-login"></a>`identity.account.login` — Đăng nhập | COMMAND / PUBLIC | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S03 |
| <a id="identity-account-reset-request"></a>`identity.account.reset_request` — Yêu cầu reset password | COMMAND / PUBLIC | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S04 |
| <a id="identity-account-reset-confirm"></a>`identity.account.reset_confirm` — Xác nhận reset password | COMMAND / PUBLIC | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S04 |
| <a id="identity-session-logout"></a>`identity.session.logout` — Đăng xuất hiện tại | COMMAND / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-session-revoke-session"></a>`identity.session.revoke_session` — Thu hồi một session của mình | COMMAND / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-session-revoke-all"></a>`identity.session.revoke_all` — Thu hồi mọi session của mình | COMMAND / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-session-read"></a>`identity.session.read` — Xem session của mình | QUERY / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S06 |
| <a id="identity-profile-read"></a>`identity.profile.read` — Xem profile của mình | QUERY / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S05 |
| <a id="identity-profile-update"></a>`identity.profile.update` — Sửa tên/avatar/timezone | COMMAND / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S05 |
| <a id="identity-profile-change-email"></a>`identity.profile.change_email` — Yêu cầu xác minh email mới | COMMAND / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S05, FX01-S06 |
| <a id="identity-profile-change-password"></a>`identity.profile.change_password` — Đổi password | COMMAND / CONTROL | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX01-S05, FX01-S06 |
| <a id="identity-mfa-enroll"></a>`identity.mfa.enroll` — Đăng ký MFA | COMMAND / CONTROL | No | Blocked | Q-02-R: confirm Google authentication meaning / enabled-MFA recovery | FX01-S06 |
| <a id="identity-mfa-remove"></a>`identity.mfa.remove` — Gỡ MFA | COMMAND / CONTROL | No | Blocked | Q-02-R: confirm Google authentication meaning / enabled-MFA recovery | FX01-S06 |
| <a id="identity-mfa-recover"></a>`identity.mfa.recover` — Dùng recovery proof | COMMAND / CONTROL | No | Blocked | Q-02-R: confirm Google authentication meaning / enabled-MFA recovery | FX01-S06 |
| <a id="identity-account-delete-request"></a>`identity.account.delete_request` — Yêu cầu xóa tài khoản | COMMAND / CONTROL | No | Superseded | Replaced by identity.account.soft_delete; no delayed-purge policy | FX01-S06 |
| <a id="identity-account-soft-delete"></a>`identity.account.soft_delete` — Đánh dấu xóa tài khoản | COMMAND / CONTROL | No | Resolved delegated | DEC-20260907: approved business scope; action/guard Resolved delegated | FX01-S06 |

| Action | Exact guard / effect | Additional prerequisites |
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
| `identity.mfa.enroll` | Optional MFA scope approved; TOTP working interpretation; email fallback when MFA off; do not disable enabled MFA via password reset | Common + dynamic source/provider guards |
| `identity.mfa.remove` | Optional MFA scope approved; TOTP working interpretation; email fallback when MFA off; do not disable enabled MFA via password reset | Common + dynamic source/provider guards |
| `identity.mfa.recover` | Optional MFA scope approved; TOTP working interpretation; email fallback when MFA off; do not disable enabled MFA via password reset | Common + dynamic source/provider guards |
| `identity.account.delete_request` | Never activate historical account purge/grace proposal | Common + dynamic source/provider guards |
| `identity.account.soft_delete` | Own current session + recent-auth5min; explicit data-retention confirmation; IsDeleted=true/State=Deleted, revoke authority, retain data; protect last active SuperAdmin; no self-reactivation | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
