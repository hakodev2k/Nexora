# Existing-feature completion scope — 2026-10-25

Status: resumable source-based execution boundary, **not full R1 acceptance**.

## Task brief / interpretation

Accountable coordinator: root; scope inventory: shopping_completion. Starting source: PR4 `impl/m01-s00-scaffold`, `a0215f7b3e85d33503cff9077b41cc065240cb81` plus concurrent assigned working changes. The current instruction authorizes completing and verifying **existing started workflows**, not starting every missing sibling under an IN_PROGRESS parent module. DEC014 automated-QA amendment supersedes former code-only restrictions; D01–D09, owner isolation, no real providers/production/data/secrets/merge and human final product acceptance remain. This task creates a scope report only; no application behavior, grant or migration changes.

Loaded current AGENTS, engineering skill/profile/Technical Lead core, routing/frontend rules, docs README/delivery/current scope/goals, DEC014 and D01–D09, ledger04 and reports16/19/20. Goals NXG-SYS02/03/05/08/12/13/14/16 and each selected FX G01/G02/G03 summarize the linked source requirements; they do not authorize siblings. Requirement/AC pointers below refer to current `docs/features/<NN>-*.md` **business rules and acceptance sections**, matching `docs/action-catalog/modules/<NN>-*.md`, and `docs/ux-ui/modules/<NN>-*.md`. These are normative inputs, not assertions that every AC has been met.

Source inspection: actual `Program.cs` endpoint registrations; endpoint files/methods; SQL service inventory; App route/component dispatch and native screen files; current three-slice contracts and handoffs. This is bounded inventory/reconciliation, not deep execution of every handler. Do not substitute source presence for working integration or copy report16's lexical-token findings as semantic proof.

Classification is two-dimensional:

- **ACTIVE / IN SCOPE**: started installed workflow; code-completeness certification remains open. This does not mean unimplemented or broken.
- **IMPLEMENTED INCOMPLETE / IN SCOPE**: a concrete gap is already identified inside that started workflow; fix or prove the diagnosis before completion.
- **OUT — UNSTARTED**: sibling has no established installed workflow in this inspection. Do not start it under this run. It is still recorded R1 backlog unless PO changes that commitment.
- **GATED** / **NEEDS DECISION**: retain the exact product/security/readiness boundary; no authorization inference from scaffolding or a module status.
- **IMPLEMENTED COMPLETE** is reserved for an exact bounded requirement set with reviewed implementation. This inventory does not newly certify any such set. QA VERIFIED, HUMAN ACCEPTED and RELEASE READY remain separate statuses.

`A` below means ACTIVE; `I` means IMPLEMENTED INCOMPLETE. Every A/I row is IN SCOPE **only for the listed started operations**, including their necessary error/permission/replay/recovery/UI integration. Separate missing product capabilities in the final column are not silently absorbed. All rows retain current-source QA/UX uncertainty until a final evidence record replaces it. A single CRUD family row explicitly enumerates its operation set; the canonical exact action IDs remain in report16/catalog rather than being renamed here.

## Resumable workflow inventory — all 40 module groups

Companion [scope CSV](21-existing-feature-completion-scope-20261025.csv) contains83 scope rows across40 module groups, including explicit absent/gated dispositions. These are execution work packages, not83 distinct accepted features. Its exact requirement/action/UX file paths were resolved from current files with0 missing sources. `ModuleACIndexNotPerWorkflowAcceptance` is the actual module AC index for navigation, **not** a claim that every indexed AC applies to or is satisfied by each workflow. The known exact subset contracts below supersede that broad navigation index for their operations.

Code shorthand is resolved to exact files in the following section. Requirements for FXnn are `docs/features/nn-*.md` BR/AC + corresponding action and UX module; source AC selection must be narrowed further in the implementing worker's contract before modifying behavior. Known exact slice bindings are listed after the table. Whole-module declarations remain historical IN_PROGRESS/gated in ledger04; they do not set row completion.

