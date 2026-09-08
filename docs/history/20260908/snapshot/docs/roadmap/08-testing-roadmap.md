# Testing Roadmap

**Status:** TEST PLAN ONLY. Không có application tests nào được viết/chạy trong task tài liệu này. Các scenario/suite IDs dưới đây là planned trace keys, không phải evidence Passed.
[Master](00-master-implementation-roadmap.md) · [Requirement-by-requirement mapping](01-requirement-traceability.md) · [Local Stable](09-local-stable-release.md)

## 1. Test throughout development

| Delivery stage | Test work phải đi cùng |
|---|---|
| RM00–RM01 | Review source coverage, business conflicts, DoR, threat model, domain/access/API design và measurement profile |
| RM03–RM04 | API/error/validation/health contracts; architecture boundaries; SQL constraints/migrations/seed |
| RM05–RM07 | Auth/RBAC/owner/shared/support/emergency/module contracts; files/jobs/notifications; component/a11y |
| RM08–RM15 | Unit + SQL/adapter integration + feature UI + critical E2E cho mỗi completed vertical slice |
| RM16 | Cross-module/resource/permissions/data lifecycle/reindex/account orchestration |
| RM17 | Full local regression, performance/soak/fault, security, backup/key/restore/runbook rehearsal và bug fixing |
| RM18 | Review evidence, all-catalog completeness, explicit Local Stable approval |
| RM21–RM22 | Re-run topology-sensitive suites, deployed security/load/recovery, independent security review, go-live |

Không đợi “Testing phase” để viết tests. Không dùng mock pass để thay SQL Server/provider topology evidence. Broaden regression khi sửa shared contract/schema hoặc có concrete risk; không chạy vô hạn tests không liên quan.

## 2. Trace-to-test contract

Mỗi requirement trong 01 map feature/suite và **TC-{RequirementID}**. TC là container cho happy/alternate/negative/boundary cases; khi implement phải tách cases con, ví dụ TC-P02-PRJ-014.01 freeze Task update, .02 reject create, .03 stale edit race. Governance/decision rows map REVIEW-{ID}; narrative sections map NR-{source/section}. Không gọi một review row là automated unit test.

Mỗi implemented scenario cần: Source IDs + approved decision/ADR + preconditions/fixtures/actor-context + operation + expected persisted/HTTP/UI/event outcome + actual evidence path/commit/tool version/profile. Expected behavior lấy từ source acceptance, không suy qua implementation.

Coverage được tính từ tập source IDs/normative sections, không phải số test methods. MAPPED chưa phải Verified; OPEN/CONFLICT không được đánh dấu pass bằng default behavior. DEFERRED/OUT có reason và absence/control checks nếu cần.

## 3. Suite inventory

