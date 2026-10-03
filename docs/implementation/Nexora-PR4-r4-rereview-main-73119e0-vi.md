# Nexora PR #4 — R4 re-review và handoff

Ngày lập: 2026-09-14 (Asia/Ho_Chi_Minh)

Trạng thái: artifact reviewable trong worktree hiện tại. R4-01 và R4-02 đã
được sửa ở source và có focused synthetic test/build evidence; các gate SQL,
multi-principal thực tế, browser, CI và independent review chưa được đánh là
verified. Report này chưa đăng lên PR, chưa commit, chưa push, chưa merge và
chưa deploy.

## 1. Task brief, authority và gate

| Trường | Giá trị / evidence |
|---|---|
| Repository / PR | `hakodev2k/Nexora`, PR `#4` |
| Branch | `impl/m01-s00-scaffold` |
| Normative main | `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` |
| Reviewed implementation HEAD | `73119e04abc00a9b34831ca0cbe4ae6dcfbb678a` |
| HEAD parent / previous reviewed | `e12fc994d34801118b38b5ff306a468af3dd44b0` |
| Evidence revision của artifact này | `WT-R4-20260914`: `HEAD 73119e04…` + uncommitted R4 source/test/report delta; đây không phải git SHA đã publish |
| Metadata | Local `main`, branch và HEAD đã được đối chiếu theo SHA yêu cầu. `git ls-remote`/`git fetch --dry-run` tới GitHub không kết nối được port 443; không tuyên bố remote freshness |
| Approval dùng | `DEC-20260909-001` từ main: M01 `S00–S11`, backend/frontend scaffold, local scripts và synthetic local SQL/capture. Chỉ dẫn hiện tại ủy quyền sửa R4-01/R4-02 và verification trong bounded M01 slice |
| Approval không dùng | PR-only `DEC014`/code-only amendment không được dùng để mở post-M01 hoặc miễn targeted tests; docs trên PR chỉ là claims/proposals |
| Paused / blocked | Giữ `FX30/34/35` Paused; không real provider/OAuth/outbound, production secret/data, paid resource, public mailbox hoặc destructive external effect |
| M01 trace | `M01-S02`, `M01-S04`, `M01-S10`, `M01-S11`; `M01-AC02`, `M01-AC04`, `M01-AC10`, `M01-AC11` |
| Actions | `identity.account.register`, `identity.account.verify`, `identity.account.resend`, `identity.account.reset_request`, `identity.account.reset_confirm`, system `notifications.dispatch.deliver`; local CLI là operator path, không phải HTTP action |
| Goals | `NXG-FX01-G01/G02`, `NXG-FX06-G01/G02`; `NXG-SYS-05`, `NXG-SYS-06`, `NXG-SYS-08`, `NXG-SYS-10`, `NXG-SYS-14`, `NXG-SYS-15` |
| Affected consumers | `LocalAccountMessageSink` qua API DI/effect worker; `Nexora.Local` qua `ReadCaptured`; focused unit executable. Không đổi migration/schema |
| Runtime/tool availability | Windows PowerShell; .NET SDK `10.0.302`; Node `v24.17.0`; npm `11.13.0`; `sqlcmd` client; SQL Express service đang Running nhưng kết nối loopback bị SSPI/TLS block; browser runtime không có browser instance |
| Review gate | Independent security/concurrency/migration review chưa có trong session: `Pending`; self-review không được dùng thay thế |
| Unresolved gates | Real SQL/RCSI/receipt/lease tests, two-principal API/CLI process, Linux run, crash/restart, browser M01, final CI metadata và independent review |

Normative files đã đọc từ `main`, không dùng PR docs để thay nguồn: `AGENTS.md`,
engineering skill/routing/core rules, current scope/delivery contracts, PO
decisions, M01 stories/API/data/UX/environment/readiness/handoff/OpenAPI,
authorization/action catalog, shared UX, system/phase/delivery/module goals và
evidence template. `OwnerId` vẫn là `PersonalSpaceId`; `UserId` vẫn là actor
identity.

## 2. Executive disposition

