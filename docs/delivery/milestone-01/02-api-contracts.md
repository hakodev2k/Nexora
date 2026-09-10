# M01 API contract — version 1, design only

**Resolved delegated technical specification.** [OpenAPI](openapi.json) là máy đọc được; chưa có API chạy. Khi schema và prose lệch nhau phải sửa trước implementation, không chọn tùy tiện. Mọi path bắt đầu `/api/v1`; route UI không là API.

## Transport, authentication và data representation

- Same-origin HTTPS. Cookie `__Host-NexoraSession`: Secure, HttpOnly, Path=/, SameSite=Lax, không Domain; raw random256-bit handle, DB chỉ SHA256 digest. No bearer token JSON. Idle30min, absolute12h, recent-auth5min; SQL session/account recheck từng protected request. Không Remember me trong M01.
- GET `/auth/csrf` tạo anti-forgery cookie protected HttpOnly Secure SameSite=Strict và trả request token. Unsafe methods gửi `X-CSRF-Token`, kể cả login/register/preview. Validate exact Origin và anti-forgery pair; missing/invalid403. Rotate sau login; tokens chỉ ở memory, response no-store. CSRF token không cấp user authority.
- Password15–128 Unicode codepoints, không trim/truncate/composition rule; từ chối common-password offline blocklist versioned. Technical choice: ASP.NET Identity versioned PasswordHasher PBKDF2-HMAC-SHA512 tối thiểu220000 iterations, unique salt; benchmark và nâng cost/rehash trước rollout, không custom password crypto. [OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html), [authentication guidance](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html).
- Email trim + invariant-case normalized lookup; domain IDNA normalized; không Gmail dot/plus rewriting. Display original. UQ gồm cả Deleted. Timezone nhận IANA; browser không xác định thì form yêu cầu chọn, không suy từ vi. Locale enum vi/en defaultvi.
- GUID opaque; UTC RFC3339 `Z`; enum codes English stable; UI translate vi/en. SQL bigint policyRevision travels as a decimal string to avoid JavaScript integer precision loss. Unknown request properties422, malformed JSON400; request body≤64KiB. Nullable chỉ khi schema ghi. No client OwnerId/Role/security field ngoài dedicated admin command.
- Protected responses `Cache-Control: no-store`; cookies/token/password never logs, analytics, URL query hay exception. Verify/reset email link dùng fragment token, form lấy vào memory và xóa fragment trước request; không auto-consume bằng GET/email scanner. Browser không redirect ra website khác.
- Rate limit technical defaults: register/resend/reset5 submissions/15min per normalized-email hash +20/IP/15min; resend cooldown60s; login/reauth10 failures/15min per subject-hash+IP plus100/IP/15min. 429+Retry-After; unknown subject cùng path/counter shape. Không log full email/credentials trong throttle keys.

## Retry và concurrency

PATCH/PUT nghiệp vụ bắt buộc strong `If-Match` từ resource/aggregate GET; thiếu428, stale412. ETag opaque quoted base64 SQL rowversion hoặc aggregate stamp do server sinh; client không suy numeric. Admin access ETag dựa target User.RowVersion, mọi role/grant change phải touch target User row. Profile changes cũng làm stale access preview, chấp nhận conservative conflict.

`Idempotency-Key` UUID bắt buộc cho register/verify/resend/reset request/reset confirm, profile update và admin commit. Giữ24h, scope = current subject (public dùng anonymous anti-forgery session hash) + operation + key. Store keyed request digest, never raw password/token; hash key outside SQL. Same key/different body409. Same key/body replay original safe result sau current authority check; không thực hiện side effect lần hai. Expired key có thể nhận fresh semantic conflict. Login/reauth không replay credentials; logout/revoke intrinsically idempotent, không replay cookie issuance. No-store cached command result; replay mutation403 nếu quyền của chính operation đã revoke. Với FX25-S03 Favorites, việc source được tham chiếu bị disable, archived, trashed hoặc mất source-read không được coi là revoke quyền Favorites: sau khi kiểm tra lại quyền operation/module và source hiện tại, replay có thể trả status gốc với projection an toàn `State=Unavailable`, không trả title/content cũ. Nếu quyền/module Favorites hiện tại không còn hợp lệ thì trả problem hiện tại và không replay body đã lưu. Preview không ghi mutation hoặc consume idempotency key.

Admin preview: `access.change.read` hoặc authorized module read trả diff, blockers và signed `previewToken` TTL2min, bind actor/session/target/bodyDigest/resourceRevision/modulePolicyRevisions. Commit gửi **cùng changes**+previewToken+If-Match, recheck proof/revisions/authority trong SQL transaction; changed dependency409 PreviewStale. Preview không là grant. Batch≤100 changes, duplicate keys422; all-or-nothing; không tự bật prerequisites/cascade dependents.

