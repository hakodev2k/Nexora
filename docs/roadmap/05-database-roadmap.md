# Domain Model and Database Roadmap

**Status:** Conceptual/physical design proposal, chưa tạo schema, DbContext, database hoặc migration.
[Master RM04 và domain phases](00-master-implementation-roadmap.md) · [Open/conflict register](01-requirement-traceability.md)

## 1. Modeling order

RM01 thống nhất vocabulary, ownership, aggregate và conceptual relationships. RM04 triển khai core schema sau approval; domain migrations đi cùng RM08–RM15 khi fields/state đã Ready. Không tạo toàn bộ tables đề xuất của Finance/Assets/Career trước discovery.

Đề xuất một SQL Server database, schemas/DbContexts theo module: identity, platform, productivity, calendar, documents, knowledge utilities, search, finance, vault, news, shopping, developer, automation, assets, career. Tên cuối cùng review tại ADR-R02, không phải các schema đã tồn tại. Mỗi context chỉ map bảng sở hữu. Shared identity/resource keys có thể FK; cross-module business links dùng typed reference + contract validation, không mở direct queries.

SQL Server provider chính thức của EF Core dùng Microsoft.EntityFrameworkCore.SqlServer. Phiên bản EF/runtime/CLI pin tương thích tại implementation. [Provider documentation](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/)

## 2. Ownership và core ERD

