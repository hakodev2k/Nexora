# Backend Roadmap

**Status:** PLANNED API/use-case design. Không tạo source hoặc gọi endpoint runtime trong task này.
[Master phases](00-master-implementation-roadmap.md) · [Database](05-database-roadmap.md) · [Frontend](06-frontend-roadmap.md) · [Tests](08-testing-roadmap.md)

## 1. Backend implementation order

RM03 host → RM04 SQL contracts → RM05 identity/current authorization → RM06 module/shared services → RM08 Productivity → RM09 Documents/utilities và RM10 remaining Productivity → RM11 Search/Dashboard → RM12 Finance/Vault → RM13 providers → RM14 Developer/GitHub/Automation → RM15 Assets/Career → RM16 integration. Future unit/integration tests đi cùng từng phần.

Document flow dependency cụ thể: trusted DocumentType/EditorMode definitions + owner context → owned Tag/Folder + File upload staging → page create/Save/version → publish/archive/trash/share projection → list/search/sidebar UI và Global Search contribution. Media contract phải có trước cover Save; không đặt media sau hết Documents chỉ vì skeleton ví dụ.

## 2. API conventions proposal — ADR-R01/R05

| Concern | Planned contract |
|---|---|
| Transport | REST JSON /api/v1; URL conventions consistent; HTTPS; request/response schema trong OpenAPI |
| Auth | Secure cookie + anti-CSRF header cho mutation theo approved ADR; API 401/403 không redirect tới HTML login |
| Scope | Owner/context từ server; client gửi resource identifier chứ không chọn arbitrary owner/scope |
| Errors | ProblemDetails với safe code/correlationId/field errors; không stack/SQL/raw payload |
| Response semantics | 201 create; 200 read/update; 204 appropriate idempotent completion; 400 validation; 401 auth; 403 action denied; 404 inaccessible resource; 409 domain conflict; 412 stale If-Match; 429 throttled |
| Concurrency | ETag/current revision token; mutation/restore requiring precondition; no last-write-wins ngầm |
| Idempotency | Scoped key + payload hash + original outcome cho retryable create/Save/import/job command; key reused khác payload trả conflict |
| Pagination | Bounded page size/cursor, stable secondary key; exact max là approved NFR/config, default proposal ≤50 |
| Filters/search | Whitelist fields/operators/sorts; normalize query; authorize trước count/facet/pagination; date/time semantics explicit |
| Validation | Required/optional semantics theo module; no placeholder cho missing ICS fields; reject unknown immutable/security mutation fields |
| Sensitive actions | Dedicated reveal/copy/purge/export/emergency routes, audit; generic update không nhận grant/role/owner |
| Background API | 202 + runId khi dài; status owner-scoped; cancellation/retry không grant authority ngầm |
| API evolution | Version/schema compatibility; OpenAPI change review; migration và consumer test trước breaking change |

Các paths bên dưới là **proposed**, không tuyên bố endpoint đã tồn tại hoặc full schema đã frozen. Mỗi concrete story cần payload/error/permission matrix trong OpenAPI trước implement.

## 3. Business rule enforcement matrix

