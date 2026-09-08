# M01 — UX interaction and acceptance mapping

Existing screen IDs are retained; [screen inventory](../../ux-ui/screen-inventory.md) and shared [forms](../../ux-ui/global/05-forms-and-validation.md) remain the design language. This slice adds exact behavior for M01 API, not a second UI system. Route proposals are not implemented pages.

| Screen / entry → exit | Primary interaction | Data and validation | Failure / read-only / empty | API / acceptance |
| --- | --- | --- | --- | --- |
| FX01-S01 `/register`, Login→Register→Check email | Email/password/timezone; optional display name; Register | Mark required; browser timezone visible/editable; email generic acceptance | Keep safe input on422; pending submit disables duplicate;202 does not claim Delivered; back Login | register, M01-AC02 |
| FX01-S02 `/verify-email`, email link or resend→Login | Explicit Verify; resend email | Token from fragment removed immediately; no GET consume | Expired/replayed same message; resend cooldown; never show guessed account state | verify/resendVerification, AC02 |
| FX01-S03 `/login`, protected route→Login→Home | Email/password/Login; Forgot password | Return path allowlisted internal path, no arbitrary URL | Generic credential error;403 correct-password unverified shows verify flow; MFA unavailable does not fall through;401 clear caches | login/getCsrf, AC03 |
| FX01-S04 `/forgot-password` and `/reset-password`, Login→request→new password→Login | Request email / set new password+confirm | Confirmation local-only; only newPassword/token submitted | Generic202; expired proof410; mismatch inline; no auto-login or MFA removal | requestReset/confirmReset, AC04 |
| FX01-S05 `/settings/profile`, avatar/menu→profile→back | Explicit Save name/timezone; link language settings | Email read-only in M01, no avatar upload/action placeholder | Dirty Save/Discard/Keep editing dialog.412 compares current values; no force Save; identity fields cannot edit rights | getMe/updateMe, AC05 |
| FX09-S02 Settings language, profile/settings→language→back | vi/en dropdown, Save | Shows current saved locale; changes app-owned copy only | Settings action denied → read-only value and reason, not hidden silent failure | updateMe locale diff, AC05 |
| FX01-S06 `/settings/security`, settings→security→back | Sessions list; revoke one/all; recent-auth dialog | Device/time metadata only, current badge | Empty historical list not empty active session; current revoke navigates Login; lost-factor/MFA enrollment shown as unavailable in internal M01, never “configured” | listSessions/revokeSession/revokeAll/reauth, AC03/06 |
| FX02-S01/S02 `/admin/users`, Admin→list→detail→list | Search email/name; select user | Table email/displayName/state/role; page25 | Loading skeleton ≠ no users; fetch503 retry; Admin403 clears data; no personal domain tabs | listUsers/getAccess, AC07 |
| FX02-S03/S04/S05 user role/permissions/modules tabs | Edit explicit changes→Preview→Confirm | Diff before/after, target, dependencies/blockers; no auto prerequisites | Blocking rows explain reason; signed preview2min; stale412/409 keeps draft and requires new preview; last SA cannot be removed | previewAccess/setRole/setPermissions/setEntitlements, AC08 |
| FX03-S01..S04 `/admin/modules`, catalog→module→policy/defaults | System enable toggle/default flag→preview→confirm | Module health/dependency/version metadata; default effect “new verifications only” | Paused/uninstalled/failed disabled; dependency blocker explicit; no executable upload/upgrade button wired to runtime | listModules/previewModule/setModulePolicy, AC09 |

## Dialog contracts

- Revoke session: “Thu hồi phiên đăng nhập?”; show sanitized device/time, current-session warning; confirm/cancel; error stays dialog; success removes row or logs out. Revoke-all explicit “bao gồm phiên hiện tại”; focus returns only if session remains valid.
- Permission/module commit: title names target; semantic diff, affected dependencies, current revision and blocking reasons; primary Confirm disabled with blockers; Escape/cancel no write.409/412 reload preview after user review, never automatically resubmit changed diff.
- Recent authentication: password only in M01, no persistent storage or modal background state leak; cancel aborts original command; valid proof resumes preview flow with current revision. MfaUnavailable fixture explains unsupported factor, no bypass.
- Dirty form: Save/Discard/Keep editing; Save waits verified result;401 clears protected payload and asks re-login, never serializes secrets into URL/localStorage. Ordinary profile drafts may remain only in current memory if still authorized.

## Common states and responsive access

Desktop: shared global sidebar + page header + form/table, optional detail panel. Tablet collapses nav, detail drawer with explicit Back. Mobile uses stacked list/detail, priority columns email/name/state with remaining fields in detail; actions persist as labeled buttons, no hover-only controls. Forms one column, sticky Save cannot obscure field errors or keyboard. Empty list offers appropriate entry only if authorized; no-result message includes Clear filters; module unavailable/permission denied/provider failure are distinct.

Keyboard: tab order heading→fields→primary→secondary; Enter submits only appropriate form, Escape closes cancelable dialogs; focus trapped/restored; errors linked via labels/descriptions and announced; status uses text/icon, not color alone. Preview diff is accessible table. All actions can be performed without dragging. Reduced motion respected; dialogs/overlays don't create navigation dead ends.

## Cross-layer review

For each M01-ACxx run UI and direct API variant against actual SQL after implementation approval; browser hide/disable is never sufficient. Capture screenshots for desktop/mobile, keyboard trace for login/form/permission dialog, response schema validation, and SQL/Audit assertions. No screenshot/runtime evidence exists yet; [readiness](06-readiness-and-evidence.md) records this explicitly.