| Scope ID | FX | Existing workflow / operations to finish and verify | State | API → SQL → UI source key | Started-work gap / explicit excluded sibling |
|---|---|---|---|---|---|
| E01a |01| Register → verify/resend; login; forgot/reset password |A| Identity → Identity → App public auth screens | Local safe delivery, throttling, validation, expiry/error and return-path evidence. MFA enrollment/recovery is OUT unstarted. |
| E01b |01| Profile read/update and current session; list/revoke-one/revoke-all/logout; reauthentication |A| Identity → Identity → App Profile/Security | Current authority, stale profile/session state, private clearing. Email-change/password-change/same-account restoration UI are not implied by profile update; NEEDS exact started-source reconciliation before inclusion. |
| E02a |02| Admin user search/read; role/permission/module access preview and commit; disable |A| AdminAccess → AdminAccess → AdminAccessScreen | Last-SuperAdmin, concurrent preview invalidation, privilege and denied paths. No new bulk administration family. |
| E03a |03| Catalog read; module policy/default preview and commit; runtime availability/nav |A| Module → ModulePolicy → ModulePolicyScreen/App | Enable/disable/default invariants, stale current authority and UI refresh. No auto-enabling gated features. |
| E04a |04| Owner share preview; list/filter/page/detail/copy-capability; create/update/revoke |A| Sharing → Sharing → App Sharing | D07 own inactive metadata/null token; opaque lifetime/epoch/replay proof. Sensitive unapproved field projections GATED D05. |
| E04b |04| Read-only token resolution and source projection, login return |A| Sharing → Sharing → SharedResourceScreen | Revocation/deletion/epoch permanent denial; no resurrected token or secret/export path. New source providers OUT unless already registered. |
| E05a |05| Consent grants create/list/revoke; Support sessions create/list/end |GATED (original inventory: A)| Support → Support → App Support | Oct27 correction: endpoint/SQL/UI source exists but migration0018 keeps FX05 Blocked/off; no later readiness migration. Positive consent/recovery is not runnable through legitimate current authority. No readiness/grant promotion; D05 sensitive projections remain denied. See workers/support-gate-20261027.md. |
| E05b |05| Emergency read-only access request and session end |GATED (original inventory: A)| Support → Support → App Support | Oct27 correction: FX05 catalog Blocked; OpenEmergency explicitly returns DecisionBlocked pending duration/recent-auth Q02, with Q04/D05 projection gates. Source presence never proves an active workflow. No break-glass policy expansion. See workers/support-gate-20261027.md. |
| E06a |06| Inbox list; read/unread; mark-all; delete; current source-open |A| Notification → Notification → App Notifications | Source unavailable states and per-type safe navigation; no arbitrary operator publish API. |
| E06b |06| Existing trusted Focus/Reminder/Automation local inbox projection writers |A (bounded source correction Oct27)| LocalModuleNotificationWriter + SqlReminderService → real SQL Notification/Delivery → inbox | Actual existing effects are transactional InApp projections with truthful unavailable Email/Push; FN-042 has real worker/browser/SQL proof. Generic FX06 channel-delivery worker is unstarted OUT, not implied by original wording. Account-message transport remains separate FX01. Writer-specific dedupe/current-authority/rollback evidence required; no invented lease/provider behavior. See workers/notification-trusted-writers-20261027.md. |
| E07a |07| Upload session/create/content/cancel; file list/detail/download |A| File → File → App Files | Clean/quarantine/rejected/retained paths; Windows three-principal storage gate remains open until actual proof. |
| E07b |07| Attach/detach existing allowed source references; preview/trash/restore/purge |A| File → File → App Files/source controls | Owner/source authorization, retained bytes and physical cleanup evidence. New Task attachment scope GATED D03. |
| E08a |08| Trash list; existing deletion-batch restore and purge |A| Trash → Trash → App Trash | Exact source cohort/reference/retention behavior, current authority and confirmations; no generic bypass of native source rules. |
| E08b |08| Existing per-source history/activity/audit production and installed history views |A| per-source handlers → per-source SQL/audit → existing history dialogs | Verify source-specific trails only. A global standalone Audit/Activity explorer is OUT unstarted unless separate installed route is demonstrated. |
| E09a |09| Profile preferences, theme/locale/timezone; application navigation and safe deep links |A| Settings/Identity → Settings/Identity → App Profile/Preferences/shell | Persist/reload, dirty leave, current owner context, mobile/keyboard/focus, denial clearing. No Workspace/team feature. |
| E10a |10| Calendar ICS preview/import rows/commit/cancel/status |A| CalendarImport → CalendarImport → CalendarImportScreen | Safe parse/timezone/duplicate/partial outcome and current permissions. Same workflow cross-bound to FX13, not a second feature count. |
| E10b |10| Calendar ICS export preview/request/list/status/content |A| CalendarExport → CalendarExport → CalendarExportScreen | Durable worker/recovery/content authority and fidelity. |
| E10c |10| Document MD/DOCX export preview/request/status/content and import preview/commit/status |A| DocumentImport/Export → DocumentImport/Export → DocumentsScreen | Format/version fidelity and stale import preview; general cross-module backup/restore is OUT unstarted; Vault portability GATED D01. |
| E11a |11| Project list/detail/create/update |A| Productivity → Productivity → App Productivity | Validation/paging/ownership/stale update and responsive forms/cards. |
| E11b |11| Existing Project state transition/delete and resulting dependent Task behavior |A| Productivity → Productivity → App Productivity/Trash | Cohort/pins/current permissions/concurrent change and restore evidence; no unrelated project extensions. |
| E12a |12| Flat Task list/detail/create/update; current Project selection |A| Productivity → Productivity → App Productivity | D03 one Project and flat Task; source/current-state/recovery evidence. |
| E12b |12| Existing criteria, rank/reorder, state transitions, delete |A| Productivity → Productivity → App Productivity | Required criteria/state/race/restore; Task action wrapping must be recaptured after current CSS. Subtasks/recurrence/snooze/attachments GATED D03. New Kanban renderer is not authorized merely by module title. |
| E13a |13| Personal Event list/detail/create/update/transition/delete and existing Calendar views |A| Productivity → Productivity → App Productivity | UTC/local/DST/overlap/source permissions/current revision and range boundary evidence. |
| E13b |13| Installed ICS import/export workflows |A| E10a/E10b | Same source/evidence, no duplicate accepted feature count. New external calendar synchronization OUT/GATED. |
| E14a |14| Source-bound Task/Event reminder read/set/cancel; existing chooser |A| Reminder → Reminder → App Reminders/source controls | One approved reminder, timezone, stale/cancel/retry/lease authority; standalone/multiple/snooze extensions GATED D03. |
| E15a |15| Daily/weekly planner query; source pin/create/update/delete; reorder |A| Planner → Planner → App Planner | Current source visibility, no stale title leakage, stable order/replay/race and empty/error UI. |
| E16a |16| Goal list/detail/create/update; existing state transitions |A| Goals → Goal → App Goals | Owner-local dates, current revision, goal-state semantics and native forms. |
| E16b |16| Numeric/Boolean target add/update/remove; numeric/Boolean progress |A| Goals → Goal → App Goals | Exact values, change preview, progress compatibility, stale requests. Task-link/unlink family not inferred from target CRUD: OUT unstarted pending installed contract. |
| E16c |16| Target/metadata/state/progress history and archive paths already exposed |A| Goals → Goal → App Goals history | Versioned history/projection/current permissions; no new target provider types. |
| E17a |17| Habit list/detail/create/update; schedule; check-in/correction; state transitions |A| Habit → Habit → App Habits | Owner-local day/DST, validity and stale schedule, weekday touch targets, completed/paused behavior. Real reminder integration not inferred from scheduling. |
| E18a |18| Manual time entry list/detail/create/update; overlap confirmation |A| TimeTracking → TimeTracking → TimeTrackingScreen | Newly frozen unknown-save body/key/ETag and synchronous guard need current focused component/browser evidence. |
| E18b |18| Timer read/start/stop/resume-as-new |A| TimeTracking → TimeTracking → TimeTrackingScreen | Same-key retry/one running timer, restart/elapsed time. New Task/Project source-link feature OUT until existing source contract is demonstrated. |
| E18c |18| Installed gross report/history; Trash/restore/purge preview/commit |A| TimeTracking → TimeTracking → TimeTrackingScreen | Report filters/query UI completeness, historical corrections, pinned purge and safe navigation. |
| E19a |19| Focus preferences; session create/read; existing phase controls; record-time conversion |A| Focus → Focus → FocusScreen | Deterministic phases/restart/recovery/current authority, one conversion and Time dependency. No new background provider effect. |
| E20a |20| Documents list/tree/folder/page metadata/body read/create/update |A| Document/Topology → Document/Folder/Tree → DocumentsScreen | Existing editor and topology workflow require complete current user-state/UX verification; do not claim full rich-editor acceptance from body save. |
| E20b |20| Versions/history/compare and existing restore operations |A| Document/Topology → Document/Folder/Tree → DocumentsScreen | Current version, restore/pins/source permission and bounded compare fidelity. |
| E20c |20| Native archive/Trash/restore/purge plus existing tag assignment |A| Document/Topology/Tag → Document/Folder/Tree/Tag → DocumentsScreen | Known prior archive test oracle requires archived-view reopen/read-only/unarchive reconciliation; do not remove failing assertions to pass. |
| E20d |20| Installed import/export dialogs |A| E10c | Shared existing workflow; new formats/providers/transfer families OUT. |
| E21a |21| Bookmark list/detail/create/update/archive/unarchive/Trash and installed restore |A| Bookmark → Bookmark → App Bookmarks/Trash |408 unknown-save freeze, normal Template-seed create, duplicates/current authority and reload. Automatic metadata fetch/source-refresh sibling OUT/GATED. |
| E22a |22| Snippet list/detail/create/update; version/history; installed lifecycle |A| Snippet → Snippet → App Snippets/Trash | Text safety, stale update/replay and exact source states; new export/template providers OUT unstarted. |
| E23a |23| Save existing Bookmark/News reference; list/filter; progress/state update/remove; reader handoff |A| Reading → Reading/NewsReading → App ReadLater/NewsReaderScreen | Current source authority/body availability/position/clear/retry; new full-body fetch/provider engine OUT/GATED. |
| E24a |24| Tag list/detail/create/update/delete and existing Document assignment |A| Organization/DocumentTag → Tag/DocumentTag → App Tags/DocumentsScreen | D08 opaque Tag replay/current authority, source compatibility and normalization. |
| E24b |24| Collection list/detail/create/update/delete; candidates/add/remove/reorder members |A| Collection → Collection → CollectionsScreen | Existing registered provider set only; owner/current-readable source references, selection/paging/order and recovery. New provider families OUT. |
| E24c |24| Manual Bookmark-seed Template list/detail/create/update/archive/unarchive/Trash/restore/purge |A| Template → Template → TemplatesScreen | Active-hidden Archived semantics; exact previous-state restore; opaque replay, owner/current action and UX. |
| E24d |24| Template preview/use → ordinary explicit Bookmark create |A| Template + Bookmark → Template/Bookmark → TemplatesScreen/App Bookmarks | Preview writes nothing; create-only Bookmark context remains usable; seed persistence/dirty leave/current denial. Task/Document/Snippet seed providers/capture/system template families OUT unstarted. |
| E25a |25| Existing provider search/filter/paging and source-open |A| Search → Search → App Search/Resource | Private counts/current source authority, installed facets and empty/error/loading. New search-provider types OUT. |
| E25b |25| Favorites list/add/remove/reorder |A| Favorite → Favorite → App Favorites | Current-readable source/title and denied/repeated actions. |
| E25c |25| Saved search list/create/update/delete/run |A| SavedSearch → SavedSearch → App Search | Query version compatibility, replay and current source-open. |
| E25d |25| Recents list/clear-preview/clear and internal source navigation |A| Recent → Recent → RecentsScreen | Private state/context remount, bounded clear and authorization. |
| E25e |25| Command palette query/execute and keyboard navigation |A| CommandPalette → CommandPalette → App palette | Whitelisted internal action/current authority/focus; no executable-plugin capability. |
| E26a |26| Dashboard summary/refresh/widgets and installed drilldown |A| Dashboard → Dashboard → App Home | Duplicate/overflow source fixes need current populated desktop/mobile proof; source privacy/counts and drilldown consistency. |
| E26b |26| Dashboard persisted layout create/update and existing quick-create handoff |A| DashboardLayout/Dashboard → DashboardLayout/Dashboard → App Home | Same-key retry, stale layout, no duplicate create and source-specific target authorization. New widgets OUT unstarted. |
| E27a |27| Manual category list/detail/create/update/delete and record list/detail/create/update |A| Finance → Finance → App Finance | Explicit registered currency/exact decimal/local date/optional note, stale/replay and allowed/denied UI. D02 accounts/ledger/budget/debt/interest/FX/transfer advanced families GATED. |
| E27b |27| Installed manual-record currency summaries and filters |A| Finance → Finance → App Finance | Never sum currencies; source fields and date boundaries/current authority. No advanced deletion policy invention; record deletion/history endpoints were not established and are not inferred from category delete. |
| E28x |28| No registered secret-bearing workflow |GATED| No Vault endpoint registration in Program | D01 docs/ADR/threat model only; store/reveal/recovery/portability/purge remain OUT/GATED. Never add artificial secret records. |
| E29a |29| News category list/create/update/lifecycle |A| NewsCategory → NewsCategory → NewsCategoryScreen | Category unknown-save command/retry/authority and native UI evidence. |
| E29b |29| Existing source metadata CRUD/lifecycle; stored article list/detail/read/save; history/clear |A| NewsReader → NewsReader → NewsReaderScreen | Stored/local data truth and current read/save source. TopicWatch/ingestion/network freshness is OUT/GATED D04, not authorized by reader metadata. |
| E30x |30| No registered Price Tracking business workflow |OUT / GATED| Program has no PriceTracking map | New tracking/refresh/alert families OUT unstarted; real providers GATED D04. A module landing/unavailable page is not started functionality. |
| E31a |31| Wishlist list/detail/create/update; existing lifecycle previews/transitions |A| Wishlist → Wishlist → WishlistScreen | Owner/revision/replay/current lifecycle and populated UI; comparison sibling OUT. |
| E31b |31| Seller list/search/detail/create/update (public name/URL only) |A| ShoppingRecords → ShoppingRecords → ShoppingRecordsScreen(sellers) | Validate inert URL, stale/replay/denial and source picker; private contact/notes contract conflict NEEDS DECISION, merge OUT unstarted. |
| E31c |31| Draft purchase Order list/search/detail/create/update with line editing and duplicate-reference confirmation |A| ShoppingRecords → ShoppingRecords → ShoppingRecordsScreen(orders) | Exact quantity/price/totals, stable linked lines, historical seller label/current source; full populated UI flow pending. Order state transitions/returns/history/Trash/purge are OUT unstarted. |
| E31d |31| Purchase-line Warranty list/search/detail/create/update (Fixed/Lifetime/Unknown metadata) |A| ShoppingRecords → ShoppingRecords → ShoppingRecordsScreen(warranties) | Same-owner source line, fixed date range, stable pins/replay/stale/error and UI; evidence Files/expiry reminders/Asset or Finance integration OUT unstarted. |
| E32a |32| Installed bounded offline tool catalog/filter/routes; encode/decode/hash/UUID/password/JSON/regex and implemented increment tools |A| DeveloperTools → DeveloperTools seam → App DeveloperTools/toolbox client | Only tools actually enabled by current catalog/client; browser payload privacy/current SQL authority and bounds. Do not implement absent tools from the module title. |
| E32b |32| Existing text diff/color contrast/URL parser/pasted header inspector |A| DeveloperTools → DeveloperTools seam → App DeveloperTools/client tools | Browser-only payload and exact installed tool code, errors/recovery/mobile/a11y. YAML/date/cron/QR/history/favorites/output persistence families not demonstrated remain OUT unstarted. |
| E33x |33| No registered GitHub Discovery business workflow |OUT / GATED| Program has no Discovery provider map | Generic Search is FX25, not GitHub Discovery. Stored search/snapshot/saved-query family OUT unstarted; any live access GATED D04. |
| E34a |34| Existing Automation definition list/detail/create/update/validate/version/import/export/Trash/restore/purge |A| AutomationDefinition → AutomationDefinition → AutomationScreen | Local-only definitions; exact lifecycle/version/schema/receipt and UI; no real provider execution. |
| E34b |34| Existing local run start/list/detail/cancel/step-retry and enable/disable |A| AutomationRun → existing run store/executor → AutomationScreen | Durable lease/retry/authority/idempotency; simulation clearly labelled. Real side effects GATED D04. |
| E34c |34| Existing schedule read/preview/update and local worker |A| AutomationDefinition → AutomationDefinition/schedule worker → AutomationScreen | Timezone/DST/bounded catchup/recovery; no unstarted action/provider types. |
| E35a |35| Connector catalog and IntegrationConnection list/detail/create/update/disable |A| IntegrationConnection → IntegrationConnection → IntegrationConnectionsScreen | Configuration/local metadata only, disabled provider truth/current owner. OAuth/webhook/send/sync/n8n execution families OUT/GATED D04. |
| E36a |36| Monitor metadata list/detail/create/update and installed lifecycle controls |A| Monitoring → Monitoring → MonitoringScreen | Local configuration/stored state only; existing safe projection/degraded states. New target probes/live outbound execution GATED D04. |
| E37a |37| Personal Asset metadata list/detail/create/update/state transition/history |A| PersonalAsset → PersonalAsset → PersonalAssetScreen/AssetScreen | Owner fields/current lifecycle/concurrency and usable forms; serial/sensitive disclosure not implicitly shareable. |
| E37b |37| Existing Asset lifecycle previews/Trash/restore/purge |A| PersonalAsset → PersonalAsset → PersonalAssetScreen | Reference/pin/current source and historical state proof; accessory/repair/loan/Warranty integration families OUT unstarted. |
| E38a |38| Digital Asset metadata list/detail/create/update/filter |A| DigitalAsset → DigitalAsset → DigitalAssetScreen | Existing subtypes and dates only; no secret/certificate/provider inspection expansion. |
| E38b |38| Renewal record/history; current lifecycle previews/transitions/history |A| DigitalAsset → DigitalAsset → DigitalAssetScreen | Owner date/renewal immutability/revision and history; provider freshness/network/secret references OUT/GATED. |
| E39a |39| Company list/detail/create/update and merge preview/commit |A| Career → Career → CareerScreen | Owner/current source/revision/history retention and chosen merge target. |
| E39b |39| Job list/detail/create/update/stage/history and installed lifecycle previews/commits |A| Career → Career → CareerScreen | Transition invariants/filter/local dates/pins/restore/replay. Resume/versioned public sharing/application families OUT unstarted. |
| E39c |39| Job appointment list/link/unlink to existing Calendar event |A| Appointment → CareerAppointment → CareerScreen/Calendar navigation | Same-owner/current-readable event, cross-module delete and current capability, no new calendar object types. |
| E40a |40| Skill list/detail/create/update/proficiency/history/lifecycle; existing merge preview/commit/history |A| Skill → Skill → SkillScreen/LearningScreen | Evidence retention, source rewrite/current authority and replay; no new skill provider integrations. |
| E40b |40| Skill evidence list/add/remove using existing source providers |A| Skill → SkillEvidence → SkillScreen | Same-owner/readable current source and locked/reference states; new evidence providers OUT. |
| E40c |40| Course list/detail/create/update/progress/lifecycle; milestone create/update/delete/order/completion |A| Course → Course → CourseScreen/LearningScreen | Existing detail routing, unknown-save/revision, completion/progress consistency. |
| E40d |40| Manual Work Log list/detail/create/update/history/report and installed lifecycle |A| WorkLog → WorkLog → WorkLogScreen | Current linked source if permitted, corrections/date/filter/report/purge consistency. |
| E40e |40| Existing Learning Plan typed API/SQL/UI; Goals/Tasks/Courses source chooser/items/order/complete/archive/unarchive |GATED| LearningPlan → LearningPlan → LearningPlanScreen | D06/central readiness remains false. Source contract repair may be necessary, but do not enable grants/action eligibility merely to test. Plan Trash/progress enrichment are OUT unstarted. |
| E40f |40| Certification list/search/detail/create/update/immutable renewal/history/archive/unarchive |A| Certification → Certification → CertificationScreen | Current local-date status, signed preview, opaque replay, renewal history and recovery/mobile/a11y evidence. Credential encryption/Files/expiry reminder/Trash/course link siblings OUT unstarted. |

