# Phase 2 — Productivity

**Phase ID:** `NX-PH-02`  
**Outcome:** User có thể lập kế hoạch và theo dõi công việc cá nhân bằng Tasks, Projects, Calendar/Events và Reminders trên desktop/mobile.  
**Depends on:** Phase 1 identity, ownership, audit, trash, file, notification và scheduler contracts.

## 1. Proposed scope split

### P0 (`PROPOSED`, cần `DEC-PRD-001..003`)

- Tasks, subtasks, status, priority, due date, tags, attachments, checklist.
- Projects với task grouping cơ bản.
- Calendar và Events gồm all-day/timed event.
- Reminders độc lập và reminder gắn Task/Event.
- Recurring Tasks/Events mức đã duyệt.
- Today/Upcoming/Overdue views và daily planner dạng derived view.

### P1

- Goals, Habits, recurring activity nâng cao.
- Time Tracking và Pomodoro.
- Weekly Planner, templates/import/export, project progress summaries.

### Deferred/out

Shared team project, task assignment cho user khác, comments/chat, real-time collaboration, external calendar sync, event invitation/attendee, resource booking. Các hạng mục này cần collaboration model mới, không được suy ra từ read-only Sharing Engine.

## 2. Primary user journeys

1. Tạo Task nhanh → đặt due/priority/reminder → xem Today → hoàn thành.
2. Tạo Project → thêm/di chuyển Tasks → theo dõi open/completed/overdue.
3. Tạo timed/all-day Event theo timezone → nhận reminder → chỉnh một occurrence hoặc series (nếu recurrence P0).
4. Tạo reminder độc lập → nhận in-app notification → snooze/dismiss.
5. Xóa nhầm Task/Event → restore từ Trash mà không mất sub-items/attachments.
6. Tìm/fil­ter theo status, dates, project, priority, tags trên mobile/desktop.

## 3. Tasks

### 3.1 Task data và validation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TSK-001` | P0 | User tạo Task với title bắt buộc; description, status, priority, dates, project, tags là optional theo scope. | Whitespace-only/over-limit title bị từ chối; owner set server-side. |
| `P02-TSK-002` | P0 | Date semantics phân biệt date-only và datetime; due không bị đổi ngày theo timezone. | Cross-timezone round-trip pass; validation cho invalid start/due rule đã duyệt. |
| `P02-TSK-003` | P0 | Status model có stable values và valid transitions. | UI/API dùng cùng state; unknown transition bị từ chối. |
| `P02-TSK-004` | P0 | Priority có stable ordering và không bị trộn với status. | Sort/filter đúng; missing priority có behavior rõ. |
| `P02-TSK-005` | P0 | User update Task theo concurrency strategy, không silent overwrite. | Two-tab edit conflict được detect/resolve theo policy. |
| `P02-TSK-006` | P0 | Complete Task ghi completion timestamp; reopen xử lý timestamp/history nhất quán. | Complete/reopen idempotent; Today/metrics cập nhật đúng. |
| `P02-TSK-007` | P0 | Task soft-delete/restore giữ subtask, checklist, tags, reminders và attachment relations theo aggregate policy. | Restore không orphan/duplicate; active share/reminder behavior đúng policy. |

Status default `PROPOSED`: `Todo`, `InProgress`, `Done`, `Cancelled`. Product Owner có thể chọn model đơn giản hơn; mã requirement giữ nguyên.

### 3.2 Subtasks và checklists

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TSK-008` | P0 | Task hỗ trợ subtasks với depth limit được quyết định; cycle bị cấm. | Không gán chính nó/descendant làm parent; move giữ owner/project rules. |
| `P02-TSK-009` | P0 | Parent completion behavior khi subtask còn mở phải được cấu hình cố định. | UI cảnh báo/chặn/allow đúng decision; không âm thầm complete children. |
| `P02-TSK-010` | P0 | Checklist item có text, order, checked state; reorder/update concurrency-safe. | Duplicate/retry không mất item; progress tính đúng. |
| `P02-TSK-011` | P0 | Checklist không đồng nghĩa subtask và không xuất hiện độc lập trong global views. | Search/reminder không coi checklist item là Task trừ khi decision đổi. |

### 3.3 Tags, attachments, filter và views

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TSK-012` | P0 | User gắn/bỏ owned tags; normalization/duplicate rule nhất quán. | Case/whitespace behavior đúng; không dùng tag User B. |
| `P02-TSK-013` | P0 | Attachment dùng File Service và thừa hưởng Task access/lifecycle. | Direct file access cross-user thất bại; orphan cleanup đúng. |
| `P02-TSK-014` | P0 | List có pagination, sort và filters status/priority/project/tag/due range. | Filter kết hợp đúng và access-scoped; URL/query state usable trên mobile. |
| `P02-TSK-015` | P0 | Views `Today`, `Upcoming`, `Overdue`, `Completed` có timezone/date rules duy nhất. | Boundary midnight/timezone tests; cancelled không bị coi completed nếu decision không nói vậy. |
| `P02-TSK-016` | P1 | Saved filter/view không mở rộng access scope. | Revoked/deleted item không xuất hiện qua saved view. |