| Finding | Phân loại tại baseline | Kết quả R4 |
|---|---|---|
| R4-01 | **Confirmed** bằng source review tại `73119e04…` | Source fixed + focused Windows test/build; actual API A/CLI B và principal C process test: **Not run / runtime blocked** |
| R4-02 | **Confirmed** bằng source review tại `73119e04…` | Source fixed + focused malformed/retention test/build; Linux run, crash/restart và long-idle process evidence: **Not run / runtime partial** |
| R4-03 | **Confirmed** là evidence defect của artifact cũ | Report hiện tại đã ghim đúng main/HEAD/evidence revision và tách status; CI/independent/runtime claims vẫn Pending, không gọi Fixed chỉ vì có report |

Không có finding nào bị gắn `Not reproduced` do thiếu SQL. Source/build/test
evidence dưới đây chỉ chứng minh đúng layer tương ứng; không thay thế SQL
Server, browser hoặc independent review.

## 3. R4-01 — Windows runtime/operator ACL

### Root cause

ACL policy cũ lấy caller hiện tại làm runtime SID trong mỗi lần khởi tạo/đọc.
API chạy principal A tạo ACL A+B khi operator B được cấu hình, nhưng CLI chạy
B tự tính lại policy từ B và không còn kỳ vọng ACE A. Đây là lỗi policy
construction, không phải lý do để mở Everyone hoặc chấp nhận ACE tùy ý.

### Source change

`src/Nexora.Infrastructure/Identity/LocalAccountMessageSink.cs`:

- `CreateWindowsAclPolicy`/`CreateWindowsAclPolicyCore` dựng một tập ổn định
  từ cấu hình `runtimeSid` và `operatorSid`, deduplicate và parse SID fail
  closed.
- `ValidateCurrentWindowsIdentity`/`ValidateCurrentWindowsIdentityCore` tách
  caller validation khỏi ACL policy: caller hiện tại chỉ được là một SID trong
  tập trusted.
- `Restrict*Permissions` và `Validate*Permissions` chỉ dùng policy set; ACL
  Windows được protected, không inherited, chỉ có Allow FullControl cho các
  SID đã cấu hình. Inherited ACE, Deny hoặc ACE ngoài set bị reject.
- API đọc `Nexora:LocalAccountMessageRuntimeSid` hoặc
  `NEXORA_LOCAL_MESSAGE_RUNTIME_SID` trong `src/Nexora.Api/Program.cs`.
- CLI truyền cùng cặp `NEXORA_LOCAL_MESSAGE_RUNTIME_SID` và
  `NEXORA_LOCAL_MESSAGE_OPERATOR_SID` trong `src/Nexora.Local/Program.cs`.
- Nếu `operatorSid` được cấu hình mà `runtimeSid` thiếu/invalid thì startup và
  read path fail closed. Khi không cấu hình operator, runtime caller hiện tại
  vẫn là compatibility fallback; hai process A/B phải cùng được cấp cặp config
  ổn định.

### Acceptance mapping

| Case | Status / evidence |
|---|---|
| API A / CLI B cùng hoạt động | Source policy đã hỗ trợ và focused test kiểm tra ACL chứa cả runtime/operator; chạy hai process với hai principal thực: **Not run**, host chưa có second-principal harness |
| A = B | Focused Windows test chạy trên host hiện tại với cùng SID, pass |
| Principal C không đọc | Focused test truyền sibling SID ngoài policy và nhận `InvalidOperationException`, pass; đọc bằng process C thật: **Not run** |
| Unsafe inherited ACL bị reject | Focused test tạo directory inherited và constructor reject, pass |
| Restart giữ behavior | Source config/explicit ACL ổn định; process restart thật: **Not run** |
| Không dùng token thật | Test chỉ dùng capture và token synthetic; report không chứa raw token |

## 4. R4-02 — malformed capture retention

### Root cause và source change

`ReadCapture` parse failure trước đây bị catch rồi bỏ qua ở các caller, nên
malformed final/pending không đi vào cleanup policy. Source hiện tại:

- `ReadCapture` reject JSON root không phải object bằng `JsonException`, cùng
  loại với truncated/shape-malformed JSON.
- `ReadCaptured`, `SweepExpiredCore` và `ReconcilePending` chỉ cleanup
  malformed file khi thỏa ownership-by-name và age grace. Final capture phải có
  tên `<message-guid>.json`; pending/temp phải có dạng
  `.<message-guid>.<effect-fence-guid>.(pending|tmp)`. Tên foreign không bị
  xóa.