| Rule/source | Backend | Database | Frontend |
|---|---|---|---|
| OWN-*/AZ-*/PERM-* | Current context/action/owner/resource evaluator cho mọi path | Same-owner constraints + scoped repositories | Hide/disable controls; never sole security gate |
| P02 Task Project required/immutable | Reject missing/change/cross-owner parent, kể cả version restore | Non-null FK + immutable write guard | Required picker at create, no move action |
| P02 reverse state reason | Transition/restore policy requires meaningful reason | Immutable history revision/reason reference | Drag opens reason dialog; failed/canceled move rollback |
| P02 terminal Project | Atomic Project-state check với Task writes | Concurrency/aggregate lock; no race child write | Readonly/no create/reopen; unfinished child không continue |
| P02 out-of-range/overlap | Recheck warning condition + explicit confirmation against current version | Save atomic; no partial effect | Full form warning; cancel giữ dữ liệu cũ |
| P02 one Reminder | Replace/reschedule single logical schedule; recheck terminal source | Unique source constraint + versioned due/outbox | Exact time hoặc 15m preset; không list nhiều reminders |
| P03 manual Save/version | Distinct successful Save → one revision; retry same command → same revision | Unique revision/idempotency, immutable payload/media references | Dirty state, Save, conflict handling; no autosave |
| P03 immutable type/editor/folder/parent | Validate all write/import/restore paths | FK/cardinality/depth + immutable write guard | Explicit type/editor every create; no change/move UI |
| P03 one Tag + Icon/Cover XOR | Server validates cardinality/source/crop | Single current Tag ref, exclusive visual fields | One Tag inline create; emoji/icon OR upload+crop |
| P03 delete in-use Tag | Recheck references at commit; no untag/cascade | Restrict active approved refs; history policy Open | Explain blocker; no fake success |
| P03 listing | Root/folder scoped query, confirmed filters/search/sort | Owner/lifecycle/context indexes; stable tie | Grid default; only Title/DocumentType/Tag in Card/Table |
| P03 publish/archive/share | Publish private; Draft suspend; Archived preserves already-valid link | Share state/token independent of content revision | Separate Archived, readonly page, no new share there |
| NTF-*/DEC-NTF-* | Durable logical intent, three independent channel attempts | Unique intent/channel; retention until User delete | Inbox read/unread/all-read/single+bulk delete, open safe source |
| VSEC-*/P04-VAC-* | Owner-only dedicated reveal/copy with audit/current authority | Ciphertext envelope/key separation | Masked; no persistent browser secret; no support reveal |
| MOD-* | Package/system/user/dependency/permission gates separate | Version/migration/enablement journal | Registry navigation and safe unavailable state |

Không tự thêm module/state/action/field từ technical example. Business conflicts và additional field decisions trong 01 chặn phần tương ứng.

## 4. Feature implementation contracts

Cho mọi hàng/module dưới: **Authorization** áp dụng Self owner + effective module/action; SharedLink/Support/Emergency chỉ read projection khi type/policy đã approved; normal Admin không xem User data. **Data access** luôn SQL owner-scoped hoặc approved ephemeral client-only processing. Mọi module phải có manifest/search/widget/job/file/sharing/trash support declaration và contract tests.

### F-ID — Identity/profile/session

Phase: RM05. Dependencies: RM04; DEC-TEC-004; DEC-SEC-001/009. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | User/Profile/Session/PersonalSpace/verification records |
| Repository / Data access | Identity data access và SessionRegistry, không generic owner bypass |
| Service / Use cases | Register, Verify, Login, Revoke, Recover, Bootstrap |
| API proposal | /auth/register, /verify-email, /login, /logout, /sessions; /me/profile |
| Business Rules / Validation | Verification+owner provision atomic; generic auth errors; last admin guard |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Token replay/expiry, normalization/password policy, state transitions |
| Integration Test | SQL race verify/bootstrap/revoke; CSRF/cookie/account isolation; suite V-ID. |

### F-ACCESS — Owner/sharing/support/emergency/RBAC

Phase: RM05–RM06. Dependencies: RM04–RM05; DEC-SHR-*; DEC-SUP-*; DEC-SEC-003/008. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Share/Allowlist, SupportGrant/Session, EmergencySession, AdminGrant |
| Repository / Data access | Scoped grant lookup + current owner/resource reader |
| Service / Use cases | Authorize, Create/RevokeShare, Grant/RevokeSupport, Start/EndEmergency |
| API proposal | /shares; /s/{token}; /me/support-grants; /support/sessions; /emergency/sessions; /admin/permissions |
| Business Rules / Validation | Three link modes; one module+24h/default choices; readonly; expiry/current grants; reason/audit+all3 |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Mode/action/expiry/projection matrix; normal SuperAdmin deny |
| Integration Test | Concurrent revoke/use; wrong owner/module; file/count/child leaks; audit/notification transaction; suite V-AZ. |

