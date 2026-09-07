# FX-29 — News / Feeds — UX/UI Specification

Review 2026-09-07 · Baseline `b85f0f314da8ca7dcee8dad156e7b538ba52c287` · Documentation only; no schema, migrations or application code executed.

Approved source behavior remains Approved; routine interaction choices below are Resolved delegated under DEC-GOV-001. Major Q-linked behaviors remain Proposed/Blocked. Route strings are navigation proposals, not existing routes or API endpoints.

## 1. Scope

RSS/Atom sources, sanitized article reader and private read/save/topic-watch state.

## 2. Requirement sources

- [FX-29 feature](../../features/29-news-and-feeds.md) — business fields, rules and AC remain authoritative.
- [Common behavior](../../features/00-shared-behavior.md) and [source routing](../../features/93-requirement-routing.md).
- [Current requirements](../../requirements/00-product-charter.md), [decision queue](../../features/90-open-decisions.md), [reconciliation](../../design-review/02-reconciliation.md).
- [Normative common screen contracts](../global/15-screen-contracts.md); [database feature coverage](../../design-database/00-overview.md).

Source identifiers reviewed: `DEC-GOV-001`, `FX-29-BR-001`, `FX-29-BR-002`, `FX-29-BR-003`, `FX-29-BR-004`, `FX-29-BR-005`, `FX-29-BR-006`, `FX-29-AC-001`, `FX-29-AC-002`, `FX-29-AC-003`, `P05-ART-001`, `P05-ART-002`, `P05-ART-003`, `P05-ART-004`, `P05-ART-005`, `P05-ART-006`, `P05-ART-007`, `P05-ART-008`, `P05-ART-009`, `P05-FED-001`, `P05-FED-002`, `P05-FED-003`, `P05-FED-004`, `P05-FED-005`, `P05-FED-006`, `P05-NEW-001`, `P05-NEW-002`, `P05-NEW-003`, `P05-NEW-004`, `P05-RFS-001`, `P05-RFS-002`, `P05-RFS-003`, `P05-RFS-004`, `P05-RFS-005`

## 3. Reference products

