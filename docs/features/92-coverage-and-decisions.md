# Coverage, dispositions và consistency review

> **Current decision amendment — 2026-09-09:** Product Owner approved the implementation-readiness recommendations in [DEC-20260909](../requirements/11-owner-decisions-20260909-implementation-readiness.md). M01 + backend/frontend scaffold + local scripts are approved for implementation only within `DEC-20260909-001`. Account restore/email reuse, TOTP recovery policy, sensitive projections, Vault hybrid recovery, initial Finance scope, initial Task/Reminder extension boundary, read-only outbound boundary and production sequencing have updated statuses in [Decision status](90-open-decisions.md). Historical conflicting paragraphs remain evidence, not active implementation input.

## Catalog coverage

Đối chiếu26 dòng catalog tại baseline d0d8418: **25 dòng Committed có feature spec;1 dòng Future vẫn Deferred**. Đây là coverage tài liệu, không là chứng nhận mọi capability/AC đã được implement hoặc mọi proposal đã Approved. Sau 2026-09-09, chỉ M01 + scaffold có implementation approval; các feature khác vẫn cần slice approval riêng.

| Catalog row | Feature specs | Current disposition |
|---|---|---|
| Core identity/profile/roles | [Identity, Registration và Profile](01-identity-and-profile.md), [Users, Roles và Action Permissions](02-users-roles-and-permissions.md) | M01 subset approved; TOTP recovery policy resolved but enrollment/recovery outside M01 |
| Personal ownership/isolation | [Identity, Registration và Profile](01-identity-and-profile.md), [Users, Roles và Action Permissions](02-users-roles-and-permissions.md) | M01 owner/auth/access foundation approved; full cross-module isolation still tested per slice |
| Module Registry/lifecycle/contributions | [Module Platform và Module Manager](03-module-platform.md) | M01 subset approved; no executable upload/marketplace |
| Sharing/Notifications/Audit/Trash/Security/Files/Settings | [Read-only Sharing Engine](04-read-only-sharing.md), [Support, Emergency Access và Security Center](05-support-emergency-and-security-center.md), [Notification Center và Delivery](06-notification-center.md), [Files, Uploads và Attachments](07-files-and-attachments.md), [Trash, Activity và Audit](08-trash-activity-and-audit.md), [Settings và Application Shell](09-settings-and-app-shell.md) | M01 delivery/audit/settings subset approved; Sharing/Files/Support full scope later; sensitive projection policy approved |
| Integrations/ImportExport/Backup/Activity | [Trash, Activity và Audit](08-trash-activity-and-audit.md), [Import, Export và Backup/Restore](10-import-export-and-backup.md), [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md) | Import/export docs active by module; Integrations/n8n Paused; Vault portability policy approved but crypto implementation gated |
| Tasks/Projects/Calendar/Events/Reminders | [Projects](11-projects.md), [Tasks, Kanban và Table](12-tasks.md), [Calendar, Personal Events và ICS](13-calendar.md), [Reminders và Due Scheduling](14-reminders-and-scheduling.md) | Core flat Productivity scope retained; extensions gated; not in M01 |
| Planner/Goals/Habits/TimeTracking/Pomodoro | [Daily và Weekly Planner](15-planner.md), [Goals và Targets](16-goals.md), [Habit Tracker](17-habits.md), [Time Tracking](18-time-tracking.md), [Pomodoro và Focus](19-pomodoro.md) | Feature specs exist; later slice approval required |
| Documents types/Files/Bookmarks/Snippets/Folders/Page hierarchy/Collections/Tags | [Files, Uploads và Attachments](07-files-and-attachments.md), [Documents, Note và Knowledge Pages](20-documents.md), [Bookmarks](21-bookmarks.md), [Code Snippets](22-snippets.md), [Tags, Collections và Templates](24-organization-and-templates.md) | Feature specs exist; later slice approval required |
| Templates/Versioning/Archive/ReadLater | [Trash, Activity và Audit](08-trash-activity-and-audit.md), [Documents, Note và Knowledge Pages](20-documents.md), [Read Later](23-read-later.md), [Tags, Collections và Templates](24-organization-and-templates.md) | Feature specs exist; later slice approval required |
| Global Search | [Search, Saved Search, Favorites và Command Palette](25-search-favorites-and-command-palette.md) | Feature specs exist; later slice approval required; owner projection gates apply |
| Advanced/SavedSearch/Favorites/Recent/History/CommandPalette | [Search, Saved Search, Favorites và Command Palette](25-search-favorites-and-command-palette.md) | Feature specs exist; later slice approval required |
| Dashboard/Home/Widgets/QuickActions | [Dashboard và Widgets](26-dashboard.md) | Feature specs exist; later slice approval required |
| Finance core + conditional extensions | [Personal Finance](27-finance.md) | Basic manual records approved as initial scope; advanced Finance gated |
| Vault item types | [Vault](28-vault.md) | Hybrid/no-operator-plaintext policy approved; crypto/key implementation gated |
| News/RSS/Sources/Categories/ReadLater/History/TopicWatch | [Read Later](23-read-later.md), [News, RSS và Topic Watch](29-news-and-feeds.md) | Read-only public outbound allowed after guard contract; not in M01 |
| Shopee tracking/history/target/alerts | [Shopee Price Tracking](30-shopee-price-tracking.md) | Paused, not moved to R2, no worker/auto-enable |
| Wishlist/Compare/Orders/Purchases/Seller/Warranty | [Wishlist, Comparison, Orders, Sellers và Warranty](31-shopping-records.md) | Feature specs exist; sensitive projection policy applies; later slice approval required |
| Developer Toolbox | [Developer Toolbox](32-developer-toolbox.md) | Local tools can be scoped later; network tools require outbound guard; not in M01 |
| GitHub New/Weekly/Detail | [GitHub Discovery](33-github-discovery.md) | Read-only public outbound allowed after guard contract; not in M01 |
| GitHub filters/snapshots/history | [GitHub Discovery](33-github-discovery.md) | Feature specs exist; later slice approval required |
| Automation/Scheduler/Jobs/Workflows/Webhooks/Monitoring | [Reminders và Due Scheduling](14-reminders-and-scheduling.md), [Automation, Scheduler và Workflows](34-automation.md), [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md), [Monitoring và Job Operations](36-monitoring-and-job-operations.md) | Core platform jobs distinct; Automation/Integrations Paused; Monitoring read-only probe only after owner target + guard contract |
| n8n integration/data sync | [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md) | Paused, not moved to R2 |
| PersonalAssets/Devices/Purchase/Warranty/Invoices/Accessories | [Personal Assets, Inventory và Devices](37-personal-assets.md) | Feature specs exist; sensitive projection policy applies; later slice approval required |
| DigitalAssets/Domains/Hosting/VPS/Certs/Services/Licenses | [Monitoring và Job Operations](36-monitoring-and-job-operations.md), [Domains, Hosting, VPS, Certificates, Licenses và Services](38-digital-assets.md) | Metadata active later; provider inspection under outbound/provider gate; later slice approval required |
| Career/Learning/Jobs/Companies/Interviews/Resumes/Skills/Courses/Certifications/WorkLog | [Career, Companies, Interviews và Resumes](39-career-and-resumes.md), [Skills, Courses, Certifications, Learning Plan và Work Log](40-learning-and-work-log.md) | Feature specs exist; DOCX/MD and internal Calendar workflow resolved; sensitive projection policy applies |
| Future no-code builder/executable marketplace | Không tạo feature R1 | Deferred, không âm thầm đưa vào Release1 |

