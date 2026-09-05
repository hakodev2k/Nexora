# Frontend and Design System Roadmap

**Status:** PLANNED; chưa scaffold React, cài package, viết component hoặc chạy frontend tests.
[Master RM07–RM16](00-master-implementation-roadmap.md) · [API contracts](04-backend-roadmap.md) · [Verification](08-testing-roadmap.md)

## 1. React architecture proposal

Vite + React + TypeScript là phương án gọn cho private application shell. React framework/build choice còn phải review ADR-R10; không tự thêm SSR, Next.js, Redux hoặc realtime collaboration.

| Layer/thư mục tương lai | Trách nhiệm |
|---|---|
| app | Bootstrap/providers/router, shell layouts, error boundaries, authentication context |
| shared/api | Typed API client, CSRF/credentials, ProblemDetails, ETag/idempotency, cancellation |
| shared/ui | Accessible primitives và design tokens; không chứa domain authorization logic |
| shared/forms | Reusable validation/error conventions, date/time controls |
| modules/{moduleId} | Routes, feature forms/views, API hooks, permission contributions và tests |
| modules/documents/editor | Block/Markdown adapters, dirty-state, schema-versioned serialization, safe preview |
| modules/calendar | Calendar views/adapters; separate ManualEvent edit và Task readonly projection |
| tests | Component/feature fixtures và critical E2E theo approved runner |

Server state fetch/caching không trộn với form draft. UI state ở component/router; auth/profile/module catalog qua small contexts. Query caching library chỉ thêm khi stale/refetch/retry complexity có use case; mọi query key owner/context-scoped. Logout/revoke/switch support context phải clear private query cache.

Không cần space switcher; mỗi account có dữ liệu cá nhân. Module navigation lấy từ registry/effective enablement, không hiển thị đồng loạt mọi route/module như một list dài chưa được review.

## 2. Routing, API và state handling

| Concern | Planned behavior |
|---|---|
| Auth routes | Register, verify result/resend, login, recovery, session expiry; generic error không enumerate |
| Self routes | Chỉ current User; direct navigation rechecks server |
| Share routes | Dedicated readonly layout; mode requires login/allowlist theo link; không owner controls/history/internal IDs |
| Support routes | Explicit target User/module/expiry banner; readonly; close session không revoke User grant |
| Emergency routes | SuperAdmin reason form trước session; audited readonly view; no hidden impersonation |
| Admin routes | Module/action matrix + diagnostics; grant không tự mở private User data |
| API client | credentials/CSRF, typed errors/correlation, request cancel, timeout/retry theo method; no blind retry mutation without idempotency |
| Concurrency | If-Match/current version, conflict response; keep unsaved content cho User review/reload/retry |
| Dirty state | Navigation/back/session-expiry warning; không autosave Document content; không hứa browser crash recovery nếu chưa có requirement |
| UI validation | Server errors map field/form; timezone and same-owner options; read-only fields still server-enforced |
| Forbidden/stale source | Safe unavailable screen; no previous private payload flicker from reused cache |
| Browser Push | Request browser permission bằng phù hợp browser flow; denied/unsupported status truthful; không có User notification channel preference |

Không expose implementation details trên user flow: User cần biết status/quyền/lỗi và cách xử lý, không cần thấy DbContext, cache key hoặc internal transaction names.

## 3. Shared component inventory

Xây trước feature-specific UI tại RM07; bổ sung khi có approved use case. Không coi danh sách control là các business feature mới.

