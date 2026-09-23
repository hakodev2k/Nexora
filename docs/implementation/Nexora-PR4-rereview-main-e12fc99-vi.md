# Nexora PR #4 — báo cáo re-review vòng 3

Ngày lập: 2026-09-14 (Asia/Ho_Chi_Minh)

Trạng thái: báo cáo bàn giao reviewable cho working tree hiện tại; chưa đăng comment/review/PR description, chưa commit, chưa push, chưa merge, chưa deploy.

## 1. Task brief và authority

| Trường | Giá trị/evidence |
|---|---|
| Repository/PR | hakodev2k/Nexora, PR #4 |
| Branch | impl/m01-s00-scaffold |
| main chuẩn | 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3 |
| HEAD thực tế tại máy | e12fc994d34801118b38b5ff306a468af3dd44b0 |
| Previous reviewed | 6d5b58dbac330b0d0d9aceb9e0b2f4695b423316 |
| Working tree | Có thay đổi chưa commit do vòng sửa này; không có thay đổi nào bị reset/ghi đè |
| Authority chuẩn | AGENTS, requirements, delivery contracts, UX/AC, actions và goals được đọc từ main tại SHA 8782f46; M01 S00–S11 và scaffold/local scripts là phạm vi main phê duyệt theo DEC-20260909-001 |
| Authorization áp dụng | Chỉ sửa vertical slice M01 local đã được người dùng ủy quyền; chỉ dẫn hiện tại nhắc DEC-20260909-014 cho local E2E nhưng không dùng tài liệu chỉ có trên PR để mở post-M01 hoặc mở FX30/34/35 |
| Amendment phiên này | Code-only: không thêm test, fixture, mock/demo record; functional QA, test-data design và runtime verification do human owner thực hiện |
| Scope gate | FX30/34/35 vẫn Paused; real provider/OAuth/outbound, production secret/data, paid resource, public mailbox và destructive external effect không thực hiện |
| Runtime/tools | Windows; .NET SDK 10.0.302; Node v24.17.0; npm 11.13.0; sqlcmd client có sẵn. Không chạy SQL fixture, browser QA hoặc provider runtime trong amendment code-only |
| Review gate | Không có independent reviewer trong session; independent security/concurrency review là Pending, không được thay bằng self-review |

Remote metadata đã được thử đọc bằng git ls-remote nhưng GitHub port 443 không khả dụng. Vì vậy các SHA ở trên là local refs đã ghim và đối chiếu với yêu cầu; không tuyên bố đã fetch được remote hiện tại.

Rules/skills đã áp dụng: .agents/skills/nexora-engineering/SKILL.md; .ai/profiles/nexora-implementation-agent.md; .ai/roles/technical-lead/README.md và core-rules; .ai/verification.md; rules/skills về modular monolith, owner isolation, authorization, background processing, SQL migration/isolation/concurrency, security/secrets/privacy, React/TypeScript/accessibility và evidence/contract testing.

## 2. Kết luận điều hành

R3-01 đến R3-08 đã có source fix trong working tree; R3-09 được thay bằng artifact hiện tại ghim main/HEAD và không kế thừa claim code-only cũ. API, Bootstrap, Local đều Release build thành công với 0 warning/0 error khi chạy tuần tự; frontend TypeScript/Vite build thành công.

Chưa có runtime evidence để kết luận SQL Server, RCSI, hai-worker fencing, receipt upgrade, Windows ACL, idle/crash retention, identity/CSRF hoặc browser M01 đã pass. Không thêm hoặc chạy test theo amendment. Do đó source fixed không đồng nghĩa runtime verified.

Không có migration file nào được sửa hoặc thêm. Readiness chỉ được nâng required journal lên các migration đã tồn tại 0025 và 0026; migration runner chưa chạy.

## 3. R3 disposition

