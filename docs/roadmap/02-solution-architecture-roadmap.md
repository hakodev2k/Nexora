# Solution Architecture Roadmap

**Status:** PROPOSED architecture để review tại RM01; chưa implement. Stack do User chỉ định: .NET 10 / ASP.NET Core, ReactJS, SQL Server, Redis. Requirement source giữ nguyên.

[Master phases](00-master-implementation-roadmap.md) · [Coverage và blockers](01-requirement-traceability.md) · [Domain/SQL](05-database-roadmap.md)

## 1. System architecture

Đề xuất modular monolith: một ASP.NET Core deployment unit ban đầu, một React application, một SQL Server database với boundaries theo module. Một API host có thể chạy background workers local; future worker process chỉ tách khi measured workload đòi hỏi, không tạo microservices mặc định.

~~~mermaid
flowchart TD
  UI["React application"] --> API["ASP.NET Core API"]
  API --> APP["Application contracts"]
  APP --> DOM["Domain rules"]
  APP --> INF["Infrastructure adapters"]
  INF --> SQL["SQL Server"]
  INF --> CACHE["Redis cache"]
  INF --> FILE["Protected file storage"]
  INF --> EXT["Email, Web Push, providers"]
  JOB["Durable job worker"] --> APP
  JOB --> SQL
~~~

| Thành phần | Trách nhiệm | Quy tắc boundary |
|---|---|---|
| ReactJS | UX, editor, forms, state/error/loading, view routing và permission-aware controls | Không quyết định owner/quyền; không giữ session credential hoặc Vault secret trong localStorage |
| ASP.NET Core | HTTP boundary, authentication, policy enforcement, safe responses, OpenAPI, DI, request limits | Mọi direct endpoint kiểm tra server; support/emergency không là impersonation |
| Application | Use cases, transaction orchestration, module contracts và projection | Không query business table module khác |
| Domain | States, invariants, validations và lifecycle policies đã duyệt | Không tham chiếu SQL, Redis, UI, HTTP hoặc provider SDK |
| SQL Server | Identity, business state, history, audit, manifests/grants, jobs/intents và durable delivery state | Source of truth; schema/version/integrity có migration |
| Redis | Cache có thể dựng lại cho public provider/reference data phù hợp | Không là truth của grants, sessions, balances, notification jobs hay secrets |
| File/Media | Binary + checksum/owner/lifecycle; controlled read/download | Không serve private storage như static public folder |
| Search | Owner-safe projections/query orchestration từ registered providers | SQL trước; authorize cả results/count/facets/highlights tại request |
| Logging | Structured correlation, safe diagnostic details; audit riêng | Không raw token/body/credential trong logs |
| Validation | Client feedback + server/domain invariants + DB constraints | Client validation không thay server; SQL constraint không thay domain errors |
| Background work | Reminder, notification retries, reindex, provider fetch, maintenance | SQL durable records; retry/lease/idempotency; recheck authority trước effects |

Microsoft cung cấp OpenAPI trong ASP.NET Core 10; dùng khả năng nền tảng trước khi thêm UI/docs packages. [ASP.NET Core 10](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0?view=aspnetcore-10.0)

## 2. Proposed solution layout — chỉ là thiết kế

| Đường dẫn tương lai | Trách nhiệm |
|---|---|
| Nexora.slnx | Solution của .NET projects |
| src/Nexora.WebApi | Composition root, controllers/endpoints, auth middleware, HTTP DTO/errors/OpenAPI |
| src/Nexora.Application | Per-module use cases; public contracts; scope/transaction interfaces; không phụ thuộc Infrastructure |
| src/Nexora.Domain | Per-module domain types, invariants, value objects, domain events |
| src/Nexora.Infrastructure | EF Core contexts/migrations theo module, file/Redis/email/HTTP adapters, worker persistence |
| frontend/nexora-web | React shell, shared components, per-module routes/features |
| tests/Nexora.UnitTests | Domain policies và deterministic algorithms |
| tests/Nexora.IntegrationTests | API, SQL Server, Redis, files, provider adapter/worker tests |
| tests/Nexora.ArchitectureTests | Dependency, ownership boundary, manifest và cross-module access rules |
| frontend/nexora-web/tests | Components/features và E2E fixtures/specs theo runner được duyệt |

