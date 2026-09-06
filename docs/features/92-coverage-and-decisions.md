# Coverage, dispositions và consistency review

## Catalog coverage

Đối chiếu26 dòng catalog tại baseline d0d8418: **25 dòng Committed có feature spec;1 dòng Future vẫn Deferred**. Đây là coverage tài liệu, không là chứng nhận mọi capability/AC đã được implement hoặc mọi proposal đã Approved.

| Catalog row | Feature specs | Disposition |
|---|---|---|
| Core identity/profile/roles | [Identity, Registration và Profile](01-identity-and-profile.md), [Users, Roles và Action Permissions](02-users-roles-and-permissions.md) | Có đặc tả; major gates theo từng file |
| Personal ownership/isolation | [Identity, Registration và Profile](01-identity-and-profile.md), [Users, Roles và Action Permissions](02-users-roles-and-permissions.md) | Có đặc tả; major gates theo từng file |
| Module Registry/lifecycle/contributions | [Module Platform và Module Manager](03-module-platform.md) | Có đặc tả; major gates theo từng file |
| Sharing/Notifications/Audit/Trash/Security/Files/Settings | [Read-only Sharing Engine](04-read-only-sharing.md), [Support, Emergency Access và Security Center](05-support-emergency-and-security-center.md), [Notification Center và Delivery](06-notification-center.md), [Files, Uploads và Attachments](07-files-and-attachments.md), [Trash, Activity và Audit](08-trash-activity-and-audit.md), [Settings và Application Shell](09-settings-and-app-shell.md) | Có đặc tả; major gates theo từng file |
| Integrations/ImportExport/Backup/Activity | [Trash, Activity và Audit](08-trash-activity-and-audit.md), [Import, Export và Backup/Restore](10-import-export-and-backup.md), [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md) | Có đặc tả; major gates theo từng file |
| Tasks/Projects/Calendar/Events/Reminders | [Projects](11-projects.md), [Tasks, Kanban và Table](12-tasks.md), [Calendar, Personal Events và ICS](13-calendar.md), [Reminders và Due Scheduling](14-reminders-and-scheduling.md) | Có đặc tả; major gates theo từng file |
| Planner/Goals/Habits/TimeTracking/Pomodoro | [Daily và Weekly Planner](15-planner.md), [Goals và Targets](16-goals.md), [Habit Tracker](17-habits.md), [Time Tracking](18-time-tracking.md), [Pomodoro và Focus](19-pomodoro.md) | Có đặc tả; major gates theo từng file |
| Documents types/Files/Bookmarks/Snippets/Folders/Page hierarchy/Collections/Tags | [Files, Uploads và Attachments](07-files-and-attachments.md), [Documents, Note và Knowledge Pages](20-documents.md), [Bookmarks](21-bookmarks.md), [Code Snippets](22-snippets.md), [Tags, Collections và Templates](24-organization-and-templates.md) | Có đặc tả; major gates theo từng file |
| Templates/Versioning/Archive/ReadLater | [Trash, Activity và Audit](08-trash-activity-and-audit.md), [Documents, Note và Knowledge Pages](20-documents.md), [Read Later](23-read-later.md), [Tags, Collections và Templates](24-organization-and-templates.md) | Có đặc tả; major gates theo từng file |
| Global Search | [Search, Saved Search, Favorites và Command Palette](25-search-favorites-and-command-palette.md) | Có đặc tả; major gates theo từng file |
| Advanced/SavedSearch/Favorites/Recent/History/CommandPalette | [Search, Saved Search, Favorites và Command Palette](25-search-favorites-and-command-palette.md) | Có đặc tả; major gates theo từng file |
| Dashboard/Home/Widgets/QuickActions | [Dashboard và Widgets](26-dashboard.md) | Có đặc tả; major gates theo từng file |
| Finance core + conditional extensions | [Personal Finance](27-finance.md) | Có đặc tả; major gates theo từng file |
| Vault item types | [Vault](28-vault.md) | Có đặc tả; major gates theo từng file |
| News/RSS/Sources/Categories/ReadLater/History/TopicWatch | [Read Later](23-read-later.md), [News, RSS và Topic Watch](29-news-and-feeds.md) | Có đặc tả; major gates theo từng file |
| Shopee tracking/history/target/alerts | [Shopee Price Tracking](30-shopee-price-tracking.md) | Có đặc tả; major gates theo từng file |
| Wishlist/Compare/Orders/Purchases/Seller/Warranty | [Wishlist, Comparison, Orders, Sellers và Warranty](31-shopping-records.md) | Có đặc tả; major gates theo từng file |
| Developer Toolbox | [Developer Toolbox](32-developer-toolbox.md) | Có đặc tả; major gates theo từng file |
| GitHub New/Weekly/Detail | [GitHub Discovery](33-github-discovery.md) | Có đặc tả; major gates theo từng file |
| GitHub filters/snapshots/history | [GitHub Discovery](33-github-discovery.md) | Có đặc tả; major gates theo từng file |
| Automation/Scheduler/Jobs/Workflows/Webhooks/Monitoring | [Reminders và Due Scheduling](14-reminders-and-scheduling.md), [Automation, Scheduler và Workflows](34-automation-and-scheduler.md), [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md), [Monitoring và Job Operations](36-monitoring-and-job-operations.md) | Có đặc tả; major gates theo từng file |
| n8n integration/data sync | [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md) | Có đặc tả; major gates theo từng file |
| PersonalAssets/Devices/Purchase/Warranty/Invoices/Accessories | [Personal Assets, Inventory và Devices](37-personal-assets.md) | Có đặc tả; major gates theo từng file |
| DigitalAssets/Domains/Hosting/VPS/Certs/Services/Licenses | [Monitoring và Job Operations](36-monitoring-and-job-operations.md), [Domains, Hosting, VPS, Certificates, Licenses và Services](38-digital-assets.md) | Có đặc tả; major gates theo từng file |
| Career/Learning/Jobs/Companies/Interviews/Resumes/Skills/Courses/Certifications/WorkLog | [Career, Companies, Interviews và Resumes](39-career-and-resumes.md), [Skills, Courses, Certifications, Learning Plan và Work Log](40-learning-and-work-log.md) | Có đặc tả; major gates theo từng file |
| Future no-code builder/executable marketplace | Không tạo feature R1 | Deferred, không âm thầm đưa vào Release1 |

