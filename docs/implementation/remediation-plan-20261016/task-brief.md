# Task brief — Kế hoạch remediation sau audit

Trạng thái: đang khảo sát, chỉ tạo tài liệu. Không phải approval thực thi hay chứng nhận hoàn tất sản phẩm.

| Trường | Bằng chứng / phạm vi |
| --- | --- |
| Accountable owner | Technical Lead (agent lập kế hoạch); developer, QA và reviewer độc lập chưa được chỉ định cá nhân |
| Outcome | Kế hoạch tiếng Việt theo toàn bộ 15 mục trong `pasted-text-1.txt`, bao gồm P0 Goals/Habits/Planner, Windows actual-principal, review, semantic RTM và các vertical slice R1 còn thiếu |
| Authorization | Session cho phép đọc source/GitHub và tạo artifact kế hoạch. Không sửa source/schema/grants/workflow/branch/PR, chạy migration, provisioning Windows hoặc thực thi implementation. Yêu cầu mới supersede code-only amendment về quyền test trong đợt thực thi sau này; lượt này mọi test chỉ là planned |
| Branch local | `impl/m01-s00-scaffold`; không đổi branch |
| Local HEAD | `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761` |
| Local committed tree | `b02b8efc9c6181a14d8788dab6c920e08c24c944`; không đại diện dirty worktree |
| PR baseline | GitHub connector xác minh PR #4 open/draft/unmerged, head `f0611fad590269fdc73a3397fd785a97d2c0ef5e`, base/main `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3` |
| Divergence | Connector compare: PR ahead local 5 commits, behind 0. Không reset/pull/cherry-pick hoặc ghi đè dirty files |
| Current CI | Implementation run `38915340562` completed/failure; SQL/API, browser, backend-unit, frontend-unit, filesystem-linux success; filesystem-windows-acl failure. Agent baseline `38915341389` success. Đây là quan sát CI đã tồn tại, không phải checks chạy trong lượt plan |
| Primary scope | P02/RM10 FX15–17; P03 FX20/24/25/26; FX06/29/33/39/40; re-audit đủ 40 FX; M01 chỉ là foundation |
| Goal binding | `NXG-FX15-G01..G03`, `NXG-FX16-G01..G03`, `NXG-FX17-G01..G03`; goal SYS/phase/cross-module và các FX P1 sẽ bind theo bảng trace trong kế hoạch |
| Requirement sources | FX15 BR001–005/AC001–003; FX16 BR001–005/AC001–003; FX17 BR001–005/AC001–003; common authorization/composition; exact actions lấy từ catalog, không dùng goal thay quyền |
| Startup đã đọc | `AGENTS.md`; `.agents/skills/nexora-engineering/SKILL.md`; `.ai/profiles/nexora-implementation-agent.md`; `.ai/roles/technical-lead/README.md`; `rules/core-rules.md`; `.ai/routing.json`; `.ai/verification.md`; docs/delivery/goals entry points |
| Routing | architecture + backend + frontend + security + database + verification + owner-isolation; paths lấy từ `.ai/routing.json`. Nội dung cần trace sâu sẽ có reading ledger, không suy full-read từ một lệnh bị truncate |
| Open gates | Lock-order compatibility; legacy receipt binding/action discovery; source provider participation; P1 story contracts; Windows privilege/launcher; independent reviewer; semantic reading chưa đầy đủ. Planner reorder ACK đã chốt đề xuất: status200/id=original PinIds[0], stable receipt binding; no Plan migration |
| Invariants | OwnerId = PersonalSpace; SQL authority; no grant/auto-enable; no real providers/production/real secrets/paid services/merge/main write; không giảm required Windows gate |

## Dirty worktree phải giữ nguyên

Observed modified: `.github/workflows/m01-ci.yml`; integration `SqlApiIntegrationTests`, `SqlCalendarImportTests`, `SqlCompositeCapabilityTests`, `SqlNewsMigrationTests`, `SqlSharingRepairTests`, `SqlTagPaginationTests`; unit `M01PolicyAndCaptureContractTests`; frontend `App.surface.test.tsx`, `App.test.tsx`, `functional/content.spec.ts`, `artifacts/vitest-junit.xml`.

Observed untracked: hai continuation reports trong `full-product-audit-20261012`; `start-isolated-browser.ps1`, `stop-isolated-browser.ps1`, `test-sql-ephemeral.sh`; `src/Nexora.Api/.local-files/`; integration `SqlProductivityAuthorityTests`, `SqlSessionAuthorityTests`, `SqlSnippetAuthorityTests`; `FileRestoreDialog.test.tsx`; browser artifacts/results. Không đọc nội dung `.local-files`, credentials hay captured messages. Không xác định author từ tên file hoặc tự nhận ownership.

## Phương thức khảo sát

Local git read ban đầu báo dubious ownership vì sandbox SID khác owner. Dùng `git -c safe.directory=D:/Projects/ASP_NET_Core_Developer_2026/Nexora ...` theo từng lệnh; không thay global config. Git network không được dùng. Đọc remote bằng host GitHub connector và pin SHA. Báo cáo rerun chỉ có trên remote, không giả rằng local HEAD đã chứa các sửa lỗi đó.

## CI job IDs đã quan sát

| Job | ID | Conclusion |
| --- | --- | --- |
| sql-api-integration | 117021240241 | success |
| browser-e2e | 117021240354 | success |
| backend-unit | 117021240523 | success |
| filesystem-linux | 117021240566 | success |
| frontend-unit | 117021240579 | success |
| filesystem-windows-acl | 117021240778 | failure |

Không có product test/build/runtime/provisioning nào được chạy trong lượt lập kế hoạch.

Follow-up audit 2026-10-17: GitHub PR head/base/draft vẫn như baseline. Đọc existing job logs xác nhận CI checkout PR merge ref `c781cb13edc7b3c0ffc8bff4152f507e6c62114f`; SQL219/0/0, xUnit49/0/0, frontend143passed/1skipped/14files; Windows30passed/0failed/4skipped và required actual-principal skip gây failure. Không gọi việc đọc log là chạy lại tests. Browser job success; exact count10 vẫn PR/report baseline chưa reparse ZIP JUnit.