### F-MOD — Module platform

Phase: RM06. Dependencies: RM05; DEC-TEC-013. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | ModuleDefinition/Version/Enablement/RegistrationDefault/MigrationState |
| Repository / Data access | ModuleRegistry + module-owned migration journal |
| Service / Use cases | Discover/Validate/Enable/Disable/Upgrade; EffectiveModules |
| API proposal | /modules/effective; /admin/modules; /admin/users/{id}/modules |
| Business Rules / Validation | Developer-only package; installed≠enabled; no data delete on disable; permission separate |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Manifest/namespace/dependency/version validation |
| Integration Test | Schema mismatch/upgrade failure; queued jobs/search/widgets disabled; re-enable data; suite V-MOD. |

### F-AUDIT — Audit/activity

Phase: RM06; RM17; RM21. Dependencies: RM05; DEC-SEC-005. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | AuditEvent/ActivityEvent |
| Repository / Data access | Append audit writer, separately scoped queries |
| Service / Use cases | RecordEvent, QueryAudit, QueryActivity |
| API proposal | /admin/audit; /me/activity |
| Business Rules / Validation | No plaintext body/secret; User cannot edit/purge audit; activity not audit |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Required event schema/redaction/outcome coverage |
| Integration Test | Denied access audits; SQL append/access tests; source Trash does not purge audit; suite V-AUDIT. |

### F-LIFE — Trash/archive/history lifecycle

Phase: RM06; RM08–RM15; RM16. Dependencies: RM04–RM06; aggregate lifecycle decisions. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Aggregate lifecycle/version/deletion-batch records |
| Repository / Data access | Per-module lifecycle handlers, platform orchestration |
| Service / Use cases | Trash, RestoreAggregate, Purge, Archive/Unarchive where supported |
| API proposal | /trash; module /{id}/restore, /purge, /archive, /unarchive |
| Business Rules / Validation | Typed policies, atomic aggregate; terminal locks; no generic ManualEvent Trash |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Transition/dependency/restore membership invariants |
| Integration Test | Concurrent parent/child delete/restore, file references, share/job invalidation; suite V-LIFE. |

### F-FILE — Files/media/attachments

Phase: RM06; RM09. Dependencies: RM05; DEC-TEC-006; DEC-SEC-006; DEC-KNW-032. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | FileObject, FileReference, staged upload and crop/version metadata |
| Repository / Data access | IFileStore local adapter + metadata owner checks |
| Service / Use cases | Upload/Validate/Attach/Read/Replace/ReleaseReference |
| API proposal | /files/uploads; /files/{id}/content; page Save binds cover ref |
| Business Rules / Validation | Size/type/signature/path/checksum; immutable object; authorized download; crop only saved via page version |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Malicious MIME/name/path, crop bounds, icon/cover XOR |
| Integration Test | Partial upload cleanup; cross-user/current-share access; old version cover retained; suite V-FILE. |

### F-NOTIFY — Three-channel Notification Center

Phase: RM06–RM07; RM08. Dependencies: RM05; DEC-TEC-009; durable jobs. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Notification/DeliveryAttempt/PushSubscription |
| Repository / Data access | SQL store/intents/outbox + adapters |
| Service / Use cases | CreateIntent, DispatchThree, Retry, MarkRead/Unread, MarkAllRead, DeleteBulk |
| API proposal | /me/notifications; /me/notifications/read-all; /me/push-subscriptions |
| Business Rules / Validation | Every category all3 attempts; no quiet hours/mute; retain until delete; audit separate |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Idempotency, safe category/template, read/bulk ownership |
| Integration Test | Independent channel failure/browser denial; deep-link recheck; restart/retry no logical duplicates; suite V-NTF. |