| ID | Priority | Disposition hiện tại | Source/symbol đã sửa | Traceability và case | Evidence |
|---|---:|---|---|---|---|
| R3-01 | P1 | Confirmed tại baseline; Source fixed, build verified; SQL runtime Not run | AccountMessageDeliveryWorker.Claim bỏ ROWLOCK xung đột, giữ ReadCommitted với UPDLOCK/READPAST/READCOMMITTEDLOCK | M01-S10, M01-AC10, notifications.dispatch.deliver; NXG-M01-05, NXG-SYS-05/06/15; cần RCSI OFF/ON, hai worker, register/capture/verify và reset | Code diff tại HEAD hiện tại; API/Local build 0/0; chưa có SQL evidence |
| R3-02 | P1 | Confirmed tại baseline; Source fixed, build verified; upgrade runtime Not run | SqlRequestReceiptStore ghi legacy authenticated namespace và dual-read namespace mới trong cùng transaction/TTL; anonymous signed binding không bị dùng chung lại | M01-S02/S03/S04, AC02/03/04, identity register/verify/resend/reset; NXG-SYS-03/05/06/15; cần receipt từ 6d5b58d retry cùng secret/user/key/body, body khác phải 409 | Các caller dùng helper chung; chưa có SQL upgrade evidence |
| R3-03 | P1 | Confirmed tại baseline; Source fixed tối thiểu theo schema code mới, build verified; readiness runtime Not run | SqlReadinessProbe.RequiredMigrations yêu cầu 20260913_0025_review_hardening.sql và 20260913_0026_identity_local_delivery.sql | M01-S00/S10, M01-AC10/AC11; NXG-SYS-05/14/15; DB journal dừng 0024/0025 phải NotReady, 0026 mới Ready; live vẫn process-only | Source list hiện yêu cầu 0026; chưa chạy DB/readiness |
| R3-04 | P2 | Confirmed tại baseline; Source fixed, build verified; policy SQL runtime Not run | ActionGrantPolicy.CanGrantAllow dùng manifest AdminGrantable; SqlAdminAccessService.SetActionGrant áp dụng cho cả Allow và Deny | M01-S08, M01-AC08, access.permission.set; NXG-FX02-G02/G03, NXG-SYS-03; register/dispatch/role.set bị từ chối, preference read/update hợp lệ | Source policy và mutation gate; chưa có SQL negative/positive case |
| R3-05 | P2 | Confirmed tại baseline; source effect-boundary fix implemented, build verified; concurrency/runtime Not run | Worker có AcquireEffect và IsCurrentEffect JIT recheck; LocalAccountMessageEffectSink dùng fence, pending file, promote/reconcile exact ownership; không giữ SQL transaction qua file I/O | M01-S10, M01-AC10, notifications.dispatch.deliver; NXG-SYS-05/06/08/15; pause A, reclaim/terminal B, revoke/resend/consume trước effect | Worker/sink source; chưa chứng minh race boundary bằng SQL/filesystem |
| R3-06 | P2 | Confirmed risk tại baseline; Source fixed fail-closed, build verified; Windows/Linux ACL Not run | LocalAccountMessageSink validate/restrict ACL hoặc Unix mode, reject inherited/readable unsafe directory, optional operator SID; CLI dùng cùng validation | M01-S10, M01-AC10, local capture; NXG-FX06-G02/G03, NXG-SYS-06/08; hai principals và inherited-readable directory phải reject/không đọc được | Source ACL path; chưa có Windows two-principal hoặc Linux mode run |
| R3-07 | P2 | Confirmed risk tại baseline; Source fixed bounded cleanup, build verified; idle/crash runtime Not run | SweepExpired độc lập SQL traffic; đóng handle trước delete; dọn owned expired JSON, pending và temp theo age/naming; worker sweep khi idle | M01-S10, M01-AC10; NXG-FX06-G02/G03, NXG-SYS-06/08/15; expired JSON, idle app, kill sau temp write/restart | Source cleanup/recovery path; chưa chạy Windows delete/kill/restart |
| R3-08 | P2 | Confirmed tại baseline; Source fixed, frontend build verified; browser Not run | SecurityScreen nhận profile.timeZoneId; created/lastSeen/expires dùng IANA profile timezone, locale chỉ đổi format | M01-S05/S06, M01-AC05/AC06, identity.profile.read/update và identity.session.read; NXG-FX01-G02, NXG-FX09-G02/G03, NXG-SYS-11; browser timezone khác profile, vi/en, reload | npm run build exit 0; chưa có browser screenshots/QA |
| R3-09 | P2 | Evidence source fixed trong artifact này; test/runtime/CI/independent review Pending | Báo cáo mới ghim main/HEAD/previous head, tách source/build khỏi runtime, sửa disposition R2/F; không dùng PR-only code-only amendment | M01-S00–S11, M01-AC11; NXG-SYS-14; mỗi claim phải có SHA/case/command/status; CI #164 chưa được truy vấn độc lập | File báo cáo này; không đăng PR, không claim CI/SQL/browser pass |