Dependency compile-time: Domain không phụ thuộc layer khác; Application → Domain; Infrastructure → Application/Domain; WebApi → Application và Infrastructure để DI. Trong mỗi layer nhóm namespace theo module. Không tạo bốn project cho từng entity hay generic repository chỉ bọc lại EF Core. Public contract không trả IQueryable/DbContext/entities nội bộ.

Mỗi module có resource types, application contracts, EF mappings/migration journal và frontend contributions riêng. Ban đầu có thể chung các layer assemblies để giảm boilerplate, nhưng architecture tests phải chặn tham chiếu implementation/table ngoài module. Chỉ tách module assemblies khi giúp enforce boundary thực tế; không đổi product ModuleId hoặc data ownership.

## 3. Module contracts và lifecycle

Manifest v1 cần schema chính xác trước RM06: stable ModuleId, display metadata/maintainer, version + PlatformContractVersion range, personal ownership, dependencies, permissions, routes/navigation, resource types, widgets, search providers, events/triggers/actions, job types, settings, migrations. Cả dependency graph và namespace/contribution uniqueness được validate trước ready.

| Contract dự kiến | Input/output nghĩa vụ |
|---|---|
| ModuleDefinition/Registration | Trusted build đóng góp manifest và factories; không runtime upload từ User/Admin |
| ResourceReader/AccessEvaluator | Actor + access context + owner + resource/action → safe projection hoặc deny |
| SearchProvider | Scoped query/filter/page → safe result/count; versioned projection và reindex contract |
| CalendarContributionProvider | Time range + User → Task Event projections read-only; không copy quyền từ viewer |
| WidgetProvider | User/module scope → bounded read model và degraded status |
| NotificationIntent | Logical idempotency key + target User + category/source/safe content → durable logical notification và ba deliveries |
| ScheduledWork | Module/type/version/owner/source/version + due instant → leased run, status/retry/cancel policy |
| FileReference | Owner/resource/version/file purpose → authorized immutable binary reference |
| LifecyclePolicy | Trash/restore/purge/archive và relation behavior theo từng aggregate, không generic cascade ngầm |

Events versioned; internal application dispatch + SQL outbox cho tác vụ hậu commit. Không thêm broker. Cross-module handler chỉ gọi application contract; không đọc bảng consumer. Unknown/breaking contribution không tự enable. Upgrade kiểm tra compatibility, dependency và migration; failed migration safe-disabled; disable giữ data/config/audit và dừng new effects. Uninstall cần retention/migration decision.

Default bật toàn bộ module cho verified User chỉ hiệu lực trên installed/system-ready modules. Local milestone thiếu module chưa phải Release 1; tới RM18 phải có toàn catalog hoàn chỉnh. Package installed và user default enablement là hai gate khác nhau.

## 4. Domain vocabulary và ownership

Chọn internal PersonalSpace có đúng một User, unique UserId. Business OwnerId tham chiếu boundary này; CreatedByUserId/UpdatedByUserId là actors. Không có Workspace entity, role, invite, membership hoặc transfer.

Document/Page không được gộp chỉ do tên gọi. Nguồn đã chốt DEC-PRD-004, DEC-KNW-006 và P03-DOC-008 dùng một ContentItem foundation: Document module chứa các **page** ContentItem có DocumentType Document/Note/Knowledge. Vì vậy proposal dùng một DocumentPage aggregate (persistence ContentItem), không tạo KnowledgeBase/Article engine thứ hai. File DOCX là binary attachment nếu format được duyệt, không phải bản thể của page.

