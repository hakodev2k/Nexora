# Decisions, Assumptions and Traceability

**Document ID:** `NX-GOV-001`  
**Version:** `1.2-draft`  
**Status:** Active requirement governance  
**Last decision update:** `2026-09-05`

## 1. Requirement status lifecycle

`Proposed → Reviewed → Approved → Implementing → Verified → Released`

Ngoài luồng chính có `Open`, `Blocked`, `Deferred`, `Rejected`, `Superseded`, `Deprecated`. Chỉ Product Owner duyệt product behavior/scope. Technical/security owner chỉ được chốt implementation trong guardrail không làm đổi behavior đã duyệt.

## 2. Product direction decisions

| ID | Decision | Status | Consequence |
|---|---|---|---|
| `DEC-PRD-015` | Team Workspace và collaboration phải được thiết kế ngay từ đầu. | **Superseded** bởi `DEC-PRD-024` | Không được dùng làm requirement Release 1. |
| `DEC-PRD-016` | Module mới chỉ do trusted developers phát triển/ship bằng code; Admin/User không author hoặc upload executable module. | Approved | Module Contract/Registry là core; no-code builder và third-party executable marketplace Deferred. |
| `DEC-PRD-017` | Collaboration v1 là bất đồng bộ. | **Superseded** bởi `DEC-PRD-024` | Release 1 không có team collaboration. |
| `DEC-PRD-022` | Nexora là Public SaaS do chủ sở hữu Nexora vận hành tập trung. | Approved | Production readiness, public attack surface, email delivery, capacity và operations là release requirements. |
| `DEC-PRD-023` | Bất kỳ ai cũng có thể self-register; account chỉ active sau email verification và được dùng ngay, không cần Admin approval. | Approved | Phase 1 phải có registration/verification/anti-abuse flow. |
| `DEC-PRD-024` | Release 1 là personal-only: mỗi User là một cá nhân độc lập, tự nhập/quản lý data; không có Workspace, memberships, group ownership hoặc team collaboration. | Approved | Ownership, queries, roles, search, files, jobs và tests chuyển sang cross-user isolation. |
| `DEC-PRD-025` | Release 1 gồm toàn bộ module đã có requirement hiện tại; mỗi module phải hoàn thành theo approved scope, không phải demo/placeholder. | Approved | Có thể chia milestone nội bộ nhưng release chỉ complete khi catalog/acceptance đã duyệt hoàn thành. |
| `DEC-PRD-026` | Dữ liệu chủ yếu được nhập thủ công; import file chỉ hỗ trợ ở module/format đã duyệt. | Approved | External synchronization không phải dependency mặc định. |
| `DEC-PRD-004` | Documents dùng chung một `ContentItem`/Document model với nhiều page type. | Approved | Note và Knowledge là DocumentType, không phải module/resource engine riêng. |
| `DEC-KNW-001` | Content editor phải hỗ trợ cả Block editor và Markdown. | Approved | Mỗi item dùng mode đã chọn lúc tạo; storage/round-trip của từng mode phải bảo toàn nội dung. |
| `DEC-KNW-002` | Không autosave content; User bấm Save và mỗi lần Save thành công tạo một version mới. | Approved | Dirty-state/navigation warning và optimistic-concurrency check bắt buộc; không có background autosave version. |
| `DEC-KNW-003` | User chọn `Block` hoặc `Markdown` khi tạo ContentItem; EditorMode không được đổi sau khi tạo. | Approved | Mode là immutable business field; không có convert/switch editor flow trong Release 1. |
| `DEC-KNW-004` | Restore version cũ tạo một current version mới từ nội dung đã chọn và giữ nguyên toàn bộ lịch sử. | Approved | Restore không rewrite/delete version; operation phải trace được source version. |
| `DEC-KNW-005` | Toàn bộ ContentItem version history được giữ tới khi ContentItem bị permanent delete. | Approved | Không giới hạn theo tuổi/số lượng và User không xóa riêng từng version. |
| `DEC-KNW-006` | Menu chỉ có module `Documents`; `Note` và `Knowledge` là loại Document, không có module Knowledge Base/Knowledge Article riêng. | Approved | Documents page có Create button và list các page đã tạo; catalog/permissions/search dùng một module boundary. |
| `DEC-KNW-007` | Document có states `Draft`, `Published`, `Archived`; Published không thay đổi visibility và vẫn private cho tới khi owner tạo share link. | Approved | Publish không tự tạo link hoặc cấp quyền; sharing vẫn qua Sharing Engine. |
| `DEC-KNW-008` | Published Document vẫn chỉnh sửa/Save được; Archived Document read-only nhưng có thể khôi phục. | Approved | Archive chặn content mutation; unarchive trả về trạng thái Draft/Published trước khi archive. |
| `DEC-KNW-009` | Documents được tổ chức bằng Folder, Tag và quan hệ page cha-con. | Approved | Folder/page hierarchy constraints còn cần làm rõ; mọi relation owner-scoped. |
| `DEC-KNW-011` | Page mới mặc định Draft; Draft ↔ Published; cả Draft/Published có thể Archive; Unarchive khôi phục đúng trạng thái trước Archive. | Approved | Archived record phải lưu previous non-archived state; Published không tự thay đổi visibility. |
| `DEC-KNW-012` | Folder trong Documents được lồng tối đa hai cấp. | Approved | Root Folder là cấp 1, child Folder là cấp 2; không có grandchild/cycle. |
| `DEC-KNW-013` | Quan hệ page cha-con là single-parent tree tối đa hai cấp. | Approved | Một page có tối đa một page cha; root page là cấp 1, child page là cấp 2; không có grandchild/cycle. |
| `DEC-KNW-014` | Active share link của Published Document vẫn cho phép xem read-only khi Document chuyển sang Archived. | Approved | Archived không cho tạo share mới; chỉ link đang active lúc Archive tiếp tục hoạt động, subject to expiry/revoke. |
| `DEC-KNW-015` | DocumentType không được thay đổi sau khi tạo page. | Approved | Không có convert type hoặc update/import/restore bypass trong Release 1. |
| `DEC-KNW-016` | Một Document page không bắt buộc thuộc Folder và được thuộc tối đa một Folder. | Approved | Folder membership có cardinality `0..1`; không có multi-folder membership. |
| `DEC-KNW-017` | Xóa Folder chứa child Folder/pages đưa toàn bộ cây Folder và các page trong cây vào Trash. | Approved | Không chuyển contents lên parent/root; toàn bộ aggregate ngừng xuất hiện và share access bị chặn theo Trash policy. |
| `DEC-KNW-018` | Chỉ Published Document được tạo share link; Published → Draft tạm khóa link và Published lại có thể kích hoạt lại link còn hiệu lực. | Approved | Link/token được giữ khi suspended nhưng không truy cập được; expired/revoked link không tự hoạt động lại. |
| `DEC-KNW-019` | Restore Folder từ Trash khôi phục toàn bộ cây đúng như lúc xóa. | Approved | Folder, child Folder, page membership và các page trong aggregate được restore cùng nhau; không selective restore một phần cây. |
| `DEC-KNW-020` | Xóa parent page đưa parent và toàn bộ child pages vào Trash. | Approved | Không promote/orphan child page; list/search/direct/share access không trả page trong aggregate đã trash. |
| `DEC-KNW-021` | Chỉ root/parent page có Folder membership; child page không chọn Folder riêng mà đi theo parent. | Approved | Child page kế thừa effective Folder của parent; đổi Folder của parent áp dụng cho toàn bộ page tree. |
| `DEC-KNW-022` | Restore parent page từ Trash khôi phục toàn bộ page tree đúng như lúc xóa. | Approved | Parent và các child thuộc aggregate tại thời điểm xóa được restore cùng nhau; không selective restore một phần aggregate. |
| `DEC-KNW-023` | User được xóa riêng một child page khi parent vẫn active nhưng phải xác nhận cảnh báo. | Approved | Chỉ child được đưa vào Trash; parent và sibling pages không đổi. |
| `DEC-KNW-024` | Root/child structure không được thay đổi sau khi page được tạo. | Approved | `ParentPageId` là immutable: root không attach thành child; child không detach hoặc đổi parent qua update/import/restore. |
| `DEC-KNW-025` | Child page bị xóa riêng chỉ được restore sau khi parent đã active/được restore trước. | Approved | Nếu parent ở Trash thì chặn child restore cho tới khi parent được restore; parent đã permanent delete thì child không thể restore. |