Đối chiếu chính thức cho R3-01: [Microsoft Learn — Table Hints (Transact-SQL)](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table?view=sql-server-ver17) phân loại READCOMMITTEDLOCK và ROWLOCK cùng nhóm granularity, giới hạn READPAST theo isolation/RCSI, và nêu READCOMMITTEDLOCK là lựa chọn khi RCSI bật với READ COMMITTED.

## 4. Files, consumers và compatibility

| File | Thay đổi reviewable |
|---|---|
| src/Nexora.Infrastructure/Identity/AccountMessageDeliveryWorker.cs | Claim hint hợp lệ; effect lease ngắn; authority recheck; fenced completion; pending reconciliation; terminal recovery |
| src/Nexora.Application/Identity/IdentityServiceContracts.cs | IAccountMessageEffectSink và outcome/disposition contract; IAccountMessageSink cũ giữ cho source compatibility nhưng local implementation từ chối publish không fence |
| src/Nexora.Infrastructure/Identity/LocalAccountMessageSink.cs | ACL/mode fail-closed; flat fenced capture backward-readable với capture cũ; pending/promote/remove exact fence; idle retention/recovery |
| src/Nexora.Infrastructure/Identity/SqlRequestReceiptStore.cs | Authenticated old/new SubjectHash dual-read trong TTL, ghi format operation-qualified; anonymous signed binding giữ namespace riêng |
| src/Nexora.Api/Program.cs | Đăng ký effect sink singleton và optional operator SID từ cấu hình/env |
| src/Nexora.Local/Program.cs | CLI read capture áp dụng operator SID validation |
| src/Nexora.Infrastructure/Persistence/SqlReadinessProbe.cs | RequiredMigrations thêm 0025 và 0026; health response vẫn coarse |
| src/Nexora.Domain/Access/ActionGrantPolicy.cs | Manifest AdminGrantable explicit, không suy từ namespace/verb |
| src/Nexora.Infrastructure/Access/SqlAdminAccessService.cs | Gating grant mutation cho Allow và Deny bằng trusted manifest |
| web/Nexora.Web/src/App.tsx | Sessions hiển thị bằng IANA timezone từ profile |

Tất cả caller của SqlRequestReceiptStore đều tiếp tục đi qua helper chung; không có caller mới của unfenced local Publish. Capture format fenced mới là flat additive field để CLI/code cũ vẫn đọc được các trường LocalAccountMessage; parser mới vẫn đọc capture direct cũ nhưng không nhận capture cũ là fenced ownership khi promote/remove.

Migration 20260913_0026_identity_local_delivery.sql và các migration lịch sử không bị reformat/chỉnh sửa. Không đổi OwnerId semantics: OwnerId vẫn PersonalSpaceId, actor identity vẫn UserId.

## 5. R2 disposition được giữ nguyên