## Các chi tiết đã tự giải quyết hoặc PO đã chốt thêm

| Quyết định | Disposition | Nơi quy định |
|---|---|---|
| M01 implementation | Approved bounded slice | DEC-20260909-001; delivery/M01 |
| Account deleted/email reuse | Approved no email reuse, same-account restore only, no purge | DEC-20260909-002 |
| TOTP lost device | Approved recovery-code policy; implementation outside M01 | DEC-20260909-003 |
| Vault recoverability | Approved hybrid/no operator plaintext; crypto still gated | DEC-20260909-004 |
| Sensitive share/support | Approved projection allowlist/default-hidden/safe metadata | DEC-20260909-005 |
| Finance initial scope | Approved basic manual records + explicit currency; advanced gated | DEC-20260909-006 |
| Task/Reminder extensions | Approved flat first slice; extensions gated | DEC-20260909-007 |
| Read-only outbound | Approved for News/GitHub/Monitoring under network guards | DEC-20260909-008 |
| Paused modules | FX30/34/35 remain Paused, not R2 | DEC-20260909-009 |
| Production sequencing | Local Stable before production, no SLA/RPO/RTO yet | DEC-20260909-010 |
| Project InProgress → NotStarted | Resolved delegated: được, cần lý do | FX-11; DEC-PRD-032 |
| Cover format/size/pixel limit | Resolved delegated: JPEG/PNG/WebP,5MiB,25MP; scan/crop an toàn; production quota riêng | FX-07/20; DEC-KNW-032 |
| Documents Tag còn được sử dụng | Current/Archived/Trash refs chặn; history-only snapshot label không chặn catalog deletion; restore rebind/create có preview | FX-20; DEC-KNW-036 |
| List pagination/date filters/error states | Common defaults, exception ICS fully-contained | FX-COM |
| GitHub week/ties/cache | Monday UTC, stable tie,15min cache; exact window/freshness hiển thị | FX-33 |
| Routine Goals/Habits/Planner/Focus/Time Tracking flows | Delegated trong catalog hiện tại; không team/payroll/medical/AI | FX-15…19 |