## 3. Module, administration, sharing và support decisions

| ID | Decision | Status | Consequence |
|---|---|---|---|
| `DEC-PRD-027` | Mọi module Release 1 mặc định bật cho User mới. | Approved | SuperAdmin có thể thay registration-default policy về sau. |
| `DEC-PRD-028` | SuperAdmin enable/disable module theo User; SuperAdmin quản lý module và action permission theo Admin. | Approved | User/Admin không tự bypass module disable hoặc tự nâng permission. |
| `DEC-PRD-029` | Resource sharing luôn read-only với ba mode: Public Link, Authenticated Link (bất kỳ User đăng nhập có link) và Restricted Users. | Approved | Không có password-link hoặc edit/comment qua link trong Release 1. |
| `DEC-PRD-030` | SuperAdmin quyết định theo từng module/resource type việc sharing có được phép. | Approved | Manifest/registry + system policy gate share action. |
| `DEC-PRD-031` | Share hỗ trợ expiry/revoke và hiển thị dữ liệu mới nhất; resource bị xóa/trash làm link không truy cập được. | Approved | Share tham chiếu live resource, không tạo snapshot/copy. |
| `DEC-SEC-010` | Admin chỉ xem dữ liệu User khi User cấp support grant read-only cho đúng một module. | Approved | Admin cần cả module permission và active grant. |
| `DEC-SEC-011` | Support duration có ba option: 24 giờ (default), custom expiry hoặc tới khi User revoke; bất kỳ Admin đủ permission có thể dùng. | Approved | Grant gắn User + module, không gắn độc quyền một Admin; use/expiry/revoke audited. |
| `DEC-SEC-012` | SuperAdmin chỉ xem dữ liệu User không consent trong emergency flow; bắt buộc reason, audit và immediate User notification. | Approved | Không có ambient global data browsing hoặc hidden impersonation. |