### F-JOBS — Background jobs/integrations

Phase: RM06; RM13–RM14. Dependencies: RM05; DEC-TEC-008/009; secret/egress design. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | JobDefinition/Run/Outbox/IntegrationBinding |
| Repository / Data access | SQL leases/journal + safe provider clients |
| Service / Use cases | Schedule, Cancel/Reschedule, Claim, Execute, Retry, Recover |
| API proposal | /jobs/{runId} authorized status; module refresh/execute endpoints |
| Business Rules / Validation | Durable due/version; current authority; retry/backoff/timeouts; no broker requirement |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Missed-run/due-state/cancel/lease policies |
| Integration Test | Crash after effect before ACK, SQL/Redis loss, module-disable and credential revoke; suite V-JOB. |

### F-SETTINGS — System/module/user settings

Phase: RM06–RM07. Dependencies: RM05; DEC-PRD-006; secret policy. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | SettingDefinition/System/Module/User settings and secret reference |
| Repository / Data access | Typed scopes + validated versioned settings |
| Service / Use cases | Read/UpdateSettings, Replace/RevokeSecret, SetTimezone |
| API proposal | /me/preferences; /admin/settings; /modules/{id}/settings |
| Business Rules / Validation | User cannot edit system scope; masked GET; defaults documented; no notification channel setting |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Scope/default/validation/unknown setting |
| Integration Test | Atomic update + scheduler invalidation; no secret in logs/API/cache; suite V-SET. |

### F-PRODUCTIVITY — Projects/Tasks/Reminders

Phase: RM08. Dependencies: RM06–RM07; DEC-PRD-032/033; P02 lifecycle rules. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Project, Task, versions, productivity tags, Kanban rank, one Reminder |
| Repository / Data access | Project/Task aggregate data access; Calendar provider contract |
| Service / Use cases | Create/EditProject/Task, Transition, Reorder, VersionRestore, Trash/Restore, Share |
| API proposal | /projects; /projects/{id}/tasks; /tasks/{id}/transitions; /versions; /shares |
| Business Rules / Validation | Required Title/Start/End + Project Description; Task immutable project; backward reason; terminal Project freeze; warnings confirmed |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Full transition matrix, overdue/time, AC text/checklist, tag cardinality, Task date-outside warning |
| Integration Test | Same-user two-tab freeze race; aggregate trash/restore; live share; scheduler/version restore no bypass; suite V-PROD. |

### F-CALENDAR — Calendar/manual Events/ICS/timezone

Phase: RM08. Dependencies: RM06–RM08; time/ICS decisions. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | ManualEvent plus Task read projection; ICS import UID/results |
| Repository / Data access | Calendar store + registered Task read provider; no editing Task from Calendar |
| Service / Use cases | Create/Edit/Complete/CancelEvent, QueryCalendar, ImportICS, ExportICS |
| API proposal | /calendar/events; /calendar/events/{id}/complete or /cancel; /calendar/imports/ics; /calendar/exports/ics |
| Business Rules / Validation | Day default; time overlap warning; canceled struck through; no recurrence/sharing/sync; export only selected fully-contained events |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | All-day/timed/DST, UID/recurrence/required fields, status/source selection |
| Integration Test | Mixed ICS per-record partial success/dedupe/noVALARM; SQL rollback/concurrency; source Task updates current; suite V-CAL. |

### F-DOCS — Document pages/editors/versions

Phase: RM09. Dependencies: RM06–RM08; DEC-KNW-032/036/039/040; editor scope. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | ContentItem/DocumentPage, ContentVersion, media version references |
| Repository / Data access | Documents DbContext + version/idempotency store |
| Service / Use cases | CreatePage, SavePage, RestoreVersion, Publish/Draft, Archive/Unarchive, Trash/Restore |
| API proposal | /documents/pages; /{id}/save; /{id}/versions; /{id}/versions/{v}/restore; lifecycle endpoints |
| Business Rules / Validation | Explicit type/editor; immutable type/editor/parent/folder; title duplicates; one Tag; Icon OR uploaded crop; manual Save; archived readonly |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Editor round-trip/sanitize; distinctSave=oneversion; immutable fields; share publish/archive state |
| Integration Test | Two-tab stale Save, retry idempotency, version/media retention, cross-user hierarchy and expired share; suite V-DOC. |

