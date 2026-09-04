# Scope and Master Module Catalog

**Document ID:** `NX-PRD-001`  
**Version:** `1.2-draft`  
**Status:** Release 1 module set confirmed; detailed feature refinement continues  
**Purpose:** Xác định module boundary và tránh trùng lặp capability.

## 1. Quy tắc phân loại scope

- `Committed`: module/capability thuộc Release 1; phải hoàn thành theo requirement và acceptance criteria đã được duyệt.
- `Proposed`: hợp lý từ catalog hiện tại nhưng chưa được Product Owner khóa scope.
- `Deferred`: có trong tầm nhìn nhưng không thuộc release path hiện tại.
- `Excluded`: chủ động loại khỏi baseline.

Product Owner đã quyết định Release 1 bao gồm **toàn bộ module đang có requirement trong bộ tài liệu này**. Có thể chia delivery thành nhiều milestone nội bộ, nhưng không được coi placeholder/demo là module hoàn chỉnh. Feature bị loại rõ ràng trong từng module (ví dụ Project/Task import-export hoặc external calendar sync) không thuộc Definition of Done của module đó.

## 2. Module catalog Release 1

| Domain | Module/capability | Trạng thái | Phase đề xuất |
|---|---|---|---:|
| Core Platform | Public registration, email verification, Authentication, Users, Profiles, Roles, Permissions | Committed | 1 |
| Core Platform | Personal data boundary và cross-user isolation | Committed | 1 |
| Module Platform | Registry, manifest, lifecycle, enablement, dependencies, migrations, contribution contracts | Committed | 1 |
| Core Platform | Sharing Engine, Notifications, Audit, Trash, Security, Files, Settings | Committed | 1; mở rộng dần |
| Core Platform | Integrations, Import/Export, Backup/Restore, Activity/History | Committed theo format/use case được từng module duyệt | 1–8 |
| Productivity | Tasks, Projects, Calendar, Events, Reminders | Committed | 2 |
| Productivity | Planner, Goals, Habits, Time Tracking, Pomodoro | Committed | 2 |
| Knowledge | Notes, Knowledge Base, Documents, Files, Bookmarks, Snippets, Collections, Tags | Committed | 3 |
| Knowledge | Templates, Versioning, Archive, Read Later | Committed | 3 |
| Search | Global Search | Committed | 3 |
| Search | Advanced/Saved Search, Favorites, Recent, History, Command Palette | Committed | 3 |
| Dashboard | Home, widgets, quick actions, cross-module overview | Committed | 3 |
| Finance | Accounts, Transactions, Income/Expense, Bills, Payments, Subscriptions, Budget, Reports | Committed | 4 |
| Vault | Passwords, Secure Notes, API Keys, Tokens, credentials, recovery codes, generic secrets | Committed | 4 |
| Information | News Reader, RSS, sources, categories, saved/read-later/history/topic watch | Committed | 5 |
| Shopping | Shopee Product/Price Tracking, history, target price, alerts | Committed | 5 |
| Shopping | Wishlist, comparison, orders, purchases, seller tracking, warranty | Committed | 5 |
| Developer | Developer Toolbox | Committed | 6 |
| Developer | GitHub Discovery: new/weekly popular/detail | Committed | 6 |
| Developer | GitHub filters, snapshots/history | Committed | 6 |
| Automation | Scheduler, workflows, jobs, webhooks, history/logs/failures, monitoring | Committed | 6 |
| Automation | n8n integration/data sync | Committed, detailed boundary còn phải duyệt | 6 |
| Personal Assets | Inventory, devices, purchase data, warranty, invoices, accessories | Committed | 7 |
| Digital Assets | Domains, hosting, VPS, certificates, online services, licenses, expiry | Committed | 7 |
| Career/Learning | Jobs, companies, interviews, resumes, skills, courses, certifications, work log | Committed | 7 |
| Future Extensions | No-code Module Builder, third-party executable marketplace | Deferred | Sau Phase 8 hoặc decision mới |

## 3. Boundary decisions đề xuất

Các boundary dưới đây là `PROPOSED` và được theo dõi trong decision log.

### 3.1 Files là platform service

`File Storage` sở hữu binary object, metadata kỹ thuật, quota, integrity và access enforcement. Các module sở hữu quan hệ attachment và business meaning. Xóa record không được làm mất file đang được resource khác tham chiếu.

### 3.2 Notification Center không sở hữu business schedule

Module nguồn xác định **khi nào/sự kiện gì** cần thông báo; Notification Center xác định preference, delivery, read state, retry và lịch sử. Ví dụ Tasks sở hữu due date; Notification Center sở hữu notification đã phát.

### 3.3 Reminders dùng một shared contract

User có thể tạo reminder độc lập trong Productivity. Module khác tạo reminder thông qua cùng contract nhưng vẫn là owner của business object nguồn.

### 3.4 Tags và Collections