## 4. Project và Task decisions

| ID | Decision | Status |
|---|---|---|
| `DEC-TSK-001` | Mọi Task phải thuộc đúng một Project và không được chuyển sang Project khác. | Approved |
| `DEC-TSK-002` | Task states: `NotStarted` (Chưa làm), `InProgress` (Đang làm), `Completed` (Hoàn thành), `Skipped` (Bỏ qua). | Approved |
| `DEC-TSK-003` | Forward transition tự do; backward transition bắt buộc nhập reason. `Skipped` được chọn từ NotStarted hoặc InProgress cho Task chưa hoàn thành mà User không muốn tiếp tục. | Approved |
| `DEC-TSK-004` | Task required fields: Project, Title, StartDateTime, EndDateTime. Optional: Description, Acceptance Criteria text/checklist, Priority P0–P3, nhiều Tag và một Reminder. | Approved |
| `DEC-TSK-005` | P0 là priority cao nhất, P3 thấp nhất; Project/Task dùng chung tag catalog riêng của User, module khác dùng catalog riêng. | Approved |
| `DEC-TSK-006` | Task điều khiển read-only Calendar Event một chiều; Calendar không sửa Task. Event giữ lại khi Task Completed/Skipped và hiển thị source status. | Approved |
| `DEC-TSK-007` | Task active quá EndDateTime giữ business state và có computed Overdue flag; đến StartDateTime không tự đổi state. | Approved |
| `DEC-TSK-008` | Một Task có tối đa một Reminder: exact datetime hoặc preset 15 phút trước Start; phát đồng thời In-app, Email, Browser Push. | Approved |
| `DEC-TSK-009` | Project detail có Kanban/Table, mặc định Kanban; Projects page có Grid/Table, mặc định Grid. | Approved |
| `DEC-TSK-010` | Task Completed/Skipped vẫn sửa được nếu Project active; mọi thay đổi versioned, có thể restore toàn bộ version cũ thành revision mới. | Approved |
| `DEC-TSK-011` | Task delete vào Trash vô thời hạn; User tự purge. Không restore Task nếu Project terminal hoặc Project cha vẫn ở Trash. | Approved |
| `DEC-PRJ-001` | Project required: Title, Description, StartDateTime, EndDateTime; optional Priority, Tag, color/icon, notes; default state NotStarted. | Approved |
| `DEC-PRJ-002` | Project states: NotStarted, InProgress, Completed, Skipped. Completed/Skipped là terminal, không reopen; Project và Tasks trở thành read-only, không thêm Task. | Approved |
| `DEC-PRJ-003` | Nếu mọi Task terminal, hệ thống hỏi xác nhận complete Project. User vẫn có thể complete khi còn Task mở sau warning + mandatory reason. | Approved |
| `DEC-PRJ-004` | Skip Project giữ nguyên child Task states; các Task chưa xong không được tiếp tục sau khi Project terminal. | Approved |
| `DEC-PRJ-005` | Task ngoài khoảng Project được lưu sau warning + confirmation. Calendar chỉ hiển thị Task, không hiển thị Project. | Approved |
| `DEC-PRJ-006` | Delete Project đưa cả aggregate vào Trash vô thời hạn; restore Project khôi phục toàn bộ Tasks; không restore Task riêng khi parent ở Trash. | Approved |
| `DEC-PRJ-007` | Project share hiển thị live Project và toàn bộ Tasks/chi tiết Task; owner không ẩn Task riêng; history/reasons/reminders/audit không hiển thị. | Approved |
| `DEC-PRJ-008` | Project/Task import và export không thuộc Release 1; hai capability được xem xét cùng nhau sau Release 1. | Approved |

