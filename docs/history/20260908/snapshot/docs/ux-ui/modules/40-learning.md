# FX-40 — Learning — UX/UI Specification

> **Current decision amendment — 2026-09-07:** Language/internal-link rules apply; sensitive sharing remains gated and no new external LMS integration. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Personal skills/courses/certifications/learning plans/work logs with explicit progress, not a full teaching LMS.

## 2. Requirement sources

- [FX-40 feature](../../features/40-learning-and-work-log.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-40-BR-001`, `FX-40-BR-002`, `FX-40-BR-003`, `FX-40-BR-004`, `FX-40-BR-005`, `FX-40-BR-006`, `FX-40-AC-001`, `FX-40-AC-002`, `FX-40-AC-003`, `FX-40-AC-004`, `P07-CERF-001`, `P07-CRS-001`, `P07-LRN-001`, `P07-SKL-001`, `P07-WRK-001`

## 3. Reference products

- [Moodle](https://docs.moodle.org/502/en/Activity_completion) — checked2026-09-07. Evidence limit: Official docs text; no automatic completion rule imported.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Moodle](https://docs.moodle.org/502/en/Activity_completion) | Completion criteria can include a learner marking an activity complete. | ADAPT | Explicit owner milestones/progress and manual Course completion; reject teacher override, grading and LMS delivery. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX40-S01 — Learning overview: DASHBOARD profile.
- FX40-S02 — Skills / evidence / merge: FORM profile.
- FX40-S03 — Course form / progress: FORM profile.
- FX40-S04 — Certifications / renewal: FORM profile.
- FX40-S05 — Learning plans: DETAIL profile.
- FX40-S06 — Work log form / list: FORM profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX40-S01 | Learning overview | /learning | Active courses/plans; certification expiries; recent work logs; safe progress | Add Course / WorkLog |
| FX40-S02 | Skills / evidence / merge | /learning/skills; /learning/skills/:skillId | Name; self-level; description; typed evidence; merge preview | Save Skill / Record evidence |
| FX40-S03 | Course form / progress | /learning/courses/new; /learning/courses/:courseId | Name/provider/URL; progress mode; manual percent or milestone rows; status/dates/notes | Save / Record progress |
| FX40-S04 | Certifications / renewal | /learning/certifications; /learning/certifications/:certificationId | Title/issuer; masked credentialID; verification URL; optional issue/expiry; files; previous evidence | Save certification / Record renewal |
| FX40-S05 | Learning plans | /learning/plans; /learning/plans/:planId | Plan period/status; ordered existing Course/Skill/Task refs; target dates; source progress | Add existing resource |
| FX40-S06 | Work log form / list | /learning/work-log; /learning/work-log/new | Date; title/notes; duration or linked TimeEntry; category; Project/Course/Task refs; employer text | Save log |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Learning → create Course/Skill → record milestones/work log → explicit completion → certification evidence/expiry; plan links existing resources.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX40-S01 — Learning overview

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DASHBOARD — Active courses/plans; certification expiries; recent work logs; safe progress |
| Entry / proposed route | /learning; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Learning overview. Shared dashboard profile; header → controls → declared content → feedback. |
| Primary action | Add Course / WorkLog; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Skills; Certifications; Plans. Back/Cancel always has authorized fallback. |
| Content regions / fields | Active courses/plans; certification expiries; recent work logs; safe progress |
| Search / filters / sorting / pagination | Tab/range source-specific controls. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Attention summaries link to source; no course content hosting, teacher grading or social leaderboard. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 dashboard profile. Keep 'Learning overview' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX40-S02 — Skills / evidence / merge

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Name; self-level; description; typed evidence; merge preview |
| Entry / proposed route | /learning/skills; /learning/skills/:skillId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Skills / evidence / merge. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save Skill / Record evidence; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Merge; Archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name; self-level; description; typed evidence; merge preview |
| Search / filters / sorting / pagination | Name unique normalized; evidence type picker. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Merge explicit affected evidence/history; no inferred proficiency from total hours. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Skills / evidence / merge' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX40-S03 — Course form / progress

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Name/provider/URL; progress mode; manual percent or milestone rows; status/dates/notes |
| Entry / proposed route | /learning/courses/new; /learning/courses/:courseId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Course form / progress. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save / Record progress; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Complete explicitly; Abandon; Archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Name/provider/URL; progress mode; manual percent or milestone rows; status/dates/notes |
| Search / filters / sorting / pagination | Milestone reorder buttons; progress0..100. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Mode immutable after first progress. Empty milestones No milestones, not100%; complete requires explicit action. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Course form / progress' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX40-S04 — Certifications / renewal

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title/issuer; masked credentialID; verification URL; optional issue/expiry; files; previous evidence |
| Entry / proposed route | /learning/certifications; /learning/certifications/:certificationId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Certifications / renewal. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save certification / Record renewal; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Preview file; Archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title/issuer; masked credentialID; verification URL; optional issue/expiry; files; previous evidence |
| Search / filters / sorting / pagination | Expiry filter/list due ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Renewal appends history and invalidates old expiry reminder; no claim credential verified by Nexora. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Certifications / renewal' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX40-S05 — Learning plans

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Plan period/status; ordered existing Course/Skill/Task refs; target dates; source progress |
| Entry / proposed route | /learning/plans; /learning/plans/:planId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Learning plans. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Add existing resource; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Reorder; remove ref; complete/archive; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Plan period/status; ordered existing Course/Skill/Task refs; target dates; source progress |
| Search / filters / sorting / pagination | Title/status/target date; rank. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Plan target date not Task reschedule; removing plan/ref never deletes Course/Task. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Learning plans' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX40-S06 — Work log form / list

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Date; title/notes; duration or linked TimeEntry; category; Project/Course/Task refs; employer text |
| Entry / proposed route | /learning/work-log; /learning/work-log/new; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Work log form / list. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save log; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit with history; Archive/Trash; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Date; title/notes; duration or linked TimeEntry; category; Project/Course/Task refs; employer text |
| Search / filters / sorting / pagination | Date/category/source; date DESC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unique TimeEntry link prevents double counting; gross time and overlap warning, no payroll calculation. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Work log form / list' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Skill normalized unique name and self-assessed Beginner/Intermediate/Advanced/Expert; evidence links. Course name/status, ManualPercent0..100 or Milestones mode immutable after first progress. Certification issuer/credentialID sensitive/URL/dates/files. WorkLog date, duration>0<=24h per entry or unique linked TimeEntry, category/Project/employer/notes.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Separate tabs Skills,Courses,Certifications,Plans,WorkLog; Title/name search; status/type/date/expiry filters relevant tab; Updated DESC, expiry ASC for certs, date DESC logs;25/page. No implicit monetary payroll.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Separate tabs Skills,Courses,Certifications,Plans,WorkLog; Title/name search; status/type/date/expiry filters relevant tab; Updated DESC, expiry ASC for certs, date DESC logs;25/page. No implicit monetary payroll.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Course Planned/InProgress/Completed/Abandoned | Record progress; explicitly change state; archive | 100% not auto-complete; mode locked after progress |
| Skill | Manual level/evidence; explicit merge | No automatic level inferred from hours |
| Certification | Record renewal/new evidence | Keep prior expiry/history, invalidate old alert |
| Archived | Readonly; Unarchive prior state; Trash | No edits |
| Plan source unavailable | Show broken ref/remove link | No fake completion or deleting source |

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
| [learning.Skill](../../design-database/10-assets-career-learning.md#learning-skill) | Personal skill level and evidence; Technical decision |
| [learning.SkillEvidence](../../design-database/10-assets-career-learning.md#learning-skillevidence) | Typed evidence linking existing owner resource; Technical decision |
| [learning.Course](../../design-database/10-assets-career-learning.md#learning-course) | Personal course tracking, not LMS content hosting; Technical decision |
| [learning.CourseMilestone](../../design-database/10-assets-career-learning.md#learning-coursemilestone) | Course progress unit; Technical decision |
| [learning.Certification](../../design-database/10-assets-career-learning.md#learning-certification) | Personal certification and expiry evidence; Technical decision |
| [learning.Plan](../../design-database/10-assets-career-learning.md#learning-plan) | Personal learning plan referencing existing resources; Technical decision |
| [learning.PlanItem](../../design-database/10-assets-career-learning.md#learning-planitem) | Ordered plan resource reference; Technical decision |
| [learning.WorkLog](../../design-database/10-assets-career-learning.md#learning-worklog) | Personal learning/work reflection; Technical decision |
| [learning.CertificationVersion](../../design-database/10-assets-career-learning.md#learning-certificationversion) | Renewal and certification evidence history; Technical decision |
| [operations.ResourceReminderRule](../../design-database/04-files-jobs-notifications.md#operations-resourcereminderrule) | Registered module expiry/daily reminder configuration, not standalone Reminder product; Technical decision |

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

[FX-40 action catalog](../../action-catalog/modules/40-learning.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