### F-ORG — Folders/Tags/Collections/Templates

Phase: RM09. Dependencies: RM06; Documents organization/refinement decisions. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Documents Tag/Folder; typed Collections/Templates |
| Repository / Data access | Documents organization store; typed resource contracts |
| Service / Use cases | Create/RenameTag, DeleteUnusedTag, CreateFolder, TreeTrash/Restore; collection/template workflows after DoR |
| API proposal | /documents/tags; /documents/folders; /collections; /templates |
| Business Rules / Validation | Two-level trees; one Tag/page; no cascade Tag delete; Folder fixed page membership; collection/template semantics Open |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Max depth/cycle/current references; template security field reset |
| Integration Test | Concurrent Tag use/delete; full-tree lifecycle; prior Trash and Archive-child cases after decision; suite V-ORG. |

### F-KNUTIL — Bookmarks/Snippets/Read Later

Phase: RM09. Dependencies: RM06; SSRF and utility field/lifecycle scope. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Bookmark, Snippet, ReadLaterEntry/user reading state |
| Repository / Data access | Utility-owned stores + protected fetch and unified queue contract |
| Service / Use cases | CRUDBookmark/Snippet; FetchMetadata; Add/Remove/MarkRead queue |
| API proposal | /bookmarks; /snippets; /read-later |
| Business Rules / Validation | Manual bookmark survives fetch failure; no snippet execution; one reading queue; resource/type scope |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | URL normalization/dedupe, code rendering/copy, safe captured URL |
| Integration Test | SSRF redirect fixture; same-owner references; retry queue/source delete safe fallback; suite V-KNU. |

### F-MOREPROD — Planner/Goals/Habits/Time Tracking/Pomodoro

Phase: RM10. Dependencies: RM06–RM08; per-module discovery required. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Planner/Goal/Habit/TimeEntry/Pomodoro concepts, fields TBD |
| Repository / Data access | Per-module owned persistence sau approved domain design |
| Service / Use cases | Per-module use cases và APIs phải thiết kế từ discovery, không generic CRUD suy đoán |
| API proposal | Route proposals /planner, /goals, /habits, /time-tracking, /pomodoro; payloads OPEN |
| Business Rules / Validation | Committed module presence; cadence/progress/timer/stop/start/restart/retention choices chưa approved |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Acceptance vectors sau PO chốt; không reuse Task states ngầm |
| Integration Test | Task links/current authority/timer restart; complete module journeys required before RM18; suite V-MPR. |

### F-SEARCH — Global/saved Search/Favorites/Recent/History/Palette

Phase: RM11; extend RM13–RM16. Dependencies: RM06; module providers; DEC-TEC-007. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Safe search projections/SavedQuery/Favorite/Recent/SearchHistory |
| Repository / Data access | SQL projection providers; no cross-module tables |
| Service / Use cases | Query/Reindex/Repair, SaveQuery, ClearHistory, PaletteActions |
| API proposal | /search; /search/saved; /me/favorites; /me/recent; /command-palette |
| Business Rules / Validation | Safe fields/type/tag/time filters; current owner/module scope; no shared-link public discovery; no secret plaintext |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Golden query/rank/highlight/escaping/tie-break tests |
| Integration Test | Stale/rebuild index, disabled module/current grant checks incl count/facet; SQL vs cache failure; suite V-SEARCH. |

### F-DASH — Dashboard/widgets/quick actions