Root/child page tree tối đa hai cấp, Folder tree tối đa hai cấp, hai quan hệ độc lập. Root có optional Folder cố định; child có Parent cố định và kế thừa Folder; không cần bản ghi child.FolderId thứ hai dễ mâu thuẫn. Domain model/constraints chi tiết ở 05.

## 5. Authentication strategy

Authentication trả lời actor là ai; authorization trả lời action/resource nào được phép. Thiết kế identity/scope tại RM01 trước schema; implementation auth tại RM05 sau RM04, không retrofit ownership.

Đề xuất ASP.NET Core Identity để quản lý password/token primitives cùng cookie authentication cho web. Cookie protected, HttpOnly, Secure, SameSite được chọn theo same-origin design; anti-CSRF token/header cho mutation. Không đưa bearer credential vào localStorage. SQL SessionRegistry kiểm tra account/session/revocation ở mỗi private request; không chờ cookie hết hạn mới thu hồi quyền. Cookie protection key ring tách khỏi Vault encryption keys.

| Flow | Planned contract và gate |
|---|---|
| Bootstrap | Chỉ khi không SuperAdmin; one-time operator secret ngoài repo; transactional last-admin/create gate; setup đóng sau thành công |
| Register | Generic response/normalization/rate limits; account PendingVerification; không role/owner từ client |
| Verify | Single-use time-bound token; consume + activate + PersonalSpace + default enablements idempotent transaction |
| Login | Generic failure, throttling, password verification/rehash theo approved parameters |
| Logout/revoke-all | Revoke SQL session state; request sau commit bị deny; cancel/recheck queued effects |
| Recovery | Token single-use; delivery trustworthy; không giả gửi; password/reset session policy cần đóng DEC-SEC-009 |
| Profile/timezone | Whitelist editable fields; browser IANA zone detection/fallback decision; saved preference wins |
| Disabled/deleted | No new sessions; grants/jobs/data retention reconciliation; purge không chạy khi dependencies chưa xử lý |

Lifetime/idle timeout/verification/resend/password policy/recent-auth/MFA values chưa được User chốt. Đây là DECISION REQUIRED, không dùng framework default làm business approval. User-created support duration 24h/custom/until-revoke đã Approved và không hỏi lại.

## 6. Authorization algorithm

1. Parse explicit server-validated context: Self, SharedLink, Support hoặc Emergency; client không tự chọn scope bằng một flag.
2. Kiểm tra account/session active nếu mode cần authenticated actor. Anonymous chỉ đi PublicLink; bước “account active” không loại nhánh anonymous hợp lệ.
3. Kiểm tra installed/system-ready + user/actor/module/action gates theo context; default deny.
4. Resolve đúng owner/source và resource state qua module contract. Trash/purge không được đọc bằng active/share route.
5. Self: actor là owner. SharedLink: token hash, mode, session/allowlist, expiry/revoke và resource projection hợp lệ. Support: Admin module action + support permission + active grant một User/một module. Emergency: active SuperAdmin + valid short session + meaningful reason.
6. Enforce read-only và projection trước serialize; context không cấp export, mutation, reveal/copy hoặc ownership transfer.
7. Ghi required audit và enqueue required immediate notification bằng durable transaction trước khi cho emergency data access. Nếu không thể ghi control bắt buộc, deny an toàn.
8. Recheck authoritative permission/module/grant khi request mới hoặc trước job side effect. Client navigation cache không được thay quyền.

Proposal dùng SQL authoritative authorization cho request mới, không positive permission cache ở Redis. Bounded propagation tới UI/job được đo riêng; không dùng TTL để hợp thức hóa share/revoke access sau commit. Mọi operation in-flight có cancellation/finish policy explicit trước coding.

Last active SuperAdmin invariant dùng serialized transaction/guard row, không chỉ đếm ngoài transaction. Module grant chỉ cho quản trị; normal Admin/SuperAdmin personal routes vẫn Self.