### 3.4 Recurring Tasks

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-REC-001` | P0/P1 | Recurrence rule hỗ trợ subset đã duyệt (daily/weekly/monthly/custom) với timezone và end condition. | Rule invalid bị từ chối; DST/month-end cases documented/tested. |
| `P02-REC-002` | P0/P1 | Quyết định generate occurrence on schedule hay upon completion; không được trộn behavior. | Retry không tạo duplicate occurrence; missed-run policy test. |
| `P02-REC-003` | P0/P1 | Edit hỗ trợ rõ `this occurrence` và/hoặc `series`; exception có trace tới series. | Edit/delete một occurrence không corrupt future series. |
| `P02-REC-004` | P0 | Stop/delete recurrence ngăn future creation nhưng không tự xóa historical completed occurrence. | Job queued kiểm tra series state trước create. |

## 4. Projects

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PRJ-001` | P0 | User tạo Project với name bắt buộc, description/status/dates optional. | Owner/private default; validation và concurrency như Task. |
| `P02-PRJ-002` | P0 | Baseline `PROPOSED`: Task thuộc tối đa một Project nhưng có thể không thuộc Project. | Move/unassign atomic; cross-owner project assignment bị chặn. |
| `P02-PRJ-003` | P0 | Project detail hiển thị scoped tasks, counts và progress definition rõ. | Count không bao gồm trashed/unauthorized; status filters consistent. |
| `P02-PRJ-004` | P0 | Archive/complete Project không tự complete Tasks nếu chưa có explicit confirmation/rule. | Open task handling được báo; background reminder không mất ngầm. |
| `P02-PRJ-005` | P0 | Delete/restore Project có policy cho Tasks (`detach`, `cascade trash`, hoặc block) được Product Owner duyệt. | Transaction/fault tests không orphan hoặc mất Tasks. |
| `P02-PRJ-006` | P1 | Project hỗ trợ sharing chỉ khi Task visibility semantics được định nghĩa. | Share Project không tự lộ private Task ngoài approved composition. |

Project state default `PROPOSED`: `Active`, `Completed`, `Archived`. Không triển khai member/assignee/team trong baseline.

## 5. Calendar và Events

### 5.1 Calendar

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-CAL-001` | P0 | User có ít nhất một personal calendar; multiple calendars là decision P0/P1. | Event luôn thuộc calendar hợp lệ của owner. |
| `P02-CAL-002` | P0 | Day/week/month/list views hiển thị event theo user timezone và responsive. | All-day không trượt ngày; overlapping timed events vẫn đọc/operate được. |
| `P02-CAL-003` | P0 | Calendar filter/visibility là preference per-user, không phải permission. | Hide calendar không xóa event hoặc reminder. |

### 5.2 Events

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-EVT-001` | P0 | Tạo Event với title; loại `AllDay` hoặc `Timed`; start/end validation rõ. | End trước start bị từ chối; all-day lưu date semantics. |
| `P02-EVT-002` | P0 | Timed Event lưu instant + originating timezone cần thiết để recurrence/display đúng. | User đổi display timezone không làm thay đổi instant. |
| `P02-EVT-003` | P0 | Event hỗ trợ description/location/tags/attachments theo scope đã duyệt. | External content encoded; files scoped. |
| `P02-EVT-004` | P0 | Update/delete/restore Event xử lý linked reminders và recurrence exceptions. | Reminder không fire cho deleted/cancelled event; restore theo policy. |
| `P02-EVT-005` | P0/P1 | Recurring Events theo rule subset, exception và edit-series semantics đã quyết định. | DST, month-end, single occurrence update/delete tests pass. |
| `P02-EVT-006` | P1 | Calendar/Event sharing chỉ read-only và không tạo invitation semantics. | Viewer không edit; hidden/private fields policy được test. |

External sync, attendees/invites và shared collaborative calendars là deferred.