## Exact source key resolution

All API keys resolve below `src/Nexora.Api/Features/`; their endpoint extension is registered in `src/Nexora.Api/Program.cs` except explicitly absent/gated rows. SQL keys resolve to `Sql<Key>Service.cs` under `src/Nexora.Infrastructure/`, unless noted. UI names resolve below `web/Nexora.Web/src/`; `App` means the named inline screen in `App.tsx`. AdminAccessScreen, ModulePolicyScreen, DocumentsScreen and SharedResourceScreen are also inline in App.tsx, not separate files. AssetScreen is exported from PersonalAssetScreen.tsx. SQL service resolution is a concrete first-party file, not a Redis/in-memory substitute. Inspect the actual method before asserting an operation is complete.

| Key | Exact API file | SQL directory / exception |
|---|---|---|
| Identity | Identity/IdentityEndpoints.cs | Identity |
| AdminAccess | Access/AdminAccessEndpoints.cs | Access |
| Module / ModulePolicy | Modules/ModuleEndpoints.cs | Modules/SqlModulePolicyService.cs |
| Sharing | Sharing/SharingEndpoints.cs | Sharing |
| Support | Support/SupportEndpoints.cs | Support |
| Notification | Notifications/NotificationEndpoints.cs | Notifications |
| File | Files/FileEndpoints.cs | Files |
| Trash | Trash/TrashEndpoints.cs | Trash |
| Settings | Settings/SettingsEndpoints.cs | Settings |
| Productivity | Productivity/ProductivityEndpoints.cs | Productivity |
| Reminder | Reminders/ReminderEndpoints.cs | Reminders |
| Planner | Planner/PlannerEndpoints.cs | Planner |
| Goals / Goal | Goals/GoalsEndpoints.cs | Goals/SqlGoalService.cs |
| Habit | Habits/HabitEndpoints.cs | Habits |
| TimeTracking | TimeTracking/TimeTrackingEndpoints.cs | TimeTracking |
| Focus | Focus/FocusEndpoints.cs | Focus |
| Document | Documents/DocumentEndpoints.cs | Documents |
| Topology / Folder / Tree | Documents/DocumentTopologyEndpoints.cs | Documents/SqlDocumentFolderService.cs and SqlDocumentTreeService.cs |
| DocumentTag | Documents/DocumentTagEndpoints.cs | Documents |
| CalendarImport / CalendarExport / DocumentImport / DocumentExport | Transfer/<Key>Endpoints.cs | Transfer |
| Bookmark | Bookmarks/BookmarkEndpoints.cs | Bookmarks |
| Snippet | Snippets/SnippetEndpoints.cs | Snippets |
| Reading / NewsReading | Reading/ReadingEndpoints.cs | Reading/SqlReadingService.cs and SqlNewsReadingService.cs |
| Organization / Tag | Organization/OrganizationEndpoints.cs | Organization/SqlTagService.cs |
| Collection / Template | Organization/<Key>Endpoints.cs | Organization |
| Search / Favorite / SavedSearch / Recent / CommandPalette | Discovery/<Key>Endpoints.cs | Discovery |
| Dashboard / DashboardLayout | Dashboard/<Key>Endpoints.cs | Dashboard |
| Finance | Finance/FinanceEndpoints.cs | Finance |
| NewsCategory / NewsReader | News/<Key>Endpoints.cs | News |
| Wishlist / ShoppingRecords | Shopping/<Key>Endpoints.cs | Shopping |
| DeveloperTools | DeveloperTools/DeveloperToolsEndpoints.cs | DeveloperTools authority seam and existing toolbox engine; not SqlDeveloperToolsService |
| AutomationDefinition / AutomationRun | Automation/<Key>Endpoints.cs | Automation/SqlAutomationDefinitionService.cs plus existing AutomationRun store/executor/worker files |
| IntegrationConnection | Integrations/IntegrationConnectionEndpoints.cs | Integrations |
| Monitoring | Monitoring/MonitoringEndpoints.cs | Monitoring |
| PersonalAsset / DigitalAsset | Assets/<Key>Endpoints.cs | Assets |
| Career / Appointment | Career/CareerEndpoints.cs and AppointmentEndpoints.cs | Career/SqlCareerService.cs and SqlCareerAppointmentService.cs |
| Skill / SkillEvidence / Course / WorkLog / LearningPlan / Certification | Learning/<Key>Endpoints.cs (evidence routes inside SkillEndpoints) | Learning/Sql<Key>Service.cs |

