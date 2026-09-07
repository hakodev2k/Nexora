# FX-28 — Vault — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal encrypted Vault, masked default and owner-only sensitive actions; keys/recovery/portability Q-04.

## 2. Requirement sources

- [FX-28 feature](../../features/28-vault.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-28-BR-001`, `FX-28-BR-002`, `FX-28-BR-003`, `FX-28-BR-004`, `FX-28-BR-005`, `FX-28-BR-006`, `FX-28-AC-001`, `FX-28-AC-002`, `FX-28-AC-003`, `FX-28-AC-004`, `P04-VLT-007`, `P04-CRY-001`, `P04-CRY-002`, `P04-CRY-003`, `P04-CRY-004`, `P04-CRY-005`, `P04-VAC-001`, `P04-VAC-002`, `P04-VAC-003`, `P04-VAC-004`, `P04-VAC-005`, `P04-VLT-001`, `P04-VLT-002`, `P04-VLT-003`, `P04-VLT-004`, `P04-VLT-005`, `P04-VLT-006`, `P04-VLT-008`

## 3. Reference products

- [Bitwarden](https://bitwarden.com/help/managing-items/) — checked2026-09-07. Evidence limit: Official docs, no live Vault or secret inspected.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Bitwarden](https://bitwarden.com/help/managing-items/) | Vault item management separates item list/detail and archive/delete actions. | ADAPT | Masked personal-only item workflow, recent-auth reveal/copy; reject organization/shared Vault and reference retention. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep masking, owner authorization and explicit transient reveal boundaries visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX28-S01 — Vault list: BROWSE profile.
- FX28-S02 — Vault item detail: DETAIL profile.
- FX28-S03 — Vault create / edit: FORM profile.
- FX28-S04 — Vault history: HISTORY profile.
- FX28-S05 — Password generator: WORKBENCH profile.
- FX28-S06 — Vault Trash / security policy: BROWSE profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX28-S01 | Vault list | /vault | Type categories; permitted item labels; favorite if supported; no secret excerpts | New Vault item |
| FX28-S02 | Vault item detail | /vault/items/:itemId | Masked sensitive fields; field labels; type; version; owner-only action controls | Copy selected secret after authorization |
| FX28-S03 | Vault create / edit | /vault/items/new; /vault/items/:itemId/edit | Explicit type/name; typed secret fields; optional service/URL/tags/notes | Save encrypted item |
| FX28-S04 | Vault history | /vault/items/:itemId/history | Version/time/type; encrypted snapshot preview only after owner auth | Restore as new encrypted version |
| FX28-S05 | Password generator | /vault/generator | Length/character options; strength explanation; masked generated result | Generate |
| FX28-S06 | Vault Trash / security policy | /vault/trash; /settings/vault | Owner-only deleted items metadata; key/recovery policy state | Preview Restore |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Vault → unlock/step-up under policy → category → item list → masked detail → explicit Reveal/Copy → close/clear; edit creates encrypted version.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX28-S01 — Vault list

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Type categories; permitted item labels; favorite if supported; no secret excerpts |
| Entry / proposed route | /vault; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Vault list. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Vault item; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open; Generator; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Type categories; permitted item labels; favorite if supported; no secret excerpts |
| Search / filters / sorting / pagination | Approved owner-safe name/tag/type search only; Updated DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Conservative locked view reveals no titles until metadata disclosure policy resolved. Global Search excludes payload. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Vault list' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX28-S02 — Vault item detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Masked sensitive fields; field labels; type; version; owner-only action controls |
| Entry / proposed route | /vault/items/:itemId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Vault item detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Copy selected secret after authorization; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Reveal field; Edit; History; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Masked sensitive fields; field labels; type; version; owner-only action controls |
| Search / filters / sorting / pagination | No general content filters. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Reveal/Copy each explicit, transient and recent-auth gated. Toast never includes secret; clear on leave/lock/revoke; screenshot prevention not promised. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Vault item detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX28-S03 — Vault create / edit

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Explicit type/name; typed secret fields; optional service/URL/tags/notes |
| Entry / proposed route | /vault/items/new; /vault/items/:itemId/edit; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Vault create / edit. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save encrypted item; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Generate secret; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Explicit type/name; typed secret fields; optional service/URL/tags/notes |
| Search / filters / sorting / pagination | Type immutable after create; masked/reveal input controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No form autosave/draft to storage. Leaving dirty secret form confirms discard without logging or exporting payload. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Vault create / edit' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX28-S04 — Vault history

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — Version/time/type; encrypted snapshot preview only after owner auth |
| Entry / proposed route | /vault/items/:itemId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Vault history. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Restore as new encrypted version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back; Copy approved field. Back/Cancel always has authorized fallback. |
| Content regions / fields | Version/time/type; encrypted snapshot preview only after owner auth |
| Search / filters / sorting / pagination | Latest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Never fetch all historical plaintext at once. Restore current key version; no decryption in generic History service. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Vault history' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX28-S05 — Password generator

| Dimension | Specification |
| --- | --- |
| Purpose / profile | WORKBENCH — Length/character options; strength explanation; masked generated result |
| Entry / proposed route | /vault/generator; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Password generator. Shared workbench profile; header → controls → declared content → feedback. |
| Primary action | Generate; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Copy; Use in current Vault field; Clear. Back/Cancel always has authorized fallback. |
| Content regions / fields | Length/character options; strength explanation; masked generated result |
| Search / filters / sorting / pagination | Bounded CSPRNG options. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No automatic generator-history persistence or clipboard read. Copy best effort, no clipboard-clear guarantee. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 workbench profile. Keep 'Password generator' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX28-S06 — Vault Trash / security policy

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Owner-only deleted items metadata; key/recovery policy state |
| Entry / proposed route | /vault/trash; /settings/vault; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Vault Trash / security policy. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Preview Restore; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Purge after approved checks; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Owner-only deleted items metadata; key/recovery policy state |
| Search / filters / sorting / pagination | Type/date search within allowed metadata. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No zero-knowledge/recoverable claim until Q-04. Shared-link creation and encrypted export blocked where unresolved. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Vault Trash / security policy' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Name1..200 and immutable one of nine types; type-specific fields inside encrypted payload (see DB payload contracts). URL/tags metadata classified, not assumed public. Recent-auth policy Q-02; recovery/operator-safe metadata/encrypted import-export Q-04. No organization/shared Vault/public share.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Category/type sidebar, owner locally authorized search only within approved encrypted metadata design; no global Vault payload search. List safe/masked labels according to Q-04, updated DESC25/page. No secrets in URL/query/preview.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Category/type sidebar, owner locally authorized search only within approved encrypted metadata design; no global Vault payload search. List safe/masked labels according to Q-04, updated DESC25/page. No secrets in URL/query/preview.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Locked / step-up required | Authenticate, leave | No ciphertext-derived plaintext preview |
| Owner authorized Active | Masked view; explicit Reveal/Copy; edit/version; Trash | No persistent browser secret storage |
| Archived | Readonly owner masked view; eligible Unarchive/Trash | No edit |
| Support/Emergency | Denied pending approved metadata projection Q-04 | Never ambient Reveal/Copy/export/decrypt |
| Trash | Owner restore/purge under key/reference policy | Purge irreversible and not recovery guarantee |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
| D-ARCHIVE / D-UNARCHIVE | Source + previous state and affected references | Archive / Unarchive; Cancel | Only features with archive lifecycle; preserve source-specific prior state/cohort. |
| D-UNSAVED / D-CONFLICT | Authorized dirty source/current revision, no secrets in diagnostics | Save/Discard/Keep editing or Reload/Reapply/Cancel | No silent discard/overwrite; revoked access clears protected data. N/A on pure readonly screens. |

Risk style/focus/retry defaults: [UX-08 dialog contracts](../global/08-lifecycle-destructive-actions.md). Reason dialogs focus mandatory reason; irreversible confirm initially focuses Cancel. Pending response not successful action; retries use same safe idempotency key.

## 17. Loading / Empty / Error / Degraded

Each screen inherits explicit UX-15A states: initial skeleton, empty owner data, no filtered matches, fetch failure, stale authorized data, module unavailable, permission denied/revoked and conflict. This module does not need a fabricated provider-specific error surface for its ordinary local data; its registered cross-module sources may still be unavailable and must be labeled.

## 18. Permissions / Read-only / Sensitive contexts

Sensitive field projections are constrained by Q-03/Q-04 where relevant; do not infer permission from metadata labels. No secret/private salary/serial/contact/financial values in generic previews or diagnostics. Vault Support/Emergency metadata denied pending Q-04; no public share and no ambient decrypt.

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
| [vault.Item](../../design-database/07-finance-vault.md#vault-item) | Encrypted personal Vault item with minimal outer envelope; Proposed: Q-04 cryptographic architecture |
| [vault.ItemVersion](../../design-database/07-finance-vault.md#vault-itemversion) | Immutable authenticated encrypted secret payload; Proposed: Q-04 |
| [vault.KeyEnvelope](../../design-database/07-finance-vault.md#vault-keyenvelope) | Key metadata/wrapped data key, not master key; Proposed: Q-04 |
| [vault.RotationRun](../../design-database/07-finance-vault.md#vault-rotationrun) | Resumable key rotation inventory; Proposed: Q-04/Q-08 |

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

## Canonical action binding — catalog v1

[FX-28 action catalog](../../action-catalog/modules/28-vault.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
