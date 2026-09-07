# Nexora — Action Catalog v1

2026-09-07 · Source baseline `78d8107c40cb1d78c007a14d5904126b097c4007` · **Documentation only; chưa được phép implement.**

Catalog có **714 operation contracts / 40 FX features**, mapping **197 UX Screen IDs**. Có 638 contracts được phân loại có thể cấp riêng cho Admin khi các gate liên quan đã đóng; **161 contracts đang Blocked**. Hai tập này có giao nhau: “grantable” không đồng nghĩa “ready”. Các action key là thiết kế Resolved delegated, không tuyên bố PO đã Approved mọi feature/proposal.

User được bật/tắt **module**; Admin được bật/tắt module **và action**. Không thêm bảng phân quyền từng action cho User. SuperAdmin là người đổi grants. Mọi quyền còn bị giới hạn bởi owner, lifecycle, scope và current module policy.

## Đọc theo thứ tự

1. [Context và effective permission](00-authorization-contract.md).
2. [Composition và protected-field diff](01-composition-and-field-guards.md).
3. [Disable / revoke / background work](02-revocation-and-runtime.md).
4. [Permission editor UX](03-permission-editor.md).
5. [SDK registration, small modules và evolution](04-module-action-contract.md).
6. [Acceptance scenarios](05-verification.md).
7. [Screen bindings](06-screen-bindings.md) và [consistency findings](07-review-and-open-gates.md).
8. [CSV review data](catalog.csv), không phải seed/migration/implementation.

## Catalog theo feature

| FX | Catalog | Operations | Admin-grantable | Blocked |
| --- | --- | ---: | ---: | ---: |
| FX-01 | [Identity / Profile](modules/01-identity.md) | 18 | 0 | 4 |
| FX-02 | [Users / Roles / Permissions](modules/02-access.md) | 9 | 2 | 0 |
| FX-03 | [Module Platform](modules/03-modules.md) | 10 | 2 | 0 |
| FX-04 | [Read-only Sharing](modules/04-sharing.md) | 6 | 4 | 0 |
| FX-05 | [Support / Emergency](modules/05-support.md) | 7 | 1 | 0 |
| FX-06 | [Notifications](modules/06-notifications.md) | 10 | 6 | 0 |
| FX-07 | [Files / Attachments](modules/07-files.md) | 14 | 13 | 0 |
| FX-08 | [Trash / Activity / Audit](modules/08-lifecycle.md) | 8 | 7 | 0 |
| FX-09 | [Settings / Shell](modules/09-settings.md) | 4 | 4 | 0 |
| FX-10 | [Import / Export / Backup](modules/10-transfer.md) | 13 | 8 | 6 |
| FX-11 | [Projects](modules/11-projects.md) | 13 | 13 | 0 |
| FX-12 | [Tasks](modules/12-tasks.md) | 18 | 18 | 0 |
| FX-13 | [Calendar](modules/13-calendar.md) | 13 | 13 | 0 |
| FX-14 | [Reminders / Scheduling](modules/14-reminders.md) | 6 | 3 | 0 |
| FX-15 | [Planner](modules/15-planner.md) | 7 | 7 | 0 |
| FX-16 | [Goals](modules/16-goals.md) | 22 | 22 | 0 |
| FX-17 | [Habits](modules/17-habits.md) | 16 | 16 | 0 |
| FX-18 | [Time Tracking](modules/18-time.md) | 13 | 13 | 0 |
| FX-19 | [Pomodoro / Focus](modules/19-focus.md) | 9 | 8 | 0 |
| FX-20 | [Documents](modules/20-documents.md) | 27 | 27 | 2 |
| FX-21 | [Bookmarks](modules/21-bookmarks.md) | 12 | 11 | 1 |
| FX-22 | [Snippets](modules/22-snippets.md) | 14 | 13 | 0 |
| FX-23 | [Read Later](modules/23-reading.md) | 7 | 7 | 0 |
| FX-24 | [Tags / Collections / Templates](modules/24-organization.md) | 23 | 23 | 1 |
| FX-25 | [Search / Favorites / Command Palette](modules/25-discovery.md) | 15 | 14 | 0 |
| FX-26 | [Dashboard](modules/26-dashboard.md) | 8 | 8 | 0 |
| FX-27 | [Finance](modules/27-finance.md) | 45 | 45 | 45 |
| FX-28 | [Vault](modules/28-vault.md) | 22 | 18 | 20 |
| FX-29 | [News / Feeds](modules/29-news.md) | 28 | 26 | 3 |
| FX-30 | [Price Tracking](modules/30-prices.md) | 18 | 16 | 18 |
| FX-31 | [Shopping Records](modules/31-shopping.md) | 36 | 36 | 2 |
| FX-32 | [Developer Toolbox](modules/32-toolbox.md) | 33 | 31 | 2 |
| FX-33 | [GitHub Discovery](modules/33-github.md) | 17 | 16 | 0 |
| FX-34 | [Automation](modules/34-automation.md) | 20 | 19 | 20 |
| FX-35 | [Integrations / Webhooks / n8n](modules/35-integrations.md) | 21 | 18 | 21 |
| FX-36 | [Monitoring / Job Operations](modules/36-monitoring.md) | 14 | 13 | 5 |
| FX-37 | [Personal Assets](modules/37-assets.md) | 29 | 29 | 2 |
| FX-38 | [Digital Assets](modules/38-digital.md) | 17 | 16 | 4 |
| FX-39 | [Career / Jobs / Resumes](modules/39-career.md) | 34 | 34 | 3 |
| FX-40 | [Learning / Work Log](modules/40-learning.md) | 58 | 58 | 2 |

## Không được hiểu nhầm

- 40 FX là feature scopes/logical namespaces, không bắt buộc40 assemblies/DbContexts hoặc40 entitlement độc lập. Binding mỗi key với installed ModuleId do trusted manifest khai báo; không tự split/merge quyền đã cấp.
- QUERY/COMMAND có server enforcement. COMPOSITE yêu cầu cả wrapper nếu grantable và action nguồn/đích; SYSTEM không là checkbox Admin. LOCAL pure tools chỉ enforce khả dụng trong Nexora, không thể ngăn một người dùng chạy thuật toán tương tự ngoài trình duyệt.
- Không wildcard `*` grant; không `access_all_user_data`. Public/share/support/system contexts không mượn Owner permission.
- Q-01…Q-12 giữ trạng thái tại [Decision log](../features/90-open-decisions.md); số lượng action không chứng minh implement-ready.

[Database binding](../design-database/16-action-catalog-binding.md) · [Architecture](../architecture/04-authorization-and-sensitive-data.md) · [UX/UI](../ux-ui/README.md)