Support active-data/Trash/history, Vault metadata và existing-share-policy/restore còn Open. Gate truy cập đó không ship cho tới khi quyết định; không dùng emergency để vượt rule Vault owner-only.

## 7. Persistence, integration và delivery

SQL source change + outbox/intents commit atomically. Worker lấy lease, lưu attempt và checks source/version/authority. Retry phải giữ một logical effect; external delivery có thể at-least-once khi provider không hỗ trợ dedupe, không hứa exactly-once Email/Push. Cần ACK/idempotency policy và báo limitation khi ambiguity.

ASP.NET hosted services cung cấp lifecycle worker, không tự cung cấp durable job storage; SQL journal/lease/retry/recovery vẫn phải thiết kế. [Hosted services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0)

Task → Calendar có thể là **read projection không lưu duplicate state**: Task provider đăng ký stable Event identity từ Task; Calendar gọi contract, đồng bộ ngay current saved data, hiển thị terminal status và ẩn Trash. Calendar API không sửa projection; mở Task detail. Manual Event dùng store riêng. Đây là technical proposal phải prove P02-TSK-023/025; không yêu cầu durable Calendar mirror table hoặc dependency Task→Calendar runtime. Nếu later chọn materialized projection phải chứng minh cùng AC, repair/idempotency và freshness trước đổi ADR.

Other cross-module links dùng typed resource reference, cùng owner, validate nguồn qua contract. Không có job dùng SQL cross-module query để bypass contract. Finance ledger/Task aggregate cần transactions ngay; search/widgets/provider fetch dùng eventual projections với current authorization.

## 8. File, search, logs và validation

Local file adapter nằm ngoài webroot; upload staging/quarantine → validate MIME/signature/size/checksum → immutable object → Save tham chiếu object. Crop metadata gắn version; không overwrite cover của old version. Storage interface cho phép future object storage mà business layer không đổi. Định dạng/quota/scanning vẫn DECISION REQUIRED trước mở upload tương ứng.

Documents local list search chỉ Title/Tag; Global Search dùng fields được module đăng ký. SQL query parameterized, bounded pagination, owner/module filters trước count/page. SQL Full-Text là lựa chọn sau measurement/collation/tokenization profile, không mặc định Elasticsearch/OpenSearch/Algolia.

Operational logs dùng built-in ILogger JSON console/local sink, correlation + redaction. Audit persistent append-only logical behavior và permission riêng; tamper-evidence/protection against DB admin là security-design decision, không hứa tuyệt đối bất biến chỉ vì không có DELETE endpoint.

Validation ba lớp: client field hints; application/domain rules kể cả lifecycle/version restore; FK/unique/check/concurrency SQL. Parse errors/ProblemDetails không trả body/SQL/path/secrets.

## 9. Technology additions — không thay stack

