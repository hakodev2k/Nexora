# Identity, Registration và Profile

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../history/20260908/snapshot/docs/features/01-identity-and-profile.md) is historical evidence, not implementation input.

FX-01 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Đăng ký Public SaaS, email verification, login/logout/reset, profile và Personal Space.

[Auth0](https://auth0.com/docs/manage-users/user-accounts/verify-emails): Email verification trước khi sử dụng các chức năng yêu cầu tài khoản xác minh.

**Áp dụng cho Nexora:** Email xác minh rồi sử dụng ngay, không Admin approval; mỗi User cá nhân riêng, không tạo Workspace.

**Màn hình:** `/register, /verify-email, /login, /settings/profile, /settings/security`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Đăng ký email/password → PendingVerification + verification intent; chưa tạo Personal Space trước xác minh.
2. Xác minh token qua explicit POST → Active; tạo đúng một PersonalSpace và default grants cho installed/Ready/active-scope modules atomically; sang Login dùng ngay, không Admin approval. Paused/uninstalled không bật.
3. Login → session; profile đổi tên/avatar/timezone; reset password qua email với kết quả không tiết lộ account tồn tại.
4. Logout session hiện tại hoặc revoke all sessions; account deletion = soft-delete; restore/email reuse và export portability là scope riêng chưa duyệt.

## Dữ liệu và validation

- Email chuẩn hóa lookup, unique theo identity policy; display name 1–100; password theo security policy nguồn, không log.
- UserId/PersonalSpaceId server-generated; status PendingVerification/Active/Disabled/Deleted; IsDeleted đồng nhất Deleted; emailVerifiedAt server-only.
- Timezone IANA nhận từ browser và User đổi được; avatar qua File Service. Locale vi mặc định, User chọn en; độc lập timezone/currency.

## Hành vi và lifecycle

- **FX-01-BR-001:** Verify token single-use, resend vô hiệu token trước; TTL24h, resend60s là delegated defaults cần security gate.
- **FX-01-BR-002:** Reset token30min, single-use; password reset revoke sessions và gửi Security notification all 3. Không tự đăng nhập từ reset link.
- **FX-01-BR-003:** PendingVerification chỉ verify/resend/logout/help; direct module API bị chặn.
- **FX-01-BR-004:** Đổi email cần verify email mới, giữ email cũ cho tới thành công, thông báo cả địa chỉ phù hợp mà không lộ secret.
- **FX-01-BR-005:** Không phân biệt lỗi account tồn tại ở forgot-password; throttle server-side; Google Authenticator TOTP optional đã chốt; lost-device recovery theo P-H01, passkey/social login ngoài scope.

## Quyền, API và tích hợp

- Register/VerifyEmail/ResendVerification/Login/Logout/RevokeSessions/ResetPassword/UpdateProfile.
- Provision Personal Space idempotent; account status kiểm tra ở API/jobs; module registration policy snapshot audit.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-01-AC-001:** Retry verify không tạo Personal Space thứ hai.
- **FX-01-AC-002:** Unverified/Disabled User gọi Task API bị deny.
- **FX-01-AC-003:** Đổi timezone giữ instant timed events, đổi giờ hiển thị; không đổi all-day date.
- **FX-01-AC-004:** Reset link replay không đổi password lần nữa.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [03-security-and-privacy.md](../requirements/03-security-and-privacy.md): `AUTH-001`, `AUTH-002`, `AUTH-003`, `AUTH-004`, `AUTH-005`, `AUTH-006`, `AUTH-007`, `AUTH-008`, `AUTH-009`, `AUTH-010`
- [08-workspaces-and-collaboration.md](../requirements/08-workspaces-and-collaboration.md): `PDS-OWN-001`, `PDS-OWN-002`, `PDS-OWN-003`, `PDS-OWN-004`, `PDS-OWN-005`, `PDS-OWN-006`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-AUT-001`, `P01-AUT-002`, `P01-AUT-003`, `P01-AUT-004`, `P01-AUT-005`, `P01-AUT-006`, `P01-AUT-007`, `P01-AUT-008`, `P01-AUT-009`, `P01-AUT-010`, `P01-AUT-011`, `P01-AUT-012`, `P01-PDS-001`, `P01-USR-001`, `P01-USR-002`, `P01-USR-003`, `P01-USR-004`, `P01-USR-005`

Chỉ account restore/email reuse và lost-device MFA recovery còn proposal; [M01](../delivery/milestone-01/README.md) password/email/profile không bị chặn bởi các phần đó.

## Current language and account lifecycle

UI language default vi regardless of browser locale; Settings explicitly switches en/vi, persists per account and updates all app-owned UI/notifications at render/composition time without translating user content. Timezone and currency independent. Deleted account cannot login/reset into active status; retention disclosed before account soft delete. Q02 method confirmed: optional Google Authenticator TOTP (DEC-20260908-Q02-TOTP); enabled-MFA recovery remains pending. No Google OAuth flow added.