Native files explicitly observed include TimeTrackingScreen, FocusScreen, ShoppingRecordsScreen, WishlistScreen, CertificationScreen, TemplatesScreen, CollectionsScreen, CareerScreen, DigitalAssetScreen, PersonalAssetScreen, CourseScreen, LearningScreen, LearningPlanScreen, SkillScreen, WorkLogScreen, NewsCategoryScreen, NewsReaderScreen, MonitoringScreen, IntegrationConnectionsScreen, AutomationScreen and CalendarImport/ExportScreen. Shell dispatch and shared source/resource-open remain part of the source path; a native file alone is not reachability proof.

### Known exact bounded contracts / AC

- E24c/d: [Template contract](workers/templates-20261025-contract.md), P03-ORG-004/005, FX24-S04/S05, BR002/003, AC001/002; nine exact `organization.template.*` actions, Bookmark seed only. Reviewed migration0093 state constraint and opaque authority-sensitive replay remain in scope.
- E31b/c/d: [Shopping contract](workers/shopping-20261025-contract.md), authoritative P05 Seller/DraftOrder/purchase-line Warranty fields/actions/UX. Nine exact `shopping.seller/order/warranty.read/create/update` actions. Migration0094, typed records and exact amount rules are installed. Full FX31 siblings remain outside this run.
- E40f: [Certification contract](workers/certifications-20261025-contract.md), P07-CERF-001, FX40-S04, BR004/006; six exact read/create/update/renew/archive/unarchive actions. Reminder AC003 is an unstarted sibling dependency, not silently accepted by metadata renewal.
- E18a/b: [recovery contract/report](workers/time-recovery-20261025-code-only.md), `FX-18-BR-001/002/003/005`, `FX-18-AC-001`, `time.timer.start`, `time.entry.create/update`, FX18-S01/S03. New permission/fault/retry proof can now be executed under amendment20; old source review/build does not become runtime proof retroactively.
- Exact per-operation test case mapping for other A rows remains a **work item**, not claimed complete by this source inventory. Use `docs/goals/05-task-and-evidence-template.md` before changing their implementation status.