Phase: RM11; extend RM13–RM16. Dependencies: RM08–RM10; DEC-PRD-005. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Widget definitions/per-user layout references |
| Repository / Data access | Source read contracts + bounded fan-out |
| Service / Use cases | ReadWidgets, RunQuickAction via source, UpdateLayout after scope approval |
| API proposal | /dashboard; /dashboard/layout |
| Business Rules / Validation | Widget set/customization OPEN; no duplicate source state; quick-create full required form |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Today/overdue/timezone definitions; disabled widget filtering |
| Integration Test | One widget timeout, consistent totals, user isolation, mobile/a11y widgets; suite V-DASH. |

### F-FIN — Finance accounts/ledger/bills/subscriptions/budget/reports

Phase: RM12. Dependencies: RM06/RM11; DEC-PRD-007; financial field/state decisions. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Accounts/Transactions/Categories/Bills/Payments/Subscriptions/Budgets/Reports; expansions |
| Repository / Data access | Finance ledger context/transaction boundary |
| Service / Use cases | Post/Edit/Trash/RestoreTransaction, PayBill, GenerateOccurrence, ComputeReport |
| API proposal | /finance/accounts, /transactions, /bills, /subscriptions, /budgets, /reports; approved import/export only |
| Business Rules / Validation | Field/state/refinement OPEN; decimal/currency; transfer atomic; no bank/payment initiation |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Golden ledger/rounding/transfer/report and approved debt/split rules |
| Integration Test | Two-leg/payment idempotency; concurrent edits; cache invalidation; cross-owner category/account/file; suite V-FIN. |

### F-VAULT — Vault/secret encryption/rotation

Phase: RM06 security adapter; RM12 product. Dependencies: DEC-SEC-002/004; DEC-PRD-008; DEC-SUP-002; backup key design. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | VaultItem/EncryptedVersion/KeyVersion/RotationRun |
| Repository / Data access | Encrypted repository + ISecretProtector/IKeyProvider; masked reader |
| Service / Use cases | SaveEncrypted, Reveal/CopySelf, Trash/Restore, Rotate/Recover |
| API proposal | /vault/items; /{id}/reveal; /{id}/copy; privileged crypto operations |
| Business Rules / Validation | No plaintext prohibited sinks; owner-only reveal; support/emergency no implicit reveal/export; approved key/step-up gate |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Envelope tamper/context swap, field-safe errors, masking timers |
| Integration Test | Wrong/missing key restore, resumable rotation, SQL/Redis/log/browser scan; new/old key retention; suite V-VAULT. |

### F-NEWS — News/Feeds/Topic Watch

Phase: RM13. Dependencies: RM06/RM09; DEC-PRD-009; DEC-SEC-007. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | FeedSource/Article/UserReadState/TopicWatch |
| Repository / Data access | Feed stores + SSRF-safe parser/fetch adapter |
| Service / Use cases | Subscribe/Refresh/Parse/Dedupe, Read/Save/ReadLater, TopicMatch |
| API proposal | /news/sources; /news/articles; /news/refresh; /news/topics |
| Business Rules / Validation | RSS/curated exact scope Open; deterministic noLLM; excerpt attribution; provider status |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | RSS/Atom/malformed/XXE/date/GUID fixtures; keyword/cooldown |
| Integration Test | Concurrent manual+scheduled refresh, 304/429/backoff, owner-private state and shared public cache; suite V-NEWS. |

### F-SHOP — Price tracking/shopping records

Phase: RM13. Dependencies: RM06/RM12; DEC-PRD-010/011; provider feasibility. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Tracker/PriceDefinition/Variant/Snapshot/Alert; Wishlist/Comparison/Order/Purchase/Seller/Warranty |
| Repository / Data access | Shopping store + approved Shopee adapter + source contracts |
| Service / Use cases | Track/Refresh/Pause, RecordSnapshot/EvaluateAlert; manual shopping records |
| API proposal | /shopping/trackers, /history, /alerts, /wishlist, /comparisons, /orders, /purchases, /sellers |
| Business Rules / Validation | Acquisition+fields+rules Open; comparable currency/variant; no fake zero; all3 notifications |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Golden valid/invalid price series/cooldown; record state validation after DoR |
| Integration Test | Provider403/429/schema change; duplicate snapshot/alert; pause/delete race; warranty/File/Finance references; suite V-SHOP. |

