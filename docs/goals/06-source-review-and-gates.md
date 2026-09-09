# Source review, scope gates và validation record

Task gốc: PO yêu cầu viết goals từ phase đầu đến cuối và riêng mọi module, thêm docs vào GitHub. Authorization lúc tạo file gốc: documentation/add/update và PR; không application implementation/deployment.

## Current addendum — 2026-09-09

Product Owner đã approve `DEC-20260909-001`: **M01 stories S00-S11 + backend/frontend scaffold + local scripts** được phép implement local-first. Goals vẫn không tự cấp approval, nhưng implementation agents không được coi toàn bộ application là blocked nữa nếu task nằm đúng M01 package đã approved.

Current PO decision source mới: [requirements/11-owner-decisions-20260909-implementation-readiness.md](../requirements/11-owner-decisions-20260909-implementation-readiness.md). Decision status mới: [features/90-open-decisions.md](../features/90-open-decisions.md). Delivery boundary mới: [delivery/01-current-scope.md](../delivery/01-current-scope.md).

## Nguồn dùng để xây goals

| Nguồn | Vai trò trong bộ goals |
| --- | --- |
| [Product charter](../requirements/00-product-charter.md), [catalog](../requirements/01-scope-and-module-catalog.md), [requirements index](../README.md) | GOAL-001…008, personal-only/Public SaaS và catalog committed |
| [Current PO decisions 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md), [Current PO decisions 2026-09-07](../requirements/10-owner-decisions-20260907.md), [decision status](../features/90-open-decisions.md) | Approval boundary, soft-delete/no-email-reuse, TOTP recovery-code policy, sensitive projections, Vault hybrid policy, basic Finance, flat Task first slice, outbound boundary, paused modules và production sequencing |
| [Delivery scope](../delivery/01-current-scope.md), [proposals](../delivery/02-decision-proposals.md), [M01](../delivery/milestone-01/README.md) | Approved M01 package, active slice khác full R1, resolved/gated policy và exact stories/evidence gates |
| [Master roadmap](../roadmap/00-master-implementation-roadmap.md), [Local Stable](../roadmap/09-local-stable-release.md), [Production](../roadmap/10-production-roadmap.md) | RM00–RM22 và gate local trước production |
| [Product phases](02-product-phase-goals.md) | Nối 9 phase requirement P00–P08 với delivery steps |
| [40 feature specs](../features/README.md), [shared behavior](../features/00-shared-behavior.md), [format contract](../features/95-docx-md-and-internal-calendar.md) | Outcome, lifecycle/field rules và AC riêng module; nguồn được link ở từng file goals |
| [Action catalog](../action-catalog/README.md), [authorization](../action-catalog/00-authorization-contract.md), [composition](../action-catalog/01-composition-and-field-guards.md) | Exact contexts/prerequisites/gates; không dùng scope flags thay authority |
| [Architecture](../architecture/README.md), [module boundary](../architecture/02-module-boundaries.md), [security recovery](../architecture/07-owner-decisions-security-and-recovery.md) | Contracts, owner, SQL/outbox/revocation và recovery không plaintext operator |
| [DB](../design-database/README.md), [bindings](../design-database/16-action-catalog-binding.md), [invariant tests](../design-database/13-query-and-invariant-tests.md), [UX](../ux-ui/README.md) | Source để agent bind concrete field/data/action/screen tests trước code; không sao chép toàn physical schema vào goals |
| [Capacity policy](../architecture/08-capacity-and-verification-policy.md), [NFR](../requirements/04-non-functional-requirements.md) | Performance/reliability/security evidence; Local Stable trước production, không hứa unlimited, zero bugs hoặc SLA chưa chốt |
| [AGENTS](../../AGENTS.md), [.ai routing](../../.ai/routing.json), [verification](../../.ai/verification.md) | Mandatory loader, existing authorization, traceability, truthful evidence và independent review |

Skills/rules dùng cho lượt docs này: Nexora engineering startup; Technical Lead/core rules; architecture module-boundary/data-ownership rules; QA test-strategy/contract-testing procedures. Các procedures hướng dẫn thiết kế acceptance, không được dùng để tuyên bố tests ứng dụng đã chạy.

## Các bẫy source đã xử lý trong goals

