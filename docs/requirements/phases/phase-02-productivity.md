# Phase 2 — Productivity

**Phase ID:** `NX-PH-02`  
**Version:** `1.2-draft`  
**Status:** Project, Task và Calendar business behavior approved; remaining Productivity modules continue discovery  
**Outcome:** Mỗi User quản lý Project, Task, Calendar/Event và Reminder của chính mình trên desktop/mobile, không có Workspace hoặc team collaboration.  
**Depends on:** Phase 1 identity/email verification, personal ownership, Module Platform, sharing/support access, audit, trash, notifications, scheduler và timezone contracts.

## 1. Confirmed scope

### 1.1 Approved Release 1 behavior in this revision

- Projects và Tasks theo state, field, view, search, filter, history và Trash rules bên dưới.
- Task tự tạo một read-only Calendar Event và là source of truth.
- Manual personal Calendar Event.
- Một Reminder cho mỗi Task/Event; In-app + Email + Browser Push đồng thời.
- Calendar import/export file `.ics`.
- Read-only Task/Project sharing theo module policy.
- Cross-user isolation và Admin support/SuperAdmin emergency access.

### 1.2 Explicit exclusions for Project/Task/Calendar

- Workspace, assignment cho User khác, comments/replies/mentions/follows và team collaboration.
- Task có thể tồn tại ngoài Project hoặc chuyển sang Project khác.
- Reopen Project đã Completed/Skipped.
- Sửa Task từ Calendar.
- Project hiển thị như Calendar Event.
- Calendar Event sharing.
- Recurring manual Calendar Event và import recurring `.ics`.
- External calendar synchronization, attendees/invitations/resource booking.
- Drag/drop hoặc resize Event trực tiếp trên Calendar.
- Project/Task import và export trong Release 1.
- Live presence/realtime co-editing.

### 1.3 Committed modules còn cần discovery

Planner, Goals, Habits, Time Tracking và Pomodoro vẫn thuộc Master Catalog Release 1 theo `DEC-PRD-025`, nhưng các requirement cũ chỉ là proposal. Chúng chưa đạt Definition of Ready và không được tự suy diễn từ Project/Task/Calendar.

## 2. Terminology và aggregate boundaries

| Term | Meaning |
|---|---|
| Project | Aggregate root chứa toàn bộ Tasks của Project. |
| Task | Work item bắt buộc thuộc đúng một Project và không đổi Project. |
| Task Calendar Event | Read-only projection được Task tạo/cập nhật/xóa khỏi active Calendar. |
| Manual Event | Event cá nhân độc lập do User tạo trực tiếp trong Calendar. |
| Terminal Task | Task `Completed` hoặc `Skipped`; vẫn editable khi Project còn active. |
| Terminal Project | Project `Completed` hoặc `Skipped`; Project + Tasks read-only vĩnh viễn. |
| Overdue Task | Task NotStarted/InProgress có EndDateTime đã qua; đây là computed flag, không phải state. |

Project xóa/restore/purge là aggregate operation bao gồm Tasks. Task có history/lifecycle riêng khi Project vẫn active.

## 3. Project requirements

### 3.1 Fields và validation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PRJ-001` | P0 | Project required fields: `Title`, `Description`, `StartDateTime`, `EndDateTime`. | Blank required field bị từ chối; End phải sau Start; owner set server-side. |
| `P02-PRJ-002` | P0 | Optional fields: Priority P0–P3, nhiều Tag, color hoặc icon, Notes. | Optional field round-trip đúng; unknown priority/tag ownership bị từ chối. |
| `P02-PRJ-003` | P0 | Project mới mặc định `NotStarted`. | Create không truyền status tạo đúng NotStarted; tampered terminal default bị chặn. |
| `P02-PRJ-004` | P0 | Project date range do User nhập; Task ngoài range được cảnh báo nhưng User có thể xác nhận để lưu. | Không auto-clamp Task; confirmation outcome và override được history/audit theo policy. |
| `P02-PRJ-005` | P0 | Priority ordering: P0 cao nhất, P3 thấp nhất. Project và Task dùng chung một Tag catalog riêng của User. | Sort/filter đúng; User khác hoặc module khác không dùng nhầm tag catalog. |

### 3.2 Project state