Tags sử dụng chung vocabulary và ownership model nhưng mỗi module phải khai báo loại resource được tag. Collections là container có type; không cho phép gom resource tùy ý nếu module chưa khai báo support.

### 3.5 Read Later chỉ có một capability

News article, bookmark hoặc URL được đưa vào một reading queue thống nhất; không tạo hai danh sách `Read Later` độc lập ở Knowledge và News.

### 3.6 Licenses và Warranty

- License key/activation secret thuộc Vault.
- Thông tin quyền sử dụng, ngày mua/gia hạn/expiration thuộc Digital Assets.
- Warranty policy/expiration thuộc asset hoặc purchase; Notification Center chỉ phát cảnh báo.

### 3.7 Activity History và Audit Log

Activity History phục vụ user experience và có thể được dọn theo retention. Audit Log phục vụ security/compliance, chỉ append theo logical behavior và có access control/retention riêng.

### 3.8 Automation và Background Jobs

Background job infrastructure là platform service. Automation Center là product surface cho workflow/schedule do user hoặc admin cấu hình. Internal jobs không mặc nhiên xuất hiện như user automation.

### 3.9 Personal ownership và external sharing

Mỗi resource thuộc đúng một User/Personal Space. `CreatedByUserId`/`UpdatedByUserId` mô tả actor nhưng không cho phép client chọn owner. Release 1 không có Workspace hoặc team-owned resource. External Sharing Engine chỉ tạo read-only grant cho resource hiện tại và không chuyển ownership hay cấp edit/comment.

### 3.10 Module và navigation item

Module là developer-owned package có manifest, lifecycle, permissions, entities, migrations và contributions. Một module có thể đóng góp nhiều route; một route/widget/platform capability không nhất thiết là module độc lập. Admin/User không tạo hoặc upload module code.

### 3.11 Concurrency không phải collaboration

Project/Task/Document vẫn cần history và optimistic concurrency để xử lý nhiều tab/session của cùng User, nhưng Release 1 không có assignment cho người khác, comments/mentions/follows giữa members, live cursor hoặc collaborative editing.

## 4. Capability dependencies

| Consumer | Dependency bắt buộc |
|---|---|
| Mọi business module | Identity, ownership scope, authorization, audit baseline, trash policy |
| Mọi business module | Personal owner boundary và cross-user negative authorization tests |
| Mọi developer-built module | Module Registry, manifest, dependency/version/migration và contribution contracts |
| Documents/Knowledge/Vault/Assets | File service; Vault còn cần encryption/key management |
| Tasks/Calendar/Finance/Shopping/Assets | Notification contract và scheduler |
| Global Search | Search projection từ từng module + access filter tại query time |
| Sharing | Resource registry, ownership, access evaluator, token, authentication/allowlist, revoke và expiration controls |
| Shopee/News/GitHub | Integration client, rate-limit handling, cache, job history |
| Automation | Scheduler, secrets reference, permission engine, audit, retry/idempotency |
| Dashboard | Read models/API từ module đã phát hành; không sở hữu dữ liệu nguồn |

## 5. Scope rules cho mọi phase

1. Một module chỉ đạt Definition of Ready khi có developer/maintainer, manifest, personal owner model, user journey, state model, permission actions, audit events, migrations và acceptance criteria.
2. Candidate feature không được triển khai như behavior mặc định nếu chưa có decision.
3. Mọi integration phải có degraded behavior; lỗi provider không được làm sập application shell.
4. Mọi list/detail/export/search phải áp dụng cùng access policy như business API.
5. Mọi module phải khai báo rõ support hoặc không support: sharing, files, tags, notifications, trash, search, import/export.
6. Mọi module Release 1 phải khai báo `Personal` ownership; `Workspace` không phải supported scope.
7. Enable/disable module không được thay thế permission check hoặc tự xóa data.

## 6. Scope exclusions xuyên suốt

- AI/LLM processing.
- GitHub private data hoặc write operation.
- Edit-through-public-share và anonymous collaboration.
- Team Workspace, membership, group ownership và team collaboration.
- Commercial billing/plan enforcement.
- Native mobile clients.
- Tự động thu thập credential của user từ browser/OS.
- Tích hợp bên ngoài không có trong phase document hoặc decision record được duyệt.
- Live presence/cursor, realtime co-editing, CRDT/Operational Transformation.
- No-code module creation bởi User/Admin.
- User/Admin upload executable module, frontend bundle hoặc database migration.
- Third-party executable marketplace trước khi có security/supply-chain specification.

## 7. Điều kiện khóa Master Module Catalog

Catalog được coi là locked cho một release train khi:

- tất cả module có trạng thái và phase;
- boundary decisions ở mục 3 được duyệt hoặc thay thế;
- module P0 có acceptance criteria và owner;
- dependency/risks đã được ghi;
- Personal ownership, sharing và support-access behavior đã được khai báo;
- Module manifest/lifecycle/migration/enablement requirements đã được đáp ứng;
- Product Owner phê duyệt các mục `PROPOSED` được đưa vào committed scope.