- `MalformedCaptureGrace` là 24 giờ; orphan temp grace là 15 phút; pending
  retention là 24 giờ. Không có bulk delete tùy ý.
- Pending reconciliation kiểm tra exact message ID/fence trong filename và
  payload trước khi resolve/promote/remove.
- File handle đọc được đóng trước khi đưa vào delete list. Temp writer giữ
  advisory lock ngoài `FileShare.None`; cleanup thử lấy lock sau khi mở bằng
  `FileShare.ReadWrite`, fail closed nếu file đang bận hoặc platform lock
  không hỗ trợ.
- Worker gọi `SweepExpired` trước SQL claim, nên retention không phụ thuộc có
  message mới; `ReconcilePending` xử lý owned pending sau startup khi worker
  chạy. Cleanup failure chỉ phát generic warning, không ghi path/token/content.

### Acceptance mapping

| Case | Status / evidence |
|---|---|
| Truncated final JSON trước/sau grace | Focused test: file gần đây giữ nguyên, file cũ owned bị xóa, pass |
| Non-object malformed JSON | Focused test với old `[]`, pass |
| Malformed pending trước/sau grace | Focused test qua `ReconcilePending`, recent giữ; old xóa, pass |
| File tên không thuộc hệ thống | Focused test rename thành foreign name, malformed old file vẫn giữ, pass |
| File đang được ghi | Windows active temp handle giữ file; sau release sweep được, pass. Linux external-writer process: **Not run** |
| Idle và restart | Test khởi tạo sink thứ hai rồi reconcile/sweep explicit, pass ở code path; full API worker restart/idle runtime: **Not run** |
| Windows/Linux profile | Windows test đã chạy; Unix mode test bị OS guard trên Windows nên không phải Linux runtime evidence; Linux run: **Not run** |
| Cleanup failure observable | Source generic logger/stderr path, không leak; fault-injected failure: **Not run** |

### Traceability matrix

| Source | Story / AC / action | Goal | Case | Evidence hiện tại |
|---|---|---|---|---|
| `LocalAccountMessageSink.CreateWindowsAclPolicy*`, `ValidateCurrentWindowsIdentity*`, `Restrict/Validate*Permissions` | `M01-S10/S11`, `M01-AC10/AC11`; system `notifications.dispatch.deliver`; operator local capture path | `NXG-SYS-06`, `NXG-SYS-08`, `NXG-SYS-10`, `NXG-SYS-15`; `NXG-FX06-G01/G02` | API A/CLI B, A=B, C deny, inherited ACL reject, restart | Windows focused test + Release build pass; real two-principal/restart pending |
| `src/Nexora.Api/Program.cs`, `src/Nexora.Local/Program.cs` | `M01-S02/S04/S10/S11`, `M01-AC02/AC04/AC10/AC11`; `identity.account.register/verify/resend/reset_request/reset_confirm` | `NXG-FX01-G01/G02`, `NXG-SYS-05/06/14/15` | Same trusted runtime/operator config across API and CLI; no public transport | API/Local build pass; process E2E and SQL delivery pending |
| `ReadCapture`, `ReadCaptured`, `SweepExpiredCore`, `ReconcilePending`, `TryDelete`, lock helpers | `M01-S10/S11`, `M01-AC10/AC11`; durable system delivery, no user grant | `NXG-SYS-06`, `NXG-SYS-08`, `NXG-SYS-14/15`; `NXG-FX06-G02` | Truncated/non-object/malformed pending, foreign name, age grace, active handle, idle/restart | 29/29 unit run pass; Linux/crash/fault-injection/SQL runtime pending |
| `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs` | `M01-AC10/AC11`; test evidence only, không tạo product action | `NXG-SYS-14/15` | Synthetic isolated capture/ACL/mode/retention invariants | Windows host run exit 0; Unix branch OS-skipped, no real token |
| Current R4 report và verification log | `M01-S11`, `M01-AC11`; no HTTP action | `NXG-SYS-14`, `NXG-SYS-15`, `NXG-SYS-16` | Exact SHA/revision, source vs runtime vs pending, no stale CI claim | Artifact này; uncommitted worktree, CI/independent review pending |

## 5. R4-03 — evidence và tests

Artifact cũ `Nexora-PR4-rereview-main-e12fc99-vi.md` giữ vai trò lịch sử,
không phải evidence hiện tại. Report này ghi rõ:

- baseline normative `main=8782f46…`, reviewed HEAD `73119e04…`, parent
  `e12fc994…` và worktree evidence revision không phải commit;
- source-fixed, build-verified, focused-test-passed, runtime-not-run,
  scope-blocked và independent-review-pending là các trạng thái khác nhau;
- không lấy PR-only code-only amendment làm căn cứ miễn test;
- không điền CI run/SHA chưa truy vấn được; remote/CI metadata hiện là
  `Pending` do network/connector không khả dụng;
- PR description chỉ là draft ở cuối report, chưa đăng.

Focused tests được thêm tại `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs`
và test project tham chiếu Infrastructure. Chúng dùng thư mục tạm riêng, token
synthetic và không tạo demo record/production data. 29/29 custom unit tests pass
trên Windows; test Unix mode chỉ là no-op do OS guard và không được tính là
Linux verification.

## 6. Files/symbols và compatibility

| File | Symbols / thay đổi |
|---|---|
| `src/Nexora.Infrastructure/Identity/LocalAccountMessageSink.cs` | Stable Windows ACL policy, caller validation, malformed JSON ownership/age cleanup, pending identity binding, cross-platform temp locking, parser root validation, generic cleanup observation |
| `src/Nexora.Api/Program.cs` | Đọc `LocalAccountMessageRuntimeSid` config/env và truyền vào sink |
| `src/Nexora.Local/Program.cs` | CLI truyền runtime/operator SID pair vào `ReadCaptured` |
| `tests/Nexora.UnitTests/Nexora.UnitTests.csproj` | Thêm Infrastructure reference |
| `tests/Nexora.UnitTests/Program.cs` | Register focused sink tests |
| `tests/Nexora.UnitTests/LocalAccountMessageSinkTests.cs` | ACL, malformed retention, restart/reconcile, active temp, Unix-mode focused cases |
| `docs/implementation/Nexora-PR4-r4-rereview-main-73119e0-vi.md` | Current unposted handoff artifact |

Constructor và `ReadCaptured` nhận `runtimeSid` ở cuối sau optional
`operatorSid`, giữ source compatibility cho caller positional cũ. Trên Windows,
deployment API/CLI cần cấu hình cùng trusted runtime/operator SID set; thiếu
runtime khi đã cấu hình operator sẽ fail closed thay vì tự suy caller mới.

Capture format fenced flat trước đó vẫn giữ: field `DeliveryFence` là additive,
CLI cũ có thể deserialize các trường message; capture legacy direct vẫn được
đọc nhưng không được dùng làm fenced ownership cho promote/remove. Không thêm
migration, không đổi migration checksum, không reset receipt/secret/TTL và
không xóa dữ liệu để làm test xanh.

## 7. Disposition R3-01–R3-09

| ID | Disposition hiện tại | Source/evidence | Runtime gate |
|---|---|---|---|
| R3-01 | Source fixed, build/test verified; SQL runtime Not run | `AccountMessageDeliveryWorker.Claim` bỏ `ROWLOCK` xung đột, giữ `ReadCommitted` + `UPDLOCK/READPAST/READCOMMITTEDLOCK` | SQL Server RCSI OFF/ON, two-worker claim, register→capture→verify/reset |
| R3-02 | Source fixed, build/test verified; upgrade SQL Not run | `SqlRequestReceiptStore` dual-read old/new authenticated namespace, signed anonymous binding giữ riêng | old receipt từ version trước, same key/body one effect + safe replay, body khác `409` |
| R3-03 | Source fixed, build/test verified; readiness SQL Not run | Required migrations yêu cầu `0025/0026`; `/health/live` vẫn process-only | DB dừng 0024/0025 NotReady, 0026 Ready, no secret response |
| R3-04 | Source fixed, focused policy test/build verified; SQL mutation Not run | `AdminGrantable` manifest gate áp dụng grant mutation | register/dispatch/role.set reject, preference read/update allow, stale row không thành authority |
| R3-05 | Source fixed, build/test verified; race runtime Not run | Effect lease/JIT authority, exact fence pending/promote/reconcile | pause A, reclaim/terminal B, revoke/resend/consume trước effect |
| R3-06 | Source fixed và được gia cố bởi R4-01; Windows same-principal test pass, Linux/two-principal Not run | Stable ACL policy, protected ACL/mode, no Everyone | API A/CLI B, C deny, inherited reject, restart |
| R3-07 | Source fixed và được gia cố bởi R4-02; focused retention test pass; full runtime Not run | bounded idle cleanup, exact ownership, handle/lock boundary | Windows/Linux expire, idle, temp crash/restart, cleanup failure |
| R3-08 | Source fixed, frontend build verified; browser Not run | Sessions dùng profile IANA timezone | browser timezone khác profile, vi/en, reload |
| R3-09 | Partial: current source/evidence artifact corrected; CI/independent/runtime Pending | Current matrix không kế thừa stale revision/PR-only exemption | final CI exact SHA, committed artifact, independent review |