| Code | Vietnamese | Meaning |
|---|---|---|
| `NotStarted` | Chưa làm | Project chưa bắt đầu. |
| `InProgress` | Đang làm | Project đang được thực hiện. |
| `Completed` | Hoàn thành | Project kết thúc thành công; terminal. |
| `Skipped` | Bỏ qua | User không tiếp tục Project; terminal. |

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PRJ-010` | P0 | Khi mọi Task đều Completed/Skipped, hệ thống hỏi User xác nhận có hoàn thành Project hay không. | Không auto-complete Project; cancel confirmation giữ state hiện tại. |
| `P02-PRJ-011` | P0 | User có thể Complete Project khi còn Task NotStarted/InProgress sau warning, confirmation và mandatory reason. | Blank reason bị từ chối; child Task states giữ nguyên. |
| `P02-PRJ-012` | P0 | Skip Project giữ nguyên state của mọi Task. | Không tự skip/complete/delete child Tasks. |
| `P02-PRJ-013` | P0 | Completed/Skipped Project không được reopen hoặc chuyển sang state trước. | UI không có action; direct API transition bị từ chối. |
| `P02-PRJ-014` | P0 | Project terminal và toàn bộ Tasks bên trong trở thành read-only; không tạo thêm Task. | Update/create/history-restore/Task state change đều bị chặn. |
| `P02-PRJ-015` | P0 | Task còn dở trong terminal Project không thể tiếp tục hoặc chuyển sang Project khác. | Không có clone/move/continue action ngầm. |
| `P02-PRJ-016` | P0/TBD | Behavior InProgress → NotStarted chưa được Product Owner chốt. | Implementation không tự cho phép/chặn trước khi `DEC-PRD-032` đóng. |

### 3.3 Project views

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PRJ-020` | P0 | Projects page có `Grid` và `Table`; default là Grid. | Preference/default không bị nhầm với Task Kanban. |
| `P02-PRJ-021` | P0 | Project card/Table row chỉ bắt buộc hiển thị Title, StartDateTime và EndDateTime. | Ba field nhất quán và timezone-correct. |
| `P02-PRJ-022` | P0 | Filter theo Tag, time range và Status. | Filter kết hợp đúng owner scope và Project range rule đã thiết kế. |
| `P02-PRJ-023` | P0 | Search Project chỉ theo Title. | Description/Notes/Task text không match; partial/case behavior phải thống nhất. |
| `P02-PRJ-024` | P0 | Default sort là Title A–Z. | Tie-break ổn định để pagination không duplicate/missing. |
| `P02-PRJ-025` | P0 | Khi mở Project, Task view mặc định là Kanban. | Projects Grid default không ảnh hưởng detail default. |

### 3.4 Project history và Trash

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PRJ-030` | P0 | Mọi thay đổi Project tạo immutable version/history entry. | Field/state/reason/actor/time có trace; history không sửa/xóa bởi User. |
| `P02-PRJ-031` | P0 | Delete Project đưa cả Project và toàn bộ Tasks vào Trash. | Operation atomic; active search/Calendar/share/reminder không còn expose aggregate. |
| `P02-PRJ-032` | P0 | Project/Tasks trong Trash giữ vô thời hạn tới khi User purge. | Không auto-purge theo tuổi. |
| `P02-PRJ-033` | P0 | Restore Project khôi phục toàn bộ Tasks như state tại lúc xóa. | Không restore partial tree; Task Calendar projections trở lại theo current Task state. |
| `P02-PRJ-034` | P0 | Không bao giờ restore riêng Task khi Project cha vẫn ở Trash. | Task restore endpoint trả domain error an toàn. |
| `P02-PRJ-035` | P0 | Permanent delete Project yêu cầu confirmation và purge toàn aggregate. | Không còn Task, Calendar projection, reminder hoặc active share; audit event không bị purge. |

### 3.5 Project sharing

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-PRJ-040` | P0 | Khi Project resource type được SuperAdmin cho share, owner có thể tạo read-only link theo ba shared access modes. | Link mode/expiry/revoke tuân theo `PDS-SHR`; Admin support viewer không tạo link thay owner. |
| `P02-PRJ-041` | P0 | Project link hiển thị Project và toàn bộ Tasks; owner không thể ẩn Task riêng. | New/current active Task tự xuất hiện; Task trong Trash không hiện. |
| `P02-PRJ-042` | P0 | Viewer thấy Task detail: Title, Description, Acceptance Criteria, Priority, Tags, Start, End, Status, Overdue. | Không lộ Reminder, history, backward reason, audit hoặc internal security metadata. |
| `P02-PRJ-043` | P0 | Project share luôn live/current, không snapshot. | Update hợp lệ hiển thị trên request sau; revoke/expiry/trash chặn. |