| Components | Trách nhiệm và validation |
|---|---|
| Button/Input/Textarea/Select | Labels, disabled/pending, required/errors, keyboard; destructive semantics rõ |
| Modal/ConfirmationDialog | Focus trap/return focus, cancel, duplicate submit; reason/warning confirmation khi requirement cần |
| Dropdown/Menu | Keyboard và accessible actions; không hover-only |
| DatePicker/DateTime/Timezone | UTC/date-only mapping, Start/End validation, DST ambiguity UI sau decision |
| TagPicker/InlineTagCreate | Cấu hình single Documents Tag hoặc multiple Productivity Tags; catalog owner/module scoped |
| Badge/Status/Priority | Text + visual, không dựa màu; P0 cao nhất/P3 thấp nhất |
| Card/Grid/Table | Responsive, deterministic list state, exact module column configuration |
| Pagination/SearchInput/FilterPanel | Bounded request; query cancellation; correct filter semantics, empty/no results |
| FileUpload/ImageCrop/VisualPicker | File validation feedback, staging/cancel/progress; crop preview and valid region; Icon/Cover exclusive |
| Toast/InlineAlert | Outcome/error announcements; critical control không chỉ toast tự biến mất |
| Loading/Empty/Error/Denied/Degraded | Clear next action, safe correlation reference; preserve source data correctly |
| VersionPicker/ConflictDialog | Readable revision/time, safe preview; restore creates new revision; no silent overwrite |
| Kanban interaction | Keyboard alternative cho drag/reorder; state transition reason và rollback |
| Chart + table equivalent | Source/time/currency label, accessible equivalent, no invented data |

A11y target version/browsers/breakpoints cần approved profile; desktop/mobile bắt buộc, tablet usable. Không khóa numerical WCAG/performance targets từ proposal như thể User đã ký.

## 4. Documents UX specification giữ nguyên

### Library, listing và navigation

- Menu chỉ **Documents**; Note/Knowledge là DocumentType.
- Entry list: Folder cấp 1 + root page ngoài Folder; không flatten toàn bộ tree; exclude Archived/Trash.
- Grid mặc định; Table alternative. Card và Table chỉ business display fields **Title, DocumentType, Tag**. Action controls vẫn cần để dùng chức năng, nhưng không thêm cover/Icon/Status/date/parent/path như cột nội dung mặc định.
- Filters: DocumentType, Tag, created-date range. Search: Title/Tag; không body, không thêm Status/updated-date filter.
- Default sort UpdatedAt DESC với stable tie; đổi Grid/Table giữ cùng query/scope.
- Open parent: child navigation qua page sidebar. Đây không phải Tree View của library và không mở quyền đọc children qua share ngầm.
- Archived là mục riêng; không trộn Trash. Archived readonly và Unarchive về previous Draft/Published.
- Folder-open direct/recursive contents, current-folder/global search scope, Folder cards/sorting với page mix và Archived hierarchy cần decision; thể hiện blocker UI specification, không bịa default.

### Create và editors

1. Create full form bắt buộc User chọn DocumentType và EditorMode **mỗi lần**, không remembered/preselected value.
2. Title bắt buộc, body optional; Title trùng mọi nơi hợp lệ.
3. Root có optional Folder ở create; child chọn parent và không chọn Folder riêng. Sau create không đổi Folder/Parent/type/editor.
4. Optional một Tag, tạo Tag ngay trong form; optional Emoji/builtin Icon **hoặc** uploaded cover.
5. Editor mở theo selected mode: Block hoặc Markdown. Không conversion/switch editor.
6. User bấm Save; distinct successful Save tạo version. Retry request cũ không tạo duplicate version. Không background autosave.
7. Dirty content + failed request/expired session/conflict phải được xử lý rõ. Block types/formatting/tables/links/embedding/paste/Markdown extensions chưa clear; không suy toàn bộ Google Docs feature set.
8. Body render/sanitization policy được test ở owner editor, preview, shared view và search highlights.

### Cover image

Upload → preview → crop/select visible area → manual Save. Image bytes/metadata schema qua File Service, no external URLs. Cancel không đổi current cover. Replace image tạo new immutable file reference; old versions giữ file/crop cũ. Archived/shared/support/emergency view không crop hoặc replace. Formats/size/dimensions là DEC-KNW-032; không đặt mặc định tùy ý.

### State, sharing và versions

| UI action | Behavior |
|---|---|
| Publish Draft | Vẫn private; share chỉ khi owner tạo link và module policy cho phép |
| Published Save | Cho sửa/Save; live share đọc saved version mới nhất |
| Published → Draft | Suspend link còn tồn tại; token giữ; publish lại chỉ active nếu chưa expired/revoked |
| Archive Draft/Published | Readonly + previous state; chuyển mục Archived; không tạo share mới |
| Archive from Published | Active valid link vẫn read-only; không revive suspended/expired/revoked |
| Unarchive | Previous Draft/Published; child cascade/navigation còn Open |
| Restore old version | New current revision + giữ full history; immutable fields không đổi |
| Delete parent/Folder | Warning/confirm theo policy; whole tree Trash; không orphan |
| Delete child riêng | Explicit warning; parent/siblings giữ nguyên |
| Restore child riêng | Parent active trước; parent Trash/purged chặn |
| Delete used Tag | Chặn và báo lý do; không tự gỡ Tag/page; Trash/history reference policy còn Open |