Không suy các dòng delegated là User trực tiếp trả lời. Security/capacity verification vẫn bắt buộc.

## Mâu thuẫn và giới hạn cần tránh

| ID | Phát hiện | Xử lý |
|---|---|---|
| CONS-01 | P00-009 dẫn DEC-PRD-034 nhưng decision đó không được định nghĩa trong baseline | Đã bỏ reference chưa định nghĩa khỏi P00-009 và dẫn tới FX-01/Q-01/Q-02 là các gate identity hiện hữu; không gán ý nghĩa giả cho DEC-PRD-034 |
| CONS-02 | Phase5 scope prose cũ có “in-app”, email/browser P1 | Quyết định all3 mới hơn luôn thắng, gồm News/Shopping/Module alerts; không coi Email/Push optional |
| CONS-03 | Finance source nói privileged export | Support/Emergency mới đã giới hạn read-only, no export; owner export có permission/audit riêng |
| CONS-04 | Một số phase P1/“if added” có thể bị hiểu là tự deferred | Module catalog Committed không tự giảm; capability conditional cần Q-group chốt, không nhập toàn tính năng sản phẩm tham chiếu |
| CONS-05 | Interview Calendar contract chưa khớp hai nguồn Calendar đã chốt | Q-12, không tự thêm third source |
| CONS-06 | Zero-knowledge/recovery/Vault authenticated sharing chưa chốt | DEC-20260909-004/005 chốt policy, nhưng crypto/projection implementation vẫn cần contracts |
| CONS-07 | Raindrop/Google Drive/Notion có tree/move/retention khác User | Nexora Folder/Parent immutable, Trash indefinite và manual Save giữ nguyên |
| CONS-08 | Source có account/financial delete/restore còn chưa đóng semantics | Account policy đã chốt tại DEC-20260909-002; Finance advanced deletion vẫn gated |
| CONS-09 | Generic source formatting requirements không đủ nói toàn bộ converter round-trip | FX-32 xác định safe subset, loss warnings và deterministic fixtures; library/runtime ADR còn cần thiết |

## Phân biệt các lớp hoàn thành

- **Catalog mapped:** mọi dòng committed có nơi đặc tả.
- **Behavior specified:** source rules + delegated rules + explicit proposal gates đã được ghi.
- **Approved for implementation:** chỉ M01 + scaffold theo DEC-20260909-001, hoặc future slice có PO approval riêng.
- **Implemented/verified:** chưa được tạo bởi docs amendment này; cần code/evidence thật.

[Requirement routing](93-requirement-routing.md) là bảng chỉ đường từ ID nguồn, không thay test-case-to-implementation trace. Các requirement không có ID riêng ở Planner/Goals/Habits được trace từ catalog vào FX IDs, không bịa P02 requirement cũ.