## 4. Task requirements

### 4.1 Cardinality, fields và validation

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TSK-001` | P0 | Mỗi Task bắt buộc thuộc đúng một Project. | Create thiếu/invalid/cross-user Project bị từ chối. |
| `P02-TSK-002` | P0 | Task không được chuyển sang Project khác sau khi tạo. | API ignore/reject ProjectId change; history restore không bypass invariant. |
| `P02-TSK-003` | P0 | Required fields: Project, Title, StartDateTime, EndDateTime. | Blank Title hoặc End ≤ Start bị từ chối. |
| `P02-TSK-004` | P0 | Optional: Description, Acceptance Criteria, Priority P0–P3, nhiều Tag và một Reminder. | Missing optional field save được; duplicate tag normalized theo catalog rule. |
| `P02-TSK-005` | P0 | Acceptance Criteria hỗ trợ text và checklist. | Mode/data round-trip; checklist order/check state không biến thành child Task. |
| `P02-TSK-006` | P0 | Một Task có tối đa một Reminder. Update thay giá trị Reminder hiện có; request tạo một Reminder độc lập thứ hai bị từ chối. | Không có thời điểm nào tồn tại hai active reminders cho cùng Task. |
| `P02-TSK-007` | P0 | Task ngoài Project date range được warning; chỉ lưu sau User confirmation. | Cancel không persist; confirm persist Task không đổi Project dates. |

Subtasks và Task attachments chưa được Product Owner chốt trong revision này; không được coi Acceptance Criteria checklist là subtask.

### 4.2 Task state machine

| Code | Vietnamese | Meaning |
|---|---|---|
| `NotStarted` | Chưa làm | Chưa bắt đầu. |
| `InProgress` | Đang làm | Đang thực hiện. |
| `Completed` | Hoàn thành | Đã hoàn thành. |
| `Skipped` | Bỏ qua | Task chưa hoàn thành nhưng User không muốn làm nữa. |

| From | To | Rule |
|---|---|---|
| NotStarted | InProgress hoặc Completed | Forward, không cần reason. |
| NotStarted | Skipped | Cho phép, không cần reason. |
| InProgress | Completed hoặc Skipped | Forward/terminate, không cần reason. |
| InProgress | NotStarted | Backward, bắt buộc reason. |
| Completed | InProgress hoặc NotStarted | Backward, bắt buộc reason; chỉ khi Project active. |
| Completed | Skipped | Không đi trực tiếp; phải reopen về active state với reason trước. |
| Skipped | InProgress hoặc NotStarted | Backward/reopen, bắt buộc reason; chỉ khi Project active. |

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TSK-010` | P0 | State mới mặc định NotStarted, trừ create từ Kanban InProgress column thì state khởi tạo InProgress. | Create từ terminal column bị chặn; form full vẫn bắt buộc. |
| `P02-TSK-011` | P0 | Forward transition tự do; backward transition phải có non-empty reason. | UI/API cùng rule; reason lưu history nhưng không xuất hiện trong share view. |
| `P02-TSK-012` | P0 | Skipped chỉ thể hiện Task chưa hoàn thành bị User dừng từ NotStarted/InProgress. | Không coi Skipped là Completed trong metric trừ khi metric ghi “terminal”. |
| `P02-TSK-013` | P0 | Completed/Skipped Task vẫn editable nếu Project active. | Field edit được; state backward vẫn yêu cầu reason. |
| `P02-TSK-014` | P0 | Project terminal override Task action: toàn bộ Task read-only dù Task state nào. | Direct Task endpoint rechecks Project state. |

