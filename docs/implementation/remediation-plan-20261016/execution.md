# Thực thi remediation — 2026-10-17

User authorization: “lên plan rồi implement luôn đi”, tiếp tục implementation-plan.md và phụ lục theo từng slice. Áp dụng execution amendment hiện tại: code-only, không thêm/chạy functional tests, không thêm fixture/mock/demo records; chỉ build/typecheck để phát hiện lỗi implementation. Không production/provider thật/paid services/merge/main write. Đây không còn là lượt plan-only.

Accountable owner: Technical Lead implementation agent; independent reviewer chưa được chỉ định, gate Pending. Không spawn agent.

Rebaseline: PR4 vẫn draft/open/head f0611fad590269fdc73a3397fd785a97d2c0ef5e/base8782f46; local branch impl/m01-s00-scaffold tại1cd27a46, 12modified tracked files và untracked work được giữ. Chỉ sửa những source P0 unchanged giữa local và remote; không reset/pull hoặc ghi đè tests/workflow đang dirty. Remote repaired Finance/Notifications/Document/shared UI phải được reconcile trước publish final PR state, không bị gọi là regression đã giữ chỉ bằng build local.

Thứ tự: Goals transaction+projection+receipt/client → Habits → Planner/source participant → Windows independent harness → remaining P1/RTM contracts and slices. Toàn scope còn active, không đóng whole plan vì một patch build pass.

Selected goals: NXG-SYS-02/03/05/08/13/14, NXG-FX16-G01/G02/G03, FX16-S01..S04, FX16-BR001–005/AC001–003, action00/01/module16; tiếp theo exact IDs của WP-HABIT/WP-PLAN. Rules: nexora-engineering/profile/routing baseline+backend+frontend+database+security+verification+owner-isolation; concrete source/contract paths và rollback/test design kế thừa plan/source ledger, đọc thêm theo bề mặt sửa.

## Slice đã sửa: Goals authority / opaque replay / client intent

Working tree trên HEAD `1cd27a46a2ba1f24ab2b6f32f84771230f4b1761`, chưa commit. Năm product files:

- `src/Nexora.Infrastructure/Goals/SqlGoalService.cs`: Serializable transaction cho List/Get; bỏ NOLOCK toàn service; current authority cùng connection/transaction cho keyless và keyed mutations; final action/read proof sau audit/receipt; detail được đọc lại sau các writes đó. Create có target cần target.create. Update giữ explicit goal.read prerequisite; progress giữ target.read prerequisite.
- Cùng service: canonical JSON version2, normalized decimal string, strong quoted canonical 8-byte rowversion; giữ operation namespace và dual subject hashes/TTL24h. Completed transition receipts được tra trước current lifecycle/ETag, giữ start/reopen riêng. Receipt v2 chỉ có user/owner/resource/action set/status/code, không body hoặc private detail. Replay trả opaque ACK sau current original-action check; không dựng historical response từ current rows.
- `src/Nexora.Infrastructure/Identity/SqlRequestReceiptStore.cs`: optional inspect-only không reserve/reclaim; optional legacy canonical digest chỉ match code-only receipt. Defaults của các caller khác không đổi. Legacy typed progress/transition có route ID được ACK; legacy create/metadata ambiguous trả `CommittedOutcomeUnavailable`, không chạy lại hoặc đoán ID.
- `src/Nexora.Api/Features/Goals/GoalsEndpoints.cs`: serialize committed ACK đúng success/status, không tạo GoalDetail giả hoặc ETag cho ACK.
- `web/Nexora.Web/src/api.ts`: validate status, UUID/route identity, ACK shape và core detail/target fields; malformed response là outcome chưa xác nhận.
- `web/Nexora.Web/src/App.tsx`, chỉ GoalsScreen: ACK clear protected data, không auto-GET; fence stale reads; clear khi authority/resource denied; không error-as-empty. Unknown outcome giữ closure payload/route/ETag/key trong memory, khóa command mới, explicit retry cùng command. Progress modal đóng để retry control không bị modal che. Không ghi browser storage.

Design refinement: receipt v2 dùng ACK-only replay, kể cả original response có detail. Vì không trả lại detail khi replay, không cần lưu projection digest để so với current state; không nâng receipt opaque thành DTO khi read grant được cấp lại. Fresh response vẫn có thể trả authorized current detail. Đây là safe narrower response của receipt envelope, không thêm read prerequisite cho create/transition.

Migration/data/background/provider impact: không schema migration, không data rewrite, không thêm provider/job. Rollback: revert đúng năm product file hunks và execution artifacts của lượt này; không reset branch hoặc revert user tests/workflow. Receipts v2 tồn tại tối đa TTL24h; rollback về handler cũ trả replay409 thay vì thực thi lại, nhưng không tương đương seamless downgrade và cần owner runtime review trước release.

## Verification thực tế và giới hạn

- `dotnet build src/Nexora.Api/Nexora.Api.csproj -c Release --no-restore`: PASS, final backend source, 0 warnings / 0 errors (2.26s ở lần cuối). Đây là compile, không runtime verification.
- `npm run build --prefix web/Nexora.Web`: TypeScript + Vite PASS trên final frontend source, 79 modules, Vite2.98s, JS840.67kB/gzip212.79kB. Một lần đầu TypeScript báo nullable `apiError.code`; đã sửa `?? ''` rồi build lại thành công. Vite cảnh báo bundle >500kB; không gọi warning-free.
- `git ... diff --check -- <five product files>`: không whitespace findings; Git có LF/CRLF conversion warnings.
- 12 pre-existing modified tracked file fingerprints: 0 mismatches so với `worktree-modified-fingerprint.csv`. Không sửa các tests/workflow/artifacts đang dirty từ trước.
- Không thêm tests/fixtures, không chạy unit/integration/E2E/browser hoặc application runtime, không dùng SQL/provider/secret/data thật. Không tái dùng CI counts cũ chứng nhận source mới.
- Final five product-source SHA256 nằm trong `execution-source-fingerprints.csv`, để phân biệt working tree đã build với HEAD chưa commit.

## Chưa hoàn tất

Đây là implementation slice Goals, **không phải hoàn tất toàn remediation plan**. Habits/Planner, Windows3SID, P1 work packages và semantic RTM40FX chưa được sửa trong lượt này. Một số Habits sources mới được đọc để chuẩn bị, chưa có implementation claim.

Goals pending: owner SQL/API/browser verification, review độc lập security/shared receipt compatibility, contract resolution cho derived Goal list projection khi thiếu target.read, và UX reconciliation cho legacy committed result không có ID. Không đánh dấu FX16 hoặc P0 toàn bộ complete.

PR/local vẫn lệch 5 remote commits như rebaseline; chưa reconcile/publish, chưa commit/push/merge. Remote Finance/Notifications/Documents/shared UI repairs phải giữ khi reconcile. Independent review Pending, không thay bằng self-review.
