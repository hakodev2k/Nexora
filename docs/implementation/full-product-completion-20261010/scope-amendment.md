# Proposed scope amendment — FX30 / FX34 / FX35

Trạng thái tài liệu: Proposed PR amendment ghi nhận chỉ thị PO trong phiên làm việc hiện tại. Chỉ thị session có hiệu lực cho engineering trong phạm vi nêu dưới; việc nhập amendment này vào main chưa diễn ra. Không gán DEC ID mới hoặc approval timestamp.

## Task brief và authority

Accountable owner: implementation agent / Technical Lead. Repository hakodev2k/Nexora; PR #4; branch `impl/m01-s00-scaffold`; source HEAD `74771b70a262fa0b9d47bfc038dd3dbea23aa7dd`; main đã kiểm tra qua branch API: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`.

Nguồn main `docs/requirements/11-owner-decisions-20260909-implementation-readiness.md`, DEC-20260909-009, Q-06/Q-07 và delivery/goals vẫn ghi pause. Không sửa lịch sử hoặc coi PR-only commentary là quyết định PO mới. `docs/requirements/12-owner-decisions-local-e2e-implementation.md` tồn tại ở PR HEAD, không tồn tại ở main đã kiểm tra (404). Authority cho lần tiếp tục này là lời PO trực tiếp, không suy từ tài liệu PR.

Trích nguyên văn chỉ thị PO hiện tại:

> The Product Owner has now explicitly requested full development of FX30, FX34 and FX35, including real-provider integration capability.

> This explicit new Product Owner instruction supersedes that pause **for implementation planning, local coding and simulated verification of these three modules only**.

> **Critical distinction: authorization to implement real-provider functionality is not authorization to activate live provider operations.**

Cho phép hoàn thành documented contract-ready engineering, real-provider adapters, SQL/API/React, secure jobs/webhook/scheduler infrastructure, simulation, isolated synthetic SQL/test fixtures, independent reviews và validated commits trên PR #4. Yêu cầu test hiện tại supersede code-only/no-new-fixtures amendment cũ cho nhiệm vụ này. Không xin lại blanket implementation approval.

Không tự bật module cho account hiện hữu/mới. Không live API, credential provisioning/account registration, paid quota, external mutation/notification, public webhook endpoint, production, merge hoặc PR khác. Live quota mặc định zero. Real-provider configuration/credential presence không cấp quyền thực thi.

## Trạng thái độc lập

| Module | Catalog rows | Engineering authorization | Contract readiness | Functional/security acceptance | Real-provider readiness | Activation / production |
| --- | ---: | --- | --- | --- | --- | --- |
| FX30 | 18 | Local code + simulated verification authorized | Exact action audit pending; D01/D02 và dependencies cần đối chiếu | Chưa xác minh trong phiên này | Chưa chứng minh | PROVIDER_INACTIVE; G3/G4 chưa approve |
| FX34 | 20 | Local code + simulated verification authorized | Exact action audit pending; D03, schedule policies và dependencies cần đối chiếu | Chưa xác minh trong phiên này | Chưa chứng minh | PROVIDER_INACTIVE đối với external actions; G3/G4 chưa approve |
| FX35 | 21 | Local code + simulated verification authorized | Exact action audit pending; D04–D09 và dependencies cần đối chiếu | Chưa xác minh trong phiên này | Chưa chứng minh | PROVIDER_INACTIVE; public inbound exposure denied; G3/G4 chưa approve |

59 rows không chuyển thành Implemented. Inventory lịch sử được giữ nguyên; từng action cần handler/schema/UI/permission/AC/evidence audit riêng. MISSING, PARTIALLY_IMPLEMENTED hay IMPLEMENTED_UNVERIFIED chỉ được gán sau kiểm tra source đầy đủ. Provider inactivity không đồng nghĩa implementation pause.

Goals: NXG-SYS-02/03/05/06/08/12/14/16 và NXG-FX30-G01…G03, NXG-FX34-G01…G03, NXG-FX35-G01…G03. Goal G01 về pause được đọc cùng chỉ thị mới: local engineering đã resume, external activation vẫn denied. Feature AC references cần audit trước code: FX-30-AC-001…003; FX-34-AC-001…004; FX-35-AC-001…003. Phase P05-PRD/PHS/ALT, P06-AUT/WHK/N8N và INT contracts chưa được kiểm tra đầy đủ trong phiên này; không tuyên bố slice-ready.

## Gate register v1 — candidate provider operations

Danh sách này là candidate scope, không là connector registry được duyệt. Khi D04 xác định provider khác, bổ sung row riêng cho từng provider/operation; không generic arbitrary-provider approval.