### 4.3 Time, overdue và Calendar projection

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TSK-020` | P0 | Task có StartDateTime và EndDateTime theo User timezone; server lưu instant an toàn. | DST/timezone round-trip; End > Start. |
| `P02-TSK-021` | P0 | Đến StartDateTime không tự chuyển NotStarted sang InProgress. | Scheduler không tạo state transition; User tự đổi. |
| `P02-TSK-022` | P0 | Qua EndDateTime khi Task NotStarted/InProgress: giữ state và computed `Overdue=true`. | Completed/Skipped không Overdue; không persist state Overdue riêng. |
| `P02-TSK-023` | P0 | Save Task tự tạo/cập nhật một Task Calendar Event. | Retry idempotent; Task có tối đa một active projection. |
| `P02-TSK-024` | P0 | Đồng bộ một chiều Task → Calendar; Task Event read-only và mở Task detail. | Calendar edit endpoint từ chối Task Event mutation. |
| `P02-TSK-025` | P0 | Completed/Skipped Task Event vẫn hiển thị với source status; Task vào Trash làm projection ẩn. | Restore qua active Project làm projection quay lại; terminal Project vẫn hiển thị read-only Task Events theo data state. |

### 4.4 Reminder

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-RMD-001` | P0 | Task Reminder được đặt bằng exact datetime hoặc preset 15 phút trước StartDateTime. | Preset tính đúng timezone; invalid instant có validation rõ. |
| `P02-RMD-002` | P0 | Khi đến hạn, Reminder đồng thời tạo In-app notification, Email và Browser Push. | Ba delivery attempts độc lập; retry không tạo duplicate logical notification. |
| `P02-RMD-003` | P0 | Complete/Skip Task, terminal Project hoặc delete Task/Project hủy pending Reminder. | Queued stale job rechecks resource state trước delivery. |
| `P02-RMD-004` | P0 | Update Task time/reminder cập nhật scheduler atomically. | Old trigger không fire sau successful reschedule. |

Independent Reminder và snooze/dismiss chưa được Product Owner chốt. Quiet hours không thuộc Release 1 theo `DEC-NTF-006`.

### 4.5 Project Task views

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-VIW-001` | P0 | Project detail có Kanban theo state và Table; default Kanban. | Hai view dùng cùng filtered dataset/state. |
| `P02-VIW-002` | P0 | Kanban có bốn columns theo Task states và hỗ trợ kéo Task giữa states. | Transition/reason rules được enforce; failed move rollback UI. |
| `P02-VIW-003` | P0 | User reorder Task thủ công trong cùng column. | Stable order persisted; concurrent retry không duplicate/mất Task. |
| `P02-VIW-004` | P0 | Create trong Kanban chỉ có ở NotStarted/InProgress và luôn mở full Task form. | Required fields không bị bypass; created state khớp source column. |
| `P02-VIW-005` | P0 | Kanban card hiển thị Title, Priority, Start, End và Overdue. | Optional Priority có empty behavior; timezone đúng. |
| `P02-VIW-006` | P0 | Table columns: Title, Status, Priority, StartDateTime, EndDateTime. | Sort/render nhất quán với Kanban. |
| `P02-VIW-007` | P0 | Filter Task chỉ theo Status và time range. | Combination đúng; không tự thêm Project/Priority/Tag filter trong approved scope. |
| `P02-VIW-008` | P0 | Search Task trong Project theo Title và Tag. | Description/Acceptance Criteria không match. |
| `P02-VIW-009` | P0 | Card/row/detail mở Task detail. | Direct route rechecks owner/module/Project state. |

### 4.6 Task history và Trash

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-HIS-001` | P0 | Mọi thay đổi Task tạo immutable version với actor/time/changed data. | Create/edit/state/reason/tags/time/reminder/delete/restore có trace phù hợp. |
| `P02-HIS-002` | P0 | User có thể restore toàn bộ old Task version khi Project active; restore tạo current revision mới. | Không rewrite/delete history; invariant/validation được re-evaluate. |
| `P02-HIS-003` | P0 | Delete Task đưa vào Trash vô thời hạn tới User purge. | Active views/search/Calendar/share/reminder không expose Task. |
| `P02-HIS-004` | P0 | Không restore Task nếu parent Project terminal. | Domain error rõ; không reopen Project. |
| `P02-HIS-005` | P0 | Không restore Task riêng khi Project parent ở Trash. | Chỉ Project aggregate restore khôi phục child. |
| `P02-HIS-006` | P0 | Permanent delete Task yêu cầu confirmation và invalidates Calendar projection/share/reminder; audit/history retention theo policy. | Retry idempotent; no orphan relations. |