| Suite | Coverage / phase | Dependency |
|---|---|---|
| V-GOV | F-GOV — Requirement governance; RM00–RM01 | PO review; source decisions |
| V-ENV | F-ENV — Local environment; RM02 | RM01 local profile |
| V-API | F-API — Backend/API conventions; RM03 | RM01–RM02 |
| V-DB | F-DB — SQL/EF Core foundation; RM04 | RM01 domain + RM03 |
| V-ID | F-ID — Identity/profile/session; RM05 | RM04; DEC-TEC-004; DEC-SEC-001/009 |
| V-AZ | F-ACCESS — Owner/sharing/support/emergency/RBAC; RM05–RM06 | RM04–RM05; DEC-SHR-*; DEC-SUP-*; DEC-SEC-003/008 |
| V-MOD | F-MOD — Module platform; RM06 | RM05; DEC-TEC-013 |
| V-AUDIT | F-AUDIT — Audit/activity; RM06; RM17; RM21 | RM05; DEC-SEC-005 |
| V-LIFE | F-LIFE — Trash/archive/history lifecycle; RM06; RM08–RM15; RM16 | RM04–RM06; aggregate lifecycle decisions |
| V-FILE | F-FILE — Files/media/attachments; RM06; RM09 | RM05; DEC-TEC-006; DEC-SEC-006; DEC-KNW-032 |
| V-NTF | F-NOTIFY — Three-channel Notification Center; RM06–RM07; RM08 | RM05; DEC-TEC-009; durable jobs |
| V-JOB | F-JOBS — Background jobs/integrations; RM06; RM13–RM14 | RM05; DEC-TEC-008/009; secret/egress design |
| V-SET | F-SETTINGS — System/module/user settings; RM06–RM07 | RM05; DEC-PRD-006; secret policy |
| V-PROD | F-PRODUCTIVITY — Projects/Tasks/Reminders; RM08 | RM06–RM07; DEC-PRD-032/033; P02 lifecycle rules |
| V-CAL | F-CALENDAR — Calendar/manual Events/ICS/timezone; RM08 | RM06–RM08; time/ICS decisions |
| V-DOC | F-DOCS — Document pages/editors/versions; RM09 | RM06–RM08; DEC-KNW-032/036/039/040; editor scope |
| V-ORG | F-ORG — Folders/Tags/Collections/Templates; RM09 | RM06; Documents organization/refinement decisions |
| V-KNU | F-KNUTIL — Bookmarks/Snippets/Read Later; RM09 | RM06; SSRF and utility field/lifecycle scope |
| V-MPR | F-MOREPROD — Planner/Goals/Habits/Time Tracking/Pomodoro; RM10 | RM06–RM08; per-module discovery required |
| V-SEARCH | F-SEARCH — Global/saved Search/Favorites/Recent/History/Palette; RM11; extend RM13–RM16 | RM06; module providers; DEC-TEC-007 |
| V-DASH | F-DASH — Dashboard/widgets/quick actions; RM11; extend RM13–RM16 | RM08–RM10; DEC-PRD-005 |
| V-FIN | F-FIN — Finance accounts/ledger/bills/subscriptions/budget/reports; RM12 | RM06/RM11; DEC-PRD-007; financial field/state decisions |
| V-VAULT | F-VAULT — Vault/secret encryption/rotation; RM06 security adapter; RM12 product | DEC-SEC-002/004; DEC-PRD-008; DEC-SUP-002; backup key design |
| V-NEWS | F-NEWS — News/Feeds/Topic Watch; RM13 | RM06/RM09; DEC-PRD-009; DEC-SEC-007 |
| V-SHOP | F-SHOP — Price tracking/shopping records; RM13 | RM06/RM12; DEC-PRD-010/011; provider feasibility |
| V-TOOLS | F-TOOLS — Developer Toolbox; RM14 | RM06; DEC-PRD-012; DEC-SEC-007 for network tools |
| V-GH | F-GITHUB — GitHub Discovery/rankings/history; RM14 | RM06; public API query/week/cache decisions |
| V-AUTO | F-AUTO — Automation/webhooks/n8n/data sync; RM14 | RM06/RM12/RM13; DEC-PRD-013; DEC-SEC-007 |
| V-ASSET | F-ASSETS — Personal Assets/purchase/warranty; RM15 | RM06/RM09/RM12; DEC-PRD-014 |
| V-DIGITAL | F-DIGITAL — Digital Assets/renewals/licenses; RM15 | RM06/RM12; DEC-PRD-014; egress for observation |
| V-CAREER | F-CAREER — Career/Resume/Learning/Work Log; RM15 | RM09/RM10/RM12; DEC-PRD-014; interview Calendar mapping |
| V-QUALITY | F-QA — UX/a11y/security/NFR/observability; RM03–RM17; RM21 | Approved measurement profile/targets; all slice AC |
| V-RESTORE | F-BACKUP — Backup/restore/recovery; RM12; RM17; RM21 | Schema/file/key inventory; DEC-TEC-011 |
| V-PORT | F-PORT — Import/export/account lifecycle integration; RM08–RM16; RM21 | Per-module format/retention decisions; no Project/Task import-export |
| V-DEPLOY | F-DEPLOY — Production planning/deployment/public release; RM19–RM22 | RM18 LOCAL STABLE RELEASE; DEC-TEC-012 |

| V-CACHE | F-CACHE — Redis cache/fallback/invalidation; RM02; RM06; RM11–RM14; RM17 | DEC-TEC-005; measured cache use case; SQL authoritative controls |
| V-UI | F-FRONT — React foundation/design system; RM07; RM08–RM16 UI | RM03–RM06 contracts; DEC-TEC-001; approved UX/language profile |

## 4. Authorization matrix — bắt buộc cho mọi supported resource

| Dimension | Cases |
|---|---|
| Actors | Anonymous, unverified, owner, another verified User, disabled User; Admin no permission; Admin permission no grant; qualified Admin; SuperAdmin normal route |
| Context | Self, public share, any-authenticated share, restricted users, support, emergency |
| Permission/module | Installed/system/user on/off; missing action; revoked action; grant wrong owner/module; dependencies/schema not ready |
| Grant lifecycle | Active, expired, revoked, malformed/guessed token; current allowlist add/remove; wrong owner; role demotion |
| Resource | Active, Draft/Published/Archived per module, terminal, Trash, purged; child/file/history relation |
| Paths | List/detail/count/facet/search/export/import/write/file/cache/job/notification deep link/Calendar projection/widget/share metadata |
| Races | Permission/owner account/module/resource/grant changes after enqueue or UI load; active request vs revoke; parallel last-admin removal |

