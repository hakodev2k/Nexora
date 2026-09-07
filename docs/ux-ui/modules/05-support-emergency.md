# FX-05 — Support / Emergency / Security Center — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Explicit readonly Support and Emergency operating modes with consent/audit, not impersonation.

## 2. Requirement sources

- [FX-05 feature](../../features/05-support-emergency-and-security-center.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-05-BR-001`, `FX-05-BR-002`, `FX-05-BR-003`, `FX-05-BR-004`, `FX-05-BR-005`, `FX-05-AC-001`, `FX-05-AC-002`, `FX-05-AC-003`, `P01-PDS-003`, `P01-PDS-004`

## 3. Reference products

- [Microsoft Customer Lockbox](https://learn.microsoft.com/en-us/purview/customer-lockbox-requests) — checked2026-09-07. Evidence limit: Official documentation; no organization or live lockbox test.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Microsoft Customer Lockbox](https://learn.microsoft.com/en-us/purview/customer-lockbox-requests) | Customer approval, limited-duration access and audit records distinguish support access. | ADAPT | Owner one-module readonly grant; keep24h default and any qualified Admin, not Microsoft duration/organization workflow. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX05-S01 — Grant support: FORM profile.
- FX05-S02 — Security Center: BROWSE profile.
- FX05-S03 — Support session: DETAIL profile.
- FX05-S04 — Emergency entry: FORM profile.
- FX05-S05 — Emergency session: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX05-S01 | Grant support | /settings/security/support/new | Single module; three duration choices; exact expiry preview; readonly disclosure | Grant access |
| FX05-S02 | Security Center | /settings/security/access | Active grants; target module; expiry; access sessions; actual actor; audit outcome | Grant support |
| FX05-S03 | Support session | /support/:accessSessionId/:moduleCode | Persistent SUPPORT MODE: User, Module, Expiry countdown, Read-only, End Session | End Session |
| FX05-S04 | Emergency entry | /admin/emergency/new | Target User; module; mandatory reason; audit/notification disclosure | Start emergency access |
| FX05-S05 | Emergency session | /emergency/:accessSessionId/:moduleCode | Stronger EMERGENCY ACCESS banner: reason context, User, Module, expiry, audit status | End Session |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Owner Security Center → grant one module default24h → Admin opens Support session → persistent banner → end/revoke; Emergency separate reason-first audited entry.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX05-S01 — Grant support

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Single module; three duration choices; exact expiry preview; readonly disclosure |
| Entry / proposed route | /settings/security/support/new; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Grant support. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Grant access; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Single module; three duration choices; exact expiry preview; readonly disclosure |
| Search / filters / sorting / pagination | Module searchable select; no Admin picker. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Consent final confirmation states any currently qualified Admin;24h selected, custom validates future expiry. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Grant support' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX05-S02 — Security Center

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Active grants; target module; expiry; access sessions; actual actor; audit outcome |
| Entry / proposed route | /settings/security/access; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Security Center. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Grant support; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Revoke grant; inspect access. Back/Cancel always has authorized fallback. |
| Content regions / fields | Active grants; target module; expiry; access sessions; actual actor; audit outcome |
| Search / filters / sorting / pagination | Mode/module/time/state; latest first. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Immediate all-channel notices reference this page; revoke grant ends future access for all sessions. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Security Center' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX05-S03 — Support session

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Persistent SUPPORT MODE: User, Module, Expiry countdown, Read-only, End Session |
| Entry / proposed route | /support/:accessSessionId/:moduleCode; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Support session. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | End Session; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Return to Admin landing. Back/Cancel always has authorized fallback. |
| Content regions / fields | Persistent SUPPORT MODE: User, Module, Expiry countdown, Read-only, End Session |
| Search / filters / sorting / pagination | Module readonly browse only. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Banner remains during scroll, mobile and detail overlays. Actual Admin identity distinct from target User; switching module ends scope not extends grant. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Support session' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX05-S04 — Emergency entry

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Target User; module; mandatory reason; audit/notification disclosure |
| Entry / proposed route | /admin/emergency/new; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Emergency entry. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Start emergency access; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Target User; module; mandatory reason; audit/notification disclosure |
| Search / filters / sorting / pagination | Exact target lookup under privileged permission. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No business content before successful audit/intent commit; error stays form with reason in memory only. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Emergency entry' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX05-S05 — Emergency session

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Stronger EMERGENCY ACCESS banner: reason context, User, Module, expiry, audit status |
| Entry / proposed route | /emergency/:accessSessionId/:moduleCode; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Emergency session. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | End Session; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | View own session audit. Back/Cancel always has authorized fallback. |
| Content regions / fields | Stronger EMERGENCY ACCESS banner: reason context, User, Module, expiry, audit status |
| Search / filters / sorting / pagination | Readonly provider navigation. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Audit status must committed before content; do not label pending-audit access as allowed. User gets immediate intent all3channels. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Emergency session' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Support requires one module and duration24h(default), custom end or until revoke. Qualified Admin not individually selected. Emergency requires target account/module and reason20..1000characters; proposed30min bounded session pending security review, no silent extension. Recent-auth policy Q-02; Vault metadata Q-04 blocks it.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Owner grants/access history filter module/mode/state/date; newest first25/page; reason never exposed in global search or notification body.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Owner grants/access history filter module/mode/state/date; newest first25/page; reason never exposed in global search or notification body.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Support active | Inspect approved readonly module; End Session | No edit/export/reveal/copy-secret/other modules |
| Consent revoked/expired | Show ended mode; Return | Clear protected data immediately on detection |
| Emergency requested | Reason+audit+notification intent before read | Audit failure blocks entry |
| Vault sensitive context | Unavailable pending Q-04 | No ambient decrypt even SuperAdmin |

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
| [security.SupportGrant](../../design-database/03-security-sharing.md#security-supportgrant) | Owner consent scoped to exactly one module; Technical decision |
| [security.AccessSession](../../design-database/03-security-sharing.md#security-accesssession) | Explicit Support or Emergency operating context; Technical design; session duration/recent-auth Proposed Q-02 |
| [security.AuditEvent](../../design-database/03-security-sharing.md#security-auditevent) | Append-only security audit distinct from User activity; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-02](../../features/90-open-decisions.md#q-02) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
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