## 5. Calendar requirements

### 5.1 Calendar views, filter và search

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-CAL-001` | P0 | Calendar hiển thị Task Calendar Events và Manual Events của current User. | Không hiển thị Project như Event; cross-user data absent. |
| `P02-CAL-002` | P0 | Views: Month, Week, Day, Agenda; default Day. | Desktop/mobile usable; selected date/timezone retained appropriately. |
| `P02-CAL-003` | P0 | Overlapping Events được phép; save phải cảnh báo nhưng không block sau confirmation. | Both Events remain visible/readable; cancellation of warning does not save. |
| `P02-CAL-004` | P0 | Filter theo Status và time range. | Task-status và Manual-Event-status options được phân biệt rõ trong UI. |
| `P02-CAL-005` | P0 | Search theo Title của mọi Event và Project Title của Task-generated Event. | Manual Event không match Project; Description không thuộc approved search fields. |
| `P02-CAL-006` | P0 | Không drag/drop hoặc resize để sửa; chỉ edit trong detail form. | Direct gesture không persist; Task Event form read-only và link về Task. |

### 5.2 Manual Event fields

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-EVT-001` | P0 | Required: Title, Description, Start, End. | Blank field hoặc End ≤ Start bị từ chối với Timed Event. |
| `P02-EVT-002` | P0 | Optional capability chỉ gồm một Reminder và All-day flag trong approved scope. | Không recurrence/location/meeting URL/tag/attachment/color field nếu chưa có decision. |
| `P02-EVT-003` | P0 | All-day Event dùng date semantics và có thể kéo dài một hoặc nhiều ngày. | Không lệch ngày khi timezone đổi; end-date convention documented/tested. |
| `P02-EVT-004` | P0 | Timed Event dùng instant + User timezone; đổi account timezone giữ instant và đổi display time. | Event 09:00 zone A hiển thị corresponding local time zone B sau change. |
| `P02-EVT-005` | P0 | Manual Event có tối đa một Reminder với exact datetime hoặc preset 15 phút trước Start, phát cả ba channels. | Same rules/idempotency như Task Reminder. |

### 5.3 Manual Event state

| Code | Meaning |
|---|---|
| `Scheduled` | Event dự kiến/đang chờ User xử lý. |
| `Completed` | Event đã hoàn thành; terminal. |
| `Canceled` | Event bị hủy; terminal. |

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-EVT-010` | P0 | Manual Event mới mặc định Scheduled. | Create không truyền state tạo Scheduled. |
| `P02-EVT-011` | P0 | “Delete” Manual Event chuyển state sang Canceled, không đưa Trash. | Reminder bị cancel; Event vẫn tồn tại. |
| `P02-EVT-012` | P0 | Canceled Event vẫn hiển thị trên Calendar với kiểu gạch ngang. | Filter Canceled hoạt động; visual không chỉ dựa vào màu. |
| `P02-EVT-013` | P0 | Completed/Canceled Event read-only và không reopen. | UI/API update bị chặn. |
| `P02-EVT-014` | P0 | Event qua End mà vẫn Scheduled giữ nguyên và hiển thị bình thường, không Overdue marker hoặc auto-complete. | Scheduler/time passage không mutate state/visual overdue. |
| `P02-EVT-015` | P0 | Không có user-facing version history cho Manual Event; security/operational audit vẫn ghi create/edit/complete/cancel theo policy. | History UI absent; audit không chứa body quá mức cần thiết. |

### 5.4 Sharing và synchronization

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-CAL-010` | P0 | Calendar Event không shareable qua generic Sharing Engine. | Share action/API bị từ chối cho Task Event và Manual Event. |
| `P02-CAL-011` | P0 | Release 1 không đồng bộ trực tiếp với Google Calendar, Outlook hoặc external service. | Không polling/webhook/OAuth sync; `.ics` file operation không bị coi là sync. |
| `P02-CAL-012` | P0 | Calendar hỗ trợ manual import và export file `.ics`. | Permission, file validation, timezone và report requirements pass. |

