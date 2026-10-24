# PR #4 — Exhaustive source-driven popup and nested-panel audit register

**Status:** SOURCE-INVENTORY-ONLY, interactive UI acceptance **NOT COMPLETE**. Code source `web/Nexora.Web/src/App.tsx` at branch `impl/m01-s00-scaffold` plus component list `web/Nexora.Web/src` (retrieval on 2026-10-19). The rows below enumerate **all JSX invocation occurrences** of five shared popup families in the monolithic App component. One JSX usage may be reached from several user states (source lifecycle, actor, editing, errors). Conversely component files may include additional dialogs or nested confirmations not captured by JSX searches. This register is an **enumerated test plan**, not a claim all instances rendered/verified.

Actual first nine-shard run [39214811832](https://github.com/hakodev2k/Nexora/actions/runs/39214811832) captured 3 distinct Finance desktop dialog screenshots and 1 Admin mobile dialog; it did not exercise the many other source usages. New exact-branch rerun [39216459096](https://github.com/hakodev2k/Nexora/actions/runs/39216459096) must populate runtime rows by evidence rather than by code search alone.

## Common dialog inspection contract

For every row: use synthetic correct-owner populated real SQL resource; click **only the open trigger**, not destructive confirm; screenshot **desktop1440/mobile390**, plus 320px and 768px for content overflow; assert one heading, accessible name/description, focus containment/ESC/focus return, cancel/dirty leave, well-labelled inputs/error/409/412 and no data exposure; call current action and source rights before any save. Archive/Trash/Purge/share revoke confirmations must never be auto-accepted for broad testing. Record `Pass/Fail/Blocked/NotRun` with artifact path, viewport, persona, and SHA for every dialog state. Absent rights is a legitimate tested gated state but **not** dialog acceptance.

## Shared JSX invocation register (60 occurrences)

| ID | Family | Enclosing screen/component | Source line | Title/trigger clue | Runtime dialog inspection |
|---|---|---|---|---|---|
| D-001 | `ActionDialog` | ConfirmActionButton | `App.tsx:757` | - | NOT_EXERCISED |
| D-002 | `ActionDialog` | PromptActionButton | `App.tsx:798` | - | NOT_EXERCISED |
| D-003 | `ConfirmActionButton` | SharingScreen | `App.tsx:1621` | Thu hồi link chia sẻ? | NOT_EXERCISED |
| D-004 | `ConfirmActionButton` | SupportScreen | `App.tsx:1695` | Thu hồi consent ${grantRecord.moduleCode}? | NOT_EXERCISED |
| D-005 | `ActionDialog` | FilesScreen | `App.tsx:1770` | Rời upload đang chuẩn bị? | NOT_EXERCISED |
| D-006 | `ConfirmActionButton` | NotificationsScreen | `App.tsx:1910` | Xóa ${selectedItems.length} mục khỏi Inbox? | NOT_EXERCISED |
| D-007 | `ResourceFormDialog` | NotificationsScreen | `App.tsx:1924` | Rời Inbox | NOT_EXERCISED |
| D-008 | `ActionDialog` | TrashScreen | `App.tsx:2010` | - | NOT_EXERCISED |
| D-009 | `ConfirmActionButton` | DocumentInlineTagCreator | `App.tsx:2140` | Dừng đối chiếu tạo Tag? | NOT_EXERCISED |
| D-010 | `ActionDialog` | ReauthenticateDialog | `App.tsx:2507` | - | NOT_EXERCISED |
| D-011 | `ActionDialog` | AdminAccessScreen | `App.tsx:2743` | - | NOT_EXERCISED |
| D-012 | `ActionDialog` | ModulePolicyScreen | `App.tsx:2867` | - | NOT_EXERCISED |
| D-013 | `ResourceFormDialog` | FavoritesScreen | `App.tsx:3228` | Thêm favorite | NOT_EXERCISED |
| D-014 | `ConfirmActionButton` | FavoritesScreen | `App.tsx:3234` | Xóa favorite? | NOT_EXERCISED |
| D-015 | `ConfirmActionButton` | FavoritesScreen | `App.tsx:3234` | Xóa favorite? | NOT_EXERCISED |
| D-016 | `PromptActionButton` | FavoritesScreen | `App.tsx:3234` | Xóa favorite? | NOT_EXERCISED |
| D-017 | `ResourceFormDialog` | ProductivityScreen | `App.tsx:3940` | Rời biểu mẫu hiện tại? | NOT_EXERCISED |
| D-018 | `ActionDialog` | ProductivityScreen | `App.tsx:3966` | - | NOT_EXERCISED |
| D-019 | `ConfirmActionButton` | ProductivityScreen | `App.tsx:3978` | Hoàn tất Project “${project.name}”? | NOT_EXERCISED |
| D-020 | `PromptActionButton` | ProductivityScreen | `App.tsx:3978` | Hoàn tất Project “${project.name}”? | NOT_EXERCISED |
| D-021 | `PromptActionButton` | ProductivityScreen | `App.tsx:3978` | Hoàn tất Project “${project.name}”? | NOT_EXERCISED |
| D-022 | `ConfirmActionButton` | ProductivityScreen | `App.tsx:3997` | Bắt đầu Task? | NOT_EXERCISED |
| D-023 | `ConfirmActionButton` | ProductivityScreen | `App.tsx:3998` | Hoàn thành Task? | NOT_EXERCISED |
| D-024 | `ConfirmActionButton` | ProductivityScreen | `App.tsx:3999` | Bỏ qua Task? | NOT_EXERCISED |
| D-025 | `PromptActionButton` | ProductivityScreen | `App.tsx:4001` | Chuyển Task về Chưa bắt đầu? | NOT_EXERCISED |
| D-026 | `PromptActionButton` | ProductivityScreen | `App.tsx:4002` | Sửa Acceptance Criteria | NOT_EXERCISED |
| D-027 | `PromptActionButton` | ProductivityScreen | `App.tsx:4003` | Sắp xếp Task trong cột | NOT_EXERCISED |
| D-028 | `ConfirmActionButton` | ProductivityScreen | `App.tsx:4005` | Xóa Task “${task.title}”? | NOT_EXERCISED |
| D-029 | `ConfirmActionButton` | ProductivityScreen | `App.tsx:4019` | Hủy sự kiện “${item.title}”? | NOT_EXERCISED |
| D-030 | `ConfirmActionButton` | RemindersScreen | `App.tsx:4253` | Gỡ reminder cho “${source.sourceTitle}”? | NOT_EXERCISED |
| D-031 | `ResourceFormDialog` | RemindersScreen | `App.tsx:4253` | Gỡ reminder cho “${source.sourceTitle}”? | NOT_EXERCISED |
| D-032 | `ResourceFormDialog` | BookmarksScreen | `App.tsx:4550` | - | NOT_EXERCISED |
| D-033 | `ConfirmActionButton` | BookmarksScreen | `App.tsx:4566` | Archive bookmark “${item.title}”? | NOT_EXERCISED |
| D-034 | `ConfirmActionButton` | SnippetsScreen | `App.tsx:4903` | Archive snippet “${item.title}”? | NOT_EXERCISED |
| D-035 | `ActionDialog` | SnippetsScreen | `App.tsx:4907` | Bỏ bản nháp Snippet? | NOT_EXERCISED |
| D-036 | `ResourceFormDialog` | ReadLaterScreen | `App.tsx:5098` | Thêm vào Read Later | NOT_EXERCISED |
| D-037 | `ConfirmActionButton` | ReadLaterScreen | `App.tsx:5117` | URL snapshot bất hoạt | NOT_EXERCISED |
| D-038 | `PromptActionButton` | ReadLaterScreen | `App.tsx:5117` | URL snapshot bất hoạt | NOT_EXERCISED |
| D-039 | `ConfirmActionButton` | OrganizationTagsScreen | `App.tsx:5349` | Dừng đối chiếu thao tác Tag? | NOT_EXERCISED |
| D-040 | `ConfirmActionButton` | OrganizationTagsScreen | `App.tsx:5354` | Dừng đối chiếu thao tác Tag? | NOT_EXERCISED |
| D-041 | `ResourceFormDialog` | OrganizationTagsScreen | `App.tsx:5354` | Dừng đối chiếu thao tác Tag? | NOT_EXERCISED |
| D-042 | `ConfirmActionButton` | OrganizationTagsScreen | `App.tsx:5367` | Xóa tag “${tag.name}”? | NOT_EXERCISED |
| D-043 | `ConfirmActionButton` | DeveloperToolsScreen | `App.tsx:5503` | Xóa input/output chưa lưu? | NOT_EXERCISED |
| D-044 | `ConfirmActionButton` | GoalsScreen | `App.tsx:5753` | Hoàn thành goal? | NOT_EXERCISED |
| D-045 | `ConfirmActionButton` | GoalsScreen | `App.tsx:5753` | Hoàn thành goal? | NOT_EXERCISED |
| D-046 | `ConfirmActionButton` | GoalsScreen | `App.tsx:5753` | Hoàn thành goal? | NOT_EXERCISED |
| D-047 | `ConfirmActionButton` | GoalsScreen | `App.tsx:5753` | Hoàn thành goal? | NOT_EXERCISED |
| D-048 | `ResourceFormDialog` | PlannerScreen | `App.tsx:5917` | Thêm pin | NOT_EXERCISED |
| D-049 | `ConfirmActionButton` | PlannerScreen | `App.tsx:5922` | Unpin Task? | NOT_EXERCISED |
| D-050 | `ConfirmActionButton` | HabitsScreen | `App.tsx:6091` | Archive Habit? | NOT_EXERCISED |
| D-051 | `ConfirmActionButton` | HabitsScreen | `App.tsx:6091` | Archive Habit? | NOT_EXERCISED |
| D-052 | `ResourceFormDialog` | HabitsScreen | `App.tsx:6091` | Archive Habit? | NOT_EXERCISED |
| D-053 | `ResourceFormDialog` | HabitsScreen | `App.tsx:6091` | Archive Habit? | NOT_EXERCISED |
| D-054 | `ConfirmActionButton` | FinanceScreen | `App.tsx:6368` | Dừng đối chiếu thao tác Finance? | NOT_EXERCISED |
| D-055 | `ResourceFormDialog` | FinanceScreen | `App.tsx:6368` | Dừng đối chiếu thao tác Finance? | NOT_EXERCISED |
| D-056 | `ConfirmActionButton` | FinanceScreen | `App.tsx:6373` | Xóa category “${category.title}”? | NOT_EXERCISED |
| D-057 | `ConfirmActionButton` | FinanceScreen | `App.tsx:6377` | Dừng đối chiếu thao tác Finance? | NOT_EXERCISED |
| D-058 | `ResourceFormDialog` | FinanceScreen | `App.tsx:6377` | Dừng đối chiếu thao tác Finance? | NOT_EXERCISED |
| D-059 | `ConfirmActionButton` | SecurityScreen | `App.tsx:6899` | - | NOT_EXERCISED |
| D-060 | `ConfirmActionButton` | SecurityScreen | `App.tsx:6900` | - | NOT_EXERCISED |

## Specialized component files (29)

| Component file | Screen/state test needed | Current disposition |
|---|---|---|
| `AutomationExecutionDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `AutomationLifecycleDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `AutomationPurgeDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `AutomationRunPanel.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `AutomationScheduleDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DashboardDrilldownDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DashboardLayoutEditor.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DocumentBodyEditor.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DocumentExportDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DocumentHistory.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DocumentImportPanel.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DocumentMarkdownPreview.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `DocumentTreeDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `FileMetadataDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `FileRenameDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `FileRestoreDialog.test.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `FileRestoreDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `FileTrashDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `GoalMetadataHistory.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `GoalProgressHistory.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `GoalStateHistory.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `GoalTargetDefinitionHistory.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `NewsArticleDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `ResourceFormDialog.test.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `ResourceFormDialog.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `ShareDisclosurePreview.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `ShareLinkEditor.test.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `ShareLinkEditor.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |
| `WorkLogHistory.tsx` | Instantiate eligible owner source, all create/edit/detail/empty/denied/dirty/error/retry variants; screenshot, keyboard and mobile | NOT_FULLY_EXERCISED |

## Additional gaps the source list does not resolve

- Plain inline create forms within Projects/Tasks/Goals/Habits/Calendar and popovers from library components may not use any of the five shared tags. Audit those through feature navigational route and action catalogs rather than pretending one dialog per React component.
- Native `alert/confirm/prompt` browser dialogs and nested underlay menus can be absent from JSX search; Playwright dialog listener plus human QA is required.
- Source counts are only an upper/lower inventory for **the listed JSX constructs**. No exhaustive action count or percentage of all product dialogs is claimed.
- Current shim captures at most two automatically discovered non-destructive dialogs per first-level route and one per module route. A real full-popup acceptance pass needs **targeted scenarios** for the remaining register rows and explicit negative actions. The PR remains Draft by owner decision D09=A.