~~~mermaid
erDiagram
  User ||--|| PersonalSpace : owns
  User ||--o{ Session : authenticates
  User ||--o{ AdminGrant : receives
  PermissionDefinition ||--o{ AdminGrant : permits
  PersonalSpace ||--o{ OwnedResource : isolates
  ModuleDefinition ||--o{ UserModuleEnablement : enables
  User ||--o{ UserModuleEnablement : receives
  OwnedResource ||--o{ ShareGrant : exposes
  ShareGrant ||--o{ ShareRestrictedUser : limits
  User ||--o{ SupportGrant : consents
  ModuleDefinition ||--o{ SupportGrant : scopes
~~~

OwnedResource trong diagram là conceptual common ownership contract, không bắt buộc một generic EAV/business table. Business entity vẫn thuộc module schema. PersonalSpace unique UserId và không có WorkspaceType/member tables.

| Concept | Dữ liệu/key tối thiểu đề xuất | Constraints/invariants |
|---|---|---|
| User/Profile | UserId, normalized identifier/email, verification/account state, timezone, profile whitelist | Identifier normalization/uniqueness race-safe; roles không editable từ profile payload |
| PersonalSpace | PersonalSpaceId, UserId | Unique UserId; exactly one boundary sau verify; không client-selected owner |
| Role/UserRole | Role code/UserId | Confirmed roles SuperAdmin/Admin/User; last active SuperAdmin invariant transactional |
| PermissionDefinition/AdminGrant | module.action, actor/role recipient, grant version | Unknown grant deny; unique grant tuple; no access_all business-data grant |
| Session | SessionId/token hash or protected reference, UserId, issued/expiry/revoked/version | SQL authority; expiry/revoke checked; cookie key không ở business table |
| Verification/RecoveryToken | Hash/purpose/UserId/expiry/consumed marker | Single-use conditional update; no raw token trong logs/DB field plain |
| ModuleDefinition/Version/MigrationState | Stable IDs, contract/version compatibility, migration journal/status | Unique ID/version; failed migration không Ready |
| System/User enablement + RegistrationDefault | ModuleId/UserId, policy version, enabled, audit metadata | Unique scopes; defaults snapshot/version, no bypass system/dependency gate |
| ResourceType/Reference | Module/type/opaque resourceId/owner + capabilities | Unknown type deny; không generic bypass đọc mọi entity |
| ShareGrant/Allowlist | Token hash, owner/resource, mode, expiry, status, allowed User IDs | Current-state check; allowlist unique; no full token lưu/log; restore-policy Open |
| SupportGrant/Session | Target User/module, duration/expiry/revoked, actual Admin actor | Exactly one module; read-only; any qualified Admin; session end không revoke grant |
| EmergencySession | Target/module/SuperAdmin/reasonRef/expiry/revoked | Mandatory audit before read; short duration technical decision; no implicit reveal |
| AuditEvent/ActivityEvent | Immutable audit schema; separate activity | Audit không theo generic Trash; business User không update/purge audit |
| FileObject/FileReference | Owner, opaque storage key, size/type/checksum/state; resource/version/purpose ref | File reuse không cross-user; reference cleanup không xóa object còn được dùng |
| Notification/DeliveryAttempt | Logical intent key, target User/category/source; channel/state/attempt | Unique logical key; ba channel jobs; inbox retained until User delete; audit độc lập |
| Job/Run/Outbox | Owner/module/source/version/due/time/status, lease, attempt/idempotency | Durable SQL; no arbitrary executable payload; source/authority recheck |
| Settings/IntegrationSecretRef | System/module/user scope, schema/version/sensitivity | Masked GET; secret reference/encrypted material riêng, no plaintext setting |

Owner composite references cho cùng module: foreign key (OwnerId, ParentId) → (OwnerId, Id) để DB hỗ trợ chặn cross-user relations. FK không thay access checks cho reads. Actor IDs chỉ audit attribution; không được gán bằng payload.

## 3. Documents model — Document, Page và ContentItem

Nguồn DEC-PRD-004/DEC-KNW-006/P03-DOC-008 đã chốt một model nội dung chung. **Document module** là khu vực UX; **Page/DocumentPage** là aggregate; **ContentItem** là tên foundation/storage có thể dùng cho aggregate đó. DocumentType Document/Note/Knowledge là kiểu page. Không có container KnowledgeBase hay Article entity riêng. File DOCX nếu được support là FileObject, không đồng nhất với page.

~~~mermaid
erDiagram
  PersonalSpace ||--o{ DocumentPage : owns
  DocumentType ||--o{ DocumentPage : classifies
  DocumentFolder o|--o{ DocumentFolder : contains
  DocumentFolder o|--o{ DocumentPage : locates_root
  DocumentPage o|--o{ DocumentPage : parents
  DocumentTag o|--o{ DocumentPage : labels
  DocumentPage ||--|{ ContentVersion : preserves
  ContentVersion ||--o{ FileReference : references
  FileObject ||--o{ FileReference : supplies
~~~

Diagram self-relations phải kèm max-depth rules sau; cardinality ERD tự nó không giới hạn hai cấp.

| Field/concept | Bắt buộc/cardinality | Rule được giữ |
|---|---|---|
| DocumentType | Bắt buộc chọn mỗi create, một trong Document/Note/Knowledge | Không default/remember/infer; immutable; không tự thêm quản lý custom types |
| EditorMode | Bắt buộc chọn Block hoặc Markdown | Immutable; schema version per mode, không conversion |
| Title | Nonblank; length bound cần quyết định | Trùng Title mọi nơi hợp lệ; không unique index/auto-rename |
| Body | Optional | Empty page lưu được; Block JSON/Markdown text theo canonical schema |
| TagId | 0..1 | Documents catalog của current User; inline-create được; current reference ngăn xóa Tag |
| FolderId | 0..1 chỉ root | Chọn create và cố định kể cả null; child không có Folder riêng |
| ParentPageId | 0..1 | Immutable; root/child tối đa hai cấp; không attach/detach/reparent |
| IconKind/IconValue hoặc CoverFileId/Crop | Không có hoặc một loại visual | Emoji/builtin icon OR uploaded image; không custom icon upload/external URL; crop state giữ trong version |
| State | Draft/Published/Archived | Create Draft; previousState khi Archive; không lẫn Trash |
| CurrentVersion/RowVersion | Required technical metadata | Business version number khác SQL concurrency token; mỗi distinct Save thành công thêm một immutable version |
| Created/Updated actor/time | Server-controlled | UTC instant; update sort không tự thêm cột UI |
| Trash batch/lifecycle metadata | Theo policy | Restore tree atomic; không rewrite old versions/relations trái invariant |

Folder tree: level1 root, level2 child; không level3/cycle. Page tree: root và child; child không làm parent. Root chỉ chọn Folder cùng owner khi create; effective Folder của child lấy từ root. Move Page bị cấm; Move Folder workflow còn OPEN mặc dù source acceptance có nhắc move-concurrency. Không coi câu ví dụ “Move Folder” trong prompt là approval.

Current page constraint đề xuất: không đồng thời ParentPageId và own FolderId; Icon/Cover XOR; immutable type/editor/parent/folder enforced bằng domain + write guard, không chỉ disabled input. Cross-row max-depth/cycle cần transaction/range lock/guard phù hợp, không thể chỉ CHECK đơn hàng. Restore/import cũng đi cùng rules.

Tag current references dùng restrictive relation, không cascade delete nội dung. Tag references trong Trash/version history phải đóng DEC-KNW-036 trước chọn FK/history serialization/purge strategy. Không tự áp history FK khiến delete bị chặn mãi khi chưa có decision.

## 4. Productivity và Calendar

~~~mermaid
erDiagram
  PersonalSpace ||--o{ Project : owns
  Project ||--o{ Task : contains
  Task ||--o{ TaskVersion : versions
  Project ||--o{ ProjectVersion : versions
  ProductivityTag ||--o{ TaskTag : labels
  ProductivityTag ||--o{ ProjectTag : labels
  Task ||--o{ TaskTag : has
  Project ||--o{ ProjectTag : has
  Task ||--o| Reminder : schedules
  PersonalSpace ||--o{ ManualEvent : owns
  ManualEvent ||--o| Reminder : schedules
~~~

Task Calendar Event là logical read projection của Task; không bắt buộc một persistence entity riêng. Xem ADR-R01/R06 tại 02. Stable external ICS UID không lộ internal resource ID và cần scope/dedupe design.

| Aggregate | Required fields | Optional / invariants |
|---|---|---|
| Project | Title, Description, StartDateTime, EndDateTime; owner | End > Start; priority P0–P3; many Tags; color/icon; notes; default NotStarted |
| Task | ProjectId, Title, StartDateTime, EndDateTime; owner | End > Start; Description; AC text/checklist; priority; many Tags; tối đa một Reminder; ProjectId immutable |
| ManualEvent | Title, Description, Start, End; owner | All-day date range hoặc timed instant; một Reminder optional; no extra location/attendee/tag/recurrence fields |
| Reminder | Source typed ref/owner/module; exact due hoặc Start-relative 15m preset | Unique one active per Task/Event; scheduling version; cancel/recheck source terminal/Trash/disabled |
| ICS source identity | Owner + source UID | Unique dedupe scope không cross-user; imported entry luôn ManualEvent/Scheduled/no reminder |
| Kanban order | Project/status/rank/Task | Stable persisted order; rank/rebalance và concurrent move không duplicate/missing |

Project terminal Completed/Skipped không reopen; blocks Task mutations/create/version restore. Skip/complete override giữ child states. Complete có open Tasks cần warning+reason; “all Tasks terminal” chỉ prompt, không auto-complete. Project InProgress→NotStarted còn DEC-PRD-032.

Task Completed/Skipped vẫn editable khi Project active; backward state restore phải qua reason + validation, kể cả restore lịch sử. Overdue tính từ current instant, active Task status và End; không persist Overdue như state. Start tới không auto InProgress. Read models dùng một definition.

ManualEvent terminal Completed/Canceled readonly/no reopen; delete là cancel, không Trash, không version history UI. Past Scheduled không Overdue. All-day lưu DATE/range semantics riêng, không chuyển UTC rồi mất ngày; end-exclusive internal convention là technical proposal cần approve/UI mapping rõ. All-day reminder clock/zone, floating ICS time, missing UID/DURATION rules còn OPEN.

## 5. Domain inventory cho toàn catalog

Các hàng dưới là conceptual inventory để refinement và phân công, không schema/business state đã được duyệt. Detail chưa clear được đánh dấu OPEN REQUIREMENT trong 01.

| Feature/domain | Concepts phải phân tích | Relationships và non-negotiable boundaries |
|---|---|---|
| Planner/Goals/Habits | Plan, goal/progress, habit/schedule/occurrence | Project/Task references cùng owner; không tự suy recurrence/status/rollover |
| Time Tracking/Pomodoro | Time entry/timer/session/break | Task link optional/bắt buộc cần chốt; concurrency/restart/rounding/overlap chưa suy |
| Bookmarks/Read Later | Bookmark, captured URL, reading queue/user state | URL normalization/dedupe, public metadata fetch; one reading queue |
| Snippets/Templates/Collections | Snippet, Template, TypedCollection/Member | Không execute code; template không copy share/secret/history; collection projection cần approved composition |
| Global Search/Dashboard | Safe projection, saved query/favorite/recent/history, widget layout | Source-owned state; no secret index; query-time scope; widget set còn OPEN |
| Finance | Account, Category, Transaction, Transfer legs, Bill, Payment, RecurringTemplate/Occurrence, Subscription, Budget, Reports | Decimal/currency; same-owner references; no implicit ledger update từ Shopping/Assets |
| Finance expansions | SavingsGoal, Debt/Loan, Split, import mapping | Existing proposals; dedicated field/state/rounding/discovery trước schema |
| Vault | VaultItem/EncryptedVersion, safe metadata, KeyVersion/RotationRun | Envelope ciphertext/nonce/tag/context; key material ngoài SQL ciphertext store; only owner reveal/copy |
| News | FeedSource, Article, UserReadState, Saved/TopicWatch | Public article cache tách private subscriptions/read state; no LLM |
| Shopping | Tracker, Variant/PriceDefinition, Snapshot, AlertRule, Wishlist, Comparison, Order/Purchase, Seller, Warranty | Valid comparable prices; no price 0 from failure; purchase manual; Finance/Vault ref only |
| Developer/GitHub | ToolDefinition, optional history/favorite, RepositoryProjection/RankingSnapshot/SavedFilter | Input default ephemeral; public GitHub only; total stars current-week definition preserved |
| Automation/n8n | DefinitionVersion, Trigger/Action binding, Run/StepRun, WebhookDelivery, IntegrationBinding | Developer-registered actions; secret refs; no user executable code; n8n no DB/master key |
| Personal Assets | Asset/device/subtype, Accessory, Purchase/Warranty/Claim | Type/field/state details open; same-owner file/Finance/Vault refs |
| Digital Assets | Domain, Hosting/VPS, Certificate, OnlineService, License | Metadata only, secret in Vault; no shell/registrar writes |
| Career/Learning | Job, Company, Interview, ResumeVersion, Skill, Course, Certification, LearningPlan, WorkLog | Resume exact version; sensitive projections; Interview→Calendar semantics must be decided |
| Import/Export/Backup | ImportBatch/Result, export artifact, backup manifest/restore job | Module-approved formats only; all catalog data must be inventoried for backup/account lifecycle |

## 6. SQL field, constraint và index strategy

| Concern | Proposal | Decision / proof |
|---|---|---|
| Primary keys | Opaque GUID IDs; clustered-key strategy measured separately | Opaqueness không là authorization; avoid leaking internal IDs into shares/ICS |
| Text | Unicode NVARCHAR with approved max lengths; Markdown large text/Block JSON versioned | Lengths, sanitizer limits, normalized/case/accent matching need field dictionary |
| UTC instants | datetimeoffset normalized UTC, hoặc datetime2 với enforced UTC convention | Chọn thống nhất ADR-R02; keep IANA User zone, no server-local inference |
| Date-only | SQL date và date-range columns riêng | All-day/date-only round-trip giữ ngày |
| Money | DECIMAL precision/scale theo currency/range đã chốt | Không mặc định DECIMAL(18,2) cho mọi currency; golden rounding suite |
| Row concurrency | SQL rowversion + API ETag/precondition | Stale write 412/conflict; source revisions và version number riêng |
| Versions | Unique (OwnerId, AggregateId, VersionNumber), source-version ref | Retry Save key scoped; no duplicate version; immutable payload/media refs |
| Immutable fields | Application/domain guard + persistence write interception/controlled SQL | Direct mutation tests; restore/import cùng rules |
| Deletes | No broad ON DELETE CASCADE cho business aggregates | Explicit batch/transaction; audit retained; restrictive dependencies |
| Lists | Composite owner/lifecycle/status/time/title indexes theo query | Sort + stable ID tie; query plan measurement; no full personal dataset load |
| Documents list | Owner, lifecycle, parent/folder context, UpdatedAt, Id; type/tag/CreatedAt indexes khi workload cần | Filter CreatedAt khác sort UpdatedAt; initial root/folder union; page Title không unique |
| Tag search | Normalized owner/module name; exact normalization policy | Contains search không tự được accelerate bởi B-tree prefix; measure SQL query/full-text options |
| Notification/job | User/read/deleted/time; due/status/lease; unique intent key | Scan bounded; no auto-expire inbox; job retry consumes stable key |
| Grant lookup | Hashed token/session IDs, expiry, owner/resource/module tuple | Current-state checks; last-admin/allowlist races tested |

Không lập index cho mọi field ngay; mỗi index gắn query/filter/order test và migration cost. Không dùng EF InMemory/SQLite để chứng minh SQL Server constraints, transactions hoặc rowversion.

## 7. Lifecycle và retention

| Resource | Delete/Archive/Restore policy giữ nguyên | Cần chốt / technical handling |
|---|---|---|
| Project/Task | Trash vô thời hạn tới owner purge; Project restore aggregate; Task riêng chỉ khi parent active | Terminal aggregate lifecycle/purge exceptions, pre-existing Trash child membership và history purge details trong 01 |
| ManualEvent | Cancel thay delete; terminal readonly, không reopen/version history | Không route Trash/purge generic |
| Document | Draft/Published/Archived tách Trash; Save/history giữ tới permanent delete | Draft suspend share; Archived từ Published giữ valid link; restore Trash-link còn Open |
| Folder/page trees | Delete/restore whole tree như lúc xóa; riêng child delete có warning, parent active trước restore child | DeletionBatch membership/state snapshot; prior individual Trash child rule phải chốt, không auto-resurrect ngoài batch |
| Document versions/media | Full versions until page permanent delete; media old versions không bị overwrite | Reference accounting; source Tag deleted/renamed policy; no auto cleanup versions |
| Notification | Retained đến User xóa; audit không bị xóa theo | Delete inbox vs pending delivery policy còn cần decision |
| Audit | Append-only logical behavior; no User purge | Duration/integrity/export/protection design còn DEC-SEC-005 |
| Files | Object cleanup only khi không còn valid references sau approved grace | Không delete cover còn referenced bởi old version hoặc resource khác |
| Vault | Ciphertext/keys/versions/trash recovery theo approved crypto policy | Backup/key dependency; wrong/missing key fail rõ, không overwrite |
| Account | Disable/revoke/reconciliation trước purge | Cross-module data inventory/retention cần approved workflow |
| Other domain | Follow source proposal sau approval | Không áp vô thời hạn hoặc auto-purge default từ module khác |

Proposal deletion batch: record aggregate operation ID + members + original lifecycle state, để retries/restore biết cùng một deletion operation. Không biến kỹ thuật này thành quyết định resurrect các record đã Trash trước đó; case đó là OPEN REQUIREMENT.

## 8. Migration, seed và recovery plan

- Trước migration: schema/manifest compatibility, target environment assertion, backup/restore path và lock.
- Migration journal theo module + ordered version; single migration runner/SQL lock; không migration destructive từ mọi API startup.
- Empty DB bootstrap và upgrade từ oldest supported version là hai suites riêng. Failed migration không mark module Ready; transactions hoặc documented compensation cho operation không transactional.
- Forward-compatible changes ưu tiên expand/backfill/verify/contract khi cần. Không hứa rollback bằng “drop column” nếu dữ liệu đã đổi.
- Seed static module/permission/DocumentType definitions có stable IDs và idempotency; không default password, không demo data tự vào production.
- Optional synthetic user/domain fixtures là test/development profile riêng, không dùng data thật.
- Backup manifest liên kết SQL restore point, file checksums/references, configuration versions, key dependency và module schema versions. Keys protected/separate; audit restore; rebuild cache/search thay vì coi chúng là truth.
- Restore không tự replay historical notifications/webhooks/jobs; reconcile run state trước enable worker.

Done khi SQL constraints/owner/concurrency/migrations/lifecycle/backup evidence cho phase liên quan pass và field/state OPEN không còn được implement từ assumption. Chưa có migration hoặc DB nào được chạy bởi bộ roadmap này.