Chi tiết field, view, filter, search, state transition, Calendar projection, history và lifecycle nằm tại [Phase 2 — Productivity](phases/phase-02-productivity.md).

## 5. Calendar decisions

| ID | Decision | Status |
|---|---|---|
| `DEC-CAL-001` | Calendar có Task-generated Event và manual personal Event; view Month/Week/Day/Agenda, mặc định Day. | Approved |
| `DEC-CAL-002` | Overlap được phép nhưng phải cảnh báo; không drag/drop/resize, chỉ sửa qua detail form. | Approved |
| `DEC-CAL-003` | Manual Event required: Title, Description, Start, End; optional: một Reminder và All-day (một hoặc nhiều ngày). | Approved |
| `DEC-CAL-004` | Manual Event states: Scheduled, Completed, Canceled; delete nghĩa là Canceled. Completed/Canceled read-only, không reopen; Canceled vẫn hiện gạch ngang. | Approved |
| `DEC-CAL-005` | Event qua End nhưng còn Scheduled vẫn hiển thị bình thường, không auto-state hoặc overdue marker. | Approved |
| `DEC-CAL-006` | Calendar filters: Status, time range. Search: Title và Project Title cho Task-generated Event. | Approved |
| `DEC-CAL-007` | Event cá nhân không được chia sẻ bằng link; không external calendar synchronization trong Release 1. | Approved |
| `DEC-CAL-008` | Calendar import/export `.ics`; import luôn tạo manual Event, không Task; recurring/invalid/duplicate UID entries bị skip và báo cáo. | Approved |
| `DEC-CAL-009` | Import không lấy VALARM; mọi imported Event thành Scheduled; datetime quy đổi về User timezone. | Approved |
| `DEC-CAL-010` | Export cho User chọn source type, statuses và toàn bộ hoặc custom time range; custom range chỉ lấy Event nằm hoàn toàn trong khoảng. | Approved |
| `DEC-CAL-011` | Export toàn bộ supported Event data trừ Reminder; no history/audit/internal metadata. | Approved |
| `DEC-CAL-012` | User timezone tự nhận từ browser và có thể đổi; đổi timezone giữ instant, chỉ thay display time. | Approved |

## 6. Notification decisions

| ID | Decision | Status |
|---|---|---|
| `DEC-NTF-001` | Task/Event Reminder luôn phát đồng thời In-app, Email và Browser Push. | Approved |
| `DEC-NTF-002` | Notification Center chứa Reminder, Security/Account, Support/Emergency và Module/System notifications. | Approved |
| `DEC-NTF-003` | Notification tồn tại tới khi User tự xóa; không auto-expire. | Approved |
| `DEC-NTF-004` | Security/Account, Support/Emergency và Module/System notifications cũng luôn phát đồng thời In-app, Email và Browser Push. | Approved |
| `DEC-NTF-005` | Notification Center hỗ trợ mở resource/trang nguồn, read/unread, mark-all-read, xóa từng notification và xóa hàng loạt. | Approved |
| `DEC-NTF-006` | Release 1 không hỗ trợ mute module/category hoặc quiet hours; không có User channel preference. | Approved |

## 7. Baseline assumptions còn hiệu lực

| ID | Assumption | Tác động nếu sai |
|---|---|---|
| `ASM-001` | Một account tương ứng một User identity và một personal data boundary. | Multiple identities/account merge cần decision/migration mới. |
| `ASM-002` | External share chỉ read-only; authenticated-link viewer không trở thành collaborator hoặc owner. | Edit/collaboration cần authorization model mới. |
| `ASM-003` | User tự nhập dữ liệu Finance/Tasks/Knowledge/Vault; integration chỉ là capability được duyệt riêng. | Provider sync trở thành critical path nếu manual không đủ. |
| `ASM-004` | GitHub Discovery chỉ dùng public data và không cần GitHub login. | OAuth/token/privacy scope đổi nếu cần private account features. |
| `ASM-005` | “Top weekly popular” nghĩa repository tạo trong tuần hiện tại, sort tổng stars giảm dần. | Ranking/data snapshot thay đổi nếu metric là stars gained. |
| `ASM-006` | Không có AI/LLM feature; AI News chỉ là category. | Architecture/cost/privacy/UX mở rộng nếu policy đổi. |
| `ASM-007` | Phase là milestone delivery nội bộ; mọi committed module vẫn thuộc cùng Release 1. | Release acceptance thay đổi nếu Product Owner tách release train. |

