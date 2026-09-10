# Nexora — Feature Specifications

> Current specification · reconciled 2026-09-09. [Previous version](../history/20260908/snapshot/docs/features/README.md) is historical evidence, not implementation input.

Ngày: **2026-09-09** · Baseline đọc: [d0d8418](https://github.com/hakodev2k/Nexora/commit/d0d84181e0043f9ffa38b475cbe461d34449805e) · **Feature behavior docs are not runtime evidence. M01 + scaffold only is approved for implementation by `DEC-20260909-001`.**

Bộ này phân tích **40 ranh giới feature/capability** cho toàn bộ module catalog hiện tại; không có nghĩa website cần40 menu hay40 plugin độc lập. Mỗi đặc tả có sản phẩm tham chiếu, phần áp dụng/điều chỉnh cho Nexora, luồng màn hình, dữ liệu/validation, lifecycle, commands/integrations, acceptance scenarios và source requirement mapping.

Yêu cầu User đã chốt được giữ nguyên. PM/Technical tự giải quyết chi tiết thông thường theo DEC-GOV-001. Những vấn đề ảnh hưởng phạm vi lớn, dữ liệu nhạy cảm, tiền, chi phí và irreversible loss được gom thành decision groups. Sau quyết định PO ngày 2026-09-09, một số policy đã Approved, nhưng implementation vẫn chỉ approved cho M01 + scaffold; các module/slice khác cần bounded approval riêng.

## Cách đọc

1. [Hợp đồng hành vi chung](00-shared-behavior.md): quyền, lifecycle, UX/API/error/concurrency và Definition of Ready.
2. Feature tương ứng trong bảng dưới và [state/action matrices](94-state-and-action-matrices.md).
3. [Coverage và quyết định](92-coverage-and-decisions.md), [routing requirement IDs](93-requirement-routing.md).
4. [Nguồn tham chiếu](91-reference-register.md), [decision status](90-open-decisions.md) và [PO readiness decisions](../requirements/11-owner-decisions-20260909-implementation-readiness.md).
5. [Requirements](../requirements/01-scope-and-module-catalog.md) và [implementation roadmap](../roadmap/00-master-implementation-roadmap.md) tiếp tục là nguồn scope/technical gates.

## Feature index

| ID | Feature/capability | Sản phẩm tham chiếu | Major decisions |
|---|---|---|---|
| FX-01 | [Identity, Registration và Profile](01-identity-and-profile.md) | Auth0 | Q-01 resolved no email reuse/same-account restore; Q-02 TOTP recovery-code policy resolved; M01 subset approved |
| FX-02 | [Users, Roles và Action Permissions](02-users-roles-and-permissions.md) | WordPress | M01 subset approved; common gates apply |
| FX-03 | [Module Platform và Module Manager](03-module-platform.md) | WordPress | M01 subset approved; common gates apply |
| FX-04 | [Read-only Sharing Engine](04-read-only-sharing.md) | Google Drive | Q-03 sensitive projection policy resolved; field contracts still required |
| FX-05 | [Support, Emergency Access và Security Center](05-support-emergency-and-security-center.md) | Microsoft Customer Lockbox, Bitwarden | Q-03/Q-04 safe support/no plaintext operator policy resolved |
| FX-06 | [Notification Center và Delivery](06-notification-center.md) | GitHub Notifications | M01 delivery foundation subset approved; full UI outside M01 |
| FX-07 | [Files, Uploads và Attachments](07-files-and-attachments.md) | Google Drive, Google Drive | Not in M01; common gates apply |
| FX-08 | [Trash, Activity và Audit](08-trash-activity-and-audit.md) | Google Drive, Microsoft Customer Lockbox | M01 audit subset approved; account no-purge policy resolved |
| FX-09 | [Settings và Application Shell](09-settings-and-app-shell.md) | Notion Sidebar, WordPress | M01 profile/language subset approved; currency explicit when money fields exist |
| FX-10 | [Import, Export và Backup/Restore](10-import-export-and-backup.md) | Google Calendar, GitLab | Q11 resolved DOCX/MD; Vault portability policy resolved but crypto contracts gated |
| FX-11 | [Projects](11-projects.md) | Microsoft To Do, TickTick | Not in M01; common gates apply |
| FX-12 | [Tasks, Kanban và Table](12-tasks.md) | Microsoft To Do, TickTick | Q-10 initial flat Task scope resolved; extensions gated |
| FX-13 | [Calendar, Personal Events và ICS](13-calendar.md) | Google Calendar | Not in M01; common gates apply |
| FX-14 | [Reminders và Due Scheduling](14-reminders-and-scheduling.md) | TickTick | One task/event reminder core; standalone/snooze extensions gated |
| FX-15 | [Daily và Weekly Planner](15-planner.md) | Microsoft To Do, TickTick | Not in M01; common gates apply |
| FX-16 | [Goals và Targets](16-goals.md) | ClickUp Goals | Not in M01; common gates apply |
| FX-17 | [Habit Tracker](17-habits.md) | TickTick | Not in M01; common gates apply |
| FX-18 | [Time Tracking](18-time-tracking.md) | Toggl Track | Not in M01; common gates apply |
| FX-19 | [Pomodoro và Focus](19-pomodoro.md) | TickTick Focus, TickTick | Not in M01; common gates apply |
| FX-20 | [Documents, Note và Knowledge Pages](20-documents.md) | Google Docs, Notion | Q11 resolved DOCX/MD; not in M01 |
| FX-21 | [Bookmarks](21-bookmarks.md) | Raindrop.io | Not in M01; common gates apply |
| FX-22 | [Code Snippets](22-snippets.md) | GitHub Gists, DevToys | Not in M01; common gates apply |
| FX-23 | [Read Later](23-read-later.md) | Instapaper | Bookmark-reference slice implemented locally; News/body reader and common gates remain |
| FX-24 | [Tags, Collections và Templates](24-organization-and-templates.md) | Notion Templates, Raindrop.io | FX24-S01 Tag catalog implemented locally; assignment/collections/templates remain gated |
| FX-25 | [Search, Saved Search, Favorites và Command Palette](25-search-favorites-and-command-palette.md) | Notion Search, Raindrop.io Search, Notion Sidebar | Not in M01; common gates apply |
| FX-26 | [Dashboard và Widgets](26-dashboard.md) | ClickUp Dashboards | Not in M01; common gates apply |
| FX-27 | [Personal Finance](27-finance.md) | Actual Budget | Initial basic manual records approved; advanced Finance gated; sensitive projection policy applies |
| FX-28 | [Vault](28-vault.md) | Bitwarden | Hybrid/no-operator-plaintext policy approved; crypto/recovery implementation gated |
| FX-29 | [News, RSS và Topic Watch](29-news-and-feeds.md) | Feedly | Read-only public outbound boundary approved after network guard contract; not in M01 |
| FX-30 | [Shopee Price Tracking](30-shopee-price-tracking.md) | camelcamelcamel | Paused; not moved to R2; no worker/auto-enable |
| FX-31 | [Wishlist, Comparison, Orders, Sellers và Warranty](31-shopping-records.md) | AnyList | Sensitive projection policy applies where needed; not in M01 |
| FX-32 | [Developer Toolbox](32-developer-toolbox.md) | DevToys | Local tools only unless network capability gets slice contract; not in M01 |
| FX-33 | [GitHub Discovery](33-github-discovery.md) | GitHub Search API | Read-only public outbound boundary approved after network guard contract; not in M01 |
| FX-34 | [Automation, Scheduler và Workflows](34-automation.md) | n8n | Paused; not moved to R2; core platform jobs distinct |
| FX-35 | [Integrations, Webhooks và n8n](35-integrations-webhooks-and-n8n.md) | n8n | Paused; not moved to R2 |
| FX-36 | [Monitoring và Job Operations](36-monitoring-and-job-operations.md) | UptimeRobot | Owner-configured HTTP probing allowed after guard contract; not in M01 |
| FX-37 | [Personal Assets, Inventory và Devices](37-personal-assets.md) | Snipe-IT | Sensitive projection policy applies; not in M01 |
| FX-38 | [Domains, Hosting, VPS, Certificates, Licenses và Services](38-digital-assets.md) | Cloudflare Registrar, Snipe-IT | Provider inspection under outbound/provider gates; sensitive projection policy applies |
| FX-39 | [Career, Companies, Interviews và Resumes](39-career-and-resumes.md) | Teal Job Tracker | Q11 resolved DOCX/MD; Q-12 internal Calendar workflow resolved; sensitive projection policy applies |
| FX-40 | [Skills, Courses, Certifications, Learning Plan và Work Log](40-learning-and-work-log.md) | Moodle, Toggl Track | Sensitive projection policy applies; not in M01 |

## Bản đồ nghiệp vụ chính

- Owner tạo Project → tạo Task → Task projection lên Calendar → một Reminder → ba kênh Notification.
- Owner tạo Documents page → explicit Save/version → Published vẫn private → optional read-only share.
- Mọi module dùng owner isolation, Module Platform, Files/Trash/Audit/Search theo capabilities; không tự xây quyền hoặc secret store riêng.
- Finance initial core là manual record có explicit currency; advanced ledger/accounting chờ quyết định riêng.
- Vault là nguồn protected payload; hybrid recovery không cho operator plaintext.
- External providers và n8n có explicit data contract, current permission checks, retry/dedupe và trạng thái degraded; FX34/35 vẫn Paused.

## Ranh giới đã giữ

Personal-only, verified email rồi dùng ngay; không Workspace/team collaboration. Project terminal không mở lại; Event terminal không mở lại; Documents manual Save và immutable type/editor/parent/folder; notifications luôn cả ba kênh. Projects/Tasks import-export deferred, Calendar ICS included. Developer viết module; User/Admin không upload executable plugin. No-code builder/marketplace vẫn Deferred. Price Tracking, Automation và Integrations vẫn Paused.

Chi tiết delegated có thể được điều chỉnh khi review mà không yêu cầu phỏng vấn từng click. Nếu thay một quyết định Approved hoặc mở rộng phạm vi/chi phí/privacy, phải ghi lại quyết định PO trước.
