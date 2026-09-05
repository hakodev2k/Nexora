# Nexora — Master Implementation Roadmap

**Version:** 1.0-draft · **Ngày:** 2026-09-05 · **Status:** Chờ review/approval roadmap.

**PLANNING AND DOCUMENTATION ONLY.** Mọi phase dưới đây là công việc tương lai, chưa thực thi. Không có application source, database, migration, package installation, runtime configuration hoặc test execution được tạo/chạy trong task này. Phạm vi ghi tài liệu chỉ là `docs/roadmap/`. Hoàn tất bộ tài liệu rồi dừng, chờ approval trước implementation.

## 1. Baseline và cách dùng

Source of truth là toàn bộ 18 file trong [docs/requirements](../requirements/00-product-charter.md), chốt đọc tại commit [fc79a9c53bf6c896a8771e4e0a239a2d14cab214](https://github.com/hakodev2k/Nexora/commit/fc79a9c53bf6c896a8771e4e0a239a2d14cab214). Requirement docs được giữ nguyên. Prompt `Pasted markdown(10).md` bổ sung chỉ dẫn lập kế hoạch: .NET 10/ASP.NET Core, ReactJS, SQL Server, Redis; local trước production; 11 tài liệu roadmap. Chỉ dẫn stack mới được ghi tại roadmap, không sửa lịch sử các DEC-TEC còn Open trong requirement.

Roadmap mô tả HOW/WHEN theo dependency. `MAPPED` chỉ có nghĩa đã có vị trí xây/kiểm chứng; không có nghĩa Approved, Implemented hoặc Passed. Business proposals vẫn cần Product Owner chốt. Phases requirement cũ dùng `P00…P08`; delivery phases mới dùng `RM00…RM22`, không rename hoặc thay ID cũ.

| Tài liệu | Mục đích |
|---|---|
| [00-master-implementation-roadmap.md](00-master-implementation-roadmap.md) | Thứ tự triển khai và các cổng hoàn thành |
| [01-requirement-traceability.md](01-requirement-traceability.md) | Audit từng ID, phần mô tả, catalog, open/conflict register |
| [02-solution-architecture-roadmap.md](02-solution-architecture-roadmap.md) | Layer/module boundaries, contracts, authentication/authorization và ADR proposals |
| [03-local-development-roadmap.md](03-local-development-roadmap.md) | Local topology, config, ports, secrets, runbook tương lai |
| [04-backend-roadmap.md](04-backend-roadmap.md) | API/business enforcement và implementation contracts từng feature |
| [05-database-roadmap.md](05-database-roadmap.md) | Conceptual domain, SQL/EF Core, constraints, migrations, lifecycle/versioning |
| [06-frontend-roadmap.md](06-frontend-roadmap.md) | React architecture, shared components, Documents và toàn bộ feature UI |
| [07-redis-roadmap.md](07-redis-roadmap.md) | Use cases, keys, TTL, invalidation, fallback |
| [08-testing-roadmap.md](08-testing-roadmap.md) | Test suites, trace-to-scenario và evidence gates |
| [09-local-stable-release.md](09-local-stable-release.md) | Definition of Done cho toàn bộ Local Stable Release |
| [10-production-roadmap.md](10-production-roadmap.md) | Production phases chỉ bắt đầu sau Local Stable |

## 2. Scope không được rút gọn ngầm

Release 1 gồm toàn bộ module đang có requirement. Planner, Goals, Habits, Time Tracking, Pomodoro, Templates, advanced/saved Search, GitHub history, shopping records, n8n và Career/Learning không biến mất vì mang P1 hoặc chưa phỏng vấn xong. Chúng có refinement task và delivery slot; nếu muốn defer phải có Product Owner decision mới. Không cam kết ngày/cost/nhân lực khi chưa có dữ liệu estimate.

Giữ personal-only Public SaaS; không Workspace/collaboration/AI/billing/native apps/no-code builder/executable marketplace. Project/Task import/export được defer theo DEC-PRJ-008. Calendar ICS import/export vẫn thuộc Release 1; đó là export Event projection, không phải Project/Task archive/restore. Các loại import/export khác chỉ implement format được module duyệt.

## 3. Master Development Order

Mỗi hàng dưới là một phase/step duy nhất; đặc tả tám trường cho từng phase ở mục 5.

| Thứ tự | Phase | Kết quả chính |
|---:|---|---|
| 01 | [RM00 — Requirements Consolidation](#rm00) | Khóa phần requirement đủ rõ theo từng slice, không hỏi lại quyết định Approved. |
| 02 | [RM01 — Solution Architecture](#rm01) | Chốt thiết kế đủ để nền tảng không phải làm lại ownership hoặc module boundary. |
| 03 | [RM02 — Local Environment Setup](#rm02) | Có môi trường developer tái lập và smoke connectivity local. |
| 04 | [RM03 — Backend Foundation](#rm03) | Có ASP.NET Core host chuẩn, module composition root và API contracts. |
| 05 | [RM04 — Domain and Database Foundation](#rm04) | Chuyển conceptual model đã duyệt thành SQL Server schema có invariants. |
| 06 | [RM05 — Identity, Authentication and Authorization](#rm05) | Đăng ký/xác minh dùng ngay, cô lập dữ liệu cá nhân và role/action đúng. |
| 07 | [RM06 — Core Platform Services](#rm06) | Các module dùng chung lifecycle, permissions, sharing, files, jobs và notifications. |
| 08 | [RM07 — React Foundation and Shared Components](#rm07) | Có shell, routing, forms và components dùng chung trước feature UI. |
| 09 | [RM08 — Projects, Tasks and Calendar](#rm08) | Hoàn thiện vertical slice cá nhân đã làm rõ nhất. |
| 10 | [RM09 — Documents and Knowledge Utilities](#rm09) | Hoàn thiện Documents thống nhất cùng Files/Bookmarks/Snippets/Read Later. |
| 11 | [RM10 — Remaining Productivity](#rm10) | Hoàn thiện Planner, Goals, Habits, Time Tracking và Pomodoro thuộc Release 1. |
| 12 | [RM11 — Global Search and Dashboard](#rm11) | Tìm kiếm/tổng hợp đúng quyền và không sở hữu state nguồn. |
| 13 | [RM12 — Finance and Vault](#rm12) | Đạt ledger correctness và bảo vệ secret/recovery trước dữ liệu giá trị. |
| 14 | [RM13 — News and Shopping](#rm13) | Hoàn thiện feeds, tracking và shopping records với dữ liệu provider trung thực. |
| 15 | [RM14 — Developer, GitHub and Automation](#rm14) | Hoàn thiện utilities, GitHub discovery, automation/webhooks/n8n theo scope đã duyệt. |
| 16 | [RM15 — Assets, Digital Assets and Career/Learning](#rm15) | Hoàn thiện các module vòng đời tài sản và nghề nghiệp/học tập. |
| 17 | [RM16 — Cross-module Integration and Coverage Closure](#rm16) | Đóng toàn bộ catalog và các integration/lifecycle seams. |
| 18 | [RM17 — Local Qualification, Hardening and Bug Fixing](#rm17) | Chứng minh toàn hệ thống ổn định trên local bằng tests/diễn tập. |
| 19 | [RM18 — Local Stable Release](#rm18) | Ghi nhận cổng bắt buộc trước Production Planning. |
| 20 | [RM19 — Production Architecture](#rm19) | Thiết kế các lựa chọn production trên bằng chứng local. |
| 21 | [RM20 — Hosting Selection and Deployment Design](#rm20) | Chọn hosting cụ thể và runbook deploy từ options đã duyệt. |
| 22 | [RM21 — Production Deployment and Rehearsal](#rm21) | Triển khai candidate trong môi trường được duyệt và kiểm thử topology thật. |
| 23 | [RM22 — Public SaaS Release](#rm22) | Mở website công khai khi Product Owner ghi nhận Go. |

## 4. Dependency và PARALLEL

Critical path: quyết định platform → architecture → local host/connections → backend/DB → identity → platform → frontend → tất cả domain slices → integration → qualification → Local Stable. Production Architecture, Hosting Selection và Deployment đều đứng sau Local Stable.

| Nhánh PARALLEL có điều kiện | Gate chung | Điểm hội tụ |
|---|---|---|
| Wireframe/design system review và backend design | Scope/terminology/access đã rõ tại RM01 | RM07 |
| Documents (RM09) và remaining Productivity (RM10) | RM08 + File/Tag/Notification contracts frozen cho slice | RM11/RM16 |
| Finance và Vault workstreams trong RM12 | Crypto/integration/file/access contracts được duyệt | RM12 exit; cả hai phải hoàn thành |
| Pure client Toolbox và News/Shopping adapters | Tool scope/SSRF/provider contracts đã rõ; no shared schema conflict | RM14/RM16 |
| Assets/Career và Automation UI | Finance/Vault/Files/Productivity contracts đã stable | RM16 |
| Unit/integration/feature UI test authoring cùng feature development | AC đã approved | Mọi phase exit |

PARALLEL là khả năng lập lịch tương lai, không phải đã chạy nhiều phase. Không mở concurrency trên cùng migration/aggregate khi schema contract còn thay đổi. Nhóm chưa qua DoR tiếp tục refinement; nhóm đã rõ không phải hỏi lại. Không chốt Production Planning để unblock local.

## 5. Phase specifications

<a id="rm00"></a>

### RM00 — Requirements Consolidation

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Khóa phần requirement đủ rõ theo từng slice, không hỏi lại quyết định Approved. |
| Input | 18 tài liệu baseline; prompt markdown(10); lịch sử quyết định. |
| Tasks | Đối chiếu catalog, lập coverage, phân loại proposal; xử lý conflict/open backlog trong 01; lập stories và AC theo slice. |
| Technical Decisions | Chỉ Product Owner đổi business behavior; architecture không đóng thay product decision. |
| Dependencies | Approval roadmap trước mọi triển khai; decision chặn slice phải đóng. |
| Deliverables | Baseline tham chiếu, scope manifest, backlog có ID/owner/DoR. |
| Definition of Done | Không có confirmed requirement thất lạc; conflict của slice được xử lý có nguồn, reviewer; chưa coi tất cả domain đã Ready. |
| Next Step | RM01; tiếp tục refinement domain song song với thiết kế đã đủ đầu vào. |

<a id="rm01"></a>

### RM01 — Solution Architecture

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Chốt thiết kế đủ để nền tảng không phải làm lại ownership hoặc module boundary. |
| Input | RM00 cho platform; stack .NET 10, ReactJS, SQL Server, Redis. |
| Tasks | Review layer/module boundaries, conceptual domain model, access algorithm, threat model, API conventions, contracts, local profile và ADR backlog. |
| Technical Decisions | Các phương án trong 02 là PROPOSED; stack được User chỉ định. Thiết kế identity/authorization trước DB; implementation sau DB. |
| Dependencies | RM00; các DEC-TEC và DEC-SEC chặn platform; không chờ hosting. |
| Deliverables | Architecture/data/access/API/module contract v1 được review; danh sách package có lý do. |
| Definition of Done | Không vòng dependency; owner query và revoke test plan rõ; decisions nền tảng đủ cho RM02–RM06. |
| Next Step | RM02; design system có thể PARALLEL sau khi thông tin điều hướng được duyệt. |

<a id="rm02"></a>

### RM02 — Local Environment Setup

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Có môi trường developer tái lập và smoke connectivity local. |
| Input | RM01; OS/CPU, SDK/Node/SQL/Redis version profile đã chọn. |
| Tasks | Setup dependencies, local HTTPS/config/secrets/file roots; tạo tối thiểu host API và React smoke app trong implementation tương lai; kiểm tra SQL/Redis connections. |
| Technical Decisions | Đề xuất Windows x64 + SQL Server local + Redis qua WSL2; chưa chọn container production. Ports và biến cấu hình xem 03. |
| Dependencies | RM01 approval; prerequisite installation quyền developer; synthetic data. |
| Deliverables | Local profile, smoke hosts, dependency lock/pin và runbook sơ bộ. |
| Definition of Done | React/API khởi động; SQL/Redis reachable; không public bind; chưa được coi business platform hoàn thành. |
| Next Step | RM03; hai smoke hosts được mở rộng, không tạo lại project ở phase sau. |

<a id="rm03"></a>

### RM03 — Backend Foundation

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Có ASP.NET Core host chuẩn, module composition root và API contracts. |
| Input | RM02; solution/API conventions. |
| Tasks | Tạo layer boundaries, DI, options validation, safe errors, structured logging/redaction, cancellation, OpenAPI và liveness/readiness. |
| Technical Decisions | ASP.NET built-ins trước; SQL outbox/worker contracts; không microservices/message broker. |
| Dependencies | RM01–RM02; auth contract đã thiết kế nhưng chưa implement identity. |
| Deliverables | Nexora.WebApi + Domain/Application/Infrastructure skeleton; API schema và test harness. |
| Definition of Done | Safe error/health/validation contract pass; không endpoint mẫu công khai còn sót; no secret logs. |
| Next Step | RM04. |

<a id="rm04"></a>

### RM04 — Domain and Database Foundation

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Chuyển conceptual model đã duyệt thành SQL Server schema có invariants. |
| Input | RM01 domain/access design; RM03 host/config. |
| Tasks | EF Core contexts theo module; migrations, constraints/indexes, ownership keys, concurrency, lifecycle/version schema, optional seed plan. |
| Technical Decisions | SQL Server + EF Core; data model 05. Không sinh bảng cho domain còn OPEN chỉ từ ví dụ. |
| Dependencies | RM03; owner representation, normalization, key/version strategy được review. |
| Deliverables | Initial schema/migrations, migration journal, seed/upgrade/restore contracts. |
| Definition of Done | Empty DB migration và constraint/isolation tests pass; retry seed không duplicate; module failure không mark ready. |
| Next Step | RM05; schema domain tiếp tục phát triển trong chính phase domain. |

<a id="rm05"></a>

### RM05 — Identity, Authentication and Authorization

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Đăng ký/xác minh dùng ngay, cô lập dữ liệu cá nhân và role/action đúng. |
| Input | RM04 core schema; authentication/security ADR. |
| Tasks | Bootstrap SuperAdmin, register/verify/resend, login/logout/revoke/recovery, profile/timezone, personal boundary; access contexts và last-SuperAdmin invariant. |
| Technical Decisions | Đề xuất ASP.NET Identity + secure cookie và SQL session registry; durations/mật khẩu/MFA còn cần decision có nguồn. |
| Dependencies | RM04; DEC-TEC-004/014, DEC-SEC-001/003/008/009; account lifecycle refinements. |
| Deliverables | Identity APIs, policy evaluator, auth OpenAPI, cross-user fixture suite. |
| Definition of Done | Unverified/disabled/session-revoked deny; verification idempotent; normal Admin/SuperAdmin không đọc data User khác. |
| Next Step | RM06; React auth UI ở RM07. |

<a id="rm06"></a>

### RM06 — Core Platform Services

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Các module dùng chung lifecycle, permissions, sharing, files, jobs và notifications. |
| Input | RM05; platform/module/resource contracts; crypto storage design cho integration credentials. |
| Tasks | Registry/manifest, enablement/defaults/Admin grants; sharing ba mode; support/emergency; audit/activity/trash/files/settings; SQL durable intents/jobs và ba delivery adapters. |
| Technical Decisions | Chưa có Vault UI nhưng secret abstraction và protected integration credentials phải an toàn; provider lựa chọn tách khỏi hosting. |
| Dependencies | RM05; share/support open decisions; local delivery và upload policies đủ cho fixture; DEC-TEC-008/009/013. |
| Deliverables | Platform service APIs, read projections, safe local storage/adapters, module contract kit. |
| Definition of Done | Disable giữ data/chặn access và side effects; grant expiry/revoke; emergency có audit+ba attempts; SQL/Redis restart tests pass. |
| Next Step | RM07; no-code builder và marketplace không thuộc release. |

<a id="rm07"></a>

### RM07 — React Foundation and Shared Components

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Có shell, routing, forms và components dùng chung trước feature UI. |
| Input | RM03 API conventions; RM05–RM06 usable APIs. |
| Tasks | Mở rộng React smoke app; auth state, API client, registry navigation, permission UI, error/loading/empty states, responsive layout, design system; Notification/Support/Admin surfaces. |
| Technical Decisions | Đề xuất Vite + TypeScript + Router; fetch và local state trước; library lựa chọn có lý do trong 06. |
| Dependencies | RM06 đủ contracts; language/locale/a11y targets cần review; UI thiết kế có thể PARALLEL từ RM01. |
| Deliverables | React app structure, components, auth/admin/inbox journeys tích hợp API. |
| Definition of Done | Keyboard/mobile, session-expiry, denied route, CSRF và zero client-authority bypass pass. |
| Next Step | RM08; shared components tiếp tục mở rộng theo nhu cầu đã duyệt. |

<a id="rm08"></a>

### RM08 — Projects, Tasks and Calendar

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Hoàn thiện vertical slice cá nhân đã làm rõ nhất. |
| Input | RM06–RM07; Phase 2 approved rules. |
| Tasks | Projects Grid/Table; Tasks Kanban/Table/full form/history/trash; terminal aggregate rules; Task Calendar contribution, manual Events, reminders và ICS. |
| Technical Decisions | Calendar Task Event là projection từ Task, không nguồn state thứ hai; ICS là ngoại lệ portability Calendar. |
| Dependencies | RM05–RM07; DEC-PRD-032/033 và time/trash/state ambiguities trong 01 phải xử lý theo slice. |
| Deliverables | APIs, schema, UI và tests Project/Task/Calendar; share live composition. |
| Definition of Done | Toàn bộ P02 approved AC, race/freeze/restore/ICS/timezone và ba-channel Reminder suites pass. |
| Next Step | RM09 và RM10 có thể PARALLEL sau khi shared contracts ổn định. |

<a id="rm09"></a>

### RM09 — Documents and Knowledge Utilities

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Hoàn thiện Documents thống nhất cùng Files/Bookmarks/Snippets/Read Later. |
| Input | RM06–RM08, module/File contracts và approved Documents answers. |
| Tasks | Triển khai Tag/Folder/page lifecycle, immutable type/editor/parent/folder, manual Save/version restore, media crop; Grid/Table/navigation; mở rộng utilities theo DoR. |
| Technical Decisions | ContentItem là Document page theo quyết định đã có; Block/Markdown canonical payload riêng. Không suy ra autosave hay collaboration. |
| Dependencies | DEC-KNW-032/036/039/040; editor scope, file/share/export/template/collection detail còn OPEN. |
| Deliverables | Documents APIs/schema/editors/UI, safe file previews; utilities được duyệt và test evidence. |
| Definition of Done | P03 Documents confirmed AC pass; exact ba field list; versions/media không bị overwrite; chưa đóng nếu committed utilities còn thiếu. |
| Next Step | RM11; utilities chưa clear được refinement và hoàn thành trước RM16. |

<a id="rm10"></a>

### RM10 — Remaining Productivity

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Hoàn thiện Planner, Goals, Habits, Time Tracking và Pomodoro thuộc Release 1. |
| Input | RM08 Project/Task contracts; catalog committed. |
| Tasks | Refine riêng từng module: journey, fields, state, cadence/progress/timer rules, links, history/lifecycle, UI, adapters và tests; implement sau DoR. |
| Technical Decisions | Không áp bốn Task states hoặc một Reminder sang các module này khi chưa duyệt. |
| Dependencies | RM00 discovery cho năm module; RM06–RM08; PARALLEL với RM09 nếu không đổi contract. |
| Deliverables | Năm module đủ chức năng theo scope được Product Owner duyệt; per-module specs và acceptance suite. |
| Definition of Done | Không module nào chỉ là placeholder; open business rules liên quan đã đóng; no duplicate Task state ownership. |
| Next Step | RM11 và Career/Learning tại RM15. |

<a id="rm11"></a>

### RM11 — Global Search and Dashboard

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Tìm kiếm/tổng hợp đúng quyền và không sở hữu state nguồn. |
| Input | RM08–RM10 read contributions; shell. |
| Tasks | SQL safe search projections/reindex, filters/saved searches/history/favorites/command palette; dashboard widgets/quick actions; mở rộng providers khi domain tiếp theo hoàn thành. |
| Technical Decisions | Documents page search Title/Tag khác Global Search; SQL trước, không search engine riêng mặc định. |
| Dependencies | DEC-PRD-005, DEC-TEC-007; ranking/index-consistency/widget detail; contracts RM06. |
| Deliverables | Search/Dashboard APIs/UI, query corpus, provider contract tests. |
| Definition of Done | Count/facet/snippet/access negative tests pass; widget failure độc lập; quick-create không bypass required selections. |
| Next Step | RM12; provider registry tiếp tục tích hợp RM13–RM16. |

<a id="rm12"></a>

### RM12 — Finance and Vault

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Đạt ledger correctness và bảo vệ secret/recovery trước dữ liệu giá trị. |
| Input | RM06 files/jobs/secret abstraction; RM11 read/search contracts. |
| Tasks | Finance accounts/transactions/bills/payments/subscriptions/budgets/reports và P1 committed scope sau refinement; Vault types/masked/reveal/copy/versioning/rotation/restore. |
| Technical Decisions | No bank sync; no implicit cross-user reveal/export. Decimal/currency; authenticated encryption envelope và protected key store được review. |
| Dependencies | DEC-PRD-007/008, DEC-SEC-002/004, DEC-SUP-002; backup/key rehearsal. Finance/Vault có thể PARALLEL sau shared contracts. |
| Deliverables | Đủ Finance/Vault features approved, ledger fixtures, encryption/rotation/backup evidence. |
| Definition of Done | Không plaintext trong prohibited sinks; restore test secret bằng đúng key; audit reveal; mọi approved finance AC pass. |
| Next Step | RM13–RM15; không dùng real secret trước security gate. |

<a id="rm13"></a>

### RM13 — News and Shopping

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Hoàn thiện feeds, tracking và shopping records với dữ liệu provider trung thực. |
| Input | RM06 jobs/notifications; RM09 Read Later; RM12 secret refs. |
| Tasks | Feeds/read states/topic watch; Shopee variant/price history/alerts; wishlist/comparison/orders/purchases/seller/warranty; fixtures, adapters và UI. |
| Technical Decisions | Manual workflows sống được khi provider lỗi; không bypass captcha/access controls hoặc tự thêm marketplace. |
| Dependencies | DEC-PRD-009/010/011; DEC-SEC-007; acquisition feasibility và field/rule refinement. |
| Deliverables | News/Shopping module set, adapter contracts, source/price golden data, degraded UI. |
| Definition of Done | No fabricated price/zero snapshot; exact dedupe/cooldown; ba-channel delivery; approved catalog features hoàn thành. |
| Next Step | RM14; có thể PARALLEL phần Toolbox không phụ thuộc adapters. |

<a id="rm14"></a>

### RM14 — Developer, GitHub and Automation

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Hoàn thiện utilities, GitHub discovery, automation/webhooks/n8n theo scope đã duyệt. |
| Input | RM06 durable jobs; RM12 Vault refs; RM13 provider contracts. |
| Tasks | Chốt tool list rồi build deterministic utilities; GitHub rankings/detail/filter/snapshots; automation lifecycle/runs, webhooks và n8n contract/data sync. |
| Technical Decisions | Client-only tools khi phù hợp; không arbitrary code/network proxy; n8n không là core dependency, không giữ DB/master keys. |
| Dependencies | DEC-PRD-012/013; week/query/dialect/trigger/action/mapping choices; SSRF review. |
| Deliverables | Tools/GitHub/Automation/n8n features, privacy/fixture/authority/idempotency suites. |
| Definition of Done | Committed n8n/history/webhooks được làm rõ và hoàn thành; P1 không tự đồng nghĩa deferred; provider/runtime failure observable. |
| Next Step | RM15 hoặc PARALLEL với RM15 khi source contracts ổn định. |

<a id="rm15"></a>

### RM15 — Assets, Digital Assets and Career/Learning

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Hoàn thiện các module vòng đời tài sản và nghề nghiệp/học tập. |
| Input | RM09 files/documents; RM10 productivity; RM12 Finance/Vault; RM06 notifications. |
| Tasks | Inventory/devices/accessories/purchase/warranty; domains/hosting/VPS/certificates/services/licenses; jobs/companies/interviews/resumes/skills/courses/certifications/work logs. |
| Technical Decisions | Same-owner typed links; không remote shell/payment/job application; Calendar Interview mapping chưa được tự thêm source type. |
| Dependencies | DEC-PRD-014; all domain fields/state/share/retention; Calendar integration decision. |
| Deliverables | Đầy đủ module đã committed, source/version link rules, APIs/UI và suites. |
| Definition of Done | Exact resume version retained; no sensitive share leak; expiry scheduling đúng; không âm thầm bỏ module từ exit clause cũ. |
| Next Step | RM16. |

<a id="rm16"></a>

### RM16 — Cross-module Integration and Coverage Closure

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Đóng toàn bộ catalog và các integration/lifecycle seams. |
| Input | RM08–RM15 vertical slices; traceability manifest. |
| Tasks | Kiểm tra module list, all providers, registry, links/files, notification intents, search/dashboard, portability formats, account lifecycle; hoàn thiện thiếu sót. |
| Technical Decisions | Không bật import/export Project/Task bởi generic portability; không ép module khác dùng Documents tag policy. |
| Dependencies | Tất cả committed feature refinements closed; không còn OPEN/CONFLICT ảnh hưởng Release 1. |
| Deliverables | Feature coverage report, integrated build candidate, migration chain và complete source→test links. |
| Definition of Done | 100% approved requirements có evidence hoặc PO scope decision explicit; không placeholder; mọi module usable local. |
| Next Step | RM17. |

<a id="rm17"></a>

### RM17 — Local Qualification, Hardening and Bug Fixing

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Chứng minh toàn hệ thống ổn định trên local bằng tests/diễn tập. |
| Input | RM16 candidate; local capacity/NFR profile được duyệt. |
| Tasks | Unit/integration/frontend/E2E regression; abuse/security/a11y/load/fault tests; migrations/upgrade, SQL/files/key backup-restore; fix lỗi và chạy lại suite bị ảnh hưởng. |
| Technical Decisions | Tests xuyên suốt RM03–RM16; đây là qualification tổng hợp, không phải lần đầu test. Không dùng production hosting. |
| Dependencies | No unresolved data/access/crypto semantics; measurable performance and delivery bounds; approved risk gates. |
| Deliverables | Versioned results, defect register, fresh-machine runbook rehearsal và restore report. |
| Definition of Done | Không blocker bug; quality gates pass; Critical/High findings xử lý theo nguồn; valid end-to-end email/push evidence không chỉ mocks. |
| Next Step | RM18. |

<a id="rm18"></a>

### RM18 — Local Stable Release

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Ghi nhận cổng bắt buộc trước Production Planning. |
| Input | RM17 evidence cho toàn bộ catalog. |
| Tasks | Review checklist 09, release candidate tag/reference, limitations, signatures và rollback rehearsal; freeze local runbook có commands đã kiểm chứng. |
| Technical Decisions | Local Stable không phải Public SaaS launch; chỉ Product Owner + Technical/Security owner chấp thuận gate. |
| Dependencies | RM16–RM17 complete; no unapproved omission. |
| Deliverables | LOCAL STABLE RELEASE decision với build/commit/test/artifact references. |
| Definition of Done | Tất cả tiêu chí 09 đạt và approval được ghi; chưa triển khai bất kỳ public infrastructure nào bằng gate này. |
| Next Step | RM19; chỉ bắt đầu Production Planning sau approval. |

<a id="rm19"></a>

### RM19 — Production Architecture

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Thiết kế các lựa chọn production trên bằng chứng local. |
| Input | LOCAL STABLE RELEASE approved. |
| Tasks | Đánh giá trust/network/data boundaries, service responsibilities, capacity/SLO/RPO/RTO, SQL/Redis/files/key/job topology options và threat model. |
| Technical Decisions | Chưa khóa Azure/AWS/VPS/OS/container/CDN trước phase này. |
| Dependencies | RM18; owner vận hành, ngân sách/region/service constraints được làm rõ lúc đó. |
| Deliverables | Production options/architecture decision package và requirement-to-control map. |
| Definition of Done | Các phương án đáp ứng baseline, chi phí/operations/recovery có owner; chưa provision. |
| Next Step | RM20. |

<a id="rm20"></a>

### RM20 — Hosting Selection and Deployment Design

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Chọn hosting cụ thể và runbook deploy từ options đã duyệt. |
| Input | RM19 architecture/options và constraints. |
| Tasks | So sánh host/OS/container/reverse proxy/SQL/Redis/files/DNS/TLS/monitoring/backup/CI-CD; kiểm tra editions/licenses; chọn candidate và plan promotion/rollback. |
| Technical Decisions | Chỉ quyết định tại phase tương lai này; local design không mặc định K8s, cloud hay Docker production. |
| Dependencies | RM19 approval; commercial/operational choices. |
| Deliverables | Approved provider/topology, environment design, cost/capacity/ownership và deployment checklist. |
| Definition of Done | Không secret/public DB exposure; persistence/key/backup lifecycle rõ; deployment approval gate cụ thể. |
| Next Step | RM21 sau authorization cho deployment. |

<a id="rm21"></a>

### RM21 — Production Deployment and Rehearsal

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Triển khai candidate trong môi trường được duyệt và kiểm thử topology thật. |
| Input | RM20 design + explicit deployment authorization. |
| Tasks | Provision/configure tương lai; immutable build/promotion, migrate/bootstrap, TLS/domain, external adapters, monitoring/backup; deployed security/load/restore/rollback rehearsal. |
| Technical Decisions | Không coi local test là production topology evidence; dev seeds/credentials không đi production. |
| Dependencies | RM20 và production security/operations decisions; environment guards. |
| Deliverables | Deployment record, deployed test reports, operations/runbooks, rollback-ready candidate. |
| Definition of Done | P08 gates áp dụng pass; không unresolved blocker; independent security review theo requirement. |
| Next Step | RM22. |

<a id="rm22"></a>

### RM22 — Public SaaS Release

**Status:** PLANNED; chưa thực thi.

| Trường | Nội dung |
|---|---|
| Objective | Mở website công khai khi Product Owner ghi nhận Go. |
| Input | RM21 production evidence; toàn catalog accepted. |
| Tasks | Go/No-Go review, release notes/support readiness, public exposure theo plan, post-launch smoke và monitoring. |
| Technical Decisions | No-Go hoãn phát hành, không đổi mô hình thành local-only hoặc bỏ module. |
| Dependencies | Product/Technical/Security approval; operational owner. |
| Deliverables | Public Release 1, release evidence và follow-up defect/operation ownership. |
| Definition of Done | Registration/verification, personal isolation, all modules, notifications/backup/recovery hoạt động theo approved release; monitoring có người chịu trách nhiệm. |
| Next Step | Vận hành, sửa lỗi và roadmap tiếp theo qua change control. |

## 6. Governance và readiness

- `DECISION REQUIRED`: lựa chọn cần chốt trước coding phần chịu tác động; xem register trong 01 và ADR proposals trong 02.
- `BLOCKER`: task không thể bắt đầu/kết thúc khi dependency ghi ở đó chưa đóng. Không biến safe default chưa duyệt thành quyết định nghiệp vụ.
- `CONFLICT`: source có diễn giải khác nhau; lưu cả nguồn và precedence đã biết. Source requirements chỉ được sửa trong một task được phép riêng.
- Story phải có actor, scope, fields/validation, lifecycle/state, owner/actions/share/support policy, UI states, API contract, DB constraints, events/jobs, retention, acceptance và test links. Một requirement có thể map nhiều story; không tạo máy móc một ticket cho mỗi dòng.
- Work item dự kiến lưu Requirement IDs → Feature → RM phase → ADR → acceptance scenarios → PR/test/evidence khi triển khai. Không có PR/issue/application test nào được tạo trong task tài liệu này.
- Mọi phase có migration/security/accessibility/error-state checks khi liên quan. Không đợi RM17 mới test.
- Approval bộ roadmap không tự đóng mọi OPEN REQUIREMENT và không tự authorize production deployment. Gate RM18 và go-live RM22 vẫn cần evidence thực tế ở tương lai.

## 7. Hiện trạng bàn giao

Bộ này là kế hoạch đầy đủ về coverage và thứ tự làm việc, không phải tuyên bố toàn bộ requirement đã clear hoặc app đã Ready. Các domain còn proposal có refinement work, dependencies và criteria cụ thể. Công việc đầu tiên sau approval là RM00 xử lý blocker của platform; không chạy setup hoặc code trước approval.