## 8. Open product decision backlog

| ID | Decision cần chốt | Chặn phase | Owner |
|---|---|---:|---|
| `DEC-PRD-005` | Dashboard widget set và customization level | 3 | Product Owner |
| `DEC-PRD-006` | UI language, locale, currency set và first day of week | 1 | Product Owner |
| `DEC-PRD-007` | Finance transfer, split transaction, budget period và debt workflow | 4 | Product Owner |
| `DEC-PRD-008` | Vault sharing/import/export có được phép không | 4 | Product + Security |
| `DEC-PRD-009` | News ingestion: RSS only hay thêm curated/manual sources | 5 | Product Owner |
| `DEC-PRD-010` | Shopee acquisition method và legal/operational constraints | 5 | Product + Legal/Tech |
| `DEC-PRD-011` | Price alert rule: absolute target, percentage drop, lowest-price và cooldown | 5 | Product Owner |
| `DEC-PRD-012` | Developer Toolbox P0 tool list và server-side network tools | 6 | Product + Security |
| `DEC-PRD-013` | Automation v1 workflow graph và n8n boundary | 6 | Product + Architecture |
| `DEC-PRD-014` | Field/state/lifecycle chi tiết cho Personal Assets, Digital Assets và Career | 7 | Product Owner |
| `DEC-PRD-032` | Project InProgress → NotStarted có được phép và có cần reason hay không | 2 | Product Owner |
| `DEC-PRD-033` | Task subtasks/attachments và independent Reminder có thuộc approved Phase 2 scope hay không | 2 | Product Owner |
| `DEC-SHR-001` | Default/max expiration presets cho external share link | 1 | Product Owner + Security |
| `DEC-SHR-002` | Existing share bị revoke ngay hay chỉ cấm tạo mới khi SuperAdmin tắt sharing của module | 1 | Product Owner + Security |
| `DEC-SHR-003` | Share link cũ có hoạt động lại sau khi owner restore resource từ Trash hay không | 1/2/3 | Product Owner |
| `DEC-SUP-001` | Support grant chỉ được xem active data hay gồm cả Trash/history | 1 | Product Owner + Security |
| `DEC-SUP-002` | Vault support bị cấm hoàn toàn hay chỉ cho xem safe metadata | 1/4 | Product Owner + Security |
| `DEC-KNW-010` | Required/optional fields theo từng DocumentType | 3 | Product Owner |

## 9. Technical/security decisions

| ID | Decision cần chốt | Chặn phase |
|---|---|---:|
| `DEC-TEC-001` | React framework/build tool, routing, state/data-fetching conventions | 1 |
| `DEC-TEC-002` | .NET target version, modular architecture và API style | 1 |
| `DEC-TEC-003` | SQL engine, ORM/data access, migrations | 1 |
| `DEC-TEC-004` | Authentication/session/email-verification implementation | 1 |
| `DEC-TEC-005` | Redis use cases, fallback và per-User key isolation | 1 |
| `DEC-TEC-006` | File storage abstraction và local/production implementation | 1/3 |
| `DEC-TEC-007` | Search implementation/index consistency | 3 |
| `DEC-TEC-008` | Background scheduler/job engine | 1/2 |
| `DEC-TEC-009` | Email và Browser Push providers/delivery architecture | 1/2/8 |
| `DEC-TEC-010` | Logging/metrics/tracing stack | 1 |
| `DEC-TEC-011` | Backup format/tool, retention, RPO/RTO | 8 |
| `DEC-TEC-012` | Public SaaS hosting, reverse proxy, domain, TLS, CDN và scaling topology | 8 |
| `DEC-TEC-013` | Module registry/manifest/package/version/migration orchestration | 1 |
| `DEC-TEC-014` | Personal owner representation và mandatory query isolation strategy | 1 |
| `DEC-TEC-015` | Optimistic concurrency/version restore strategy cho same-User multi-tab edits | 1/2 |
| `DEC-SEC-001` | Password hashing scheme và parameter upgrade | 1 |
| `DEC-SEC-002` | Secret encryption envelope, key store, versioning và rotation | 4 |
| `DEC-SEC-003` | Support/emergency session token, propagation và audit integrity | 1 |
| `DEC-SEC-004` | MFA/recent-auth cho Admin/SuperAdmin/Vault | 4/8 |
| `DEC-SEC-005` | Audit retention/integrity/export | 1/8 |
| `DEC-SEC-006` | Upload malware scanning, quotas, unsafe content | 3/8 |
| `DEC-SEC-007` | SSRF policy cho tools/webhooks/feeds/crawler | 5/6 |
| `DEC-SEC-008` | Restricted-user lookup anti-enumeration | 1 |
| `DEC-SEC-009` | Verification/recovery email token lifetime và resend limits | 1 |