### F-TOOLS — Developer Toolbox

Phase: RM14. Dependencies: RM06; DEC-PRD-012; DEC-SEC-007 for network tools. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Developer tool definitions, optional user history/favorite |
| Repository / Data access | Mostly browser-local algorithms; server API only approved tools |
| Service / Use cases | Run/Format/Convert/Diff/Copy locally; server network requests only approved adapter |
| API proposal | /developer-tools/catalog; /developer-tools/{id}/execute only where approved |
| Business Rules / Validation | P0 list Open; no arbitrary shell/SQL/code; input ephemeral/no telemetry; bounded regex/parser |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Unicode/encoding/hash/time/cron/QR fixtures, malicious regex/XML/YAML |
| Integration Test | Storage/network privacy inspection; SSRF/rebinding/redirect suites if server network enabled; suite V-TOOLS. |

### F-GITHUB — GitHub Discovery/rankings/history

Phase: RM14. Dependencies: RM06; public API query/week/cache decisions. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Public repository projections/RankingSnapshot/SavedRepo/Filter |
| Repository / Data access | Read-only GitHub client + safe cache/query journal |
| Service / Use cases | GetNew/WeeklyPopular/Detail/Filters/Refresh/History |
| API proposal | /github-discovery/new, /weekly, /repositories, /snapshots |
| Business Rules / Validation | No OAuth/write/private data; weekly created-in-week sorted total current stars; window/exclusions Open |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Deterministic ties/filter/week fixture; field semantics |
| Integration Test | Partial/rate-limit/stale labels; no API writes; owner saved-state isolation; suite V-GH. |

### F-AUTO — Automation/webhooks/n8n/data sync

Phase: RM14. Dependencies: RM06/RM12/RM13; DEC-PRD-013; DEC-SEC-007. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | DefinitionVersion/Run/StepRun/WebhookDelivery/n8n Binding |
| Repository / Data access | SQL job engine/approved action registry + safe integration adapters |
| Service / Use cases | Validate/Enable/Disable/Run/Cancel/Retry; webhook intake/outbound; n8n sync |
| API proposal | /automations; /runs; /webhooks; /integrations/n8n |
| Business Rules / Validation | Workflow graph/list/actions/dialect/mapping Open; no uploaded code; current authority before effects; safe logs |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Definition/runs/state/missed-run/backoff/redaction vectors |
| Integration Test | Duplicate trigger after side-effect ACK loss; secret revoke; n8n disconnect/schema/replay; module upgrade/disable; suite V-AUTO. |

### F-ASSETS — Personal Assets/purchase/warranty

Phase: RM15. Dependencies: RM06/RM09/RM12; DEC-PRD-014. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Inventory/Device/Accessory/Purchase/Warranty/Claim |
| Repository / Data access | Asset store + same-owner File/Finance/Vault refs |
| Service / Use cases | Create/UpdateLifecycle/LinkPurchase/TrackWarranty/Remind |
| API proposal | /personal-assets; /warranties; /claims |
| Business Rules / Validation | Fields/states Open; serial sensitive; no implied ledger writes or credential copy |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Lifecycle/expiry/dependency/projection fixtures after DoR |
| Integration Test | Trash/restore with refs; renewed date cancels stale reminder; share no serial/private notes; suite V-ASSET. |

### F-DIGITAL — Digital Assets/renewals/licenses