- [Feedly](https://docs.feedly.com/article/288-how-to-follow-a-feed-in-your-feedly-account) — checked2026-09-07. Evidence limit: Official help text.

## 4. Reference behavior analysis

| Product | Observed behavior | Classification | Nexora decision |
| --- | --- | --- | --- |
| [Feedly](https://docs.feedly.com/article/288-how-to-follow-a-feed-in-your-feedly-account) | Follow a found feed and place it in a folder/category. | ADAPT | RSS/Atom source preview/category; reject newsletter/RSS-builder/AI scope without approval. |

No reference grants new scope, sharing rights, provider budget, retention or financial/security semantics. Explicit rejected behavior is listed in the Nexora-decision column; unapproved Q capabilities remain Proposed, not Resolved.

## 5. UX principles for this module

Make the source and current state clear before actions. Keep primary work and its source-authority boundary visible. Use common primitives; no separate module visual language.

## 6. Information architecture

- FX29-S01 — Sources: BROWSE profile.
- FX29-S02 — Follow source form: FORM profile.
- FX29-S03 — Article list: BROWSE profile.
- FX29-S04 — Article reader: DETAIL profile.
- FX29-S05 — Topic watches / matches: FORM profile.

These are screen surfaces, not necessarily separate backend resources; create/edit or views may share a route with distinct state. Module root belongs to [global IA](../global/01-information-architecture.md); no Workspace menu.

## 7. Screen inventory

| Screen ID | Screen | Route proposal | Purpose / data focus | Primary action |
| --- | --- | --- | --- | --- |
| FX29-S01 | Sources | /news/sources | Feed title/URL; category; enabled; last success; error/stale label | Follow feed |
| FX29-S02 | Follow source form | /news/sources/new | URL; detected feed title/items preview; category; title override | Follow source |
| FX29-S03 | Article list | /news | Headline; source; published/fetched time; unread marker; saved marker; freshness | Open reader |
| FX29-S04 | Article reader | /news/articles/:articleId | Sanitized article; headline; source/time; original URL; private read state | Mark read / Open original |
| FX29-S05 | Topic watches / matches | /news/watches; /news/watches/:watchId | Title; include/exclude literals; Any/All; source choices; match history | Save watch |

## 8. Navigation

Entry from global registered module, source link or authorized deep link. Each Screen has explicit Back/Cancel below; in-module return preserves query/view/date/scroll. Public/Support/Emergency routes keep their own access context, never switch silently to owner API. Create/Edit dirty-leave follows UX-05.

## 9. Primary user journeys

**Primary:** Sources → Follow URL → preview feed/category → article list → reader → read/save; Topic Watch → literal include/exclude → future matches.

**Alternative:** open existing resource from authorized Search/Favorite/notification/source link; resolve current lifecycle before rendering primary action. If this is an operational/auth screen, use its authorized parent navigation rather than inventing a favorite/shareable resource.

**Empty:** no owner data → declared New/Add action only if allowed; no matching filters → clear filters; provider-only screen → explain source/refresh, not fake CRUD.

**Error:** failed query/provider → safe message and retry; failed mutation keeps authorized input/original row; conflict → compare/reload, never blind overwrite.

**Destructive:** select actual permitted operation from section15 → preview affected resources using section16 → revalidate → confirmed result or blocked explanation. A source without destructive action is N/A, not generic Delete.

**Permission/readonly:** recheck actor/module/source; lifecycle banner and source-safe inspect only; expired/revoked mode clears data and exits authorized parent.

## 10. Screen specifications

All screens below inherit every state/layout/keyboard/exit rule in [UX-15A](../global/15-screen-contracts.md). The following rows bind concrete content, commands, controls and exceptions; inheritance is normative, not a placeholder.

### FX29-S01 — Sources

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Feed title/URL; category; enabled; last success; error/stale label |
| Entry / proposed route | /news/sources; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Sources. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Follow feed; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Edit override; pause/unfollow; refresh. Back/Cancel always has authorized fallback. |
| Content regions / fields | Feed title/URL; category; enabled; last success; error/stale label |
| Search / filters / sorting / pagination | Title/URL/category/state; title ASC25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Unfollow does not remove public cached article for other users. Feed errors inline, no silent deletion. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Sources' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX29-S02 — Follow source form

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — URL; detected feed title/items preview; category; title override |
| Entry / proposed route | /news/sources/new; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Follow source form. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Follow source; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Validate URL; Cancel. Back/Cancel always has authorized fallback. |
| Content regions / fields | URL; detected feed title/items preview; category; title override |
| Search / filters / sorting / pagination | Guarded fetch explicit. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | No valid RSS/Atom shows actionable invalid-feed error; do not fall back to unapproved scraping. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Follow source form' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX29-S03 — Article list

| Dimension | Specification |
| --- | --- |
| Purpose / profile | BROWSE — Headline; source; published/fetched time; unread marker; saved marker; freshness |
| Entry / proposed route | /news; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Article list. Shared browse profile; header → controls → declared content → feedback. |
| Primary action | Open reader; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Mark read/unread; Save for later. Back/Cancel always has authorized fallback. |
| Content regions / fields | Headline; source; published/fetched time; unread marker; saved marker; freshness |
| Search / filters / sorting / pagination | Source/category/date/read; Title search; newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Refreshing source preserves reading position/selection; failure retains labeled stale article list. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 browse profile. Keep 'Article list' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX29-S04 — Article reader

| Dimension | Specification |
| --- | --- |
| Purpose / profile | DETAIL — Sanitized article; headline; source/time; original URL; private read state |
| Entry / proposed route | /news/articles/:articleId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Article reader. Shared detail profile; header → controls → declared content → feedback. |
| Primary action | Mark read / Open original; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Save; Read Later; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Sanitized article; headline; source/time; original URL; private read state |
| Search / filters / sorting / pagination | No script/embed execution. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Missing body offers safe excerpt/original link. No Article mutation or imported third-party comments. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 detail profile. Keep 'Article reader' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

### FX29-S05 — Topic watches / matches

| Dimension | Specification |
| --- | --- |
| Purpose / profile | FORM — Title; include/exclude literals; Any/All; source choices; match history |
| Entry / proposed route | /news/watches; /news/watches/:watchId; module/source navigation or authorized deep link. |
| Header / layout | Screen title: Topic watches / matches. Shared form profile; header → controls → declared content → feedback. |
| Primary action | Save watch; available only when section15/context permits; otherwise explain lifecycle/policy. |
| Secondary actions | Enable/disable; inspect match; Back. Back/Cancel always has authorized fallback. |
| Content regions / fields | Title; include/exclude literals; Any/All; source choices; match history |
| Search / filters / sorting / pagination | Source searchable; match newest25/page. Controls not listed here are N/A, not implicit new fields. |
| Interaction / validation overrides | Validation previews matching syntax, not promise of historic alerts. Same watch/article/revision deduped. |
| Loading / empty / error | UX-15A state contract. Empty: declared data absent; no matches: clear listed query controls; fetch error: safe retry. These are distinct, no error-as-empty. |
| Disabled / readonly / Archived / Trash | UX-15A plus exact section15 matrix. No lifecycle in source = N/A. Do not render unauthorized payload behind disabled controls. |
| Conflict / destructive | Current revision and parent guards, section16 dialogs. Readonly surfaces only refresh, never forced write. |
| Desktop / tablet / mobile | UX-10 form profile. Keep 'Topic watches / matches' context and required fields; mobile prioritizes first declared identity and status/time/value, remaining fields detail/expand.  |
| Keyboard / accessibility | UX-11 and profile: visible focus, labeled controls, no drag/hover-only action, status text; dialog focus trap/return; charts/table values reachable. |
| Exit / return | Back/Cancel → actual invoking screen with view/filter/date/scroll restored; direct deep link → module root or safe Home/Admin landing; dirty form guard and revoked-data clear take precedence. |

## 11. Forms and validation

Feed HTTP(S) URL required; optional title override/category. TopicWatch Title and include phrases with explicit Any/All, exclude phrases and selected sources; no AI inference. Source fetch guards and budgets independent from UI. Owner manual overrides preserved.

[Common forms](../global/05-forms-and-validation.md) define field error timing, Save, cancellation, stale session and conflict; DB required technical columns are server-owned, never rendered as form fields.

## 12. Lists / Grid / Table / Kanban behavior

Feeds category/source/status; article Title search/date/read-state filters; Published DESC fallback fetched time;25/page; last successful fetch visible; source health is not article read state.

Use [UX-06](../global/06-lists-grids-tables-kanban.md). Only views declared above exist; no Kanban simply because resource has a status. Unlisted view types N/A; no independent custom component fork.

## 13. Search / Filter / Sort

Feeds category/source/status; article Title search/date/read-state filters; Published DESC fallback fetched time;25/page; last successful fetch visible; source health is not article read state.

Search/filter are owner and location scoped; reset cursor after query change, preserve draft separately. Date filters overlap unless explicit Calendar ICS fully-contained export. Status vocabulary is module-specific under shared semantic tokens, not all Completed states identical.

## 14. Lifecycle UX

Each row below is source-defined state/context, not a client-only flag. Parent gates and current server capability override otherwise available actions. Archive, terminal, Trash and purge remain distinct. 

## 15. Action matrix

| State / context | Available actions | Denied / UX explanation |
| --- | --- | --- |
| Source healthy | Read current articles; follow/unfollow; refresh within budget | No newsletter/RSS-builder by implication |
| Source stale/failed | Retain last successful articles with timestamp; retry | Not empty feed or zero items |
| Article unread/read | Mark read/unread; save; Read Later | User state private, not global |
| Topic watch enabled | Match future articles; alert all3channels | No historical alert flood or AI-generated match |

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
| [news.Feed](../../design-database/08-news-shopping-developer.md#news-feed) | Shared public-feed fetch metadata, no private subscriptions; Technical decision |
| [news.FeedSubscription](../../design-database/08-news-shopping-developer.md#news-feedsubscription) | Private owner feed following and category; Technical decision |
| [news.Article](../../design-database/08-news-shopping-developer.md#news-article) | Sanitized public article cache; Technical decision |
| [news.ArticleState](../../design-database/08-news-shopping-developer.md#news-articlestate) | Private read/saved state; Technical decision |
| [news.TopicWatch](../../design-database/08-news-shopping-developer.md#news-topicwatch) | Literal keyword topic alert definition; Technical decision |
| [news.TopicMatch](../../design-database/08-news-shopping-developer.md#news-topicmatch) | Alert match deduplication and provenance; Technical decision |
| [operations.SystemJob](../../design-database/04-files-jobs-notifications.md#operations-systemjob) | System-scoped public-cache/maintenance job, never fake owner; Technical decision |

No direct table access from frontend/another module. [Architecture command/query contract](../../architecture/02-module-boundaries.md) and [transaction boundaries](../../design-database/11-relations-and-transactions.md) govern source mutations.

## 22. UX decisions made by delegated authority

Screen grouping/routes, shared profile selection, action placement, empty/error wording, explicit keyboard alternatives, focus return, sensible column priority and preview anatomy are **Resolved delegated**. Existing feature defaults remain, not newly PO-approved. Reference-specific scope/cost/permissions/privacy/lifecycle/financial changes are not delegated. See [normalized decision register](../decisions/ux-decisions.md).

## 23. Major open questions

- [Q-07](../../features/90-open-decisions.md#q-07) — see [cross-layer impact/options](../../design-review/03-decision-impact.md); affected behavior remains Proposed/Blocked.
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

## Canonical action binding — catalog v1

[FX-29 action catalog](../../action-catalog/modules/29-news.md) and [screen bindings](../../action-catalog/06-screen-bindings.md) define exact keys, grantable contexts and Q gates. Descriptive verbs above are not permission names. Admin Self also needs explicit allowed action; User Self uses enabled-module owner baseline. SuperAdmin alone changes role/module/action grants. Local UI visibility does not replace server authorization.
