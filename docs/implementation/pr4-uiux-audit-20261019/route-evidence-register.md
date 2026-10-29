# PR #4 UI/UX — Historic 36-route per-screen evidence and fresh shard handoff

**Status: HISTORICAL ROUTE OBSERVATIONS ONLY — NOT PROFESSIONAL VISUAL ACCEPTANCE.** Baseline source `qa/runs/QA-20261018T140148Z-acc869/attempt1/visual-results.json` (older, distinct local tree): 180 observations = 36 routes × viewports 320/390/768/1366/1920. **Two** viewport overflow observations on Home only: 320px scrollWidth324; 768px scrollWidth834. No other route overflow in *this five-viewport, single selected-state* baseline. Many routes showed `Module chưa khả dụng` under the selected ordinary-user fixture, so their intended features were **NOT** exercised. This is not same-head visual proof and does not establish missing UX, a11y, dialog or functional correctness.

New matrix reviewer: [`pr4-uiux-sharded.yml`](../../../.github/workflows/pr4-uiux-sharded.yml). Audit source E2E lives at `web/Nexora.Web/e2e/pr4-uiux-audit.spec.ts`. Review data per exact run SHA, synthetic persona and viewport before changing statuses. Link to [review rubric](README.md), [PR4 product gap inventory](../pr4-feature-gap-review-20261019.md). No production/real provider execution; PR remains Draft.

