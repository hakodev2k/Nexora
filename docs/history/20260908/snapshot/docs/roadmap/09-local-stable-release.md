# Local Stable Release Gate

**Milestone:** RM18 — LOCAL STABLE RELEASE.
**Current status:** NOT ACHIEVED; chỉ lập kế hoạch. Không backend/frontend/SQL/Redis/test runtime nào đã được triển khai hoặc chạy trong task này.

[Master](00-master-implementation-roadmap.md) · [Runbook](03-local-development-roadmap.md) · [Tests](08-testing-roadmap.md) · [Production](10-production-roadmap.md)

## 1. Điều kiện bắt đầu review

RM16 integration complete + RM17 local qualification complete. Tất cả committed catalog modules có approved scope/acceptance và usable local implementation; không chỉ core Documents/Tasks. OPEN REQUIREMENT/CONFLICT ảnh hưởng Release 1 đã đóng hoặc có Product Owner scope-change record explicit. Không tự defer P1/candidate để đạt gate.

Scope gồm Identity/Profile/roles/module management; personal isolation/sharing/support/emergency; audit/activity/notifications/files/settings/trash/jobs/portability/backup; Projects/Tasks/Calendar/Reminders/Planner/Goals/Habits/Time Tracking/Pomodoro; Documents types/editors/files/bookmarks/snippets/org/templates/versioning/read later; all Search/Dashboard; Finance/Vault; News/Shopping records/tracking; Developer/GitHub/history/Automation/webhooks/n8n/data sync; Personal/Digital Assets/Career/Learning.

Project/Task import/export vẫn explicit Deferred; Calendar ICS vẫn required. Team/AI/billing/no-code/native/marketplace exclusions giữ nguyên.

## 2. Definition of Done và evidence

| Gate | Pass condition | Required evidence / owner |
|---|---|---|
| LSR-01 Coverage | Mọi source requirement disposition rõ; toàn approved Release 1 scope có feature/test/evidence; không placeholder | Coverage report + Product Owner |
| LSR-02 Backend | APIs/business rules/validation/errors/health/logging hoạt động local | Build + V-API/domain suites; Backend owner |
| LSR-03 React | Toàn critical UI/journeys desktop/mobile; tablet usable; loading/error/readonly/denied states | Build/lint/component/E2E/a11y evidence; Frontend/QA |
| LSR-04 SQL | Schema constraints, indexes, owner isolation, transactions/concurrency; full migration path/seed | V-DB/upgrade/empty seed + query report; Backend/Data |
| LSR-05 Redis | Connection/use-case cache hoạt động; loss/flush/reconnect không mất data hoặc bypass auth | V-CACHE outage/isolation tests; Backend |
| LSR-06 Identity/access | Verify required dùng ngay, sessions/revoke, last SuperAdmin, all four contexts | V-ID/V-AZ/V-MOD negative matrix; Security |
| LSR-07 Files/Docs | Upload/preview/crop/current/historical file refs; manual Save/version/tree rules/search correct | V-FILE/V-DOC/V-ORG; feature owners |
| LSR-08 Productivity/time | Project terminal/history/trash/share, Task→Calendar, manual Event/ICS/reminders/timezone | V-PROD/V-CAL; feature owners |
| LSR-09 Notifications/jobs | Every category all3 fan-out; real controlled Email/Push integration; safe retry/durable restart | V-NTF/V-JOB, provider/browser denial evidence; Backend/QA |
| LSR-10 Search/dashboard | Correct source fields/count/ranking/scope/degraded widgets and all module contributions | V-SEARCH/V-DASH; feature owners |
| LSR-11 Finance/Vault | Golden ledger, ciphertext/redaction/owner reveal, rotation and full key-dependent restore | V-FIN/V-VAULT; Security/Data |
| LSR-12 Remaining modules | Planner through Career/n8n all committed capabilities meet approved DoR/DoD | Domain suites + Product Owner acceptance |
| LSR-13 Cross-module integrity | Typed links/lifecycle/account disable/module disable do not leak/orphan/replay | RM16 inventory; V-PORT/V-LIFE; Technical owner |
| LSR-14 Regression | Unit/integration/frontend/critical E2E pass; no blocker bug; security findings xử lý theo gate | Versioned suite/defect reports; QA/Security |
| LSR-15 NFR | Approved local profile performance/resource budgets and reliability/accessibility verified | Load/fault/a11y results; Technical/QA |
| LSR-16 Recovery | SQL/files/config/key dependency backup restore thành công vào isolated target | Restore inventory + encryption/decrypt + source integrity checks; Data/Security |
| LSR-17 Runbook | New developer chạy full app từ clean checkout bằng exact verified commands/versions | Fresh-machine rehearsal, no manual undocumented repair; Technical owner |
| LSR-18 Decision | Product/Technical/Security ký Local Stable build cụ thể | Dated review record + commit/artifact references |

Không coi test adapter success là Email/Browser Push transport proof. Một số approved external integrations cần sandbox/live read checks trong app local; fixtures vẫn phải bao phủ timeout/denial/rate-limits. Không dùng provider-blocked feature làm demo rồi đánh dấu module xong.

## 3. Local backup/restore rehearsal trước production

1. Tạo synthetic baseline có all modules, active/revoked shares/support grants, version histories, Trash trees, files/crops, Finance ledger, Vault test secret và queued/completed jobs.
2. Ghi backup inventory: SQL point/schema versions, file references/checksums, config references, protected key dependency. Database/file backup một mình không đủ decrypt Vault.
3. Restore vào target isolated; environment guard chặn overwrite environment sai; authorized operator confirmation/audit.
4. Verify ownership/modules/users/grants/ledger/current+historical document/media, Vault decrypt với đúng key; wrong/missing key fail rõ.
5. Rebuild search/Redis an toàn; reconcile notifications/webhooks/jobs để không replay lịch sử hoặc hồi sinh revoked authority.
6. Đo elapsed/restore point; local recovery objective được review. Production RPO/RTO/off-site strategy chọn sau RM18, không bỏ qua local recovery trước dữ liệu có giá trị.

Các bước trên là planned rehearsal; không có backup hoặc restore nào đã chạy.

## 4. Local Stable sign-off template

| Field | Value khi review tương lai |
|---|---|
| Candidate commit/build | TBD — chưa có app build |
| Catalog/scope version | Baseline + approved change records |
| Test profile/report | TBD — NotRun |
| Migration/restore report | TBD — NotRun |
| Security/a11y/performance review | TBD — NotRun |
| Known limitations + authorized exceptions | Phải có owner/reason/expiry, không bỏ confirmed module ngầm |
| Product reviewer | TBD |
| Technical/Security reviewers | TBD |
| Decision/date | Pending — chỉ ký khi gates đạt |

## 5. Sau gate

Chỉ sau approval LSR mới bắt đầu RM19 Production Architecture, RM20 Hosting Selection, RM21 Deployment và RM22 Public Release. Local Stable không đồng nghĩa production infrastructure đã chọn, không là public release hoặc phép deploy tự động.

**Task hiện tại dừng sau bàn giao roadmap.** Approval tiếp theo mới cho phép bắt đầu implementation scope được chấp thuận; mọi OPEN REQUIREMENT vẫn phải được xử lý trước coding phần bị chặn.