| Bẫy khi đọc source riêng lẻ | Cách áp dụng đúng trong goals |
| --- | --- |
| Roadmap gọi Finance outcome là ledger correctness | FX27 initial scope hiện là manual records; advanced ledger/deletion conditional, không bị hủy khỏi catalog |
| Phase/feature cũ có external-open hoặc fetch mặc định | PO INTERNAL và outbound boundary mới thắng; URL có thể là metadata; read-only outbound News/GitHub/Monitoring cần allowlist/SSRF/rate-limit/degraded-state contract; notifications foundation riêng |
| P1 hoặc proposal cũ bị hiểu thành ngoài R1 | Không loại module/capability committed nếu thiếu PO rescope; không tự approve conditional extension |
| Phase 2 cũ nói Planner/Habits chưa có chi tiết | Current feature delegated specs cung cấp rules; vẫn phải tách concrete story + implementation approval, không tự đóng mọi phase gate |
| Header/table count lịch sử hoặc CurrentScope=true bị hiểu là runtime ready | Inventory không code; effective action gate, current delivery scope, readiness và approval cùng quyết định |
| TOTP method đã chốt bị hiểu là MFA enrollment/recovery đã ship | M01 password-only; recovery-code policy đã chốt nhưng implementation ngoài M01; fixture MFA-enabled không được bypass |
| Account/Vault soft-delete bị áp lên mọi table | Account policy no-purge/no-email-reuse đã chốt; các domain lifecycle vẫn theo rules riêng của module |
| SuperAdmin Vault recovery bị hiểu là quyền xem secret | Hybrid recovery không cho operator ambient plaintext/export; không ambient Support/Emergency decrypt |
| 9 product phases bị dùng thay 23 RM hoặc M01 | Giữ cả ba cấp; goals map chúng, không rename IDs hoặc cho M01 thành toàn R1 |

## Gate register

| Gate | Capability bị ảnh hưởng | Điều kiện trước khi mở / kết luận hoàn tất |
| --- | --- | --- |
| M01 implementation approval | M01 S00-S11 + backend/frontend scaffold + local scripts | **Approved by DEC-20260909-001**. Still requires implementation PR, tests and evidence before `Implemented`/`Verified locally`. |
| Future implementation approval | Mọi slice ngoài M01 | PO/session approve bounded scope; không lấy M01 approval làm full Phase1/R1 approval |
| TOTP lost-device recovery implementation | MFA enrollment/recovery release | Product policy approved: recovery codes. Implementation still needs slice approval + schema/API/UX/AC/security review. |
| Account restore implementation | Account restore/email reuse | Product policy approved: same-account restore only, no email reuse, no purge. Restore UI/API still outside M01. |
| Sensitive share/support projections | Finance/Vault/Assets/Career/Learning share/support | Policy approved: allowlist/default-hidden/safe metadata. Each resource still needs concrete projection contracts and tests. |
| Vault portability/recovery implementation | Vault release/owner export/import/recovery | Policy approved hybrid/no-operator-plaintext. Crypto/key/package ADR and restore/security evidence required. |
| Advanced Finance | Ledger/debt/FX/budget/transfers/advanced deletion | Basic manual records approved first. Advanced Finance needs later PO decision and contracts. |
| Task/Reminder extensions | Subtask/recurrence/snooze/standalone reminder/attachments | Flat Task + one reminder approved first. Extensions need later PO decision and contracts. |
| Read-only outbound | News/GitHub/Monitoring public metadata/feed/probe | Boundary approved. Needs slice contract, allowlist, SSRF, redirect/payload/timeout/rate-limit and degraded-state evidence. |
| Q-06/Q-07 Paused | FX30/34/35 | Still paused and not moved to R2. Resume requires explicit PO decision. |
| Production/capacity | Full R1, provider, budget, RPO/RTO/SLA | Local Stable first. Production targets and Go/No-Go require later evidence and PO approval. |
| Independent review | Security/data/lifecycle/cross-module changes theo AGENTS | Review thực của reviewer khác và disposition findings; thiếu thì Pending, không self-sign-off |
| Local Stable / deployment / public Go | RM18 / RM21 / RM22 | Ba cổng khác nhau theo roadmap; M01 approval không deploy/publish/provision |

## Validation của tài liệu

Bộ goals có 40 module files/120 module goals, 9 product-phase goals, 23 delivery-step goals, 5 M01 goals, 16 system goals và 15 cross-module goals. Tổng 188 goal IDs khác nhau; số lượng chỉ dùng kiểm tra không bỏ sót, không đo chất lượng hay runtime completion.

Kiểm tra trước PR gốc: đúng FX01–40 một lần; link tương đối tồn tại; IDs không trùng; source AC IDs tồn tại; module/action/UX mappings đầy đủ; current gate counts lấy từ source catalog. Chạy baseline verifier vì root startup/routing được cập nhật. Không compile/test ứng dụng, tạo SQL/migration, thêm dependencies hoặc deploy trong task đó.

### Kết quả thực hiện trước PR gốc

- PASS: 48 Markdown files, coverage đúng 40 FX, 188 goal definitions không trùng; relative links và referenced source AC IDs hợp lệ.
- PASS: `python3 .ai/scripts/verify-baseline.py` — cấu trúc package, 10 routing groups, 15 regression tests của agent gates. Đây không phải tests của app Nexora.
- PASS: `git diff --check`.
- Independent documentation reviewer: `review_goals`; phát hiện scope của Admin absent/Deny cần phân biệt User SELF và cần nhắc rõ Calendar ICS Task-projection exception. Đã sửa SYS03/FX02/X02 và FX13/RM08; reviewer kiểm tra lại, không còn material finding trong phạm vi review. Không phải penetration test hoặc runtime sign-off.

### Validation của 2026-09-09 amendment

- Docs-only update. No application code, SQL migration, package install, runtime test or production deployment executed by this amendment.
- Required follow-up: implementation PR must generate actual evidence for M01 before claiming `Implemented` or `Verified locally`.