| ID | Trạng thái hiện tại | Kết luận tại HEAD/evidence vòng này |
|---|---|---|
| R2-01 | Một phần | Main/M01 được chọn; PR-only DEC014/code-only claims không phải authority; R3-09 đã lập artifact mới |
| R2-02 | Một phần | Admin SELF Allow/Deny helper đã cải thiện; grant eligibility là R3-04; SQL/race runtime còn pending |
| R2-03 | Một phần | Helper không dùng RegistrationEnabled để thu hồi user hiện có; direct Reminder consumer vẫn còn |
| R2-04 | Còn mở | Reminder/Files Serializable+READPAST ngoài slice sửa này; identity worker có R3-01 riêng |
| R2-05 | Còn mở | Final-attempt crash của Reminder/Files chưa sửa |
| R2-06 | Còn mở | Task reconcile retry/lease chưa sửa |
| R2-07 | Còn mở | Task new reminder vẫn là post-M01 gap |
| R2-08 | Còn mở | Document dirty transition chưa sửa |
| R2-09 | Còn mở | Favorite UUID/router compatibility chưa sửa |
| R2-10 | Còn mở | Local timezone one-offset/DST chưa sửa |
| R2-11 | Còn mở | Calendar anchor/navigation chưa sửa |
| R2-12 | Còn mở | Task Project picker pagination chưa nối |
| R2-13 | Còn mở | Sharing nullable Task Priority consumer còn gated |
| R2-14 | Một phần | Admin self-demotion 204 tốt hơn; Task/Document mutation-read còn |
| R2-15 | Một phần | Identity/admin safe replay và receipt upgrade được xử lý; Trash/business replay còn |
| R2-16 | Còn mở | Files upload attempt/recovery qua crash còn gated |
| R2-17 | Còn mở | Task status trong Calendar projection chưa giữ riêng |
| R2-18 | Một phần | M01 labels/theme/timezone source đã cải thiện qua R3-08; browser/post-M01 còn |
| R2-19 | Một phần | Writer ID đúng hơn; legacy audit inventory/correction chưa có |
| R2-20 | Một phần | Local capture source được gia cố bởi R3-01/05/06/07; E2E/runtime chưa chạy; real provider không bật |
| R2-21 | Còn mở | Cross-module SQL/App boundary còn |
| R2-22 | Một phần | Claim cũ stale; artifact hiện tại đã ghim revision nhưng runtime/independent evidence còn |
| R2-23 | Source fixed | Giữ IdentityV3/220000, bounded legacy rehash và SuccessRehashNeeded; benchmark/runtime pending |
| R2-24 | Source fixed | Giữ signed anonymous binding và stable configured secret; browser/race pending, authenticated upgrade là R3-02 |

F02 và F03 đã source-fixed tại 6d5b58dbac330b0d0d9aceb9e0b2f4695b423316 và được giữ nguyên; không ghi chúng là “chưa implement” chỉ vì vòng này không sửa file tương ứng. F09 có local capture code nhưng runtime pending; F16 có locale/theme improvement nhưng browser pending; F10 có namespace compatibility source fix nhưng business replay ngoài M01 còn.

## 6. F01–F22 history table riêng

| ID | Disposition hiện tại | Case/gate |
|---|---|---|
| F01 | Confirmed; post-M01 scope blocked | Trash/business replay và read authority |
| F02 | Source fixed tại 6d5b58d; preserved; runtime pending | Password/reset identity flows |
| F03 | Source fixed tại 6d5b58d; preserved; runtime pending | Authenticated/session authority |
| F04 | Confirmed; post-M01 scope blocked | Reminder/task expired input |
| F05 | Confirmed; post-M01 scope blocked | Document draft transition |
| F06 | Partial | M01 identity current guard đã có; Reminder direct consumer còn |
| F07 | Confirmed; post-M01 scope blocked | Favorites route canonicalization |
| F08 | Confirmed; post-M01 scope blocked | Files upload crash/recovery |
| F09 | Source fixed/extended; runtime pending | Local capture worker, fence, ACL, retention; không real provider |
| F10 | Partial | M01 authenticated receipt namespace upgrade source-fixed; Trash/business replay còn |
| F11 | Source fixed; runtime pending | Identity/session security path |
| F12 | M01-compatible; post-M01 mismatch blocked | Gated module boundary |
| F13 | Core source fixed; nullable consumer blocked | Sharing Task Priority |
| F14 | Confirmed; post-M01 scope blocked | Calendar/timezone/DST |
| F15 | Confirmed; post-M01 scope blocked | Favorite typed route |
| F16 | Partial source fixed; runtime pending | M01 vi/en/theme/formatter |
| F17 | Confirmed; post-M01 scope blocked | Calendar navigation/projection |
| F18 | Confirmed; post-M01 scope blocked | Modular boundary/consumer contracts |
| F19 | Partial | Audit writer ID improved; legacy inventory unresolved |
| F20 | Source fixed in current evidence | Old revision/CI/claim matrix superseded by this report |
| F21 | Partial | M01 local capture cleanup fixed in source; post-M01 durable file cleanup blocked |
| F22 | Partial; post-M01 scope blocked | Task reminder reconcile/status |

## 7. Verification log