Phase: RM15. Dependencies: RM06/RM12; DEC-PRD-014; egress for observation. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Domain/Hosting/VPS/Certificate/Service/License metadata |
| Repository / Data access | Digital store + approved optional observation clients |
| Service / Use cases | Track/RenewMetadata/Archive/LinkSecret/ExpiryReminder |
| API proposal | /digital-assets; type-specific metadata routes |
| Business Rules / Validation | Fields/states Open; no remote execution/auto payment/registrar write; secret ref only |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | IDN/expiry/certificate/currency and reference rules |
| Integration Test | Provider timeout/SSRF if enabled; no cross-owner credential link; key references not deleted with asset; suite V-DIGITAL. |

### F-CAREER — Career/Resume/Learning/Work Log

Phase: RM15. Dependencies: RM09/RM10/RM12; DEC-PRD-014; interview Calendar mapping. Feature còn OPEN không được code trước DoR.

| Contract | Nội dung |
|---|---|
| Entity | Job/Company/Interview/ResumeVersion/Skills/Courses/Certifications/LearningPlan/WorkLog |
| Repository / Data access | Career store + typed Documents/Files/Productivity refs |
| Service / Use cases | TrackOpportunity/Interview, ResumeVersionBind, LearningProgress/WorkLog |
| API proposal | /career/jobs, /companies, /interviews, /resumes, /skills, /courses, /certifications, /work-logs |
| Business Rules / Validation | Fields/states Open; historical application exact resume version; Interview Calendar integration needs decision |
| Authorization | Common matrix ở trên + source module-specific sensitive/non-shareable rules; no implied export/reveal from view. |
| Unit Test | Job/progress/merge/expiry/duration rules after DoR |
| Integration Test | Resume replace without rewriting application; sensitive share omitted; reminder reschedule and cross-owner tests; suite V-CAREER. |

## 5. Important implementation boundaries

- Browser Push permission denied/unsupported không thể ép trình duyệt gửi. Intent vẫn fan-out ba kênh; push attempt ghi Failed/Suppressed với safe reason theo contract, không ngăn In-app/Email. “Đồng thời” không phải guarantee cả ba thiết bị nhận cùng một millisecond.
- Retry provider sau timeout có thể ambiguous acknowledgement. Dedupe logical notification/effect bắt buộc; dùng provider key nếu hỗ trợ và ghi delivery limitation thay vì hứa exactly-once.
- Calendar ICS recurring input skip cả recurrence semantics/instances theo supported subset, không expansion. Import không lấy VALARM, không auto-fill missing business fields, mỗi UID owner-scoped chỉ tạo một ManualEvent. Unsupported/floating-zone/UID-less/DURATION mapping phải được refine.
- Export ICS không xuất full Project/Task records/history/reminders; không generic Project/Task import/export endpoint. Task projection export vẫn theo Calendar source/status selection đã chốt.
- Archived Document share không có child subtree quyền tự động; Document/File/Collection projection fields/download scope còn cần decision. Sidebar owner không tự trở thành shared-tree navigation.
- Template/quick-create không preselect immutable DocumentType/EditorMode ngầm hoặc giữ secret/share/history; contract phải thống nhất explicit-selection requirement.
- Search authorization không đưa shared-link resource vào global/public search. Support search nếu được duyệt dùng one-module dedicated context, không global dashboard hoặc personal recent cache.
- Notification inbox delete không xóa audit; queued deliveries sau delete, sent payload after revoke và delivery cancellation timing cần approved policy, không âm thầm suy.
- Source-complete means all catalog modules, không chỉ F-DOCS. P1 module capability phải được refine/complete hoặc explicit PO scope change; scaffold/empty endpoint không đạt DoD.

## 6. Exit evidence

Mỗi feature contract phải được tách thành stories có fields/state/error payloads đầy đủ; test scenario TC-{RequirementID} trong 01/08 là planned identifier, không test report. Done: OpenAPI/schema/authorization/business unit và SQL integration suites pass, module contract pass, frontend critical flow pass, migration/rollback evidence khi có schema change, all3 notifications và owner/source projections đúng.
