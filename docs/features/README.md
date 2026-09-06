# Nexora — Feature Specifications

Ngày: **2026-09-06** · Baseline đọc: [d0d8418](https://github.com/hakodev2k/Nexora/commit/d0d84181e0043f9ffa38b475cbe461d34449805e) · **Documentation only; chưa được approve implement.**

Bộ này phân tích **40 ranh giới feature/capability** cho toàn bộ module catalog hiện tại; không có nghĩa website cần40 menu hay40 plugin độc lập. Mỗi đặc tả có sản phẩm tham chiếu, phần áp dụng/điều chỉnh cho Nexora, luồng màn hình, dữ liệu/validation, lifecycle, commands/integrations, acceptance scenarios và source requirement mapping.

Yêu cầu User đã chốt được giữ nguyên. PM/Technical tự giải quyết chi tiết thông thường theo DEC-GOV-001. Những vấn đề ảnh hưởng phạm vi lớn, dữ liệu nhạy cảm, tiền, chi phí và irreversible loss được gom thành12 nhóm proposal, chưa ghi là Approved. Bản này đủ để review/refine và tách backlog có trace; không tuyên bố tất cả module đã sprint-ready hoặc thay thế API/ERD/threat-model design.

## Cách đọc

1. [Hợp đồng hành vi chung](00-shared-behavior.md): quyền, lifecycle, UX/API/error/concurrency và Definition of Ready.
2. Feature tương ứng trong bảng dưới và [state/action matrices](94-state-and-action-matrices.md).
3. [Coverage và quyết định](92-coverage-and-decisions.md), [routing requirement IDs](93-requirement-routing.md).
4. [Nguồn tham chiếu](91-reference-register.md) và [12 nhóm quyết định lớn](90-open-decisions.md).
5. [Requirements](../requirements/01-scope-and-module-catalog.md) và [implementation roadmap](../roadmap/00-master-implementation-roadmap.md) tiếp tục là nguồn scope/technical gates.

## Feature index

| ID | Feature/capability | Sản phẩm tham chiếu | Major decisions |
|---|---|---|---|
| FX-01 | [Identity, Registration và Profile](01-identity-and-profile.md) | Auth0 | Q-01, Q-02, Q-09 |
| FX-02 | [Users, Roles và Action Permissions](02-users-roles-and-permissions.md) | WordPress | Không phát sinh riêng; common gates áp dụng |
| FX-03 | [Module Platform và Module Manager](03-module-platform.md) | WordPress | Không phát sinh riêng; common gates áp dụng |
| FX-04 | [Read-only Sharing Engine](04-read-only-sharing.md) | Google Drive | Q-03, Q-04 |
| FX-05 | [Support, Emergency Access và Security Center](05-support-emergency-and-security-center.md) | Microsoft Customer Lockbox, Bitwarden | Q-02, Q-04 |
| FX-06 | [Notification Center và Delivery](06-notification-center.md) | GitHub Notifications | Không phát sinh riêng; common gates áp dụng |
| FX-07 | [Files, Uploads và Attachments](07-files-and-attachments.md) | Google Drive, Google Drive | Q-08 |
| FX-08 | [Trash, Activity và Audit](08-trash-activity-and-audit.md) | Google Drive, Microsoft Customer Lockbox | Q-01, Q-08 |
| FX-09 | [Settings và Application Shell](09-settings-and-app-shell.md) | Notion Sidebar, WordPress | Q-09 |
| FX-10 | [Import, Export và Backup/Restore](10-import-export-and-backup.md) | Google Calendar, GitLab | Q-01, Q-04, Q-08, Q-11 |
| FX-11 | [Projects](11-projects.md) | Microsoft To Do, TickTick | Không phát sinh riêng; common gates áp dụng |
| FX-12 | [Tasks, Kanban và Table](12-tasks.md) | Microsoft To Do, TickTick | Q-10 |
| FX-13 | [Calendar, Personal Events và ICS](13-calendar.md) | Google Calendar | Không phát sinh riêng; common gates áp dụng |
| FX-14 | [Reminders và Due Scheduling](14-reminders-and-scheduling.md) | TickTick | Q-10 |
| FX-15 | [Daily và Weekly Planner](15-planner.md) | Microsoft To Do, TickTick | Không phát sinh riêng; common gates áp dụng |
| FX-16 | [Goals và Targets](16-goals.md) | ClickUp Goals | Không phát sinh riêng; common gates áp dụng |
| FX-17 | [Habit Tracker](17-habits.md) | TickTick | Không phát sinh riêng; common gates áp dụng |
| FX-18 | [Time Tracking](18-time-tracking.md) | Toggl Track | Không phát sinh riêng; common gates áp dụng |
| FX-19 | [Pomodoro và Focus](19-pomodoro.md) | TickTick Focus, TickTick | Không phát sinh riêng; common gates áp dụng |
| FX-20 | [Documents, Note và Knowledge Pages](20-documents.md) | Google Docs, Notion | Q-11 |
| FX-21 | [Bookmarks](21-bookmarks.md) | Raindrop.io | Không phát sinh riêng; common gates áp dụng |
| FX-22 | [Code Snippets](22-snippets.md) | GitHub Gists, DevToys | Không phát sinh riêng; common gates áp dụng |
| FX-23 | [Read Later](23-read-later.md) | Instapaper | Không phát sinh riêng; common gates áp dụng |
| FX-24 | [Tags, Collections và Templates](24-organization-and-templates.md) | Notion Templates, Raindrop.io | Không phát sinh riêng; common gates áp dụng |
| FX-25 | [Search, Saved Search, Favorites và Command Palette](25-search-favorites-and-command-palette.md) | Notion Search, Raindrop.io Search, Notion Sidebar | Không phát sinh riêng; common gates áp dụng |
| FX-26 | [Dashboard và Widgets](26-dashboard.md) | ClickUp Dashboards | Không phát sinh riêng; common gates áp dụng |
| FX-27 | [Personal Finance](27-finance.md) | Actual Budget | Q-03, Q-05 |
| FX-28 | [Vault](28-vault.md) | Bitwarden | Q-02, Q-04 |
| FX-29 | [News, RSS và Topic Watch](29-news-and-feeds.md) | Feedly | Không phát sinh riêng; common gates áp dụng |
| FX-30 | [Shopee Price Tracking](30-shopee-price-tracking.md) | camelcamelcamel | Q-06 |
| FX-31 | [Wishlist, Comparison, Orders, Sellers và Warranty](31-shopping-records.md) | AnyList | Q-03 |
| FX-32 | [Developer Toolbox](32-developer-toolbox.md) | DevToys | Q-07 |
| FX-33 | [GitHub Discovery](33-github-discovery.md) | GitHub Search API | Không phát sinh riêng; common gates áp dụng |
| FX-34 | [Automation, Scheduler và Workflows](34-automation-and-scheduler.md) | n8n | Q-07, Q-08 |
| FX-35 | [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md) | n8n | Q-07 |
| FX-36 | [Monitoring và Job Operations](36-monitoring-and-job-operations.md) | UptimeRobot | Q-07, Q-08 |
| FX-37 | [Personal Assets, Inventory và Devices](37-personal-assets.md) | Snipe-IT | Q-03 |
| FX-38 | [Domains, Hosting, VPS, Certificates, Licenses và Services](38-digital-assets.md) | Cloudflare Registrar, Snipe-IT | Q-03, Q-07 |
| FX-39 | [Career, Companies, Interviews và Resumes](39-career-and-resumes.md) | Teal Job Tracker | Q-11, Q-12 |
| FX-40 | [Skills, Courses, Certifications, Learning Plan và Work Log](40-learning-and-work-log.md) | Moodle, Toggl Track | Q-03 |

## Bản đồ nghiệp vụ chính

- Owner tạo Project → tạo Task → Task projection lên Calendar → một Reminder → ba kênh Notification.
- Owner tạo Documents page → explicit Save/version → Published vẫn private → optional read-only share.
- Mọi module dùng owner isolation, Module Platform, Files/Trash/Audit/Search theo capabilities; không tự xây quyền hoặc secret store riêng.
- Finance là nguồn ledger; Vault là nguồn protected payload; Assets/Shopping/Career chỉ tham chiếu chúng.
- External providers và n8n có explicit data contract, current permission checks, retry/dedupe và trạng thái degraded.

## Ranh giới đã giữ

Personal-only, verified email rồi dùng ngay; không Workspace/team collaboration. Project terminal không mở lại; Event terminal không mở lại; Documents manual Save và immutable type/editor/parent/folder; notifications luôn cả ba kênh. Projects/Tasks import-export deferred, Calendar ICS included. Developer viết module; User/Admin không upload executable plugin. No-code builder/marketplace vẫn Deferred.

Chi tiết delegated có thể được điều chỉnh khi review mà không yêu cầu phỏng vấn từng click. Nếu thay một quyết định Approved hoặc mở rộng phạm vi/chi phí/privacy, phải ghi lại quyết định PO trước.

