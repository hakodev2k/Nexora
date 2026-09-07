# FX-04 — Read-only Sharing — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Readonly link sharing with three audience modes, expiry and revoke under module policy.

## 2. Requirement sources

- [FX-04 feature](../../features/04-read-only-sharing.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-04-BR-001`, `FX-04-BR-002`, `FX-04-BR-003`, `FX-04-BR-004`, `FX-04-BR-005`, `FX-04-AC-001`, `FX-04-AC-002`, `FX-04-AC-003`, `P01-PDS-002`, `P01-SHR-001`, `P01-SHR-002`, `P01-SHR-003`

## 3. Reference products

- [Google Drive](https://support.google.com/drive/answer/2494822?hl=en) — checked2026-09-07. Evidence limit: Official help text; subscription-specific Drive features not Nexora entitlement.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Google Drive](https://support.google.com/drive/answer/2494822?hl=en) | Sharing exposes audience and permission choices with specific-recipient access. | ADAPT | Readonly PublicLink/AuthenticatedLink/RestrictedUsers with expiry; reject edit/comment and inherited Document-child sharing. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX04-S01 — Share dialog: DIALOG profile.
- FX04-S02 — Manage shares: BROWSE profile.
- FX04-S03 — Shared resource: DETAIL profile.
- FX04-S04 — Unavailable share: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX04-S01 | Share dialog | /sharing/new?resource=:opaqueId | Audience radio cards; expiry; allowed accounts; exact readonly disclosure preview | Create link |
| FX04-S02 | Manage shares | /settings/sharing | Resource safe title if eligible; mode; expiry; state; created time | Open share settings |
| FX04-S03 | Shared resource | /s/:token | Approved current readonly projection or exact Resume version; ownership attribution only if approved | Read content |
| FX04-S04 | Unavailable share | /s/:token/unavailable | Generic unavailable explanation; sign-in option only when safe to disclose mode | Sign in / Go Home |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Owner resource → Share → choose audience/expiry → preview disclosed fields → Create link → copy; viewer opens and passes current gate.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX04-S01 — Share dialog

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Audience radio cards; expiry; allowed accounts; exact readonly disclosure preview |
| Entry / proposed route | /sharing/new?resource=:opaqueId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Share dialog. Named modal with preview/reason and footer actions. |
| Primary action | Create link; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Audience radio cards; expiry; allowed accounts; exact readonly disclosure preview |
| Search / filters / sorting / pagination | Only allowed-user picker search. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Preview Project includes all nonTrash Tasks; Document page excludes children; sensitive Q-03 projections block creation until approved. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Share dialog' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX04-S02 — Manage shares

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Resource safe title if eligible; mode; expiry; state; created time |
| Entry / proposed route | /settings/sharing; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Manage shares. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open share settings; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Revoke selected; Create new link. Back/Cancel always has authorized fallback. |
| Content regions / fields | Resource safe title if eligible; mode; expiry; state; created time |
| Search / filters / sorting / pagination | Mode/state/resource filter; Created DESC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Revoke is immediate server-side, confirmation lists selected links not content. Existing URL cannot be reconstructed after leaving creation result because only hash is stored. Explain this before Create; owner may create a separate new link and explicitly revoke old, never silently rotate or revive it. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Manage shares' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX04-S03 — Shared resource

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Approved current readonly projection or exact Resume version; ownership attribution only if approved |
| Entry / proposed route | /s/:token; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Shared resource. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Read content; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Sign in if mode requires; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Approved current readonly projection or exact Resume version; ownership attribution only if approved |
| Search / filters / sorting / pagination | Only resource-approved readonly navigation. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No app owner controls/history/audit/reason/reminder config. Project Task detail navigation stays share-context route. No cross-owner cache reuse. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Shared resource' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX04-S04 — Unavailable share

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Generic unavailable explanation; sign-in option only when safe to disclose mode |
| Entry / proposed route | /s/:token/unavailable; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Unavailable share. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Sign in / Go Home; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Generic unavailable explanation; sign-in option only when safe to disclose mode |
| Search / filters / sorting / pagination | N/A. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Expired/revoked/unauthorized hidden from anonymous distinction; owner diagnostics separate. Never show hidden title in browser title. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Unavailable share' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Mode required: PublicLink, AuthenticatedLink(any logged-in account with link), RestrictedUsers(selected existing verified accounts). Expiry default7days, custom future instant or explicit no-expiry choice. Restricted allowlist>=1; exact-account resolution without public account enumeration. Only token hash persisted per FX-04. Plaintext URL returned once at Create and copyable only in that authorized in-memory creation result; never stored in browser persistence, audit/log or recoverable SQL envelope.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Manage links scoped owner/resource; filter mode/active/expired/revoked; newest first25/page. No full-database User autocomplete exposed to ordinary User.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Manage links scoped owner/resource; filter mode/active/expired/revoked; newest first25/page. No full-database User autocomplete exposed to ordinary User.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Eligible source | Create/revoke link; inspect projection | Need module policy and source share capability |
| Expired/revoked/Trash source | Owner sees reason; viewer generic unavailable | No data preview |
| Document Draft | Manage/revoke existing link | Create/resolve blocked; Published can resume valid nonrevoked link |
| Document Archived | Existing eligible link readonly | No new link; no child scope inherited |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Share dialog | Audience radio cards; expiry; allowed accounts; exact readonly disclosure preview | Create link / Cancel | Preview Project includes all nonTrash Tasks; Document page excludes children; sensitive Q-03 projections block creation until approved. |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-ARCHIVE / D-UNARCHIVE | Source + previous state and affected references | Archive / Unarchive; Cancel | Only features with archive lifecycle; preserve source-specific prior state/cohort. |
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
| [platform.ResourceType](../../design-database/02-core-identity-platform.md#platform-resourcetype) | Typed resource capability registration; Technical decision |
| [platform.Resource](../../design-database/02-core-identity-platform.md#platform-resource) | Identity/lifecycle directory, not generic content store; Technical decision |
| [security.ShareLink](../../design-database/03-security-sharing.md#security-sharelink) | Read-only resource link authorization; Proposed: Q-03 for unresolved lifecycle; confirmed modes preserved |
| [security.ShareAllowedUser](../../design-database/03-security-sharing.md#security-sharealloweduser) | RestrictedUsers allowlist; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-03](../../features/90-open-decisions.md#q-03) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-04](../../features/90-open-decisions.md#q-04) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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