| Command/case | Exit/status | Result and limitation |
|---|---:|---|
| git rev-parse HEAD | 0 | e12fc994d34801118b38b5ff306a468af3dd44b0 |
| git rev-parse main | 0 | 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3 |
| git ls-remote origin refs/heads/main refs/heads/impl/m01-s00-scaffold | Failed | GitHub port 443 unavailable; no remote freshness claim |
| dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release --no-restore | 0 | Build succeeded; 0 warning, 0 error; run tuần tự |
| dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release --no-restore | 0 | Build succeeded; 0 warning, 0 error; run tuần tự |
| dotnet build src/Nexora.Local/Nexora.Local.csproj --configuration Release --no-restore | 0 | Build succeeded; 0 warning, 0 error; run tuần tự |
| npm run build tại web/Nexora.Web | 0 | tsc -b và Vite 6.4.3 thành công; 31 modules |
| git diff --check | 0 | Không có whitespace error; Git có cảnh báo line-ending LF/CRLF bình thường |
| dotnet test / test projects | Not run | Bị loại khỏi amendment code-only; không thêm test |
| SQL Server real isolated fixture | Not run | sqlcmd client có sẵn nhưng không chạy migration/DB fixture/RCSI/two-worker/receipt upgrade |
| Windows ACL/Linux mode | Not run | Chưa có hai-principal filesystem run |
| Crash/idle retention | Not run | Chưa kill/restart hoặc chạy sweep trên runtime |
| Identity V3/legacy/CSRF/browser M01 | Not run | Không thực hiện functional/browser QA hoặc screenshots |
| CI/PR metadata | Pending | Không có remote/CI connector trong session; không dùng CI claim thay runtime |
| Independent review | Pending | Chưa có reviewer độc lập; không claim đã pass |

Một lượt build song song trước verification tuần tự exit 0 nhưng có MSB3026 do các project dùng chung output bị khóa lẫn nhau. Lượt tuần tự ở trên là evidence cuối cho build, với 0 warning/0 error.

## 8. Migration, recovery và rollback

Không có forward migration mới. Required readiness hiện yêu cầu journal đến 0026 vì code mới đọc DeliveryEnvelope/DeliveryLease columns. Không chạy migration và không sửa checksum/history.

Local effect protocol:

- SQL claim vẫn bounded và lease-fenced.
- AcquireEffect là transaction ngắn chỉ gia hạn effect window khi intent, token, account, PersonalSpace và lifecycle còn đúng.
- File write đi vào temp/private pending; pending không được CLI đọc.
- Sau SQL Delivered, promote exact message ID/effect fence; nếu crash giữa prepare/completion hoặc promotion, worker reconcile theo state/fence và retention dọn owned orphan hữu hạn.
- Capture direct cũ vẫn đọc được; chỉ capture mới có DeliveryFence mới được nhận ownership cho promote/remove.
- Log chỉ chứa metadata message ID/purpose; không đưa raw token, password, key, hash hoặc secret vào log/report/artifact.

Rollback an toàn: vì thay đổi hiện còn uncommitted, owner có thể review/điều chỉnh working-tree diff; không dùng reset hard. Nếu cần rollback sau khi triển khai, dùng forward-compatible code/backup restore theo release gate, không drop column, không xóa receipt, không reset secret, không blanket-delete capture. Không coi việc xóa DB/capture hàng loạt là verification.

## 9. PR description draft — chưa đăng

Draft cho PR #4:

“R3 M01 local repair trên branch impl/m01-s00-scaffold, review base main 8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3 và implementation head e12fc994d34801118b38b5ff306a468af3dd44b0. Sửa SQL claim hint/readiness schema gate, authenticated receipt upgrade compatibility, grant manifest gate, fenced local delivery effect, fail-closed ACL, bounded idle retention và profile timezone cho Sessions. Release API/Bootstrap/Local và frontend build exit 0 với 0 warning/0 error. SQL Server, browser, CI, screenshots, test suites và independent review chưa chạy trong code-only amendment; post-M01/paused findings không được mở. Chưa merge, chưa push main, chưa deploy.”

## 10. Handoff gates

Human owner cần chạy các case trong mục 3 và mục 7 bằng synthetic isolated SQL/filesystem/browser setup trước khi gọi Runtime verified. Đặc biệt cần kiểm chứng SQL Server RCSI/hints, receipt old-to-new cùng secret, two-worker effect fencing, Windows ACL/idle cleanup và profile IANA timezone.

Reviewer độc lập cho security, authorization, background effect, migration/readiness và filesystem privacy vẫn Pending. Báo cáo này không phải approval, không phải merge authorization và không thay thế CI/QA/runtime evidence.
