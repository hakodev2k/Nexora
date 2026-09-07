# FX-01 — Identity / Registration / Profile — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Register, verify, authenticate and manage own account; account deletion/MFA recovery stay gated.

## 2. Requirement sources

- [FX-01 feature](../../features/01-identity-and-profile.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-01-BR-001`, `FX-01-BR-002`, `FX-01-BR-003`, `FX-01-BR-004`, `FX-01-BR-005`, `FX-01-AC-001`, `FX-01-AC-002`, `FX-01-AC-003`, `FX-01-AC-004`, `P01-AUT-001`, `P01-AUT-002`, `P01-AUT-003`, `P01-AUT-004`, `P01-AUT-005`, `P01-AUT-006`, `P01-AUT-007`, `P01-AUT-008`, `P01-AUT-009`, `P01-AUT-010`, `P01-AUT-011`, `P01-AUT-012`, `P01-PDS-001`, `P01-USR-001`, `P01-USR-002`, `P01-USR-003`, `P01-USR-004`, `P01-USR-005`

## 3. Reference products

- [Auth0](https://auth0.com/docs/manage-users/user-accounts/verify-emails) — checked2026-09-07. Evidence limit: Official documentation text, not authenticated UI test.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Auth0](https://auth0.com/docs/manage-users/user-accounts/verify-emails) | Email verification can be a link sent to the supplied address. | ADAPT | Nexora requires verification before module access; no bulk verification, social login or Auth0 dependency inferred. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX01-S01 — Register: FORM profile.
- FX01-S02 — Verify email: DETAIL profile.
- FX01-S03 — Login: FORM profile.
- FX01-S04 — Forgot / Reset password: FORM profile.
- FX01-S05 — Profile: FORM profile.
- FX01-S06 — Security and sessions: BROWSE profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX01-S01 | Register | /register | Email; password strength/help; verification disclosure | Create account |
| FX01-S02 | Verify email | /verify-email | Masked destination; sent state; resend retry timer; token outcome | Verify / Resend email |
| FX01-S03 | Login | /login | Email; Password; visibility toggle | Log in |
| FX01-S04 | Forgot / Reset password | /password/forgot; /password/reset | Email request or new password + confirmation | Send reset link / Reset password |
| FX01-S05 | Profile | /settings/profile | DisplayName; Timezone; account email readout | Save profile |
| FX01-S06 | Security and sessions | /settings/security | Current-session label; device; last seen; expiry; security methods policy status | Review sessions |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Register → verification pending → open verification link → activate → Home; returning User Login → intended authorized route.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX01-S01 — Register

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Email; password strength/help; verification disclosure |
| Entry / proposed route | /register; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Register. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Create account; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Log in. Back/Cancel always has authorized fallback. |
| Content regions / fields | Email; password strength/help; verification disclosure |
| Search / filters / sorting / pagination | No filters/search/pagination. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Success opens verification-pending, not module UI; generic duplicate-address response avoids enumeration. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Register' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX01-S02 — Verify email

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Masked destination; sent state; resend retry timer; token outcome |
| Entry / proposed route | /verify-email; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Verify email. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Verify / Resend email; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Change email; Log in. Back/Cancel always has authorized fallback. |
| Content regions / fields | Masked destination; sent state; resend retry timer; token outcome |
| Search / filters / sorting / pagination | No list controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Open-link landing confirms intent before token consume; expired/used link offers safe resend. Activation idempotent. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Verify email' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX01-S03 — Login

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Email; Password; visibility toggle |
| Entry / proposed route | /login; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Login. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Log in; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Forgot password; Register. Back/Cancel always has authorized fallback. |
| Content regions / fields | Email; Password; visibility toggle |
| Search / filters / sorting / pagination | No list controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unverified routes to Verify; safe same-origin return target only, no arbitrary external redirect. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Login' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX01-S04 — Forgot / Reset password

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Email request or new password + confirmation |
| Entry / proposed route | /password/forgot; /password/reset; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Forgot / Reset password. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Send reset link / Reset password; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back to Login. Back/Cancel always has authorized fallback. |
| Content regions / fields | Email request or new password + confirmation |
| Search / filters / sorting / pagination | No list controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Request response generic; invalid/expired token no password form submission; successful reset revokes affected sessions by security policy. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Forgot / Reset password' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX01-S05 — Profile

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — DisplayName; Timezone; account email readout |
| Entry / proposed route | /settings/profile; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Profile. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save profile; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; Security. Back/Cancel always has authorized fallback. |
| Content regions / fields | DisplayName; Timezone; account email readout |
| Search / filters / sorting / pagination | No list controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Changing timezone keeps timed instants and all-day dates; preview one example before Save; roles absent. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Profile' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX01-S06 — Security and sessions

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Current-session label; device; last seen; expiry; security methods policy status |
| Entry / proposed route | /settings/security; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Security and sessions. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Review sessions; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Revoke selected session; Revoke others; Account deletion entry. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current-session label; device; last seen; expiry; security methods policy status |
| Search / filters / sorting / pagination | Session device search; active/revoked filter; LastSeen DESC. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Revoke confirms affected sessions; current revoke logs out. MFA/recovery/deletion controls marked policy pending until Q-01/Q-02, not faux implemented. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Security and sessions' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Register: Email and Password required; never Role/OwnerId. Profile: DisplayName and IANA Timezone; email/security changes separate step-up flows. Reset: new password + confirmation, no old secret echo. Password length/hash/lockout policy technical security ADR, MFA/recovery Q-02. Browser timezone detected but editable.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Sessions list device label, last seen, expiry; newest activity first. No global user search in Profile.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Sessions list device label, last seen, expiry; newest activity first. No global user search in Profile.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| PendingVerification | Resend verification, change erroneous address through verified flow, logout | No module access; keep pending screen |
| Active | Own Profile/Sessions, logout, revoke session | Sensitive changes step-up Q-02 |
| Disabled | Recovery/help entry, logout | No module or support bypass |
| DeletionPending | Policy-dependent Q-01 | No assumed grace/recovery duration |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. This module does not need a fabricated provider-specific error surface for its ordinary local data; its registered cross-module sources may still be unavailable and must be labeled.

## 18. Permissions / Read-only / Sensitive contexts

Owner isolation applies equally to list/count/picker/history/source links. Operational authority does not supply personal-data permission. 

Use [security UX](../global/12-security-sensitive-ux.md) and [explicit modes](../global/13-admin-support-emergency.md). Readonly banner is contextual and server enforced, not merely a disabled Save over full private DTO.

## 19. Responsive behavior

[UX-10](../global/10-responsive-design.md) plus per-screen overrides is mandatory: desktop appropriate split/table, tablet drawer, mobile stacked route/sheet with Back, all fields available. Do not reset approved default view/source state on resize. 

## 20. Accessibility

Keyboard-only primary/alternate/error/destructive flows, explicit labels and visible focus; semantics for tables/forms/statuses; chart/table equivalent; no drag-only; modal focus trap/return; touch/zoom/reduced-motion contracts [UX-11](../global/11-accessibility.md). Per-screen text is not certification; later assistive-tech tests must execute these paths.

## 21. Cross-module integration

Use registered source/command/projection contracts from [UX-14](../global/14-cross-module-interactions.md); links preserve owner/access mode and do not transfer ownership or mutate unrelated source.

Data design trace:

| Table / provider data | Purpose / dependency status |
| --- | --- |
| [identity.User](../../design-database/02-core-identity-platform.md#identity-user) | Authentication principal; not a business-data owner; Technical decision |
| [identity.Session](../../design-database/02-core-identity-platform.md#identity-session) | Revocable server-authoritative browser session; Technical decision |
| [identity.OneTimeToken](../../design-database/02-core-identity-platform.md#identity-onetimetoken) | Verification/password-reset proof; Technical decision |
| [platform.PersonalSpace](../../design-database/02-core-identity-platform.md#platform-personalspace) | Exactly one personal ownership boundary per verified User; Technical decision |
| [identity.AccountMessageIntent](../../design-database/02-core-identity-platform.md#identity-accountmessageintent) | Pre-activation verification/recovery transactional communication; Technical decision |
| [identity.MfaCredential](../../design-database/02-core-identity-platform.md#identity-mfacredential) | Conditional TOTP enrollment design; no passkey scope inferred; Proposed: Q-02 |
| [identity.RecoveryCode](../../design-database/02-core-identity-platform.md#identity-recoverycode) | One-use account MFA recovery proof; Proposed: Q-02 |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-01](../../features/90-open-decisions.md#q-01) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-02](../../features/90-open-decisions.md#q-02) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-09](../../features/90-open-decisions.md#q-09) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

## 24. Acceptance checklist

- [ ] Execute primary journey for every Screen ID and authorized deep link; Back restores context.
- [ ] Required/optional/immutable fields match feature and data dictionary; no reference-product scope added.
- [ ] Every state/action row enforced both UI and server, including parent lifecycle and permission revoke race.
- [ ] Initial empty, filtered empty, loading, failed fetch, degraded/stale, disabled/read-only and conflict are distinct.
- [ ] Each destructive/reason dialog previews exact resources, cancels without mutation and handles stale revision.
- [ ] Keyboard-only and mobile flow reaches every action, detail field and safe exit; no drag/hover-only control.
- [ ] No secret or unauthorized payload in previews, error, URL, notification, logs or persistent browser state.
- [ ] Source BR/AC IDs and Q dependencies traced; Q-gated actions not treated as Approved.

**Five-question review:** User goal and simplest IA are sections1/6/9; mature reference evidence and adaptations/rejections sections3/4; important remaining choices are explicitly Q-gated in section23, not left for frontend to invent. This checklist is specification for later execution, not tests marked passed in a docs-only task.

## Canonical action binding — catalog v1

[FX-01 action catalog](../../action-catalog/modules/01-identity.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
