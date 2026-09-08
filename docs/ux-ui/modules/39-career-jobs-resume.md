# FX-39 — Career / Jobs / Resume — UX/UI Specification

> Current specification · reconciled 2026-09-08 · Docs-only. [Previous version](../../history/20260908/snapshot/docs/ux-ui/modules/39-career-jobs-resume.md) is historical evidence, not implementation input.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Manual personal job/company/interview pipeline and exact immutable Resume versions; Calendar handoff Q-12.

## 2. Requirement sources

- [FX-39 feature](../../features/39-career-and-resumes.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-39-BR-001`, `FX-39-BR-002`, `FX-39-BR-003`, `FX-39-BR-004`, `FX-39-BR-005`, `FX-39-BR-006`, `FX-39-AC-001`, `FX-39-AC-002`, `FX-39-AC-003`, `FX-39-AC-004`, `P07-COM-001`, `P07-INT-001`, `P07-INT-002`, `P07-JOB-001`, `P07-JOB-002`, `P07-JOB-003`, `P07-JOB-004`, `P07-JOB-005`, `P07-RES-001`, `P07-RES-002`, `P07-RES-003`, `P07-RES-004`

## 3. Reference products

- [Teal](https://www.tealhq.com/tools/job-tracker) — checked2026-09-07. Evidence limit: Official product page, no authenticated pipeline test.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Teal](https://www.tealhq.com/tools/job-tracker) | Saved opportunities are organized by application stage with job/company detail. | ADAPT | Manual personal pipeline, exact Resume version; reject AI resume generation/browser scraping/outreach. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX39-S01 — Job pipeline: BROWSE profile.
- FX39-S02 — Job create / detail: FORM profile.
- FX39-S03 — Company directory / merge: BROWSE profile.
- FX39-S04 — Linked Calendar Events: FORM profile.
- FX39-S05 — Resume library / upload: BROWSE profile.
- FX39-S06 — Resume version / share: DETAIL profile.
- FX39-S07 — Application timeline: HISTORY profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX39-S01 | Job pipeline | /career/jobs | Stage columns Saved through Closed; cards Title/company/date; Table alternative | Add Job |
| FX39-S02 | Job create / detail | /career/jobs/new; /career/jobs/:jobId | Title/company/source/location/workmode/type; salary text/range private; description; current stage; exact Resume selection | Save Job |
| FX39-S03 | Company directory / merge | /career/companies; /career/companies/:companyId/merge | Name; industry; location; private contact; linked jobs count | New Company / Review merge |
| FX39-S04 | Linked Calendar Events | /career/jobs/:jobId/events | Linked Event Title/Start/End/Status; Calendar source link; no duplicate fields | Create Event / Link existing Event |
| FX39-S05 | Resume library / upload | /career/resumes | Name; language; latest version label; updated; active/archive | New Resume / Upload version |
| FX39-S06 | Resume version / share | /career/resumes/:resumeId/versions/:version | Exact file/document version preview; version/time; application pins; share disclosure | Share this version if approved |
| FX39-S07 | Application timeline | /career/jobs/:jobId/history | Stage changes; reason; company snapshot; exact submitted Resume version; Interview events | Inspect evidence |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Save job → move pipeline stage with history → choose exact Resume version when applied → create/link Personal Event → inspect timeline; Resume share separately pins selected version.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX39-S01 — Job pipeline

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Stage columns Saved through Closed; cards Title/company/date; Table alternative |
| Entry / proposed route | /career/jobs; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Job pipeline. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Add Job; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Move stage; Open; filter; Archive/Trash eligible. Back/Cancel always has authorized fallback. |
| Content regions / fields | Stage columns Saved through Closed; cards Title/company/date; Table alternative |
| Search / filters / sorting / pagination | Title/company; stage/company/date; updated DESC. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Keyboard Move stage alternative; terminal-backward reason dialog, no automatic application sending. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Job pipeline' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX39-S02 — Job create / detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title/company/source/location/workmode/type; salary text/range private; description; current stage; exact Resume selection |
| Entry / proposed route | /career/jobs/new; /career/jobs/:jobId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Job create / detail. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Job; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Change stage; Add Calendar Event; Timeline; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title/company/source/location/workmode/type; salary text/range private; description; current stage; exact Resume selection |
| Search / filters / sorting / pagination | Company picker and Resume version picker explicit. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Resume picker shows family+version+file/date; pin persists even newer Resume uploaded. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Job create / detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX39-S03 — Company directory / merge

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; industry; location; private contact; linked jobs count |
| Entry / proposed route | /career/companies; /career/companies/:companyId/merge; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Company directory / merge. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Company / Review merge; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit; merge explicitly. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; industry; location; private contact; linked jobs count |
| Search / filters / sorting / pagination | Name/location; Name ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Merge previews jobs and historical label retention; no automatic same-name merge. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Company directory / merge' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX39-S04 — Linked Calendar Events

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Linked Event Title/Start/End/Status; Calendar source link; no duplicate fields |
| Entry / proposed route | /career/jobs/:jobId/events; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Linked Calendar Events. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Create Event / Link existing Event; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back to Job; Open internal Personal Event; Event owns time/status/reminder. Back/Cancel always has authorized fallback. |
| Content regions / fields | Linked Event Title/Start/End/Status; Calendar source link; no duplicate fields |
| Search / filters / sorting / pagination | End>Start; no participant account permission controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Do not auto-create third Calendar source or duplicate reminder. Feedback/meeting links excluded from shares. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Linked Calendar Events' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX39-S05 — Resume library / upload

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Name; language; latest version label; updated; active/archive |
| Entry / proposed route | /career/resumes; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Resume library / upload. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | New Resume / Upload version; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open version; Archive; Trash. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; language; latest version label; updated; active/archive |
| Search / filters / sorting / pagination | Name/language/status; Updated DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Upload clean-file gate; Document-source version selection exact and readonly, no assumption DOCX editor exists. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Resume library / upload' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX39-S06 — Resume version / share

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Exact file/document version preview; version/time; application pins; share disclosure |
| Entry / proposed route | /career/resumes/:resumeId/versions/:version; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Resume version / share. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Share this version if approved; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Download owner file; add new version; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Exact file/document version preview; version/time; application pins; share disclosure |
| Search / filters / sorting / pagination | No current-latest substitution. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Shared Resume does not expose Job tracker. Purge blocked by application/share/file version pins; Q-11 conversion formats explicit. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Resume version / share' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX39-S07 — Application timeline

| Dimension | Specification |
| --- | --- |
| Purpose / profile | HISTORY — Stage changes; reason; company snapshot; exact submitted Resume version; Interview events |
| Entry / proposed route | /career/jobs/:jobId/history; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Application timeline. Shared history profile; header → controls → declared content → feedback. |
| Primary action | Inspect evidence; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Back to Job. Back/Cancel always has authorized fallback. |
| Content regions / fields | Stage changes; reason; company snapshot; exact submitted Resume version; Interview events |
| Search / filters / sorting / pagination | Event/date; chronological/newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Private recruitment notes only owner; Support safe projection separate, not entire history by role. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 history profile. Keep 'Application timeline' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Job Title/company/sourceURL/location/workmode/type/salary text/description/dates. Company name/URL/industry/location/notes/private contact. Calendar link stores same-owner Job/Event IDs; Personal Event form owns Title/Description/Start/End/reminder. Resume name/language and clean file or exact Document version, no AI.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Pipeline Kanban/Table; job Title/company search; stage/company/date filters; updated DESC within column delegated;25/page table. Companies name ASC. Resume versions newest, exact version label/file date always visible.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Pipeline Kanban/Table; job Title/company search; stage/company/date filters; updated DESC within column delegated;25/page table. Companies name ASC. Resume versions newest, exact version label/file date always visible.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Any job stage | Explicit stage change/history; edit notes | Backward from terminal requires warning+reason, unlike Project permanent lock |
| Linked Event | Open Calendar / unlink reference | Event edits obey Calendar Scheduled/terminal lifecycle; no separate interview execution |
| Resume Active | New immutable version; pin exact application/share version | Latest version never rewrites submitted version |
| Archived/Trash | Readonly/unarchive/restore under source rules | No share of salary/contact/interview private notes |

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
| [files.FileReference](../../design-database/04-files-jobs-notifications.md#files-filereference) | Reference-aware binary retention; Technical decision |
| [career.Company](../../design-database/10-assets-career-learning.md#career-company) | Personal employer/contact directory; Technical decision |
| [career.JobApplication](../../design-database/10-assets-career-learning.md#career-jobapplication) | Personal application pipeline record; Technical decision |
| [career.CalendarLink](../../design-database/10-assets-career-learning.md#career-calendarlink) | Current own Job↔Personal Event reference; Calendar authoritative |
| [career.ApplicationEvent](../../design-database/10-assets-career-learning.md#career-applicationevent) | Immutable job pipeline timeline; Technical decision |
| [career.Resume](../../design-database/10-assets-career-learning.md#career-resume) | Personal resume family and current version pointer; Technical decision |
| [career.ResumeVersion](../../design-database/10-assets-career-learning.md#career-resumeversion) | Exact immutable uploaded resume file/version; Technical decision |
| [career.ResumeShareVersion](../../design-database/10-assets-career-learning.md#career-resumeshareversion) | Version pin supplement to common Sharing Engine; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-03](../../features/90-open-decisions.md#q-03) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.

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

[FX-39 action catalog](../../action-catalog/modules/39-career.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.

## Current FX39-S04 replacement

Linked events at /career/jobs/:jobId/events. Header Job title, primary Add event; choose Create personal Event (full internal Calendar form) or Link existing own Scheduled Personal Event. List Title/Start/End/Status from Calendar; Open stays in Nexora, Unlink removes reference only. No round/type/participant/feedback/conference form in current scope. Calendar readonly/disabled/revoked states rendered accurately; no duplicate reminder; missing source generic unavailable; all keyboard/mobile/back/dirty/conflict patterns apply. See [format/Calendar contract](../../features/95-docx-md-and-internal-calendar.md).
