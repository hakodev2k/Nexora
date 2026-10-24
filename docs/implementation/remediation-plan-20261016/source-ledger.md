# Nguồn đọc, revision và giới hạn bằng chứng

Tài liệu này ghi điều đã quan sát trong lượt lập kế hoạch, không chứng nhận application runtime. **R**=`f0611fad590269fdc73a3397fd785a97d2c0ef5e`; **L**=`1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`; **M**=`8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. File local được đọc ở worktree L; paths product/contract không có trong connector compare L→R được coi unchanged trong5commits này, không coi local modified tests/workflow là R. [Local source hashes](source-fingerprints.csv) ghi SHA256 theo bytes thực đọc trên đĩa; [remote source blobs](remote-source-fingerprints.csv) pin R cho selected service/client/workflow/harness files, không đọc nhầm branch moving head.

## Task input và startup

`C:\Users\hakodev\.codex\attachments\b7238651-1de5-4b0b-a96b-65598a8b4c36\pasted-text-1.txt` là user instruction: tiếng Việt, plan-only, đầy đủ P0/P1/test/review/semantic RTM. Initial377-line output bị truncate ở giữa; đã đọc lại phần module Habits/Planner/Windows và các ví dụ yêu cầu bằng bounded ranges; không suy truncated text là full source read. Scope test permission trong input supersede code-only amendment cũ nhưng không cho thực thi test trong lượt plan-only.

Startup read: `AGENTS.md`; `.agents/skills/nexora-engineering/SKILL.md`; `.ai/profiles/nexora-implementation-agent.md`; `.ai/roles/technical-lead/README.md` + `rules/core-rules.md`; `.ai/routing.json`; `.ai/verification.md`. Skill dùng `nexora-engineering`, không delegation/spawn independent reviewer. Git local read gặp dubious ownership; command-scoped `-c safe.directory=...`, không global config/network. GitHub reads qua host connector, không gh/curl/git network bypass.

## Nguồn sản phẩm và contract đã đọc

`FULL` chỉ áp body đã hiển thị đủ; `SELECTED` không certify toàn file. Các lệnh dài có output truncation chỉ dùng phần nhìn thấy hoặc bounded reread; source hash/parse không tự nâng reading_state.

| Source path/group ở L, unchanged selected paths qua compare L→R | Phần thực đọc / giới hạn | Dùng vào |
| --- | --- | --- |
| docs/README.md; docs/delivery/README.md; docs/delivery/01-current-scope.md | Entry authority/scope40FX/current approval + candidate order/runtime algorithm; SELECTED khi batch truncate, không claim full303docs | Scope, gates, no repeated local approval |
| docs/requirements/10-owner-decisions-20260907.md | Decision table, propagation, POAC001–008 | Account/secret/share/internal-first/Calendar limits; older partially-resolved overridden bởi11/12 |
| docs/requirements/11-owner-decisions-20260909-implementation-readiness.md;12-owner-decisions-local-e2e-implementation.md | Current amendment, decisions002–010, overlay;12 full | Business gates versus local execution permission; plan-only session higher precedence |
| docs/action-catalog/00-authorization-contract.md | FULL contexts/order/mutation response/defaults | Same-TX/current authority+write/read separation |
| docs/action-catalog/01-composition-and-field-guards.md | FULL semantic diff/dynamic composition/atomicity/no-action | Target extra action/no move/no grant inheritance |
| docs/action-catalog/04-module-action-contract.md;09-effective-implementation-status-20260909.md | SELECTED manifest/version/context + effective default/retired/network/sensitive/local overlays; older Admin SELF paragraph conflicts current common contract | Follow authoritative common00+current PO; no role-union/prefix grants |
| docs/action-catalog/catalog.csv | Parsed exact action names, contexts/dependencies for selected FX; not full semantic733audit | API/WP exact bindings, compare key absence |
| docs/action-catalog/modules/15-planner.md;16-goals.md;17-habits.md | Catalog/guards/deny/acceptance; Goal/Habit batched output partially truncated, bounded helper/source checks reread relevant clauses. Planner cleanup guard specifically read | Explicit update read prerequisites, unpin unavailable exception, per-action sets |
| docs/action-catalog/modules/06-notifications.md;20-documents.md;24-organization.md;25-discovery.md;26-dashboard.md;29-news.md;33-github.md;39-career.md;40-learning.md | SELECTED catalog keys+guards;39 appointment full guard lines,26 layout rows,20 catalog all exact keys. Không claim toàn per-row field contract complete | P1 slices and contract gates |
| docs/features/15-planner.md;16-goals.md;17-habits.md | FULL fields/flows/BR/AC/current subset | P0 invariants; advanced subset preserved |
| docs/features/20-documents.md;24-organization-and-templates.md;25-search-favorites-and-command-palette.md | FULL behavior and linked source IDs | Immutable topology/version, namespace/templates/search |
| docs/features/06-notification-center.md;26-dashboard.md | FULL via smaller follow-up read | Three attempts/watermark/current source; layout/widgets |
| docs/features/29-news-and-feeds.md;33-github-discovery.md;39-career-and-resumes.md;40-learning-and-work-log.md | Behavior/BR/AC/trace sections seen in bounded batch; no claim linked phase bodies all read | Stored reader, weekly snapshots, Resume/Event/evidence |
| docs/features/95-docx-md-and-internal-calendar.md | FULL supported formats/parser/Calendar link policy | DOCX/MD bounds, Scheduled existing Event selection, required fields, unlink |
| docs/requirements/phases/phase-03-knowledge-search-dashboard.md;phase-07-assets-and-career.md | SELECTED P03 content model/priority/source clauses, P07-RES/INT/SKL/CERF/LRN/WRK exact rows; rest pending | Current requirement IDs/inconsistencies |
| docs/goals/README.md;01-system-goals.md;04-cross-module-verification.md;05-task-and-evidence-template.md | FULL selected individual outputs | System/goal→source→action→code→test/evidence schema |
| docs/goals/02-product-phase-goals.md;03-delivery-step-goals.md | SELECTED NXG-P01/02/03/05/06/07-01, NXG-RM10-01 | Phase/delivery binding, no M01=R1 |
| docs/goals/modules/15-planner.md;16-goals.md;17-habits.md | Goal rows+dependencies+source trace;15 bounded follow-up;16/17 visible bodies | P0 goal bindings; reminder outstanding |
| docs/goals/modules/06-notification-center.md;20-documents.md;24-organization-and-templates.md;25-search-favorites-and-command-palette.md;26-dashboard.md;29-news-and-feeds.md;33-github-discovery.md;39-career-and-resumes.md;40-learning-and-work-log.md | FULL bodies read in three bounded batches trong audit2026-10-17 | P1 exact goal rows/dependencies/AC trace; older “chưa approve” wording overridden by current014, not runtime acceptance |
| docs/ux-ui/global/15-screen-contracts.md | State matrix/keyboard/exit/dialog rules; SELECTED output some tail truncated | Shared UI state/recovery |
| docs/ux-ui/modules/15-planner.md;16-goals.md;17-habits.md | SELECTED screen/journey/lifecycle/dialog/response/integration; full long batches truncated, no full read claim | Week/day/picker/progress/schedule/dirty/focus |
| docs/ux-ui/modules/20-documents.md | SELECTED section inventory / S01–S10 headings | Document slice screen boundaries; body detail still story-read gate |
| docs/ux-ui/modules/24-organization-tags-templates.md;25-search-favorites-command-palette.md;26-dashboard.md;29-news-feeds.md;33-github-discovery.md;39-career-jobs-resume.md;40-learning.md | SELECTED actual screen IDs/overrides. Truncated middle not marked read | Source contracts + identified UX/catalog mismatch; not all screen inheritance acceptance |
| docs/design-database/05-productivity-calendar.md | SELECTED PlannerPin329/Goal354/Target382/Progress411/Habit434/Schedule464/CheckIn491; repetitive metadata output truncated | Data types/keys/history/owner FK; design not installed schema |
| docs/design-database/06-documents-knowledge-discovery.md | SELECTED Folder/Page/Version/Archive, Organization tables, SavedQuery/Recent/Dashboard; important field/keys/invariants read; partial broad outputs | Additive schema proposals/location/version/config |
| docs/design-database/08-news-shopping-developer.md | SELECTED Feed/Subscription/Article/State, developer Repository/RankingSnapshot/Entry/Saved; exact public/private split | Stored source schemas, no invented owner authority from cache |
| docs/design-database/10-assets-career-learning.md;17-owner-decision-delta.md | SELECTED ResumeVersion XOR/exact pin, SkillEvidence/Plan/WorkLog constraints, CertificationVersion invariants; CalendarLink new full table lines103–126 | Cross-module version/event source and schema reconciliation |
| docs/design-database/11-relations-and-transactions.md;15-field-classification.md | SELECTED transaction catalogue/cross-module distinction and classification entry; exhaustive FK index not all read | No distributed atomicity assumptions; not source of production schema truth |
| docs/design-database/classification/05-productivity-calendar.md;06-documents-knowledge-discovery.md;08-news-shopping-developer.md;10-assets-career-learning.md | SELECTED field rows private owner/public-source/Sensitive notes/CredentialIdEncrypted, Resume exact refs | No private DTO/receipt/Share/Support leakage; encryption field contract gate |
| docs/delivery/milestone-01/05-environment-and-runbook.md;06-readiness-and-evidence.md | SELECTED setup/verification/rollback/gates+S00–S11 evidence rows; not execute planned scripts | Environment and distinction historical Specified/current CI |
| docs/delivery/milestone-01/02-api-contracts.md;03-data-and-transactions.md;docs/delivery/09-pr4-local-module-implementation.md | SELECTED common wire/no-store/unknown-field/strong ETag/24h receipt and current continuation contract paragraphs, not whole long files | Keep current source API compatibility and don't use historical continuation result as current evidence |

## Source code inspected

| Source / revision | Method/position scope | Finding/evidence limit |
| --- | --- | --- |
| R SqlGoalService.cs | List32–85/Get87–96; Create99–186; Update187–258 signatures/receipt/commit flow; Progress260–356; Transition358–435; helpers443–490/588–612/650–679 | Keyless/checks/replay/fullDTO/NOLOCK/ETag counterexamples; no runtime exploit |
| R SqlHabitService.cs | List31/Get48/Create58–109; Update111–167; SetSchedule169–231; RecordCheckIn233–317; Transition319–364; capability366; receipt666–678 | Some batch middle truncated, CheckIn233–269 reread; postcommit detail and namespace/action distinctions proven source |
| R SqlPlannerService.cs | List30–64/Pin65–119; Update120–180 selected; Reorder182–229/Unpin231–264; source SQL273–365; rank408/ETag/receipt443–455 | Source payload vs availability/cleanup/atomic set/replay; source lock graph incomplete |
| L→R unchanged Application/{Goals,Habits,Planner} service contracts | FULL DTO/input/interface surfaces | Mutations typed full detail, read targets/date scopes |
| L→R unchanged Api/Features/{Goals,Habits,Planner} contracts/endpoints | Endpoint method/path/principal/binding/header/result mapping; endpoints full shown | ACK requires API mapping change not just service |
| L→R unchanged SqlRequestReceiptStore.cs | TryClaim/Complete/ReadExisting/TTL/HMAC/subject aliases/expiry; selected chunked reads | ResultJson existing; code-only legacy phân loại theo typed request digest/ResultCode: supported ACK/204 hoặc reconciliation khi thiếu binding/action proof; dual canonical reader needs review |
| L→R unchanged SqlSelfCapability.cs | Public overloads + EvaluateCore current session/account/owner/role/deps/Admin semantics | Same conn/TX overload exists; don't invent engine |
| L→R unchanged SqlProductivityService.cs | `MutateTask`493–555 selected + source GetEvent, CheckReceipt references; not all source writers | Observed Task-before-receipt path; cannot claim global lock order safe |
| L→R unchanged migrations0014/0023/0024; M01MigrationManifest | Selected Goal precision/UQ, Planner/Habit schemas/trigger/composite FK gates; manifest through0058 | No applied migration executed; no checksum changes |
| R App.tsx selected5300–6300 | Goals5343/Planner5565/Habits5740 methods, `.goal/.habit` and auto load, request key reset, current Finance recovered pattern | Actual consumer coupling; existing new shared UI retained |
| R api.ts1800–2020 | FULL bounded selected Planner/Habit/Goal functions | Full DTO generic mutation types/void Unpin; mutation union change planned |
| L→R unchanged P1 service interfaces+Program registration | Document/Organization/Search/Dashboard/Career/Skill/Course/NewsCategory contracts; Program338–377 | Missing registered workflows at API boundary, not filename-only gap |
| L→R unchanged LocalAccountMessageSink.cs | ACL/private temp/pending/final/restart methods, configured runtime/operator+current identity validation,1111–1150/1244–1388 selected | FullControl A/B policy, C token proof not implemented; not an independent security review |
| L→R unchanged tests LocalAccountMessageSinkTests/Program/TestRunner, filesystem wrapper | Register explicit Skip, actual require-os failure semantics, hold-capture support, existing ACL positive policy test | Source confirmed permanent actual-principal gate; no test execution |
| R workflow m01-ci.yml;start-ci-sql.sh;start-e2e-target.py | Workflow full body, SQL script full; browser script selected private target/ownership/keys/cleanup | CI-only requirements observed; no fake local CI env, no target provisioning |
| L package.json | FULL scripts/dependencies listing | Existing build/unit/coverage/e2e commands; no lint invented |

## Remote audit artifacts đã lấy qua connector

| Path trong `docs/implementation/full-product-rerun-20261016/` @ R | Git blob SHA / actual analysis |
| --- | --- |
| verification.md | `b86557912be7b2814902e49194d28e52d62cf359`; history/read, current CI checked separately |
| missing-capabilities.md | `5f0ffe109f8c1a46c0f0405b6aa3dd2279d190a1`; source/backlog and limits; not product authority |
| semantic-requirements.csv | `a01b6390981b3a92146f4b50f32c04a335afd865`; fetch_file trả content rỗng dù có SHA, fallback **fetch_blob** thành công;733 rows,721 pending action-specific input |
| requirements-declarations.csv | `849642d2692d04a9ed9e2371e8583649ca8887ce`; fetch_blob thành công,2028rows all UNVERIFIED_CURRENT |
| source-reading-ledger.csv | `036d446da8996bc69e25d24d7c8fd914c11661ca`;303sources historical read states; selected source rows/provenance seen, not pretend full semantic303 |
| semantic-source-rules.csv.gz | `77c61a0e3800ffa4535f1c0d853e3b148c93ec7d`; full base64 retrieved/decompressed,3901rows; schema+sample inspected, not3901semantic full read |
| source-reading-contract-index.csv.gz | `bbf7c55c59ac2b39d99fc3fe8e23e58195aa77d2`; decompressed17302rows includes metadata/reference; no normative denominator |
| requirements-extracted-unreviewed.csv.gz | `77e2619e78820ac52ad64a18befb7f42b7c07888`; decompressed11633rows; preserve unread classification |

Initial gzip request with start/end lines returned only base64prefix, deliberately not used for count/evidence. Full fetch repeated without range, separate temp cache, PowerShell GZipStream+CSV parse exit0. Python invocation failed because `python` unavailable; no success claimed for it. Only planning artifact files added to repo; remote source caches placed in task-specific OS temp, no source product overwrite.

Parsed semantic statuses:1 IMPLEMENTED_SOURCE_PENDING_ACCEPTANCE,63 LOCAL_SIMULATION_CONTRACT_REQUIRED_REAL_PROVIDER_DENIED,109 MISSING_LOCAL_WORKFLOW,3 MISSING_PROJECTION_CONTRACT_GATED,42 MISSING_SECURITY_CONTRACT_GATED,11 RETIRED_RUNTIME_DENIAL_UNVERIFIED,504 UNVERIFIED_CURRENT. These are inherited classifications, not all independently re-audited findings at R. No COMPLETE. Historical source ledger78full/164selected/61unread is inherited, not this task's fresh full-read totals.

## External technical references kiểm tra trong lượt plan

Chỉ primary Microsoft documentation dùng cho Windows launcher design, không grant execution/provisioning. [CreateProcessWithLogonW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createprocesswithlogonw): target identity must access executable/current directory; vì vậy staging/control traversal là required anti-false-positive. [New-LocalUser](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.localaccounts/new-localuser?view=powershell-5.1): name bounded20characters. Launcher selection/privilege support vẫn environment spike pending, không tuyên bố API đã chạy.

## Evidence limitations và next reading

Chưa chạy build/unit/API/SQL/browser/ACL test, migration hoặc Windows account provisioning; chưa mutate GitHub branch/commit/PR. Current CI job status được connector kiểm tra. Follow-up 2026-10-17 đọc existing log summaries: SQL/API219/0failed/0skipped, xUnit49/0/0, frontend143passed/1skipped/14files; Windows34discovered/30passed/0failed/4skipped và requiredWindows skip gây failure. Đây là đọc log đã tồn tại, không chạy lại tests. Log checkout xác nhận tested PR merge ref `c781cb13edc7b3c0ffc8bff4152f507e6c62114f`, PR head R/base M. Browser job success; exact10cases và local production4/dev2 giữ nhãn PR/report baseline vì chưa parse lại final browser ZIP JUnit. Artifact IDs SQL12052840284/backend12052795023/frontend12052251743/browser12052845315 đã quan sát qua connector, không giả đã đọc toàn ZIP. Source/security review performed ở đây là author investigation, **không independent review**.

Trước từng slice implementation, mở đầy đủ remaining paragraphs/physical fields/classification/inherited UX và current story để đóng exact input/output/error/lock/read-field gates. WP-RTM bao gồm source unread/selected remaining và40FX audit. Không certify full semantic RTM/full product bằng file hashes, decompression, route inventory hoặc source existence.