## 6. ICS import requirements

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-ICS-001` | P0 | Mỗi supported VEVENT được import thành Manual Event của current User, không Task/Project relation. | Imported record owner server-side; không tạo Task hoặc Task Calendar Event. |
| `P02-ICS-002` | P0 | Import xử lý per-record/partial success. | Một invalid entry không rollback valid entries; report deterministic. |
| `P02-ICS-003` | P0 | Entry recurring (ví dụ có RRULE/recurrence semantics) bị skip. | Mixed file vẫn import valid non-recurring entries. |
| `P02-ICS-004` | P0 | Entry thiếu Title, Description, Start hoặc End, hoặc time range invalid, bị skip. | Không tự điền placeholder/default business data. |
| `P02-ICS-005` | P0 | Source UID được lưu; UID đã tồn tại bị skip, không update hoặc duplicate. | Import cùng file hai lần tạo đúng một Event/UID. |
| `P02-ICS-006` | P0 | VALARM/reminder trong file bị bỏ qua. | Imported Event không có Reminder. |
| `P02-ICS-007` | P0 | Mọi imported Event có state Scheduled bất kể source status. | CANCELLED/CONFIRMED/TENTATIVE source đều normalize Scheduled nếu entry hợp lệ. |
| `P02-ICS-008` | P0 | Datetime có timezone khác được quy đổi sang User account timezone nhưng giữ instant. | Cross-zone golden cases pass; all-day giữ date semantics. |
| `P02-ICS-009` | P0 | Import report nêu tổng, imported, skipped và reason theo entry. | Recurring/invalid/duplicate UID có reason phân biệt; không lộ content nhạy cảm vào log. |

## 7. ICS export requirements

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-ICS-020` | P0 | User chọn source: Manual Events, Task Events hoặc cả hai. | File chỉ chứa selected source types. |
| `P02-ICS-021` | P0 | User chọn states/statuses cần export theo selected source types. | Manual và Task status vocabulary không bị trộn sai. |
| `P02-ICS-022` | P0 | User chọn toàn bộ Calendar hoặc custom time range. | All option không yêu cầu range; invalid range rejected. |
| `P02-ICS-023` | P0 | Custom range chỉ gồm Event có Start và End đều nằm hoàn toàn trong range. | Overlap một phần bị loại; boundary equality behavior documented/tested. |
| `P02-ICS-024` | P0 | Export toàn bộ supported Event business information ngoại trừ Reminder. | Title/Description/time/all-day/status/source-safe metadata round-trip theo schema; no VALARM. |
| `P02-ICS-025` | P0 | History, audit, transition reason, internal ID/security metadata không export. | File inspection/projection whitelist pass. |
| `P02-ICS-026` | P0 | Export dùng timezone semantics của User và encoding/schema documented. | Imported by compliant calendar preserves intended instants/dates. |

## 8. Account timezone

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-TZ-001` | P0 | Default timezone tự nhận từ browser; User có thể đổi trong account settings. | Detection failure có fallback; saved preference thắng detection ở session sau. |
| `P02-TZ-002` | P0 | Timed Task/Event lưu instant; timezone change chỉ đổi display. | Existing scheduled instant/reminder không bị dịch chuyển. |
| `P02-TZ-003` | P0 | All-day date không đổi khi User đổi timezone. | Cross-timezone test giữ calendar dates. |

## 9. Notification integration — approved partial baseline

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P02-NTF-001` | P0 | Notification Center chứa Task/Calendar Reminder, Security/Account, Support/Emergency và Module/System categories. | Category/source stored and owner-scoped. |
| `P02-NTF-002` | P0 | Task/Event Reminder luôn tạo In-app, Email và Browser Push delivery đồng thời. | Independent channel attempts, idempotency và retry pass. |
| `P02-NTF-003` | P0 | Notification giữ tới khi User tự xóa; không auto-expire. | Age-based purge không xóa active inbox row. |
| `P02-NTF-004` | P0 | Delete inbox notification không xóa Audit Event liên quan. | Security/support audit retention độc lập. |
| `P02-NTF-005` | P0 | Mọi category khác cũng phát đồng thời cả ba channel; không có channel preference, mute hoặc quiet hours. | Provider/browser failure độc lập; UI/API không có suppression setting. |
| `P02-NTF-006` | P0 | Notification Center hỗ trợ open source, read/unread, mark-all-read, single delete và bulk delete. | Owner scope, safe deep link và idempotent bulk behavior pass. |