| Route | Previous H1 or disabled message | 5-viewport horizontal reflow | Owner shard | Next states/dialogs to verify | Current-head final visual decision |
|---|---|---|---|---|---|
| `/` | Xin chào, QA UserA | **FAIL historic 320px,768px** | navigation | Home: verify one Quick Create, load/refresh/personalization, module discovery and density | **NOT_REVIEWED** |
| `/admin/access` | Không có quyền truy cập quản trị | No whole-page overflow in prior snapshot | admin | Verify SuperAdmin permission table, edits, revoke/deny, confirmation and last-admin | **NOT_REVIEWED** |
| `/admin/modules` | Không có quyền truy cập quản trị | No whole-page overflow in prior snapshot | admin | Verify server policy, disabled state, selection/confirmation and no hidden rights | **NOT_REVIEWED** |
| `/bookmarks` | Bookmarks | No whole-page overflow in prior snapshot | knowledge | Seed 2+ links: create/edit/filter/archive/open dialog, keep-on-ACK, empty/loading | **NOT_REVIEWED** |
| `/command-palette` | No heading captured | No whole-page overflow in prior snapshot | navigation | Keyboard Ctrl/Cmd+K, search/filter, focus restore and command execution | **NOT_REVIEWED** |
| `/developer/tools` | Developer tools | No whole-page overflow in prior snapshot | business / gated | Each toolbox tab, input validation/clear/copy/history and failure state | **NOT_REVIEWED** |
| `/favorites` | Favorites | No whole-page overflow in prior snapshot | navigation | Populated list, source revoked, remove, pagination/filter and direct open | **NOT_REVIEWED** |
| `/files` | Module chưa khả dụng | No whole-page overflow in prior snapshot | knowledge | Authorized clean/quarantine/upload/metadata/rename/trash/restore and protected download | **NOT_REVIEWED** |
| `/finance` | Finance records | No whole-page overflow in prior snapshot | business / gated | Category+record dialogs, blank currency, exact decimal/date, filters and edit conflict | **NOT_REVIEWED** |
| `/goals` | Goals | No whole-page overflow in prior snapshot | productivity | Numeric/Boolean targets, progress/history dialogs and source link denied | **NOT_REVIEWED** |
| `/habits` | Habit tracker | No whole-page overflow in prior snapshot | productivity | Schedule weekly picker, check-in details, positive count, archive/restore | **NOT_REVIEWED** |
| `/learning/work-log` | Module chưa khả dụng | No whole-page overflow in prior snapshot | knowledge | Entry forms, times, filter/history/dialog, archive/Trash/recover | **NOT_REVIEWED** |
| `/modules/FX11` | Projects | No whole-page overflow in prior snapshot | productivity | Project create/edit/status/Trash and Tags selector (no raw JSON) | **NOT_REVIEWED** |
| `/modules/FX12` | Tasks | No whole-page overflow in prior snapshot | productivity | Task linked to Project, multiple controls, state/Reminders, type-safe picker | **NOT_REVIEWED** |
| `/modules/FX13` | Calendar | No whole-page overflow in prior snapshot | productivity | Event create/edit/ICS/calendar/day/week/timezone, recurrence gated | **NOT_REVIEWED** |
| `/modules/FX18` | Time Tracking | No whole-page overflow in prior snapshot | productivity | Timer/manual records, overlap, running state, stop/restart, report | **NOT_REVIEWED** |
| `/modules/FX19` | Focus | No whole-page overflow in prior snapshot | productivity | Focus setup/clock phase/break, complete/session record | **NOT_REVIEWED** |
| `/modules/FX20` | Documents | No whole-page overflow in prior snapshot | knowledge | Page tree/markdown/editor/version/history/tag dialogs, import/export | **NOT_REVIEWED** |
| `/modules/FX29` | Module chưa khả dụng | No whole-page overflow in prior snapshot | knowledge | News source/list/article detail/read, provider simulated/unavailable state | **NOT_REVIEWED** |
| `/modules/FX31` | Module chưa khả dụng | No whole-page overflow in prior snapshot | business / gated | Wishlist items/list/filter/order comparison, optional provider gated | **NOT_REVIEWED** |
| `/modules/FX33` | Module chưa khả dụng | No whole-page overflow in prior snapshot | business / gated | GitHub discovery: inspect missing/gated source-only behavior | **NOT_REVIEWED** |
| `/modules/FX39` | Module chưa khả dụng | No whole-page overflow in prior snapshot | business / gated | Career forms, Resume, Applications, appointments, pinned versions | **NOT_REVIEWED** |
| `/modules/FX40` | Module chưa khả dụng | No whole-page overflow in prior snapshot | business / gated | Skills/Courses/Learning Plan/Work Log and history, source guards | **NOT_REVIEWED** |
| `/notifications` | Notifications | No whole-page overflow in prior snapshot | navigation | Empty/read/unread/batch/source-open/revoked/ACK, pagination | **NOT_REVIEWED** |
| `/organize/tags` | Tag management | No whole-page overflow in prior snapshot | knowledge | Namespace select, add/edit/delete/tag replay and Collections missing | **NOT_REVIEWED** |
| `/planner` | Daily & weekly planner | No whole-page overflow in prior snapshot | productivity | Daily/weekly select, task/calendar pin/unpin/reorder, source revoked | **NOT_REVIEWED** |
| `/read-later` | Read Later | No whole-page overflow in prior snapshot | knowledge | Bookmarks/news source, position, later queue, denied states | **NOT_REVIEWED** |
| `/recent` | Đã mở gần đây | No whole-page overflow in prior snapshot | navigation | Recent items, source resolution, remove and read rights | **NOT_REVIEWED** |
| `/search` | Global Search | No whole-page overflow in prior snapshot | navigation | Global search filter/facet/query/loading/empty/permission result | **NOT_REVIEWED** |
| `/search/saved` | Saved Searches | No whole-page overflow in prior snapshot | navigation | Save/edit/delete search, parameters, filters, exact reconstruction | **NOT_REVIEWED** |
| `/settings/profile` | Profile | No whole-page overflow in prior snapshot | navigation | Name/locale/timezone/theme/edit/stale 412 and save/dirty back | **NOT_REVIEWED** |
| `/settings/security` | Security & sessions | No whole-page overflow in prior snapshot | navigation | Sessions, revoke/confirmation, MFA gates, responsive table | **NOT_REVIEWED** |
| `/sharing` | Module chưa khả dụng | No whole-page overflow in prior snapshot | knowledge | Link create/edit/revoke/audience/expiry/preview, token-null revoked metadata | **NOT_REVIEWED** |
| `/snippets` | Code snippets | No whole-page overflow in prior snapshot | knowledge | Create/edit/version/history/code wrapping/copy, languages and mobile | **NOT_REVIEWED** |
| `/support` | Module chưa khả dụng | No whole-page overflow in prior snapshot | business / gated | Consent/support request/emergency-only metadata, no secret field | **NOT_REVIEWED** |
| `/trash` | Trash | No whole-page overflow in prior snapshot | navigation | Archive/Trash/restore/purge confirmation source impact and retention | **NOT_REVIEWED** |

