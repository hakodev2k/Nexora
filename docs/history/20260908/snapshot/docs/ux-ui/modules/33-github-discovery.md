# FX-33 — GitHub Discovery — UX/UI Specification

> **Current decision amendment — 2026-09-07:** Internal saved/public-metadata views only; outbound ingestion boundary pending, no external-open or OAuth/write. [Normative PO decisions](../../requirements/10-owner-decisions-20260907.md). Conflicting older proposal paragraphs below are historical; current field/action overrides are in the linked delta. Docs-only.

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

Public GitHub discovery: Top10New and Weekly created-window ranking by total stars, not star gains.

## 2. Requirement sources

- [FX-33 feature](../../features/33-github-discovery.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-33-BR-001`, `FX-33-BR-002`, `FX-33-BR-003`, `FX-33-BR-004`, `FX-33-BR-005`, `FX-33-BR-006`, `FX-33-AC-001`, `FX-33-AC-002`, `FX-33-AC-003`, `P06-GHA-001`, `P06-GHA-002`, `P06-GHA-003`, `P06-GHA-004`, `P06-GHD-001`, `P06-GHD-002`, `P06-GHD-003`, `P06-GHD-004`, `P06-GHD-005`, `P06-GHD-006`, `P06-GHR-001`, `P06-GHR-002`, `P06-GHR-003`, `P06-GHR-004`, `P06-GHR-005`

## 3. Reference products

- [GitHub Search API](https://docs.github.com/en/rest/search/search) — checked2026-09-07. Evidence limit: Official API docs, not visual Explore UI evidence; Nexora layout delegated.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [GitHub Search API](https://docs.github.com/en/rest/search/search) | Search supports qualifiers, explicit sorting and incomplete-results indication. | ADAPT | Public created-this-week sorted total-stars metric, timestamp/partial state; reject OAuth/private/write scope. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX33-S01 — Discovery feed: BROWSE profile.
- FX33-S02 — Repository detail: DETAIL profile.
- FX33-S03 — Saved queries / repositories: BROWSE profile.
- FX33-S04 — Ranking snapshots: BROWSE profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX33-S01 | Discovery feed | /developer/github | New/Weekly tabs; rank1..10; fullName; description; language; total stars; created date; captured time | Open repository |
| FX33-S02 | Repository detail | /developer/github/repositories/:repositoryId | Public metadata; stars/forks/subscribers if known; topics/license; source URL; freshness | Open on GitHub |
| FX33-S03 | Saved queries / repositories | /developer/github/saved | Saved query filters/version or repo safe labels/notes | Run query / Open repository |
| FX33-S04 | Ranking snapshots | /developer/github/snapshots | Window; query version; captured time; completeness; rank metrics | Compare compatible snapshots |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Discovery → choose New/Weekly filters → view timestamped Top10 → repository detail → save repo/query → compare compatible snapshots.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX33-S01 — Discovery feed

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — New/Weekly tabs; rank1..10; fullName; description; language; total stars; created date; captured time |
| Entry / proposed route | /developer/github; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Discovery feed. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open repository; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit filters; save query; refresh allowed. Back/Cancel always has authorized fallback. |
| Content regions / fields | New/Weekly tabs; rank1..10; fullName; description; language; total stars; created date; captured time |
| Search / filters / sorting / pagination | Language/topic/window; explicit metric sort; Top10no pagination. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Weekly UI explains created this UTC week, ranked by total stars. Partial result banner even if ten rows available but provider incomplete. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Discovery feed' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX33-S02 — Repository detail

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Public metadata; stars/forks/subscribers if known; topics/license; source URL; freshness |
| Entry / proposed route | /developer/github/repositories/:repositoryId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Repository detail. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Open on GitHub; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Save in Nexora; note; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Public metadata; stars/forks/subscribers if known; topics/license; source URL; freshness |
| Search / filters / sorting / pagination | No OAuth permission request. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Stars/forks/subscribers distinct; missing subscriber metric Unknown, not reuse watchers_count blindly. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Repository detail' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX33-S03 — Saved queries / repositories

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Saved query filters/version or repo safe labels/notes |
| Entry / proposed route | /developer/github/saved; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Saved queries / repositories. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Run query / Open repository; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Rename; remove saved ref. Back/Cancel always has authorized fallback. |
| Content regions / fields | Saved query filters/version or repo safe labels/notes |
| Search / filters / sorting / pagination | Name/language search; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Saved state private, source public; remove ref does not alter GitHub. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Saved queries / repositories' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX33-S04 — Ranking snapshots

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Window; query version; captured time; completeness; rank metrics |
| Entry / proposed route | /developer/github/snapshots; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Ranking snapshots. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Compare compatible snapshots; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Open snapshot; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Window; query version; captured time; completeness; rank metrics |
| Search / filters / sorting / pagination | Window/query/time; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Different windows/filters cannot produce misleading rank/star deltas; incompatible compare disabled with reason. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Ranking snapshots' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Public filters language/topics/date window supported by provider contract. Week Monday00:00UTC→nextMonday, sort total Stars then created desc thenId. No OAuth, private repos, writes or starred-at-GitHub action. Saved filter name<=100.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Top10only; explicit total-stars label and window. Provider15min cache delegated; partial/incomplete/rate limit visible. Local saved repos search FullName/title;25/page.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Top10only; explicit total-stars label and window. Provider15min cache delegated; partial/incomplete/rate limit visible. Local saved repos search FullName/title;25/page.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Complete snapshot | Inspect rank/metrics/source link | Do not label total-stars rank as weekly star growth |
| Partial/rate-limited | Show partial/stale timestamp and retry-after | No fabricated full Top10 or zero missing metric |
| Saved repository | Owner notes/favorite/query | No external GitHub write |
| Repository removed/private | Safe cached public snapshot with unavailable label under policy | No private-data fetch |

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
| [discovery.SavedQuery](../../design-database/06-documents-knowledge-discovery.md#discovery-savedquery) | Named validated query, not raw SQL; Technical decision |
| [developer.Repository](../../design-database/08-news-shopping-developer.md#developer-repository) | Public GitHub metadata cache; Technical decision |
| [developer.RankingSnapshot](../../design-database/08-news-shopping-developer.md#developer-rankingsnapshot) | Reproducible public ranking window snapshot; Technical decision |
| [developer.RankingEntry](../../design-database/08-news-shopping-developer.md#developer-rankingentry) | Ordered public repository snapshot metrics; Technical decision |
| [developer.SavedRepository](../../design-database/08-news-shopping-developer.md#developer-savedrepository) | Personal bookmark of public GitHub repository; Technical decision |
| [operations.SystemJob](../../design-database/04-files-jobs-notifications.md#operations-systemjob) | System-scoped public-cache/maintenance job, never fake owner; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

No additional major product question identified for this module beyond shared security/capacity implementation gates. Do not reopen routine UX details already delegated.

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

[FX-33 action catalog](../../action-catalog/modules/33-github.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