Notification behavior đã được chốt tại `DEC-NTF-001..006`.

## 10. Permissions và support access

- User CRUD Project/Task/Calendar của chính mình khi module enabled.
- SuperAdmin quyết định Tasks/Projects sharing capability; Calendar Event luôn non-shareable.
- Share viewer read-only theo Public/Authenticated/Restricted mode.
- Admin chỉ read trong đúng module nếu có active User support grant + required action.
- SuperAdmin normal module route không browse data User khác; emergency path cần reason/audit/immediate notification.
- Share/support/emergency không cấp import/export, history restore, purge, reveal/copy hoặc mutation.
- Project/Task `import/export` actions không tồn tại trong Release 1; Calendar `import/export` tồn tại.

## 11. Edge cases bắt buộc

- Project complete confirmation đúng lúc Task status thay đổi ở tab khác.
- Project terminal trong khi Task edit/restore/reminder request đang in flight.
- Task nằm Trash khi Project terminal hoặc Project vào Trash.
- Project restore cùng tên/ID relation; Task Calendar projection rebuild idempotent.
- Backward drag thiếu reason, duplicate request hoặc UI optimistic move thất bại.
- Task vượt Project range; time boundary/DST/timezone change.
- Reminder queued khi Task/Event/Project bị terminal, canceled, trashed, purged hoặc module/User disabled.
- Project live share trong lúc Task add/update/trash; revoke/expiry/access race.
- `.ics` mixed valid/invalid/recurring/duplicate UID, malformed timezone, all-day multi-day và large bounded file.
- Export range partial-overlap, source/status selection và timezone boundaries.
- User A direct-ID/search/count/file/calendar/import/export attempt vào User B data.
- Admin support grant sai module/expired/revoked và SuperAdmin emergency without reason.

## 12. Verification scenarios

1. Tạo Project → tạo Task bắt buộc → Task Event xuất hiện read-only trên Calendar.
2. Drag Task forward; drag backward yêu cầu reason; history ghi đúng revision.
3. Task qua End giữ state và hiện Overdue; đến Start không auto-transition.
4. Complete Project có open Tasks chỉ sau warning + reason; Project/Tasks sau đó read-only và không reopen.
5. Delete/restore Project khôi phục full aggregate; không restore child riêng; purge invalidates share/reminders/calendar projection.
6. Project link ở ba modes hiển thị live full Task projection nhưng không history/reason/reminder/audit.
7. Manual Event overlap warning; Canceled gạch ngang; Completed/Canceled không sửa/reopen; past Scheduled bình thường.
8. Timezone change giữ instant; all-day date không đổi.
9. Import mixed `.ics` tạo đúng valid non-recurring non-duplicate Scheduled Events, no VALARM, report đầy đủ.
10. Export selected source/status/full-or-range, range chỉ fully-contained, no Reminder/internal metadata.
11. Reminder đến hạn tạo đúng một logical notification và ba delivery attempts.
12. Cross-user/share/support/emergency authorization matrix pass.

## 13. Disposition of version 1.1 proposals

| Previous requirement group | Disposition |
|---|---|
| Workspace assignment/comments/mentions/follows (`P02-COL-*`) | Superseded by personal-only `DEC-PRD-024`. |
| Workspace Calendar/Space switching | Superseded. |
| Recurring Calendar Event | Excluded from approved Release 1 Calendar scope. |
| Project/Task import/export | Explicitly Deferred until after Release 1. |
| Task recurrence, subtasks, attachments, independent Reminder/snooze | Open; not approved by current answers. |
| Goals/Habits/Time Tracking/Pomodoro/Planner | Committed modules, detailed discovery pending. |

## 14. Exit criteria

- Every approved requirement above maps to API/data/UX design and automated acceptance tests.
- `DEC-PRD-032/033` được đóng hoặc tách rõ khỏi implementable slice; `DEC-NTF-001..006` đã Approved.
- Project/Task state, terminal aggregate, history/Trash/share/reminder/Calendar projection tests pass.
- Calendar Event, timezone và `.ics` import/export tests pass.
- Cross-user/share/support/emergency negative matrix reaches 100%.
- Email/Browser Push provider failure does not block In-app or core CRUD.
- Desktop/mobile/accessibility and no Critical/High security/data-integrity finding remain.