## Popup families needing **individual** interactive capture

Each popup below requires screenshots at 320/390/768/1440 (or clear justified N/A), keyboard/focus trap/restore, validation/dirty cancel/Save or prohibited effect guard, empty/populated/auth-denied source, permission/race response; presence of a component is not completion.

| Family | Source components | Must inspect | Current status |
|---|---|---|---|
| Account and security | Login, Register, Verify, Forgot, Reset, session revoke | Form validity, helper text, errors, keyboard and role redirects | PRIOR SCREENSHOTS PARTIAL; current pending |
| Global/command | CommandPalette, navigation/more menu | Keyboard shortcut, result authority, close/focus, mobile header | HISTORIC SCREENSHOT; current pending |
| Resources | ResourceFormDialog, ActionDialog, ConfirmActionButton, PromptActionButton | Every create/edit/archive/Trash/revoke/change-state flow | NOT_FULLY_EXERCISED |
| Productivity/Goals | GoalTargetManager + Goal progress/metadata/state/target histories | Numeric vs Boolean, source/missing, every history modal | NOT_FULLY_EXERCISED |
| Document | DocumentTreeDialog, DocumentImportPanel, DocumentExportDialog, DocumentHistory, BodyEditor | Tree navigation, compare/export, import preview/version cancel | NOT_FULLY_EXERCISED |
| Files | FileMetadataDialog, FileRenameDialog, FileTrashDialog, FileRestoreDialog | Quarantine/status, retention, protected download | NOT_FULLY_EXERCISED |
| Sharing | ShareLinkEditor, ShareDisclosurePreview, CreatedShareLink | Per-field allowlist, token one-time copy, revoke/expiry | NOT_FULLY_EXERCISED |
| Dashboard | DashboardLayoutEditor, DashboardDrilldownDialog, DashboardQuickCreate | Duplicate CTAs, widget registry, stale layout/drag/reflow | HISTORIC FAIL, current pending |
| Automation | AutomationExecutionDialog, Lifecycle, Schedule, ProjectPicker, Purge, RunPanel | Preview only; **never activate real provider or destructive action** | NOT_FULLY_EXERCISED |
| Learning/Career/News | SkillEvidence, WorkLogHistory, CareerAppointments, NewsArticleDialog | Media/layout and source unavailability/permission | NOT_FULLY_EXERCISED |

## Historic screenshot review (six synthetic images from browser artifact, NOT new release evidence)

| Screenshot | UI judgement | UX defect with real visual evidence | Severity |
|---|---|---|---|
| `playwright-output/m01-identity-*/home.png` desktop/mobile | Strong legible typography and reusable rounded controls, but very lengthy vertical dashboard and raw module catalog reduce polish | Avatar/profile long generated ID wraps large HOME welcome heading; multiple “Tạo mới” blocks and mixed technical module terminology; loader present at capture | P1 |
| `playwright-output/ui-smoke-public-*/public-validation.png` desktop/mobile | Registration uses consistent centered card, clear large CTA, red invalid field and explicit error alert | Technical `Timezone IANA` should be searchable local time zone with human readable labels; error banner detached from input may slow correction | P2 |
| `playwright-output/ui-smoke-authenticated-*/committed-ack.png` desktop/mobile | Color/spacing/readability and privacy-conscious opaque ACK are positive | Mobile command palette button wraps into three text lines; email wraps each syllable, logout takes prime viewport; `Provider boundary` is backend jargon; lengthy metadata explanations dominate bookmark first screen | P1 mobile header / P2 copy |

**Visual score discipline:** Do not infer 2/2 “professional” from presence of a screenshot. Every route/pop-up must receive a human visual score (hierarchy, consistency, efficiency, copy, responsive, keyboard, failure recovery) and evidence before release signoff. Sampled axe checks are only automated indicators. Current snapshots after the observer hotfix are pending until all nine independent area artifacts are read.
