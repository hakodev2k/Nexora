# Nexora audit checkpoint — 2026-10-12

> Historical checkpoint below. Current continuation: [2026-10-13 code-only task brief](continuation-20261013-code-only.md) and [Task/session/File source and failure journal](session-20261012-task-boundaries.md). HEAD before publication `940f53c90254f0b4d2f8c4be6a2b3e5bd9e57589`. Earlier SQL-runtime absence statements below have historical scope; selected real SQL runs subsequently occurred. Later FX22 changes have compilation/source review only. Full-product verdict remains INCOMPLETE. RTM now has ten PARTIAL rows (four Tag plus six Snippet); the remaining723 stay UNVERIFIED, with zero COMPLETE. These counts are audit dispositions, not defect counts or acceptance percentages.

**Verdict: INCOMPLETE.** Chưa hoàn tất nhiệm vụ audit và implementation toàn bộ Nexora. Những thay đổi dưới đây là production repairs thực tế trên PR #4, không phải tuyên bố toàn bộ requirement đã được phát hiện, triển khai hoặc nghiệm thu.

Authority `main`: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`. Implementation baseline: `fd7d087142e7e437cf742fdf401acea1757179cb`, branch `impl/m01-s00-scaffold`. Source fingerprint sau sửa nằm trong [verified-source-files.json](verified-source-files.json). Không thay đổi `main`, merge, migration, account/module enablement, secrets hoặc live provider.

## A. Requirements coverage

| Inventory | Số lượng | Ý nghĩa và giới hạn |
|---|---:|---|
| Main documentation hiện hành | 303 | Toàn bộ file ngoài `docs/history`; inventory + SHA256, không chứng nhận đã semantic-review hết |
| Feature groups | 40 | Exact catalog grouping, không phải 40 module complete |
| Catalog actions | 733 | Exact unique action keys trên main; không phải tổng số requirements |
| Screens | 202 | Definition inventory, không chứng nhận UI behavior |
| Candidate source identifiers | 1,327 | Regex index chứa cả declaration và reference; chưa deduplicate/resolve semantic, không dùng làm denominator requirements |

[main-source-inventory.csv](main-source-inventory.csv) ghi từng file/SHA/size. [source-identifier-index.csv](source-identifier-index.csv) ghi 4,644 occurrences cùng file/line/text. [action-traceability.csv](action-traceability.csv) giữ tất cả 733 actions, expected guards, actors/context, dependencies, screens và candidate code references. Reference presence/absence không chứng minh implementation hoặc acceptance. Đây là **RTM ban đầu**, chưa có đủ pre/postconditions và acceptance evidence cho toàn bộ requirements.

Disposition của 733 catalog rows: COMPLETE 0; PARTIAL 4 (Tag catalog actions có code repairs nhưng chưa đủ whole-contract/runtime evidence); UNVERIFIED 729. Các nhóm MISSING/INCORRECT/BROKEN/BLOCKED chưa được đếm toàn diện: số 0 trong một export không có nghĩa repository không còn gaps. Không báo coverage percentage hoặc total requirements discovered vì denominator chưa được chứng nhận.

## B. Confirmed defects và changes

| ID / requirement | Expected | Baseline actual / root cause | Severity | Production changes | Verification / remaining |
|---|---|---|---|---|---|
| G01; authorization steps 2/5/7, NXG-SYS-03, NXG-X-02 | Mọi prerequisite resolved; Admin có Allow cho từng action, Deny thắng | `SqlSelfCapability.Evaluate` dùng EXISTS cho bất kỳ action; Finance ListRecords yêu cầu record + summary nhưng evaluator không AND toàn bộ | High | COUNT tất cả distinct requested permissions/Allows; canonical-only authority callers trong Productivity, Search, Favorite, Dashboard và Notifications; giữ receipt/audit identifiers | Independent source review; real SQL composite tests compile, NOT RUN |
| G02; organization.tag.read, FX24-S01 / common list | 25/page, stable seek tới toàn bộ catalog | `SqlTagService.List` TOP bounded + NextCursor luôn null; UI chỉ lấy 100 | Medium | Name/ID keyset, take+1, encrypted owner/filter/limit-bound cursor; API cursor forwarding; UI Load more/dedup/reset/stale-response guard | Cursor unit tests + UI pagination pass; SQL/API paging NOT RUN |
| G03; FX-COM-005, common search | Literal search, reject quá dài | Raw LIKE biến %/_/[ thành SQL wildcard; query bị truncate âm thầm | Medium | Escape LIKE; 422 query >50; committed reads thay NOLOCK trong Tag paths | SQL literal-search tests compile, NOT RUN |
| G04; FX-COM-007, FX24-S01 conflict | Giữ draft, reload exact current ETag | Revision recovery tìm ID trong first100 rows, không tìm được Tag nằm ngoài page | Medium | Owner-scoped GET by ID + ETag; UI giữ draft, chỉ thay revision sau explicit recovery | UI conflict test pass; actual API authorization NOT RUN |
| G05; FX-COM-006/AC003 | Retry cùng key/payload trả cùng result, không ghi thêm | Tag receipts chỉ lưu ResultCode; mọi successful retry trả409; normalized-name digest conflated display-case changes | High | Existing SQL receipt columns lưu safe DTO/status; current transactional mutation authorization; DTO replay kiểm tra read + live owner resource; delete replay204; JSON canonical input tránh case/delimiter collision | Real SQL replay/conflict/revocation tests compile, NOT RUN |
| T01; existing unit quality gate | Manifest assertion bao gồm installed migrations | Test expected list dừng0046 trong khi production manifest đã có0047–0053 | Medium | Bổ sung đúng7 existing migration filenames, giữ exact-equality assertion và toàn bộ tests | Initial FAIL1/48pass retained; final49/49pass |

Evidence functions: `src/Nexora.Infrastructure/Authorization/SqlSelfCapability.cs::Evaluate`; `Finance/SqlFinanceService.cs::ListRecords`; `Organization/SqlTagService.cs::List/Get/CheckReceipt/CompleteReceipt`; `Organization/TagListCursor.cs`; `src/Nexora.Api/Features/Organization/OrganizationEndpoints.cs`; `web/Nexora.Web/src/App.tsx::OrganizationTagsScreen`; unit manifest test. Exact current candidate line references có trong RTM; baseline source phải dùng SHA ở đầu báo cáo.

5 production defects đã có code repair; không coi đó là 5 fully verified/resolved acceptance findings. Một pre-existing test-maintenance defect đã xử lý. Không thêm fake provider, fake success, empty interface hoặc demo production records. Synthetic data chỉ xuất hiện trong regression tests được yêu cầu trong prompt mới.

## C. Remaining gaps và dependency-aware backlog

| Workstream | State / evidence | Next concrete work | Dependency / gate |
|---|---|---|---|
| Complete requirements discovery | INCOMPLETE: 303-file inventory và identifier index chưa là complete semantic inventory | Đọc/resolve từng declaration, decisions/deltas; bind exact actors, pre/postconditions, contract, BR/AC và dependencies; phân biệt historical/proposed/retired | Không cần blanket implementation approval mới |
| Full forward/reverse implementation audit | UNVERIFIED729 catalog actions; current reports/PR body có nhiều stale states | Trace UI/API/auth/domain/SQL/provider/response cho mọi requirement; audit ngoài changed-files; classify bằng actual behavior | Existing code/reference inventory không được promote thành COMPLETE |
| SQL/API verification of repairs | NOT RUN: không có SQL Server test runtime/`NEXORA_TEST_SQL_CONNECTION` | Chạy mới `SqlCompositeCapabilityTests` và `SqlTagPaginationTests` trên isolated loopback SQL; kế đó affected regression | Không thay bằng SQLite/InMemory; production connection không được dùng |
| Browser E2E / runtime startup | NOT RUN; frontend tests là jsdom/MSW | Chạy app thật với isolated SQL/config; keyboard/mobile/loading/conflict/revocation/race journeys | Local test environment, không real provider activation |
| Tags whole contract | PARTIAL: catalog-only slice; assignment/Collections/Templates vẫn chưa có installed vertical flow | Contracts + source module provider guards trước assignment/templates; current read versus mutation ACK audit | Các source cardinality/lifecycle/security rules phải được giữ |
| Wider authorization / mutations | Source reviewer xác nhận existing Productivity paths trả full DTO mà chưa kiểm tra read riêng và có auth checks ngoài write transaction | Audit/repair opaque ACK versus read projections và before-commit authority, receipt replay trên mọi service | Security-sensitive source + real SQL race evidence; chưa certify bằng bản sửa evaluator |
| GitHub Discovery / Price / Integrations | No installed product journey: App ModuleScreen fallback; Program không map corresponding feature services/APIs | Exact local action contracts, SQL/API/UI/worker and fail-closed provider boundaries; không coi registered catalog thành feature | Price/Integrations live execution chưa approve; không đánh đồng provider inactivity với local-code pause |
| Vault | Feature spec có encrypted/version/recovery requirements; chưa có installed Vault API/store/UI flow | Resolve current crypto/key/recovery/support/portability contracts, threat model và independent review trước secret functionality | Q/security decisions phải đối chiếu full main; không tự invent recovery protocol |
| Remaining domain/platform capabilities | Current source có bounded slices; không chứng nhận toàn bộ lifecycle/history/transfer/search/share/support/notifications | Audit từng feature/AC, rồi triển khai dependency-first vertical slices từ complete contracts | Local implementation đã được user yêu cầu; chỉ genuinely ambiguous/risky/external items mới cần clarification |
| Whole-project re-audit | INCOMPLETE | Recheck toàn bộ RTM, cả previously implemented paths và regressions; run required CI/runtime gates | Không đóng nhiệm vụ bằng selected tests green |

Các workstreams trên vẫn là authorized backlog; báo cáo này không chuyển chúng thành “PO-blocked” để hợp thức hóa việc chưa hoàn tất. Chưa có đủ discovery để gom một danh sách blocking business questions đáng tin cậy; không hỏi lại blanket permission.

## D. Actual verification ledger

Commands chạy trong clean clone + changed source fingerprint, .NET SDK10.0.100 / local runtime10.0.0 / Node/npm. Không sử dụng production data/secrets.

| Command / scope | Actual result |
|---|---|
| `npm ci --ignore-scripts` | PASS, 209 packages; lockfile unchanged |
| `npm run build` after final frontend source | PASS; existing bundle >500KiB warning retained |
| `npm run test:unit` final frontend | PASS122, existing skipped1; 11 files pass; 3 new Tag component tests included |
| `dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore -m:1` final | PASS49, failed0, skipped0; 2 new cursor tests included |
| `dotnet build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --no-restore -m:1` final | PASS0warnings/0errors; compilation includes new SQL tests, not runtime SQL acceptance |
| `dotnet build src/Nexora.Api/Nexora.Api.csproj -c Release --no-restore -m:1` | PASS0warnings/0errors; separate from runtime startup |
| `git diff --check` | PASS |
| Independent `auth_review` source review | No blocking defect in final reviewed authorization/Tags delta; existing wider risks and runtime gaps retained |
| SQL/API/integration/migration execution | NOT RUN: SQL test connection/runtime absent |
| Browser E2E / live-provider runtime | NOT RUN |
| Remote CI / all40-feature semantic re-audit | NOT CERTIFIED at this checkpoint |

Failure history preserved: SDK installer extraction returned failure because tar attempted unavailable ownership changes; extracted SDK was independently checked and usable. Initial parallel restore returned failure without diagnostics; serial `-m:1` locked restores succeeded. Initial .NET unit run48pass/1fail exposed stale migration assertion. New UI conflict test initially failed due missing CSRF test handler, then incorrect token response shape; corrected fixture to actual `requestToken` contract, targeted3 pass and full122 pass. No assertion/acceptance criterion weakened, no test removed. Raw runtime/user artifacts are excluded from publication.

Integration source adds9 cases: composite capability3; paging/search/replay6. They were compiled only. Unit/UI evidence proves cursor serialization and component workflows, **không** chứng minh SQL ordering/isolation/transactions/ETag/API runtime. Superseded request protection được source-review, chưa có controlled network-race test.

## E. Quality verdict

**INCOMPLETE** — confirmed production repairs exist and selected build/unit/component checks pass, but full requirements discovery, whole-project semantic audit, remaining implementation backlog, real SQL/API acceptance, full regression/CI and final whole-RTM re-audit are not complete. Không dùng verdict IMPLEMENTED — VERIFICATION PENDING cho toàn sản phẩm vì nhiều chức năng còn chưa implement hoặc chưa được audit. Không có full-feature/R1/production completion claim.
