# FX-10 — Import / Export / Backup — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Module import/export and system backup/restore are four separate operations.

## 2. Requirement sources

- [FX-10 feature](../../features/10-import-export-and-backup.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-10-BR-001`, `FX-10-BR-002`, `FX-10-BR-003`, `FX-10-BR-004`, `FX-10-BR-005`, `FX-10-BR-006`, `FX-10-AC-001`, `FX-10-AC-002`, `FX-10-AC-003`, `P08-BKP-001`, `P08-BKP-002`, `P08-BKP-003`, `P08-BKP-004`, `P08-BKP-005`, `P08-BKP-006`, `P08-BKP-007`

## 3. Reference products

- [Google Calendar](https://support.google.com/calendar/answer/37118?co=GENIE.Platform%3DDesktop&hl=en) — checked2026-09-07. Evidence limit: Official help text; Nexora validation rules come from PO/features.
- [GitLab](https://docs.gitlab.com/administration/backup_restore/) — checked2026-09-07. Evidence limit: Official operational docs, no restore executed.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Google Calendar](https://support.google.com/calendar/answer/37118?co=GENIE.Platform%3DDesktop&hl=en) | Import & Export is a settings entry with file selection and target calendar selection. | ADAPT | Nexora .ics preview/report, always ManualEvent; reject recurrence/reminder import and external sync. |
| [GitLab](https://docs.gitlab.com/administration/backup_restore/) | System backup and restore are administrative recovery workflows. | ADAPT | Separate owner export/import from system recovery; no claim SQL/files/keys are recovered without verification. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX10-S01 — Import: FORM profile.
- FX10-S02 — Import preview: BROWSE profile.
- FX10-S03 — Export: FORM profile.
- FX10-S04 — Operation status: DETAIL profile.
- FX10-S05 — System backup / restore: ADMIN profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX10-S01 | Import | /data/import?module=:moduleCode | Allowed format; selected file; module-specific mapping/options | Validate file |
| FX10-S02 | Import preview | /data/import/:operationId/preview | Row number; safe candidate summary; Valid/Invalid/Duplicate; reason | Import valid rows |
| FX10-S03 | Export | /data/export?module=:moduleCode | Allowed source types/statuses; all/custom range; field disclosure | Generate export |
| FX10-S04 | Operation status | /data/operations/:operationId | Queued/running/completed/failed; counts; row errors; output expiry | Download result / View report |
| FX10-S05 | System backup / restore | /admin/recovery | Backup manifest; SQL/files/key/version checks; run status; verification results | Review recovery plan |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Authorized module → Import file → validate preview → apply → row report; Export → select allowed source/status/range → generate → private download.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX10-S01 — Import

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Allowed format; selected file; module-specific mapping/options |
| Entry / proposed route | /data/import?module=:moduleCode; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Import. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Validate file; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Allowed format; selected file; module-specific mapping/options |
| Search / filters / sorting / pagination | Format selector no unsupported entries. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Scan/parse limits before preview; no production-data mutation on selection. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Import' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX10-S02 — Import preview

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Row number; safe candidate summary; Valid/Invalid/Duplicate; reason |
| Entry / proposed route | /data/import/:operationId/preview; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Import preview. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Import valid rows; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel; Back to options. Back/Cancel always has authorized fallback. |
| Content regions / fields | Row number; safe candidate summary; Valid/Invalid/Duplicate; reason |
| Search / filters / sorting / pagination | Outcome filter; source order;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | ICS recurring/invalid/duplicate skipped with report; reminder ignored and all imported valid Scheduled clearly stated. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Import preview' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX10-S03 — Export

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Allowed source types/statuses; all/custom range; field disclosure |
| Entry / proposed route | /data/export?module=:moduleCode; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Export. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Generate export; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | Allowed source types/statuses; all/custom range; field disclosure |
| Search / filters / sorting / pagination | Calendar type Manual/Task/Both; exact containment. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Do not promise Projects/Tasks standalone export; Calendar ICS business fields exclude Reminder/history/reasons/internal IDs. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Export' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX10-S04 — Operation status

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Queued/running/completed/failed; counts; row errors; output expiry |
| Entry / proposed route | /data/operations/:operationId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Operation status. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Download result / View report; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Retry eligible failure; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Queued/running/completed/failed; counts; row errors; output expiry |
| Search / filters / sorting / pagination | Report row filter;25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Retry reuses operation semantics/idempotency; partial apply shows actual applied/skipped/failed counts. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Operation status' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX10-S05 — System backup / restore

| Dimension | Specification |
| --- | --- |
| Purpose / profile | ADMIN — Backup manifest; SQL/files/key/version checks; run status; verification results |
| Entry / proposed route | /admin/recovery; module/source navigation or authorized deep link. |
| Header / layout | Screen title: System backup / restore. Separate Admin shell, grouped target metadata/matrix and before/after review. |
| Primary action | Review recovery plan; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | View run report; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Backup manifest; SQL/files/key/version checks; run status; verification results |
| Search / filters / sorting / pagination | State/date filter; newest. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No owner data browser. Restore into isolated target requires approved runbook and Q-08; destructive production action never delegated UI default. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 admin profile. Keep 'System backup / restore' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Format choices only registered/approved. Projects and Tasks no import/export R1; Calendar ICS may export selected Task projections. File required import; export source/type/status/range required per module. System recovery requires separate operational approval and isolated target, not ordinary User form.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Operation list own jobs newest first; state/module filter25/page. Import preview row outcome filter Valid/Invalid/Duplicate; stable source row order.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Operation list own jobs newest first; state/module filter25/page. Import preview row outcome filter Valid/Invalid/Duplicate; stable source row order.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| PreviewReady | Apply valid rows, cancel | No invalid/recurring ICS rows applied |
| Running | View progress, cooperative cancel | No success before commit |
| ExportReady | Owner download while valid | Recheck grants; no public artifact URL |
| Backup Verified | Authorized recovery preview | Not user export; no one-click overwrite production |
| Format Q pending | Explain unsupported scope | No fake successful conversion |

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
| [platform.ModuleMigration](../../design-database/02-core-identity-platform.md#platform-modulemigration) | Per-module migration journal design; Technical decision |
| [operations.Idempotency](../../design-database/04-files-jobs-notifications.md#operations-idempotency) | Bounded command retry ledger; Technical decision |
| [operations.ImportBatch](../../design-database/04-files-jobs-notifications.md#operations-importbatch) | Validated module import operation; Technical decision |
| [operations.ImportRow](../../design-database/04-files-jobs-notifications.md#operations-importrow) | Per-row preview and result; Technical decision |
| [operations.ExportJob](../../design-database/04-files-jobs-notifications.md#operations-exportjob) | Owner export request distinct from backup; Technical decision |
| [operations.BackupRun](../../design-database/04-files-jobs-notifications.md#operations-backuprun) | System-only recovery inventory; Technical decision |
| [operations.RestoreRun](../../design-database/04-files-jobs-notifications.md#operations-restorerun) | Isolated system restore rehearsal or approved recovery; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-01](../../features/90-open-decisions.md#q-01) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-04](../../features/90-open-decisions.md#q-04) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-08](../../features/90-open-decisions.md#q-08) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
- [Q-11](../../features/90-open-decisions.md#q-11) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-10 action catalog](../../action-catalog/modules/10-transfer.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