## Next executable work / ownership / stop conditions

| Priority | Scope | Concrete next work | Owner / boundary |
|---|---|---|---|
|1|E20c| Reproduce/resolve prior Documents archive oracle against current archive-view contract; retain meaningful readonly/source checks and rerun affected regression | root; no assertion deletion or broad scope expansion |
|1|E18a/b/E21a| Focused component fault/replay/duplicate/denial tests for latest Time and Bookmark recovery changes; follow with actual UI/API/SQL scenarios when isolated runtime ready | learning worker + root runtime |
|1|E31b/c/d/E40f| Complete populated create→reload→edit→stale/retry/denial flows for current Seller/DraftOrder/Warranty and Certification; inspect form/mobile accessibility; fix actual started-path defects | Shopping worker local component files/tests; root shared SQL/browser launch |
|2|E09/E12/E24/E26| Verify current global form primitives, portal styles, friendly copy and Task wrapping after final edits; scoped UI remediation then affected regression | templates/UI worker, root App/global styles; avoid concurrent edits |
|2|Every A/I| Narrow candidate BR/AC to exact operation, bind handler/SQL/UI/tests, review current denial/isolation/replay/concurrency and final revision evidence; missing evidence stays NOT VERIFIED | coordinator assigns disjoint modules; do not equate existing suite PASS with coverage |
|Gate|E28x/E30x/E33x/E40e and D02/03/04/05| Retain exact OUT/GATED disposition; finish independently executable active work. Missing business/security decision cannot be invented; no grants/provider activation to unblock tests | root / PO only where exact decision genuinely needed |

No tests, fixtures or runtime launches were performed by this inventory task. Existing execution in report19 remains historical; new current-source executions belong to new worker/QA reports. No invented counts, completion percentages, module acceptance or Release Ready claims. `OUT` applies only to this mission's execution boundary: it does not defer/cancel committed R1 scope, retire an action or rewrite ledger04. Overall R1 remains HOLD; the smaller existing-workflow mission also remains incomplete until its exact selected rows receive implementation, applicable automated QA, independent review and human acceptance evidence.