## Các chi tiết đã tự giải quyết

| Quyết định | Disposition | Nơi quy định |
|---|---|---|
| Project InProgress → NotStarted | Resolved delegated: được, cần lý do | FX-11; DEC-PRD-032 |
| Cover format/size/pixel limit | Resolved delegated: JPEG/PNG/WebP,5MiB,25MP; scan/crop an toàn; production quota riêng | FX-07/20; DEC-KNW-032 |
| Documents Tag còn được sử dụng | Current/Archived/Trash refs chặn; history-only snapshot label không chặn catalog deletion; restore rebind/create có preview | FX-20; DEC-KNW-036 |
| Folder xuất hiện khi filter page | Giữ navigation row, không gán Type/Tag giả | FX-20 |
| Title Document có sửa được | Có ở Draft/Published, manual Save/version; không đổi immutable relations | FX-20 |
| Save change note | Optional; distinct Save vẫn tạo version | FX-20 |
| List pagination/date filters/error states | Common defaults, exception ICS fully-contained | FX-COM |
| GitHub week/ties/cache | Monday UTC, stable tie,15min cache; exact window/freshness hiển thị | FX-33 |
| Routine Goals/Habits/Planner/Focus/Time Tracking flows | Delegated trong catalog hiện tại; không team/payroll/medical/AI | FX-15…19 |

Không suy các dòng này là User trực tiếp trả lời. Security/capacity verification vẫn bắt buộc.

## Mâu thuẫn và giới hạn cần tránh

| ID | Phát hiện | Xử lý |
|---|---|---|
| CONS-01 | P00-009 dẫn DEC-PRD-034 nhưng decision đó không được định nghĩa trong baseline | Đã bỏ reference chưa định nghĩa khỏi P00-009 và dẫn tới FX-01/Q-01/Q-02 là các gate identity hiện hữu; không gán ý nghĩa giả cho DEC-PRD-034 |
| CONS-02 | Phase5 scope prose cũ có “in-app”, email/browser P1 | Quyết định all3 mới hơn luôn thắng, gồm News/Shopping/Module alerts; không coi Email/Push optional |
| CONS-03 | Finance source nói privileged export | Support/Emergency mới đã giới hạn read-only, no export; owner export có permission/audit riêng |
| CONS-04 | Một số phase P1/“if added” có thể bị hiểu là tự deferred | Module catalog Committed không tự giảm; capability conditional cần Q-group chốt, không nhập toàn tính năng sản phẩm tham chiếu |
| CONS-05 | Interview Calendar contract chưa khớp hai nguồn Calendar đã chốt | Q-12, không tự thêm third source |
| CONS-06 | Zero-knowledge/recovery/Vault authenticated sharing chưa chốt | Q-04; no plaintext Admin reveal hoặc bảo đảm recovery khi chưa có key design |
| CONS-07 | Raindrop/Google Drive/Notion có tree/move/retention khác User | Nexora Folder/Parent immutable, Trash indefinite và manual Save giữ nguyên |
| CONS-08 | Source có account/financial delete/restore còn chưa đóng semantics | Q-01/Q-05; không nhận journal correction proposal là approved financial policy |
| CONS-09 | Generic source formatting requirements không đủ nói toàn bộ converter round-trip | FX-32 xác định safe subset, loss warnings và deterministic fixtures; library/runtime ADR còn cần thiết |

## Phân biệt các lớp hoàn thành

- **Catalog mapped:** mọi dòng committed có nơi đặc tả.
- **Behavior specified:** source rules + delegated rules + explicit proposal gates đã được ghi.
- **Ready for implementation:** chỉ khi Q liên quan/ADR/schema/permission/state tests/dependencies được review và PO approve implement.
- **Implemented/verified:** chưa có trong lượt này.

[Requirement routing](93-requirement-routing.md) là bảng chỉ đường từ ID nguồn, không thay test-case-to-implementation trace. Các requirement không có ID riêng ở Planner/Goals/Habits được trace từ catalog vào FX IDs, không bịa P02 requirement cũ.