## 6. Reminders

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-RMD-001` | P0 | User tạo reminder độc lập hoặc linked tới supported resource. | Linked resource/owner được validate; unknown type bị chặn. |
| `P02-RMD-002` | P0 | Reminder có due instant/date rule, timezone, message safe và state rõ. | Invalid/past-time behavior theo decision; secret không được nhúng. |
| `P02-RMD-003` | P0 | Scheduler phát idempotent notification; retry không duplicate. | Same occurrence key tạo tối đa một logical notification. |
| `P02-RMD-004` | P0 | User `dismiss`, `snooze`, `cancel`; action/state transitions atomic. | Snooze tạo next trigger đúng; stale job kiểm tra state. |
| `P02-RMD-005` | P0 | Update/delete/complete resource nguồn áp dụng reminder policy rõ. | Complete Task không nhận reminder cũ; Event reschedule cập nhật trigger. |
| `P02-RMD-006` | P1 | Quiet hours/channel preferences áp dụng qua Notification Center. | Critical/non-critical rules nhất quán. |

Reminder state default `PROPOSED`: `Scheduled`, `Triggered`, `Snoozed`, `Dismissed`, `Cancelled`, `Failed`.

## 7. Planner, Goals, Habits, Time Tracking và Pomodoro

### 7.1 Daily/Weekly Planner

Planner P0 là derived view của Tasks/Events/Reminders, không sao chép record. Drag/reorder hoặc daily intention notes là P1.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PLN-001` | P0 | Today view tổng hợp đúng resource được phép, theo timezone. | No cross-user leak; stale/deleted item không hiển thị. |
| `P02-PLN-002` | P1 | Reorder/plan-for-day không làm đổi due date trừ khi user chọn explicit action. | UI phân biệt scheduling preference và business due date. |

### 7.2 Proposed P1 modules

| ID | Pri | Requirement |
|---|---:|---|
| `P02-GOL-001` | P1 | Goal có title, timeframe, status và optional measurable target; relation với Task/Habit không cascade ngầm. |
| `P02-HAB-001` | P1 | Habit có schedule/timezone, check-in, skip và streak rule minh bạch; timezone change không rewrite history. |
| `P02-TIM-001` | P1 | Time entry có start/end/duration, optional Task/Project; overlap/rounding/manual edit policy rõ. |
| `P02-POM-001` | P2 | Pomodoro timer là client/user session feature; refresh/background behavior và notification permission rõ, không tuyên bố độ chính xác khi browser suspended. |

## 8. Search, sharing, permissions và audit integration

- Phase 2 cung cấp search projection contract; Global Search UI phát hành Phase 3.
- Share support P0: Task item `PROPOSED`; Project/Calendar collection là P1 sau composition decision.
- User chỉ thao tác owned data; Admin theo `tasks/projects/calendar.*` + scope; SuperAdmin privileged access.
- Audit bắt buộc: permanent delete, privileged access, share lifecycle, import/export, recurrence/automation failure có security impact.
- Normal edits/completions có thể vào Activity History thay vì Audit trừ khi accessed bằng admin scope.

## 9. Edge cases bắt buộc

- Midnight, DST và user đổi timezone.
- Recurrence on day 29/30/31; missed scheduler run; duplicate retry.
- Parent/subtask cycle và complete-parent-while-child-open.
- Project deleted/archived có open tasks.
- Reminder/job queued khi resource/user bị deleted/disabled.
- Concurrent reorder/checklist update/two-tab task edit.
- Attachment upload partial hoặc file bị quarantine/missing.
- Permission/share revoked khi detail đang mở.

## 10. Phase verification scenarios

1. Hai User tạo Tasks trùng title; không ai thấy/search/export được Task của người kia.
2. User tạo recurring Task có reminder, scheduler retry, chỉ một occurrence/notification được tạo.
3. User reschedule recurring Event một occurrence; future series không đổi ngoài decision.
4. Xóa/restore Project theo selected child policy không mất Task/attachment.
5. Timezone switch giữ instant event và date-only due date đúng semantics.
6. Admin có `tasks.view` nhưng không `access_all` không xem được private Task User.
7. Mobile user tạo, filter, complete, snooze và restore mà không cần desktop-only action.

## 11. Exit criteria

- Scope P0 đã được Product Owner khóa; `DEC-PRD-002/003` đóng.
- P0 requirements và negative authorization tests pass.
- Recurrence/reminder job idempotency, timezone và restart tests pass.
- Today/Upcoming/Overdue definitions có testable business rules.
- Responsive/accessibility P0 journeys pass.
- Search projections và dashboard read contracts sẵn sàng cho Phase 3.
- Không có Critical/High security/data-integrity finding mở.