| Provider / operation | G1 implementation | G2 simulation | G3 pilot | G4 production | Approval evidence |
| --- | --- | --- | --- | --- | --- |
| Shopee — identity/variant preview, price fetch | Authorized engineering; exact authorized source contract unresolved | NotRun | BLOCKED_BY_DECISION D01/D02/D06/D07/D09 | Not approved | Session chỉ approve engineering |
| n8n — outbound selected Nexora events | Authorized engineering; target/projection/auth unresolved | NotRun | BLOCKED_BY_DECISION D04/D05/D06/D07/D09 | Not approved | Không provider-specific pilot approval |
| n8n — inbound permitted commands | Authorized engineering; commands/signature/target unresolved | NotRun | BLOCKED_BY_DECISION D04/D05/D06/D08/D09 | Not approved | Không public exposure approval |
| Provider chưa chọn — connection test / OAuth / webhook transport | Chưa có provider-operation contract | NotRun | BLOCKED_BY_DECISION D04/D06/D07/D09 | Not approved | Không suy rộng approval |

FX34 external action types phải tham chiếu exact provider-operation gate; save definition không cấp quyền execute. G2 success không mở G3; G3 không mở G4. Simulation must reject any accidental real transport.

## Decision requests — chưa giải quyết

Mỗi row là request để đối chiếu current source trước khi PO chốt; recommendation không là policy approved.

| Request | Options | Recommendation | Effect / safe interim |
| --- | --- | --- | --- |
| D01 authorized Shopee source | Authorized provider contract đủ selected-variant scope / simulation-only | Simulation-only cho tới proof of legal API eligibility, regions, quotas, storage rights | Không private endpoint, scraping bypass, cookies hoặc fabricated observation |
| D02 comparable prices / alert semantics | Explicit region/currency/variant + base/sale/shipping/tax/voucher contract / defer affected calculations | Chốt exact comparable series và crossing/rearm/cooldown trước rule implementation | Không mix series; giữ last valid observation; unknown không price zero/OutOfStock |
| D03 executable registry / topology | PO chọn bounded trusted action/trigger topology / defer execution | Registry allowlist; PO chốt sequence/graph và mappings | Arbitrary code/shell/SQL/HTTP/module upload/privilege changes denied |
| D04 providers / n8n directions | Self-hosted / cloud / both; approve inbound và outbound riêng | Chọn một bounded target và từng event/command trước | Không generic provider support hoặc ambient authority |
| D05 data projections | Explicit per-event field allowlist / no external projection | Approve minimum nonsensitive fields trước | Vault/Finance/private documents/full records không tự forward |
| D06 auth / secret lifecycle | Reviewed scoped auth/encrypted reference/rotation/revocation contract / simulation credentials only | Simulation-only đến khi secure storage và key lifecycle review xong | Không provision real secrets; không plaintext logs/SQL/Redis |
| D07 budgets | Explicit approved request/cost/concurrency/retry limits / no live budget | Zero live quota cho tới approval | Không biến số đề xuất thành operator budget |
| D08 retention | Per-record retention approved + independent audit retention / defer affected cleanup | PO chốt observations/runs/logs/payload metadata/credentials/audit riêng | Không destructive cleanup hoặc gộp audit vào Trash |
| D09 activation | Named bounded G3 pilot / continue simulation-only; G4 riêng | Simulation-only đến khi provider-specific readiness/owner/kill-switch/incident/rollback approval đủ | Không public endpoint, pilot hoặc production tự động |

## Loaded instructions / gates

Đã đọc qua GitHub connector tại pinned PR HEAD: AGENTS.md; .agents/skills/nexora-engineering/SKILL.md; .ai/profiles/nexora-implementation-agent.md; .ai/roles/technical-lead/README.md và rules/core-rules.md; .ai/routing.json; .ai/verification.md; verification route gồm contract-testing.md, test-strategy.md và ground-truth README/completion-claim-policy.md/evidence-grounded-verification.md. Đã đọc baseline docs/README.md, delivery/README.md, delivery/01-current-scope.md, goals/README.md, goals/01-system-goals.md, goals/05-task-and-evidence-template.md và ba module goals trên pinned main.

Architecture/backend/database/security/frontend/owner-isolation routes và exact story/API/DB/UX contracts phải đọc trước affected code; chưa tuyên bố đã load chúng. Independent security/code review pending, không thay bằng self-review hoặc CI.

## Impact và next gate

Amendment chỉ ghi authority/audit; không code, migration, account flags, grants, secrets hoặc runtime state thay đổi. Rollback: revert documentation commit. Main và historical decisions nguyên trạng.

Local shell không khởi tạo được; Node runtime cũng exit/reset. Không thể inspect dirty worktree, actual SQL migration journal, private runtime hoặc chạy build/tests. Tiếp tục implementation cần khôi phục host execution/filesystem runtime, không cần blanket PO permission. Không cấp migration number mới trước reconciliation. Baseline findings và next safe slice ở baseline-audit.md.