## Endpoint / request / result

`PUBLIC` vẫn có CSRF/rate limits. `SELF` gồm CONTROL/profile hoặc exact settings action cho locale. `SUPER` luôn fresh auth cho write, không cho Admin tự gọi. GET admin metadata có operational grant theo catalog, riêng getAccess/preview/commit dành SuperAdmin trong M01.

| operationId | HTTP path (prefix omitted) | Request schema | Success | Story / context |
| --- | --- | --- | --- | --- |
| getCsrf | GET /auth/csrf | none |200 Csrf | S03 PUBLIC |
| register | POST /auth/registrations | Registration |202 Accepted | S02 PUBLIC |
| verify | POST /auth/verifications | TokenProof |200 Verification | S02 PUBLIC |
| resendVerification | POST /auth/verifications/resend | EmailRequest |202 Accepted | S02 PUBLIC |
| login | POST /auth/login | Credentials |200 Login + Set-Cookie | S03 PUBLIC |
| logout | POST /auth/logout | none |204, clear cookie | S03 current session; absent already204 |
| requestReset | POST /auth/password-resets | EmailRequest |202 Accepted | S04 PUBLIC |
| confirmReset | POST /auth/password-resets/confirm | ResetProof |204 | S04 PUBLIC |
| reauth | POST /auth/reauth | PasswordProof |204; refresh RecentAuthenticatedAt | S03 SELF |
| getMe | GET /me | none |200 Profile + ETag | S05 SELF |
| updateMe | PATCH /me | ProfilePatch (at least1field) |200 Profile + new ETag | S05 SELF, semantic field permissions |
| listSessions | GET /me/sessions | limit/cursor |200 SessionPage | S06 SELF |
| revokeSession | DELETE /me/sessions/{sessionId} | none |204 | S06 SELF |
| revokeAll | POST /me/sessions/revoke-all | none |204, clear cookie | S06 SELF |
| listUsers | GET /admin/users | query/limit/cursor |200 UserPage | S07 metadata grant |
| getAccess | GET /admin/users/{userId}/access | none |200 Access + ETag | S07 SUPER |
| previewAccess | POST /admin/users/{userId}/access/preview | AccessChanges |200 Preview | S08 SUPER |
| setRole | PUT /admin/users/{userId}/access/role | RoleCommit |200 Access + ETag | S08 SUPER |
| setPermissions | PUT /admin/users/{userId}/access/permissions | PermissionCommit |200 Access + ETag | S08 SUPER |
| setEntitlements | PUT /admin/users/{userId}/access/modules | EntitlementCommit |200 Access + ETag | S08 SUPER |
| listModules | GET /admin/modules | limit/cursor |200 ModulePage | S07 catalog grant |
| previewModule | POST /admin/modules/{moduleId}/preview | ModuleChanges |200 Preview | S09 SUPER |
| setModulePolicy | PUT /admin/modules/{moduleId}/policy | ModuleCommit |200 Module + ETag | S09 SUPER |

## Exact field/effect rules

