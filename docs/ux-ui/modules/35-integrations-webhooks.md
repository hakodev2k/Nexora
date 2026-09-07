# FX-35 — Integrations / Webhooks / n8n — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Explicit scoped connections, webhooks and optional n8n exchange, no shared DB/master keys.

## 2. Requirement sources

- [FX-35 feature](../../features/35-integrations-webhooks-and-n8n.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-35-BR-001`, `FX-35-BR-002`, `FX-35-BR-003`, `FX-35-BR-004`, `FX-35-BR-005`, `FX-35-BR-006`, `FX-35-AC-001`, `FX-35-AC-002`, `FX-35-AC-003`, `P06-WHK-001`, `P06-WHK-002`

## 3. Reference products

- [GitHub Webhooks](https://docs.github.com/en/webhooks/using-webhooks/handling-webhook-deliveries) — checked2026-09-07. Evidence limit: Official developer docs, not proof of exact admin UI design.
- [n8n](https://n8n.io/features/) — checked2026-09-07. Evidence limit: Official product page; executions docs extraction failed, no live workflow tested.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [GitHub Webhooks](https://docs.github.com/en/webhooks/using-webhooks/handling-webhook-deliveries) | Delivery handling is a separate event-processing workflow. | ADAPT | Signature/dedupe/status/redacted delivery view; no exposing payload secrets or assuming replay is safe. |
| [n8n](https://n8n.io/features/) | Workflows expose triggers, per-step outputs and execution debugging. | ADAPT | Versioned definition/run/step details; reject code/AI/loops/unbounded graph, keep Q-07 for allowed flow and egress. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX35-S01 — Connections: BROWSE profile.
- FX35-S02 — Connection form: FORM profile.
- FX35-S03 — Webhook detail / form: FORM profile.
- FX35-S04 — Delivery history: BROWSE profile.
- FX35-S05 — Credential reference picker: DIALOG profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX35-S01 | Connections | /integrations | Name; provider; scope summary; credential reference masked; state; last test | New connection |
| FX35-S02 | Connection form | /integrations/new; /integrations/:connectionId/edit | Provider; name; endpoint; permitted scopes; Vault reference; version | Save connection |
| FX35-S03 | Webhook detail / form | /integrations/webhooks/:webhookId | Direction; endpoint/capability masked; event allowlist; signing Vault ref; enabled | Save webhook |
| FX35-S04 | Delivery history | /integrations/webhooks/:webhookId/deliveries | Message ID; direction; timestamp; outcome; attempts; redacted HTTP code | Inspect delivery |
| FX35-S05 | Credential reference picker | /integrations/credential-picker | Owner Vault-safe item label/type if approved; selected scope | Use reference |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Connections → select approved provider/scopes/Vault reference → Test with egress preview → Save → Webhook delivery list → inspect redacted outcome/retry.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX35-S01 — Connections

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; provider; scope summary; credential reference masked; state; last test |
| Entry / proposed route | /integrations; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Connections. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New connection; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; disable. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; provider; scope summary; credential reference masked; state; last test |
| Search / filters / sorting / pagination | Name/provider/state; name ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | n8n optional; platform usable without it. System provider configuration separate Admin permission route. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Connections' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX35-S02 — Connection form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Provider; name; endpoint; permitted scopes; Vault reference; version |
| Entry / proposed route | /integrations/new; /integrations/:connectionId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Connection form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save connection; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Preview test; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Provider; name; endpoint; permitted scopes; Vault reference; version |
| Search / filters / sorting / pagination | Registered provider/scopes only. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Test displays data leaving Nexora before explicit run; no credential value echoed in error. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Connection form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX35-S03 — Webhook detail / form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Direction; endpoint/capability masked; event allowlist; signing Vault ref; enabled |
| Entry / proposed route | /integrations/webhooks/:webhookId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Webhook detail / form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save webhook; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Test approved payload; disable; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Direction; endpoint/capability masked; event allowlist; signing Vault ref; enabled |
| Search / filters / sorting / pagination | Event/projection selectors, no arbitrary code. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Inbound signing replay window/MessageId status visible; inbound URL capability copy is sensitive and owner-authorized. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Webhook detail / form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX35-S04 — Delivery history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Message ID; direction; timestamp; outcome; attempts; redacted HTTP code |
| Entry / proposed route | /integrations/webhooks/:webhookId/deliveries; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Delivery history. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Inspect delivery; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Retry eligible; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Message ID; direction; timestamp; outcome; attempts; redacted HTTP code |
| Search / filters / sorting / pagination | State/date/direction; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Delivery Retry distinct Automation run Retry. No raw body or auth headers in list/detail. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Delivery history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX35-S05 — Credential reference picker

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Owner Vault-safe item label/type if approved; selected scope |
| Entry / proposed route | /integrations/credential-picker; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Credential reference picker. Named modal with preview/reason and footer actions. |
| Primary action | Use reference; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Owner Vault-safe item label/type if approved; selected scope |
| Search / filters / sorting / pagination | Owner-safe lookup after auth; no decrypt preview. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Integration only receives scoped secret resolution at execution boundary; support cannot pick/copy secret. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Credential reference picker' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Provider/version/endpoint and required Vault reference; owner/system scopes separated. Event MessageId/schemaVersion/time/signature; signed inbound replay protection. Outbound projection/body limits Q-07, not user free-form arbitrary secret payload.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Connection name/provider/state filters, name ASC25/page. Delivery state/date/direction filters, newest25/page; payload values not searchable.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Connection name/provider/state filters, name ASC25/page. Delivery state/date/direction filters, newest25/page; payload values not searchable.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Configured/Valid | Test permitted scope; view events; disable | No blanket access to all owner modules |
| Invalid/provider unavailable | Redacted error/retry-after | No core outage propagation |
| Webhook inbound rejected | Inspect signature/time/dedupe failure code | No workflow trigger before validation |
| Outbound Unknown/Failed | Reconcile/eligible retry | No duplicate external effect guarantee |
| Disabled | Inspect history | No new send/accept side effects |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Credential reference picker | Owner Vault-safe item label/type if approved; selected scope | Use reference / Cancel | Integration only receives scoped secret resolution at execution boundary; support cannot pick/copy secret. |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. External/staging/dispatch providers may fail independently: retain last successful authorized values with timestamp and error, offer safe retry-after, label partial results. Never show false price0, delivered notification, complete import or successful job.

## 18. Permissions / Read-only / Sensitive contexts

Sensitive field projections are constrained by Q-03/Q-04 where relevant; do not infer permission from metadata labels. No secret/private salary/serial/contact/financial values in generic previews or diagnostics. 

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
| [automation.Connection](../../design-database/09-automation-monitoring.md#automation-connection) | Explicit provider binding with scoped secret reference; Proposed: Q-07 |
| [automation.Webhook](../../design-database/09-automation-monitoring.md#automation-webhook) | Owner webhook endpoint registration; Proposed: Q-07 |
| [automation.WebhookDelivery](../../design-database/09-automation-monitoring.md#automation-webhookdelivery) | Signed inbound receipt/outbound attempt group; Proposed: Q-07 |
| [platform.SystemConnection](../../design-database/02-core-identity-platform.md#platform-systemconnection) | Operator provider configuration separate from personal integrations; Proposed: Q-07/Q-08 provider selection |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-07](../../features/90-open-decisions.md#q-07) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-35 action catalog](../../action-catalog/modules/35-integrations.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