## 10. Decision record rule

Mỗi decision khi đóng phải ghi: context, considered options, decision, rationale, consequences, security/data/migration impact, owner, date và link tới requirement bị ảnh hưởng. Không sửa lịch sử để làm như decision mới luôn tồn tại; decision cũ được giữ và đánh dấu `Superseded`.

## 11. Traceability chain

Mỗi P0/P1 requirement phải trace được:

`Goal → Requirement ID → User story/use case → Design/ADR → Work item/PR → Test case → Verification evidence → Release`

Minimum metadata cho work item/PR:

- requirement ID(s);
- decision/ADR link;
- acceptance criteria;
- security/data/migration impact;
- test evidence và known limitations.

## 12. Definition of Ready

Feature chỉ sẵn sàng development khi có actor/problem, in/out scope, happy/alternate/error states, fields/validation, state transitions, personal ownership, module enablement, permission/share/support policy, retention, audit/notification/search/file/job integration, acceptance criteria và mọi blocking decision đã đóng.

## 13. Definition of Done

- P0 acceptance criteria pass; P1 defer có Product Owner approval.
- Code review/build/tests/security checks pass.
- Cross-user isolation pass cho UI/API/direct ID/search/count/file/cache/export/job.
- Responsive/accessibility/error-state QA hoàn thành.
- Migration/backup/restore/rollback evidence có khi liên quan.
- Logs/audit/notification không lộ private/secret data.
- Docs/API/runbook được cập nhật và known risk có owner.

## 14. Risk register

| ID | Risk | Mức | Mitigation |
|---|---|---|---|
| `RSK-001` | Release 1 gồm quá nhiều module làm timeline/cost tăng mạnh | Critical | Milestone nội bộ, dependency order, feature DoR và evidence; không hạ module thành placeholder. |
| `RSK-002` | Cross-user authorization lỗi gây data leak trên Public SaaS | Critical | Owner context server-side + mandatory negative matrix ở mọi data path. |
| `RSK-003` | Vault key management sai làm lộ/mất Secret | Critical | Encryption design/review/rotation/restore rehearsal. |
| `RSK-004` | Public registration/share endpoints bị spam, enumeration hoặc abuse | High | Verification, rate-limit, anti-automation, generic responses và monitoring. |
| `RSK-005` | Search/cache trả stale permission data | High | Query-time authorization + invalidation bound. |
| `RSK-006` | Job/import/reminder retry tạo duplicate effect | High | Idempotency keys, UID/source keys, run history. |
| `RSK-007` | Server-side network tools gây SSRF | Critical | Disabled-by-default egress hoặc strict policy/test. |
| `RSK-008` | Email/Push provider outage làm mất notification | High | Independent delivery attempts, retry/backoff, operational alert. |
| `RSK-009` | Backup thiếu files/key hoặc không restore được | Critical | Full inventory + isolated restore rehearsal. |
| `RSK-010` | Module disable/upgrade làm mất data hoặc còn stale job/route | High | Lifecycle contract + dependency/migration tests. |
| `RSK-011` | Support/emergency access bị lạm dụng | Critical | Consent, one-module scope, expiry, read-only, reason, audit, immediate notification. |
| `RSK-012` | Task/Project terminal/trash/history rules không atomic | High | Aggregate transaction, state-machine tests, restore/purge scenarios. |
| `RSK-013` | `.ics` import gây duplicate/timezone/data loss | High | UID dedupe, per-record validation/report, UTC+zone semantics. |

## 15. Product Owner review queue

Sau khi Notification đã được chốt, requirement interview chuyển sang Knowledge/Documents rồi tiếp tục theo Open Product Decision backlog. Mọi câu trả lời tiếp theo phải được cập nhật vào requirement và Decision Log trước implementation.