- Registration: email/password/timeZoneId required; displayName optional default email local-part truncated100 server-side; do not expose to others publicly. Locale initialvi. 202 for known email does not reset account/send unbounded mail. Fresh pending row/token/email intent; PersonalSpace only on verify. Verify24h; token consumed transactionally; no auto-login from verify. UI leads Login immediately, no Admin approval.
- Login200 only Active/verified/notDeleted/no enabled MFA in M01. Bad user/password generic401 InvalidCredentials; correct password with Pending403 EmailVerificationRequired, Disabled/Deleted403 AccountUnavailable; enabled MFA403 MfaUnavailable without issuing session. This branch occurs after correct password, not email enumeration. Password check constant-time equivalent dummy hash for unknown user.
- Password reset30min; generic202 request whether account exists. Consuming reset updates hash/security stamp + revokes sessions, never undeletes/enables account, never removes MFA; enabled MFA409 MfaRecoveryRequired, no token consumption. Token invalid/replayed410 TokenUnavailable (same wording unknown/expired). No “email Sent” until actual delivery acknowledgment.
- Profile response exact fields: id/email/displayName/timeZoneId/locale/state/personalSpaceId/modules[]. No passwordHash/securityStamp/role edit capability. Each module entry code/enabled/unavailableReason; only own effective navigation projection. Disabled settings module hides locale editing via `settings.preference.update`; identity name/timezone CONTROL remains usable. Changed locale requires that action, even bundled with name.
- Session projection id/deviceLabel/createdAt/lastSeenAt/expiresAt/isCurrent. No IP/token/hash. Revoked sessions omitted; revoke owned already-revoked204, unknown or another owner404. RevokeAll includes current. List bounded default25/max100, nextCursor nullable; sessions order createdAt desc then Id, users normalizedEmail+Id, modules Code+Id. Cursor signed, binds subject/filter/sort; bad422; current authorization always before counts/page. No global total counts needed.
- Access response userId/role/permissions[{actionKey,effect}]/modules[{moduleId,enabled}]/etag. Effects Allow/Deny/Unset, Unset deletes explicit row. Every account retains base User role; role is effective highest role. User/Admin/SuperAdmin change sets elevated membership explicitly, never custom roles. Last active SuperAdmin protected even two commands race. Own role downgrade may revoke current access immediately after commit; safe command receipt allowed, subsequent read denied.
- Module response id/code/state/systemEnabled/registrationEnabled/policyRevision/etag. Module policy only two editable booleans in M01; sharingEnabled/settings/migration/version are rejected. Enable only installed+current scope+compatible migration/health/dependencies; State Ready or Disabled after readiness validation. Paused/Blocked action cannot be granted Allow. Disable does not delete data or revive already-invalidated links. If module sharing is disabled in later milestone, its own contract invalidates old links permanently.
- Admin commit normally returns200 Access; if the committed change removes caller authority to read that projection, return204 without content instead. Client clears target cache and re-evaluates navigation; do not leak a pre-change privileged DTO or pretend the successful mutation failed. OpenAPI action-key arrays on profile/module composite operations list candidate keys; semantic field diff determines which are required.
- Public register/verify/login are platform control surfaces, not Admin-grantable actions. Capability gate never interprets a raw URL/action string as executable handler.

## Errors — RFC9457

All errors `application/problem+json`: type (stable relative `/problems/{code}`), title (safe localized), status, code, traceId required; errors optional map JSON field→list of stable codes. Never raw input, stack or existence-sensitive title. HTTP status and body.status must agree.

| Status | Codes | Client behavior |
| --- | --- | --- |
|400|MalformedRequest|Keep form; show safe invalid request|
|401|AuthenticationRequired, InvalidCredentials|Clear protected cache on expired session, go Login|
|403|CsrfInvalid, PermissionDenied, AccountUnavailable, EmailVerificationRequired, MfaUnavailable, StepUpRequired|No bypass; step-up then retry original edit after proof|
|404|ResourceUnavailable|Safe Back; no owner/title leak|
|409|IdempotencyConflict, PreviewStale, LastSuperAdmin, DecisionBlocked, DependencyEnabled, DependencyUnavailable, MfaRecoveryRequired|Keep draft; explain exact safe blocker; regenerate preview if stale|
|410|TokenUnavailable|Offer resend/reset request, no auto consume|
|412|RevisionConflict|Fetch current snapshot; compare/reapply explicit; never force overwrite|
|422|ValidationFailed, UnknownField|Focus first field, summary links; keep safe values|
|428|PreconditionRequired|Fetch revision then retry after user review|
|429|RateLimited|Retry-After seconds and disabled submit countdown|
|503|ServiceUnavailable|No fake empty/success; retry explicitly|

### Example — registration accepted

```json
{"email":"user@example.test","password":"example-long-password","timeZoneId":"Asia/Ho_Chi_Minh","displayName":"User"}
```

Response202 (same for an existing account):

```json
{"status":"Accepted","messageCode":"CheckEmailIfEligible"}
```

### Example — optimistic profile update

PATCH `/api/v1/me`, If-Match: `"AAAAAAAAAAE="`, Idempotency-Key: `550e8400-e29b-41d4-a716-446655440000`, valid CSRF. Body:

```json
{"displayName":"Hà","locale":"en"}
```

If another tab saved first,412:

```json
{"type":"/problems/RevisionConflict","title":"Dữ liệu đã thay đổi","status":412,"code":"RevisionConflict","traceId":"example-trace"}
```

### Example — permission preview/commit

Preview body contains exactly one change kind:

```json
{"kind":"permissions","changes":[{"actionKey":"access.user.read","effect":"Allow"}]}
```

Preview response200:

```json
{"previewToken":"opaque-example","expiresAt":"2026-09-08T10:02:00Z","etag":"\"AAAAAAAAAAE=\"","changes":[{"field":"access.user.read","before":"Unset","after":"Allow"}],"blockers":[]}
```

Commit same kind/changes plus previewToken, correct If-Match and idempotency; unknown/blocked keys409, changed body409 PreviewStale. ETag/examples are synthetic and do not identify real users. Success response current Access projection as defined above; no personal business DTO.
