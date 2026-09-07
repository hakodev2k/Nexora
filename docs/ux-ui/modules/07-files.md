# FX-07 — Files / Attachments — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Owner file lifecycle, upload validation/scanning and reference-aware preview/attachment.

## 2. Requirement sources

- [FX-07 feature](../../features/07-files-and-attachments.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-07-BR-001`, `FX-07-BR-002`, `FX-07-BR-003`, `FX-07-BR-004`, `FX-07-BR-005`, `FX-07-BR-006`, `FX-07-AC-001`, `FX-07-AC-002`, `FX-07-AC-003`, `P01-PLT-003`, `P03-FIL-001`, `P03-FIL-002`, `P03-FIL-003`, `P03-FIL-004`

## 3. Reference products

- [Google Drive](https://support.google.com/drive/answer/2424368?hl=en) — checked2026-09-07. Evidence limit: Official help text; scan/replace rules are Nexora design decisions.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Google Drive](https://support.google.com/drive/answer/2424368?hl=en) | Upload is a distinct file-management workflow. | ADAPT | Add progress, validation and required scan/quarantine gate; Drive documentation is not evidence of Nexora scanning behavior. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX07-S01 — Files library: BROWSE profile.
- FX07-S02 — Upload queue: DETAIL profile.
- FX07-S03 — Preview: DETAIL profile.
- FX07-S04 — Attach file picker: DIALOG profile.
- FX07-S05 — Replace / remove reference: DIALOG profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX07-S01 | Files library | /files | Name; media type; size; scan state; updated time | Upload files |
| FX07-S02 | Upload queue | /files/uploads/:uploadId | Per-file byte progress; validating/scanning/ready labels; safe errors | Open ready file |
| FX07-S03 | Preview | /files/:fileId | Safe preview if supported; filename/type/size; reference count | Download |
| FX07-S04 | Attach file picker | /files/picker?resource=:opaqueId | Current owner Clean files; selected filename; target purpose | Attach selected |
| FX07-S05 | Replace / remove reference | /files/:fileId/references | Current versus replacement; affected current bindings; historical pins | Confirm replacement / Detach |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Files → Upload → validate/transfer/scan → Ready → preview/attach; replacement produces new revision preserving historical references.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX07-S01 — Files library

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; media type; size; scan state; updated time |
| Entry / proposed route | /files; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Files library. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Upload files; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Preview; Rename; Move to Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; media type; size; scan state; updated time |
| Search / filters / sorting / pagination | Name search; type/scan/state; updated DESC;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Provider failure not empty Files; multi-upload outcomes independent. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Files library' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX07-S02 — Upload queue

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Per-file byte progress; validating/scanning/ready labels; safe errors |
| Entry / proposed route | /files/uploads/:uploadId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Upload queue. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Open ready file; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel pending upload; Retry failed transfer. Back/Cancel always has authorized fallback. |
| Content regions / fields | Per-file byte progress; validating/scanning/ready labels; safe errors |
| Search / filters / sorting / pagination | Per-item queue, no fake100% before scan. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Upload network100% transitions Scanning; Ready only scan Clean. Navigation away warns if transfer cannot continue. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Upload queue' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX07-S03 — Preview

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Safe preview if supported; filename/type/size; reference count |
| Entry / proposed route | /files/:fileId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Preview. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Download; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Rename; Replace; Attach; Move to Trash; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Safe preview if supported; filename/type/size; reference count |
| Search / filters / sorting / pagination | No inline content editing. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unsupported preview offers owner download only if Clean/authorized. Quarantine uses safe placeholder; sandbox active formats. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Preview' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX07-S04 — Attach file picker

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Current owner Clean files; selected filename; target purpose |
| Entry / proposed route | /files/picker?resource=:opaqueId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Attach file picker. Named modal with preview/reason and footer actions. |
| Primary action | Attach selected; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Upload; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current owner Clean files; selected filename; target purpose |
| Search / filters / sorting / pagination | Name/type search;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No cross-user reused file URL; verify target editable and file Clean again at command. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Attach file picker' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX07-S05 — Replace / remove reference

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DIALOG — Current versus replacement; affected current bindings; historical pins |
| Entry / proposed route | /files/:fileId/references; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Replace / remove reference. Named modal with preview/reason and footer actions. |
| Primary action | Confirm replacement / Detach; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Current versus replacement; affected current bindings; historical pins |
| Search / filters / sorting / pagination | Reference list scoped to authorized target. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Historical versions keep old file. Permanent delete is separate action and cannot bypass pins. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dialog profile. Keep 'Replace / remove reference' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Upload file required; filename sanitized1..255; size/MIME/sniffing validation before acceptance. General quota/file limits Q-08; Documents cover JPG/PNG/WebP<=5MiB/25MP fixed delegated. Rename changes label, not extension spoofing or object contents. Replace creates new object binding.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Files name search; type/scan/lifecycle filters; Updated DESC;25/page; Table default with Name,Type,Size,Scan,Updated.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Files name search; type/scan/lifecycle filters; Updated DESC;25/page; Table default with Name,Type,Size,Scan,Updated.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Uploading/Scanning | See progress; cancel transfer if safe | No download/share/attach before Clean |
| Clean Active | Preview/download owner; rename; attach; Trash under policy | Referenced immutable file cannot be overwritten |
| Quarantined/ScanFailed | Safe metadata/error; retry scan under policy | No inline render/download bypass |
| Trash | Restore if dependencies permit; purge preview | Purge blocked by live/history version pins |

**Context intersection:** Owner Self requires active verified account, installed/system/user module gates and action+resource permission. Admin/SuperAdmin own data uses Self, not global data access. Support/Emergency only explicitly registered approved safe readonly projection for the granted module; otherwise unavailable. Secret reveal/export/mutation denied in those modes. Share viewer only if this source declares an approved readonly share projection and current link qualifies; operational screens/auth/Calendar Events/pure tools do not acquire sharing from common UI.

## 16. Dialogs

| Dialog title / ID | Explanation and affected resources | Primary / cancel | Retry/error and boundary |
| --- | --- | --- | --- |
| Attach file picker | Current owner Clean files; selected filename; target purpose | Attach selected / Upload; Cancel | No cross-user reused file URL; verify target editable and file Clean again at command. |
| Replace / remove reference | Current versus replacement; affected current bindings; historical pins | Confirm replacement / Detach / Cancel | Historical versions keep old file. Permanent delete is separate action and cannot bypass pins. |
| D-TRASH / D-RESTORE / D-PURGE | Selected source + exact cohort/reference/pin preview | Move to Trash / Restore / Delete permanently; Cancel | Only permitted source actions; irreversible purge warning, revalidate parent/revision; no generic restore bypass. |
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
| [files.FileObject](../../design-database/04-files-jobs-notifications.md#files-fileobject) | Owner-scoped uploaded binary metadata; Technical decision |
| [files.FileReference](../../design-database/04-files-jobs-notifications.md#files-filereference) | Reference-aware binary retention; Technical decision |
| [files.UploadSession](../../design-database/04-files-jobs-notifications.md#files-uploadsession) | Bounded upload and scan workflow; Technical decision |

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