| Lựa chọn đề xuất | Vấn đề cần giải quyết; vì sao stack tên chung chưa đủ | Mức cần thiết / decision |
|---|---|---|
| EF Core 10 + SQL Server provider | Mapping, transactions, migrations; SQL Server tự nó không có .NET ORM/tooling | User yêu cầu EF Core; pin compatible versions tại RM01/RM04 |
| ASP.NET Core Identity | Password/token lifecycle implementation, tránh tự viết primitive; ASP.NET host trống chưa có account store/policy | PROPOSED choice; outcomes AUTH-* bắt buộc |
| Microsoft ASP.NET OpenAPI | Machine-readable API contract | Built-in ecosystem, planned; Swagger/Scalar UI optional và chỉ dev nếu duyệt |
| Vite + TypeScript | React không tự cung cấp bundler/HMR/type checks | Build tool cần một lựa chọn; Vite/TS PROPOSED; không cần Next.js/SSR theo scope hiện tại |
| React Router | URL/history/guard coordination cho nhiều modules | PROPOSED; không bắt buộc global state manager |
| StackExchange.Redis integration qua .NET distributed-cache adapter | Backend cần Redis protocol client | Một Redis client cần thiết cho approved cache use case; pin tại RM01 |
| Block/Markdown parser/editor, sanitizer, image crop library | React/ASP.NET không cung cấp editor round-trip/XSS-safe renderer/crop UX hoàn chỉnh | Capability bắt buộc; package/feature subset/licensing/security còn DECISION REQUIRED |
| ICS/RSS/XML/YAML/Cron parsers | Formats/timezone/unsafe-input handling không chỉ CRUD | Chọn theo module gate; không tự viết parser ad hoc; required khi capability được approve |
| Unit/component/E2E runners | Repeatable assertions/browser automation | Chọn một bộ tối thiểu tại RM01; proposed xUnit, Vitest/Testing Library, Playwright; chưa cài |
| Optional query cache/form library | Repeated request/state/form complexity khi measured need | Không bắt buộc Redux, TanStack Query, Zustand hoặc form framework từ đầu |
| Optional telemetry exporter/job engine | Metrics/traces hoặc durable scheduler complexity vượt baseline | Có thể dùng .NET built-ins + SQL trước; Hangfire/Quartz/OpenTelemetry vendor stack cần ADR, không mặc định |
| Message broker/microservices/search cluster | Chưa có requirement chứng minh cần deployment phân tán | Không thêm vào local baseline |

.NET SQL Server provider có package chính thức; Vite có template React và yêu cầu Node tương thích cần pin khi implementation. [EF Core SQL Server provider](https://learn.microsoft.com/en-us/ef/core/providers/sql-server/), [Vite guide](https://vite.dev/guide/)

## 10. ADR proposals và gate

Tất cả dưới đây là **PROPOSED**, trừ stack User đã chỉ định; chưa sửa DEC-TEC trong requirements.

| ADR roadmap | Nội dung | Source decisions / khi khóa |
|---|---|---|
| ADR-R01 | Modular monolith, layers, REST/OpenAPI, module compile boundary | DEC-TEC-002/013; RM01 |
| ADR-R02 | SQL Server/EF Core, per-module contexts, owner FK, migration journal | DEC-TEC-003/014; RM01/RM04 |
| ADR-R03 | Identity/cookie/SQL session registry, CSRF và auth lifecycle | DEC-TEC-004, DEC-SEC-001/009; RM05 trước code |
| ADR-R04 | Access context, current-grant checks, support/emergency audit/session/revocation | DEC-SEC-003/008, DEC-SUP-*; RM05–RM06 |
| ADR-R05 | Rowversion, Save idempotency, immutable versions, aggregate locks | DEC-TEC-015; RM04/RM08/RM09 |
| ADR-R06 | SQL durable jobs/outbox, engine/dialect, retry/lease/cancel/missed runs | DEC-TEC-008; RM06 và RM14 expansion |
| ADR-R07 | Three-channel delivery adapters, timing/retry/dedupe and failure semantics | DEC-TEC-009; local RM06, live checks trước RM18 |
| ADR-R08 | Storage/media, scanning/quotas, crypto envelopes và key separation | DEC-TEC-006, DEC-SEC-002/006; RM06/RM09/RM12 |
| ADR-R09 | SQL search/collation/ranking/consistency and Redis policy | DEC-TEC-005/007; RM11 |
| ADR-R10 | React/build/router/forms/editor/component/test conventions | DEC-TEC-001; RM07/RM09 |
| ADR-R11 | Local measurement/observability/backup evidence | DEC-TEC-010/011; local RM17; production details RM19+ |
| ADR-R12 | Production architecture/hosting/deployment | DEC-TEC-012 và production parts các ADR khác; **sau RM18** |

ADR review tương lai phải ghi options, decision, rationale, consequence, source IDs, reviewers, date và validation. Tài liệu này đưa ra phương án cụ thể để review, không gọi tất cả lựa chọn là đã Approved.