Cross-user negative fixtures tối thiểu User A + B có dữ liệu tương tự/trùng title/tag và khác module grants. Assert không chỉ HTTP deny mà cả response body/count/facet/snippet, DB side effect, queued deliveries, file read, audit actor và cache boundaries. Anonymous PublicLink là allowed branch; không vô tình require account cho mọi share mode.

Support/emergency không được mutate/export/purge/reveal/copy/impersonate. SuperAdmin normal route phải deny cross-user như User. Support one module + 24h/custom/until-revoke + any qualified Admin; emergency reason trước read, immutable audit và immediate all3 attempts.

## 5. Critical scenario packs

### V-ID / V-MOD / V-AZ / V-NTF

- Clean bootstrap interrupted/retried và concurrent last-active-SuperAdmin guard.
- Register → verify once → Active + one PersonalSpace + default effective modules; replay/expired token không provision duplicate.
- Login/logout/revoke-all/password change/disable và recovery rate/anti-enumeration; CSRF/cookie policy.
- Module discover/install/enable/disable/re-enable/upgrade/migration-failure/dependency graph; no executable upload route.
- Share three-mode allowlist/expiry/revoke/live projection; resource Trash/purge and policy-disable rules theo closed decisions.
- Admin support scope/expiry/readonly; SuperAdmin emergency start/use/end/denied audits; no audit loss if notification provider fails.
- Một logical Notification intent → ba channel attempts; In-app success dù Email hoặc Push fail; browser permission denied không giả delivery success.
- Read/unread/all-read/single/bulk delete owner-scoped, retained until User delete; notification delete không xóa audit.
- Retry after transport ambiguous ACK không tạo duplicate logical inbox notification; provider dedupe/limitation documented.

### V-PROD — Projects/Tasks

- Required-field sets đúng từng loại; Project/Task owner/ProjectId immutable; P0 highest/P3 lowest; shared Productivity multi-Tag catalog.
- Task transitions theo explicit source table, backward reason, Skipped semantics; unspecified transitions phải chốt trước exact assertions.
- Kanban create chỉ NotStarted/InProgress full form; status drag cancel/failed rollback; reorder same column persisted; table/filter/search fields đúng.
- Task ngoài Project range warning+confirmation; cancel no write; changed Project time between warning/Save rechecks.
- Start reached giữ state; End passed compute Overdue chỉ active Task; Completed/Skipped không overdue.
- Project all Tasks terminal chỉ prompt; complete còn open Task cần reason; Skip giữ child states; terminal không reopen/add/edit/version-restore/continue Task.
- Race terminal Project vs Task edit/restore/reminder; no half-state; read projection consistent.
- Task full old-version restore tạo new revision, giữ immutable fields/state validation/backward reason.
- Delete/restore Project full tree; no child restore while parent Trash; no standalone restore to terminal parent; prior Trash membership policy cases sau decision.
- Infinite Trash retention tới User purge; purge invalidates source refs but audit retained.
- Live Project share all active Tasks + approved detailed fields; no optional hide; no reasons/history/reminders/audit.
- No Project/Task import/export endpoint; Calendar export exception in V-CAL.

### V-CAL — Calendar/ICS/time

- Task Save/current data hiện đúng một logical readonly Event; mutation from Calendar denied; terminal Task remains visible with source status; Trash hidden.
- Day default, Month/Week/Agenda, correct status vocabularies và Title/Project Title search.
- Manual Event required Title/Description/Start/End; optional all-day one/multiple days + one Reminder; overlap warning.
- Scheduled→Completed/Canceled; cancel still visible struck through; terminal readonly/no reopen; past Scheduled no overdue/auto-state; no history UI.
- One Reminder exact instant hoặc 15m Start-relative; reschedule invalidates old due job; terminal/trash/account/module stops queued effect.
- Browser timezone detect/save/change; timed instant unchanged; date-only unchanged; DST gap/overlap/floating time fixtures follow approved semantics.
- Mixed valid/invalid/recurring/duplicate UID ICS → partial success report; missing required fields skipped; duplicate UID same owner skipped; no VALARM; all imported Scheduled/manual.
- All-day DTSTART/DTEND convention, source TZID/UTC; unsupported timezone/UID/DURATION policy closed before fixtures.
- Export source/status/all-or-custom filter; only fully-contained intervals; equality boundaries documented; no Reminder/history/reason/internal IDs.
- Export Task Event is Calendar projection, not round-trip Task creation; reimport becomes ManualEvent if accepted and not deduped by approved UID policy.

