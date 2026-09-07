# FX-38 — Digital Assets — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Digital-asset metadata for Domain/Hosting/VPS/Certificate/License/OnlineService, never infrastructure control.

## 2. Requirement sources

- [FX-38 feature](../../features/38-digital-assets.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-38-BR-001`, `FX-38-BR-002`, `FX-38-BR-003`, `FX-38-BR-004`, `FX-38-BR-005`, `FX-38-BR-006`, `FX-38-AC-001`, `FX-38-AC-002`, `FX-38-AC-003`, `P07-CER-001`, `P07-CER-002`, `P07-CER-003`, `P07-DIG-001`, `P07-DIG-002`, `P07-DIG-003`, `P07-DIG-004`, `P07-DOM-001`, `P07-DOM-002`, `P07-INF-001`, `P07-INF-002`, `P07-LIC-001`, `P07-SVC-001`

## 3. Reference products

- [Cloudflare Registrar](https://developers.cloudflare.com/registrar/account-options/renew-domains/) — checked2026-09-07. Evidence limit: Official docs; no Cloudflare integration or provider action executed.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Cloudflare Registrar](https://developers.cloudflare.com/registrar/account-options/renew-domains/) | Domain renewal information distinguishes auto-renew setting and actual renewal outcome. | ADAPT | Show entered renewal metadata/freshness; reject payment/registrar control and guarantees that renewal happened. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX38-S01 — Digital asset list: BROWSE profile.
- FX38-S02 — Type-specific form: FORM profile.
- FX38-S03 — Digital detail: DETAIL profile.
- FX38-S04 — Renewal form / timeline: FORM profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX38-S01 | Digital asset list | /assets/digital | Name; type; provider; entered expiry; state; observation freshness | New Digital Asset |
| FX38-S02 | Type-specific form | /assets/digital/new; /assets/digital/:assetId/edit | Type/name; typed metadata sections; cost/currency; Vault reference; notes | Save metadata |
| FX38-S03 | Digital detail | /assets/digital/:assetId | Typed metadata; Manual vs Observed values/time; renewal history; related files/Finance/Vault refs | Record renewal |
| FX38-S04 | Renewal form / timeline | /assets/digital/:assetId/renewals | Recorded renewal date; prior/new expiry; cost/currency; evidence/note | Record renewal |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Digital assets → choose type → manual metadata Save → optional approved observations → renewal timeline → Record renewal → retain old evidence.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX38-S01 — Digital asset list

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; type; provider; entered expiry; state; observation freshness |
| Entry / proposed route | /assets/digital; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Digital asset list. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Digital Asset; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; filter; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; type; provider; entered expiry; state; observation freshness |
| Search / filters / sorting / pagination | Name/domain; type/status/expiry; expiry ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Do not merge manual expiry and observed certificate NotAfter without source label. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Digital asset list' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX38-S02 — Type-specific form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Type/name; typed metadata sections; cost/currency; Vault reference; notes |
| Entry / proposed route | /assets/digital/new; /assets/digital/:assetId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Type-specific form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save metadata; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Type/name; typed metadata sections; cost/currency; Vault reference; notes |
| Search / filters / sorting / pagination | Only relevant type fields; URL/hostname/date validation. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No remote control buttons. AutoRenew Recorded checkbox label explicitly informational. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Type-specific form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX38-S03 — Digital detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Typed metadata; Manual vs Observed values/time; renewal history; related files/Finance/Vault refs |
| Entry / proposed route | /assets/digital/:assetId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Digital detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Record renewal; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit metadata; observe approved source; Archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Typed metadata; Manual vs Observed values/time; renewal history; related files/Finance/Vault refs |
| Search / filters / sorting / pagination | History date/source; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | External provider open clearly leaves Nexora; secret reference does not reveal credential. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Digital detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX38-S04 — Renewal form / timeline

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Recorded renewal date; prior/new expiry; cost/currency; evidence/note |
| Entry / proposed route | /assets/digital/:assetId/renewals; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Renewal form / timeline. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Record renewal; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; inspect history. Back/Cancel always has authorized fallback. |
| Content regions / fields | Recorded renewal date; prior/new expiry; cost/currency; evidence/note |
| Search / filters / sorting / pagination | New expiry validity, no payment collection. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Confirmation says only updates Nexora record, does not renew service or charge account. Old evidence retained. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Renewal form / timeline' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Name/type; type-specific fields listed below. Domain Unicode+ASCII, registrar/registered/expiry/auto-renew info; Hosting/VPS plan/region/endpoints; Certificate public subject/SAN/issuer/dates/fingerprint; License vendor/product/edition/quantity/device+Vault keyRef; Service plan/account label/renewal/subscription ref. Manual and observed values separate.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Name/domain search; type/status/expiry filters; earliest expiry then name/id;25/page. Domain confusable name shows Unicode and ASCII; expiry Unknown distinct nonexpiring.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Name/domain search; type/status/expiry filters; earliest expiry then name/id;25/page. Domain confusable name shows Unicode and ASCII; expiry Unknown distinct nonexpiring.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Active/Expired/Canceled | Inspect/edit metadata; record renewal; Archive/Trash | No DNS/registrar/SSH/payment commands |
| Archived | Readonly; Unarchive; Trash | No network observation mutation that overrides manual fields |
| Observation stale/failed | Show manual values plus old observed timestamp | No claim actual renewal succeeded |
| Secrets/network scope | Vault reference only; approved public observation Q-07 | No plaintext key or private-target bypass |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-ARCHIVE / D-UNARCHIVE | Source + previous state and affected references | Archive / Unarchive; Cancel | Only features with archive lifecycle; preserve source-specific prior state/cohort. |
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
| [assets.DigitalAsset](../../design-database/10-assets-career-learning.md#assets-digitalasset) | Typed online-asset metadata, not infrastructure control plane; Technical decision |
| [assets.DomainDetail](../../design-database/10-assets-career-learning.md#assets-domaindetail) | Domain-specific metadata; Technical decision |
| [assets.HostingDetail](../../design-database/10-assets-career-learning.md#assets-hostingdetail) | Hosting plan metadata; Technical decision |
| [assets.VpsDetail](../../design-database/10-assets-career-learning.md#assets-vpsdetail) | Server inventory metadata only; Technical decision |
| [assets.CertificateDetail](../../design-database/10-assets-career-learning.md#assets-certificatedetail) | Public certificate metadata; Technical decision |
| [assets.LicenseDetail](../../design-database/10-assets-career-learning.md#assets-licensedetail) | Software license entitlement metadata; Technical decision |
| [assets.ServiceDetail](../../design-database/10-assets-career-learning.md#assets-servicedetail) | Online-service account metadata; Technical decision |
| [assets.RenewalRecord](../../design-database/10-assets-career-learning.md#assets-renewalrecord) | Manual metadata renewal history; Technical decision |
| [assets.DigitalAssetVersion](../../design-database/10-assets-career-learning.md#assets-digitalassetversion) | Entered and observed metadata change history; Technical decision |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-03](../../features/90-open-decisions.md#q-03) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
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