R3-01 dùng quy tắc table hints chính thức của SQL Server; việc source/build pass
không thay thế fixture SQL thật: [Microsoft Learn — Table hints
(Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table?view=sql-server-ver17).

## 8. R2-01–R2-24 history disposition

Các mục ngoài M01 không được mở scope trong lượt này. Những mục `Source fixed`
giữ nguyên sửa đúng của các vòng trước; không re-open chỉ vì lượt R4 không chạm
file đó.

| ID | Trạng thái giữ lại | Kết luận tại R4 |
|---|---|---|
| R2-01 | Một phần | Main/M01 là authority; PR-only amendment vẫn không có giá trị mở scope; R4-03 evidence hiện tại tách rõ |
| R2-02 | Một phần | Admin SELF Allow/Deny helper đã cải thiện; grant eligibility là R3-04; SQL/race còn pending |
| R2-03 | Một phần | Helper bỏ `RegistrationEnabled` khỏi current-user auth; direct Reminder consumer còn |
| R2-04 | Còn mở | Reminder/Files `Serializable+READPAST` ngoài repair này; identity worker riêng đã xử lý R3-01 |
| R2-05 | Còn mở | Final-attempt crash của Reminder/Files chưa sửa |
| R2-06 | Còn mở | Task reconcile retry/lease chưa sửa |
| R2-07 | Còn mở | Task new reminder quá khứ chưa sửa |
| R2-08 | Còn mở | Document dirty transition chưa sửa |
| R2-09 | Còn mở | Favorite UUID/router compatibility chưa sửa |
| R2-10 | Còn mở | Local timezone one-offset/DST chưa sửa |
| R2-11 | Còn mở | Calendar anchor/navigation chưa sửa |
| R2-12 | Còn mở | Task Project picker pagination chưa nối |
| R2-13 | Còn mở | Sharing nullable Task Priority consumer còn gated |
| R2-14 | Một phần | Admin self-demotion tốt hơn; Task/Document mutation-read còn |
| R2-15 | Một phần | Identity/admin safe replay và receipt upgrade source đã xử lý; Trash/business replay còn |
| R2-16 | Còn mở | Files upload attempt/recovery qua crash còn gated |
| R2-17 | Còn mở | Task status trong Calendar projection chưa giữ riêng |
| R2-18 | Một phần | M01 locale/theme/timezone source cải thiện; browser/full catalog còn |
| R2-19 | Một phần | Writer ID đúng hơn; legacy audit inventory/correction chưa có |
| R2-20 | Một phần | Local envelope/worker/capture được gia cố; SQL/ACL/retention/runtime delivery còn gate |
| R2-21 | Còn mở | Cross-module SQL/App boundaries còn |
| R2-22 | Một phần | Current artifact đã ghim revision; CI/runtime/independent evidence còn |
| R2-23 | Source fixed | Giữ IdentityV3/220000, bounded legacy rehash, `SuccessRehashNeeded`; benchmark/runtime pending |
| R2-24 | Source fixed | Giữ signed anonymous-session binding + stable configured secret; browser/race pending; authenticated upgrade là R3-02 |

## 9. F01–F22 history riêng

| ID | Disposition hiện tại | Giới hạn / case còn lại |
|---|---|---|
| F01 | Confirmed; post-M01 scope blocked | Trash/business replay và read authority |
| F02 | Source fixed tại `6d5b58d`; preserved; runtime pending | Identity password/reset flows |
| F03 | Source fixed tại `6d5b58d`; preserved; runtime pending | Auth/session authority |
| F04 | Confirmed; post-M01 scope blocked | Reminder/task expired input |
| F05 | Confirmed; post-M01 scope blocked | Document draft transition |
| F06 | Partial | M01 identity current guard; Reminder direct consumer còn |
| F07 | Confirmed; post-M01 scope blocked | Favorites route canonicalization |
| F08 | Confirmed; post-M01 scope blocked | Files upload crash/recovery |
| F09 | Source fixed/extended; runtime pending | Local capture worker/fence/ACL/retention; không real provider |
| F10 | Partial | Authenticated receipt namespace source fixed; Trash/business replay còn |
| F11 | Source fixed; runtime pending | Identity/session security path |
| F12 | M01-compatible; post-M01 mismatch blocked | Gated module boundary |
| F13 | Core source fixed; nullable consumer blocked | Sharing Task Priority |
| F14 | Confirmed; post-M01 scope blocked | Calendar/timezone/DST |
| F15 | Confirmed; post-M01 scope blocked | Favorite typed route |
| F16 | Partial source fixed; runtime pending | M01 vi/en/theme/formatter |
| F17 | Confirmed; post-M01 scope blocked | Calendar navigation/projection |
| F18 | Confirmed; post-M01 scope blocked | Modular boundary/consumer contracts |
| F19 | Partial | Audit writer ID improved; legacy inventory unresolved |
| F20 | Partial/source evidence corrected here | Current report không thay CI/independent/runtime evidence |
| F21 | Partial | M01 local capture cleanup source fixed; post-M01 durable file cleanup blocked |
| F22 | Partial; post-M01 scope blocked | Task reminder reconcile/status |

F02/F03 không bị ghi lại thành “chưa implement”. R2-23/R2-24 cũng không bị
viết lại mù theo prompt vòng trước.

## 10. Verification log

Các command dưới đây chạy trong worktree hiện tại sau source change cuối; build
chạy tuần tự để tránh khóa output dùng chung.

| Command / case | Exit / status | Evidence và giới hạn |
|---|---:|---|
| `git rev-parse HEAD` | `0` | `73119e04abc00a9b34831ca0cbe4ae6dcfbb678a` |
| `git rev-parse main` | `0` | `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` |
| `git rev-parse HEAD^` | `0` | `e12fc994d34801118b38b5ff306a468af3dd44b0` |
| `git branch --show-current` | `0` | `impl/m01-s00-scaffold` |
| `git ls-remote origin refs/heads/main refs/heads/impl/m01-s00-scaffold` | Failed | GitHub port 443 unavailable; no remote/CI freshness claim |
| `git fetch --dry-run origin` | Failed | Same network limitation; local refs remain pinned, no reset/overwrite |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release --no-restore` | `0` | 0 warning, 0 error |
| `dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release --no-restore` | `0` | 0 warning, 0 error |
| `dotnet build src/Nexora.Local/Nexora.Local.csproj --configuration Release --no-restore` | `0` | 0 warning, 0 error |
| `dotnet build tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --no-restore` | `0` | 0 warning, 0 error; includes final Infrastructure source |
| `dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --no-build` | `0` | `Executed 29 unit tests; failed 0`; Windows ACL/common retention executed, Unix branch OS-skipped |
| `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --no-restore` | `0` | Harness compiles, 0 warning, 0 error |
| SQL integration harness với loopback `.\SQLEXPRESS`, `Integrated Security=True`, `Encrypt=False`, database name `Nexora_Test_<guid>` | `1` | SQL connection failed với `SqlException`; no credential logged, no SQL R4 evidence |
| `sqlcmd` loopback read-only `SELECT @@VERSION` | Failed | ODBC 17 reports encryption/SSPI credential setup failure; service availability không đủ làm SQL runtime pass |
| `npm run build` tại `web/Nexora.Web` | `0` | `tsc -b` + Vite `6.4.3`, 31 modules; không phải browser QA |
| Browser runtime setup + `agent.browsers.list()` | `[]` | Không có browser instance; Sessions/browser journeys và screenshot: Not run |
| `git diff --check` | `0` | Không có whitespace error; chỉ có line-ending warning LF→CRLF của Git |
| Focused ACL/parser/retention tests | Included above | Synthetic temp files only; no real token/secret; actual second Windows principal/Linux host chưa chạy |
| Real SQL receipt old→new, RCSI/hints, readiness 0024/25→26, two-worker lease/revoke/crash | **Not run / Blocked by runtime** | SQL connection/fixture unavailable; không dùng in-memory thay thế |
| Full API register→capture→verify/reset, crash after prepare/Delivered/promotion | **Not run** | Requires real isolated SQL + worker process timing |
| Browser vi/en/profile timezone/theme/CSRF/draft/keyboard | **Not run** | Browser unavailable; không fake screenshot |
| CI final SHA/check run | **Pending** | Remote/CI connector unavailable; không kế thừa stale PR claims |
| Independent review | **Pending** | Chưa có reviewer độc lập; không self-approve |

## 11. Migration, recovery, compatibility và rollback

- Không tạo/chỉnh migration. Readiness source fix trước đó vẫn yêu cầu schema
  `0025/0026`; migration runner và readiness runtime chưa chạy.
- Config Windows phải được triển khai cùng trusted runtime/operator SID set cho
  API và CLI. Với API A/CLI B, cả hai process đọc cùng config A+B; khi A=B,
  một SID là đủ. Config thiếu runtime khi có operator intentionally fail closed.
- Capture mới giữ flat additive `DeliveryFence`; legacy capture chỉ giữ read
  compatibility, không được nhận fenced ownership. Pending/temp ownership luôn
  bind exact filename ID/fence và bounded age.
- Sau crash trước promotion, worker cần SQL state + exact fence để reconcile;
  expired/malformed orphan chỉ bị dọn theo naming/age/lock policy. Runtime
  crash injection chưa chạy.
- Khi app idle, worker sweep chạy trước SQL claim; nếu SQL unavailable, sweep
  vẫn nằm trước bước mở SQL. Điều này là source behavior, chưa là process
  restart evidence.
- Rollback hiện an toàn nhất ở reviewable uncommitted diff; không dùng hard
  reset/checkout để ghi đè user work. Không drop DB, xóa receipt, reset secret,
  blanket-delete capture hoặc rollback migration history.

## 12. PR description draft — chưa đăng

> R4 M01 local repair trên `impl/m01-s00-scaffold`, normative main
> `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`, reviewed implementation HEAD
> `73119e04abc00a9b34831ca0cbe4ae6dcfbb678a`. Sửa Windows local-capture ACL
> policy để dùng stable runtime/operator identity set và tách caller validation;
> bổ sung fail-closed inherited/foreign ACL handling. Sửa malformed final/
> pending capture retention theo exact ownership naming, bounded age, closed
> handles và cross-platform temp locking; cleanup failure không lộ content.
> Thêm focused synthetic tests. API/Bootstrap/Local/UnitTests Release build và
> frontend TypeScript/Vite build exit 0 với 0 warning/0 error; unit executable
> 29/29 pass trên Windows host. SQL Server R4 fixtures, two-principal process
> run, Linux runtime, browser, final CI metadata và independent review còn
> Pending/Not run do runtime/connector limits. R2/R3/post-M01/paused
> dispositions không được mở rộng trong change này. Chưa merge, chưa push main,
> chưa deploy.

## 13. Handoff / Definition of Done

Đã hoàn tất phần M01 được phép và khả dụng trong source/build/focused synthetic
verification cho R4-01/R4-02. Handoff chưa được gắn nhãn `Runtime verified` hoặc
`R1 complete`, vì các gate thực tế còn lại là:

1. chạy SQL Server isolated với receipt upgrade, RCSI/hints, readiness,
   two-worker claim/effect fence, revoke/resend/consume và crash boundaries;
2. chạy Windows API A/CLI B/principal C bằng hai principals thật, inherited ACL
   và restart; chạy Linux mode/lock/cleanup profile;
3. chạy browser M01 Sessions/profile timezone, locale/reload và các regression
   journeys với evidence thật;
4. gắn evidence vào một commit SHA cuối, truy vấn CI đúng SHA và có independent
   security/concurrency/migration review.

Các backlog R2/F ngoài M01 — Documents dirty transition, Favorites route,
Project picker, Calendar DST/navigation/status, nullable Sharing Priority,
Reminder/Files queues, mutation-read/replay, audit legacy và module boundaries
— vẫn mở hoặc scope-blocked theo các bảng trên; không bị gọi là Fixed trong lượt
này.

Không merge, auto-merge, merge queue, approve thay reviewer, bypass checks,
push main, force-push hoặc deploy đã được thực hiện.
