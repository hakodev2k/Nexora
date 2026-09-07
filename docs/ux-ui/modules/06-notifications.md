# FX-06 — Notification Center — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

One persistent Notification Center with all three channel attempts for every approved category.

## 2. Requirement sources

- [FX-06 feature](../../features/06-notification-center.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-06-BR-001`, `FX-06-BR-002`, `FX-06-BR-003`, `FX-06-BR-004`, `FX-06-BR-005`, `FX-06-BR-006`, `FX-06-AC-001`, `FX-06-AC-002`, `FX-06-AC-003`, `DEC-NTF-001`, `DEC-NTF-002`, `DEC-NTF-003`, `DEC-NTF-004`, `DEC-NTF-005`, `DEC-NTF-006`, `P01-PLT-004`, `P01-PLT-007`, `P01-PLT-008`, `P02-NTF-001`, `P02-NTF-002`, `P02-NTF-003`, `P02-NTF-004`, `P02-NTF-005`, `P02-NTF-006`

## 3. Reference products

- [GitHub Notifications](https://docs.github.com/en/subscriptions-and-notifications/how-tos/viewing-and-triaging-notifications/managing-notifications-from-your-inbox) — checked2026-09-07. Evidence limit: Official docs, no authenticated inbox inspected.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [GitHub Notifications](https://docs.github.com/en/subscriptions-and-notifications/how-tos/viewing-and-triaging-notifications/managing-notifications-from-your-inbox) | Inbox supports read/unread and multi-selection triage. | ADAPT | Use owner inbox actions; reject unsubscribe/mute/automatic retention because Nexora always attempts three channels and retains until deletion. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX06-S01 — Notification inbox: BROWSE profile.
- FX06-S02 — Notification detail: DETAIL profile.
- FX06-S03 — Browser push permission: DETAIL profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX06-S01 | Notification inbox | /notifications | Unread dot+text; safe title; category; source module; occurred time | Open notification |
| FX06-S02 | Notification detail | /notifications/:notificationId | Safe title/body; source button; occurred time; read state; channel outcomes | Open source |
| FX06-S03 | Browser push permission | /settings/notifications/push | Browser permission state; current subscription/device; delivery limitation | Enable browser notifications |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Bell/Inbox → inspect notification → open authorized source; select rows → mark read/unread or delete → per-item result.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX06-S01 — Notification inbox

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Unread dot+text; safe title; category; source module; occurred time |
| Entry / proposed route | /notifications; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Notification inbox. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open notification; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Mark read/unread; Mark all read; Delete selected. Back/Cancel always has authorized fallback. |
| Content regions / fields | Unread dot+text; safe title; category; source module; occurred time |
| Search / filters / sorting / pagination | All/Unread; category/date; newest;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Selection counts explicit; bulk delete confirmation no undo promise unless restore actually supported; newly arriving rows do not shift current selection. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Notification inbox' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX06-S02 — Notification detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Safe title/body; source button; occurred time; read state; channel outcomes |
| Entry / proposed route | /notifications/:notificationId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Notification detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Open source; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Mark unread; Delete; Back to Inbox. Back/Cancel always has authorized fallback. |
| Content regions / fields | Safe title/body; source button; occurred time; read state; channel outcomes |
| Search / filters / sorting / pagination | No pagination except previous/next loaded message optional not required. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Opening marks read once; source navigation rechecks authorization. Channel Accepted means provider accepted, not User saw it. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Notification detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX06-S03 — Browser push permission

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Browser permission state; current subscription/device; delivery limitation |
| Entry / proposed route | /settings/notifications/push; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Browser push permission. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Enable browser notifications; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Retry subscription; instructions for browser settings. Back/Cancel always has authorized fallback. |
| Content regions / fields | Browser permission state; current subscription/device; delivery limitation |
| Search / filters / sorting / pagination | No channel preference toggles. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Only native browser prompt after explicit click; denied state instructions, no repeated unsolicited prompts. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Browser push permission' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

No notification mute/channel opt-out/quiet-hour preferences. Browser permission explainer appears only after User action to enable Push; browser denial does not prevent Email/In-app. Notification content redacted; all categories share fixed channel contract.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Default All read+unread, newest first; Unread tab; category/date filters;25/page; select current page explicit. Mark-all-read uses visible cutoff timestamp and clearly says all current inbox, not future notifications.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Default All read+unread, newest first; Unread tab; category/date filters;25/page; select current page explicit. Mark-all-read uses visible cutoff timestamp and clearly says all current inbox, not future notifications.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Unread/Read | Open source; toggle read state; delete | No edit notification content |
| Source unavailable | Read safe message; return to Inbox | Do not show deleted/private source body |
| Delivery failed/unavailable | Inspect own channel status when exposed | No false Delivered or false seen marker |
| Deleted by User | Removed from Inbox | Audit/access event retained separately |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. External/staging/dispatch providers may fail independently: retain last successful authorized values with timestamp and error, offer safe retry-after, label partial results. Never show false price0, delivered notification, complete import or successful job.

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
| [notifications.Notification](../../design-database/04-files-jobs-notifications.md#notifications-notification) | One durable logical inbox notification; Technical decision |
| [notifications.Delivery](../../design-database/04-files-jobs-notifications.md#notifications-delivery) | Independent channel delivery work; Technical decision |
| [notifications.DeliveryAttempt](../../design-database/04-files-jobs-notifications.md#notifications-deliveryattempt) | Channel attempt evidence, immutable after terminal outcome; Technical decision |
| [notifications.PushSubscription](../../design-database/04-files-jobs-notifications.md#notifications-pushsubscription) | Browser/device push destination; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-08](../../features/90-open-decisions.md#q-08) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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