ICS uses format semantics defined by RFC 5545; task asks a supported subset, không triển khai mọi recurrence feature của tiêu chuẩn. DATE/DATE-TIME và timezone rules cần golden files và validated parser. [iCalendar RFC 5545](https://www.rfc-editor.org/info/rfc5545/)

### V-DOC / V-ORG / V-FILE

- Create explicit DocumentType+EditorMode each time, required Title, empty body accepted, duplicate Title accepted everywhere.
- Block/Markdown save/load semantic round-trip; no editor/type conversion; sanitized malicious paste/links/HTML in owner/share/highlight.
- Distinct Save even unchanged content creates one version; same command retry one result; stale two-tab Save conflict; dirty navigation/session handling.
- Restore version creates new current revision; all old versions preserved until permanent delete; immutable type/editor/folder/parent never restored differently.
- Root Folder fixed including null; child follows parent; both trees max two levels; no reparent/attach/detach/move page/cycle.
- One optional Tag inline create; block deletion current used Tag, no silent untag/cascade; concurrent Tag use/delete; Trash/history references after DEC-KNW-036.
- Icon emoji/builtin OR uploaded cover; no custom icon upload/external URL; crop inside bounds, preview/reload, cancel retains current; old version file/crop preserved.
- Grid default/Table exact fields Title/DocumentType/Tag; no Status/date/cover as added data columns; same dataset/filter/sort.
- Filters DocumentType/Tag/CreatedAt range; Title/Tag search excludes body; updated-desc tie stable.
- Entry only level1 Folders + root pages outside Folder; child in owner sidebar; Archived separate; Folder/search scope and Archive-child fixture set blocked by DEC-KNW-039/040.
- Draft↔Published still private; Published editable; Draft suspends links, republish only valid links; Archived readonly, previous state restore; valid active Published links keep viewing after Archive, no new share.
- Delete/restore whole Folder/page tree; child independent delete requires warning; restore parent before child; no selective partial cascade.
- File traversal/MIME/content sniffing/size/checksum/partial staging/missing/quarantine; direct cross-user or revoked-share downloads denied; orphan cleanup preserves all version references.
- Document/Folder Trash retention, lifecycle while Archived, version metadata scope, shared child/file projection require closed decisions; không tạo test khẳng định assumption.

### V-SEARCH / V-DASH / V-KNU / V-CACHE

- Owner-filtered results/counts/facets/highlights; no body match in Documents local search; Global Search field selection distinct.
- Stale/rebuilding/missing index rechecks current access; no Secret/plaintext; safe SQL query/pagination/collation fixtures.
- Saved/recent/favorite/history privacy; clearing own data leaves audit; palette no hidden destructive auto-execute.
- Widget isolated timeout/module disable; source Today/overdue/timezone definitions consistent; quick action validates full form.
- Bookmark SSRF redirect/private/credential URL/rebinding; manual save survives fetch fail. Snippet no execute; Read Later dedupe unified across Bookmarks/News.
- Redis versioned invalidation/cache-fill race/flush/outage/isolation/no-secret tests trong 07.

### V-MPR / V-FIN / V-VAULT

- Planner/Goal/Habit/Time Tracking/Pomodoro: derive exact states/cadence/progress/timer/restart fixture suites after dedicated discovery; no placeholder acceptance.
- Golden ledger opening/income/expense/transfer/edit/delete/restore; currency/rounding and report totals; split/debt/budget policies after approval.
- Duplicate bill/payment/recurring job → one effect; wrong-owner account/category/receipt denied; provider outage no ledger corruption.
- Marker secret absent SQL plaintext/Redis/search/log/audit/browser storage/unencrypted backup.
- Envelope tamper/owner-item swap/key-version mismatch fail-closed; old/new key rotation interruption resumes; no fake password-reset decrypt recovery.
- Masked metadata/detail, owner-only reveal/copy audited, auto-remask/recent-auth policies; support/emergency cannot reveal another User.
- Isolated full restore requires correct protected key; wrong/missing key errors explicit; files/reference and Finance ledger intact.

### V-NEWS / V-SHOP / V-TOOLS / V-GH / V-AUTO

- RSS/Atom namespace/date/dup/GUID reuse/malformed/XXE/large input/sanitization; 304/429/concurrent refresh and private user read state.
- Shopee variant/price definition/currency snapshots; unavailable/parse-change not zero; known comparable series validates current/previous/lowest/cooldown; all3 alert.
- Shopping records/warranty/Finance/File links after field/lifecycle refinement, no automated checkout or login sync.
- Utilities privacy/no persistence/no hidden network; Unicode, Base64, hash/UUID/QR/time/cron fixtures; regex ReDoS termination; XML/YAML safe subset.
- GitHub Top10 window/total-star/tie/filter/rule version fixtures; incomplete/rate-limit/stale response truthful; no OAuth/write/private APIs.
- Automation approved action registry/definition version, schedules/DST/missed runs; current actor/owner/module/secret before effect; duplicate trigger after effect ACK loss.
- Webhook auth/signature/replay/body/rate/egress; n8n version/mapping/disconnect/retry; no DB/master key transfer.
- Module contribution upgrade/disable with queued runs, cancel/retry races and safe logs/final-failure all3 notification.

### V-ASSET / V-DIGITAL / V-CAREER

- State/field/lifecycle suites after DEC-PRD-014; sensitive serial/contact/salary/credential fields omitted from share/search unless approved.
- Warranty/domain/certificate/renewal update cancels stale intent; exact date/instant semantics and lifetime/unknown expiry after refinement.
- File/Finance/Vault references owner-scoped; deleting link does not delete referenced source or silently post money.
- Resume replacement retains exact historical application version; company merge/interview reschedule/learning progress/work log totals.
- Interview Calendar source mapping approved before auto-event test; không mở thêm Event source type do assumption.
- Optional remote observation SSRF/provenance/stale/failure; no remote shell/registrar writes/AI resume/job application.

## 6. Test environments và tools proposal

| Layer | Proposed approach | Evidence required |
|---|---|---|
| Unit | xUnit .NET; pure deterministic domain logic, clock abstractions | Assertions độc lập persistence/UI implementation |
| SQL integration | Isolated SQL Server database(s) + real migrations/constraints/rowversion | Transaction/locking/precision/FK/concurrency results; no EF InMemory substitute |
| Redis/file/adapters | Isolated namespace/root; fault injection and cleanup guards | Real protocol/IO paths + synthetic fixtures; no external user targets |
| Frontend | Vitest/Testing Library proposal | Controls/feature logic/keyboard/errors/API contract integration |
| E2E | Playwright proposal; verified browser profile | Critical desktop/mobile routes + traces/screenshots safe |
| Security | Secret/dependency scan + malicious fixtures + auth matrix + deployed review | No unsafe scopes, false negative “hidden button” security proof |
| Performance | Runner selected after approved workload; built-in instrumentation where enough | Versioned hardware/data/network profile, percentile/error/resource results |
| Backup/restore | Isolated target, environment guard, key/file/DB inventory | Restore proof, not just job exit code |

Runner/library choices cần review/pin tại RM01, chỉ vì stack chưa có assertion/browser driver. Không install hoặc generate tests trong task roadmap.

## 7. NFR measurement và regression gate

Source PERF/MET/UX browser/A11Y numerical targets còn PROPOSED. Ví dụ source P95 API ≤500ms, P99 ≤1500ms, search P95≤1s và Web Vitals budgets chỉ thành pass/fail gate khi hardware/browser/network/dataset/users/jobs/files/notifications/share-rate profile được duyệt. Không công bố “fast enough” hoặc production SLO từ local smoke.

Profile phải ghi workload, warm/cold cache, SQL indexes, concurrency, test duration, representative dataset và correlation/metrics. Backpressure test chạy cùng provider/index/backup bursts, SQL slowdown, Redis loss, file failures, worker crash, retry storm scenarios.

Defect handling: report requirement/suite, expected/actual, severity, reproduction seed, affected module/owner path, evidence và fix owner. Fix xong rerun failed test + shared-contract affected suites; all regression gates ở RM17. Known limitation không tự thay requirement hoặc waive Critical/High finding.

## 8. Test evidence manifest — mẫu tương lai

| Field | Giá trị bắt buộc |
|---|---|
| Build/commit | Immutable tested commit/artifact, dependency versions |
| Scenario | Requirement IDs + TC/REVIEW/NR IDs + approved ADR |
| Profile | Environment/tool versions/dataset/browser/timezone |
| Result | NotRun / Blocked / Passed / Failed; actual log/report link |
| Security/privacy | Redacted fixtures; no real User secret/production data |
| Failure | Defect ID, reason, owner, retest status |
| Gate reviewer | Technical/Security/Product reviewers theo phase |

**Hiện tại:** mọi application scenario là NotRun/Planned. Chỉ kiểm tra tài liệu/coverage sẽ được báo trong bàn giao roadmap. Local Stable hoặc Production readiness không được đánh dấu đạt bằng việc soạn tài liệu.