Version diff/metadata save boundaries/change note/Title-edit behavior phải follow source approved scope và remaining refinement; không tự thêm side effect autosave.

## 5. Productivity và Calendar surfaces

| Surface | Confirmed UX |
|---|---|
| Projects | Grid mặc định, Table; Title/Start/End; Tag/time/status filters; Title search; A–Z |
| Project detail | Kanban mặc định, Table alternative; no workspace/members/assignment/comments |
| Task create | Full form kể cả “quick-create”; bắt buộc Project/Title/Start/End; create ở NotStarted/InProgress |
| Kanban card | Title/Priority/Start/End/Overdue; drag state, reorder same column, open detail |
| Task Table | Title/Status/Priority/Start/End; Status/time filters; Title/Tag search |
| Task transition | Forward theo source matrix; backward reason; canceled/failed move rollback |
| Project complete | All Tasks terminal chỉ prompt; còn open Tasks warning + reason; terminal không reopen/modify/create Task |
| Calendar | Day default; Month/Week/Day/Agenda; status/time filters; Title/Project Title search |
| Manual Event | Full form Title/Description/Start/End, optional All-day/one Reminder; overlap warn; no drag/resize |
| Task Event | Readonly current Task projection; navigate Task detail; không Calendar edit |
| Terminal/past Event | Canceled struck through; Completed/Canceled readonly/no reopen; past Scheduled bình thường |
| ICS | Import per-entry summary; export source/status + all/custom fully-contained range; no Reminder |
| Reminders | Exact time hoặc 15m before Start; delivery all3; no channel picker/quiet hours |
| Notification Center | Open source; read/unread/all-read; single/bulk delete; retained until User delete |

Task terminal still editable khi Project active; Project terminal override mọi Task edit. Trash/history flows hiển thị domain reason, không lỗi generic khó hiểu. Task/Project import/export controls absent; Calendar ICS controls hiện riêng.

## 6. UI coverage cho catalog còn lại

| Phase | Surfaces phải được refine và hoàn thành |
|---|---|
| RM10 | Planner, Goals, Habits, Time Tracking, Pomodoro; timer/progress/cadence UI sau decisions |
| RM09 utilities | Files/preview/download, Bookmarks/Snippets, unified Read Later, Collections/Templates |
| RM11 | Global/saved Search, Favorites/Recent/History, Command Palette, Dashboard/widget customization approved |
| RM12 | Finance account/transaction/bill/payment/subscription/budget/reports + approved P1; Vault masked list/detail/reveal/copy/trash/version |
| RM13 | Feed source/status/read/saved/topic watch; product/variant/current-history/alert + wishlist/comparison/order/purchase/seller/warranty |
| RM14 | Utilities input/output/privacy/limits; GitHub ranks/detail/filter/cache/history; Automation editor/run logs/failures/webhooks/n8n settings |
| RM15 | Assets/purchase/warranty, Digital assets/licenses/expiry, Jobs/Companies/Interviews/Resumes/Skills/Courses/Certifications/Work Logs |
| RM16 | Cross-module deep links, disabled contributions, files/shares/support contexts, account lifecycle reconciliation |

Chưa wireframe state/field chưa Approved như committed behavior. Empty/error/degraded/read-only/permission/retry states và mobile/a11y có trong mỗi feature specification, không chỉ happy path.

## 7. Frontend exit gates

API/UI parity cho required fields/states/immutable fields; ETag and retry behavior; permission/module/context direct-route tests; no Secret in browser persistence/telemetry; component keyboard/focus/error tests; critical E2E; responsive desktop/mobile and tablet smoke. Build/lint/test commands chỉ pin và kiểm chứng khi implementation; kế hoạch này không tuyên bố tests đã chạy.
