# Source review, scope gates và validation record

Task: PO yêu cầu viết goals từ phase đầu đến cuối và riêng mọi module, thêm docs vào GitHub. Authorization: documentation/add/update và PR; không application implementation/deployment. Base `main`: `e95a4b0c20f5677bada6a65ffbc1acbfd8735523` (PR #1 agent baseline đã merge). Branch dự kiến: `docs/phase-module-goals`.

## Nguồn dùng để xây goals

| Nguồn | Vai trò trong bộ goals |
| --- | --- |
| [Product charter](../requirements/00-product-charter.md), [catalog](../requirements/01-scope-and-module-catalog.md), [requirements index](../README.md) | GOAL-001…008, personal-only/Public SaaS và catalog committed |
| [Current PO decisions](../requirements/10-owner-decisions-20260907.md), [decision status](../features/90-open-decisions.md) | Quyền ưu tiên, soft-delete, TOTP, pause, manual Finance, formats và internal navigation |
| [Delivery scope](../delivery/01-current-scope.md), [proposals](../delivery/02-decision-proposals.md), [M01](../delivery/milestone-01/README.md) | Active slice khác full R1, D-H/P-H và exact stories/gates |
| [Master roadmap](../roadmap/00-master-implementation-roadmap.md), [Local Stable](../roadmap/09-local-stable-release.md), [Production](../roadmap/10-production-roadmap.md) | RM00–RM22 và gate local trước production |
| [Product phases](02-product-phase-goals.md) | Nối 9 phase requirement P00–P08 với delivery steps |
| [40 feature specs](../features/README.md), [shared behavior](../features/00-shared-behavior.md), [format contract](../features/95-docx-md-and-internal-calendar.md) | Outcome, lifecycle/field rules và AC riêng module; nguồn được link ở từng file goals |
| [Action catalog](../action-catalog/README.md), [authorization](../action-catalog/00-authorization-contract.md), [composition](../action-catalog/01-composition-and-field-guards.md) | Exact contexts/prerequisites/gates; không dùng scope flags thay authority |
| [Architecture](../architecture/README.md), [module boundary](../architecture/02-module-boundaries.md), [security recovery](../architecture/07-owner-decisions-security-and-recovery.md) | Contracts, owner, SQL/outbox/revocation và recovery không plaintext operator |
| [DB](../design-database/README.md), [bindings](../design-database/16-action-catalog-binding.md), [invariant tests](../design-database/13-query-and-invariant-tests.md), [UX](../ux-ui/README.md) | Source để agent bind concrete field/data/action/screen tests trước code; không sao chép toàn physical schema vào goals |
| [Capacity policy](../architecture/08-capacity-and-verification-policy.md), [NFR](../requirements/04-non-functional-requirements.md) | Performance/reliability/security evidence; không hứa unlimited, zero bugs hoặc SLA chưa chốt |
| [AGENTS](../../AGENTS.md), [.ai routing](../../.ai/routing.json), [verification](../../.ai/verification.md) | Mandatory loader, existing authorization, traceability, truthful evidence và independent review |

Skills/rules dùng cho lượt docs này: Nexora engineering startup; Technical Lead/core rules; architecture module-boundary/data-ownership rules; QA test-strategy/contract-testing procedures. Các procedures hướng dẫn thiết kế acceptance, không được dùng để tuyên bố tests ứng dụng đã chạy.

## Các bẫy source đã xử lý trong goals

| Bẫy khi đọc source riêng lẻ | Cách áp dụng đúng trong goals |
| --- | --- |
| Roadmap gọi Finance outcome là ledger correctness | FX27 hiện là manual records; advanced ledger/deletion chỉ conditional P-H05, không bị hủy khỏi catalog |
| Phase/feature cũ có external-open hoặc fetch mặc định | PO INTERNAL và P-H07 thắng; URL có thể là metadata, không auto navigate/fetch; notifications foundation riêng |
| P1 hoặc proposal cũ bị hiểu thành ngoài R1 | Không loại module/capability committed nếu thiếu PO rescope; không tự approve conditional extension |
| Phase 2 cũ nói Planner/Habits chưa có chi tiết | Current feature delegated specs cung cấp rules; vẫn phải tách concrete story + implementation approval, không tự đóng mọi phase gate |
| Header/table count lịch sử hoặc CurrentScope=true bị hiểu là runtime ready | Inventory không code; effective action gate, current delivery scope, readiness và approval cùng quyết định |
| TOTP method đã chốt bị hiểu là MFA recovery đã chốt | M01 password-only; P-H01 còn chặn enrollment/recovery release; fixture MFA-enabled không được bypass |
| Account/Vault soft-delete bị áp lên mọi table | Chỉ domains được PO nêu; Project/Task/Documents manual purge và Calendar Cancel giữ rules riêng |
| SuperAdmin Vault recovery bị hiểu là quyền xem secret | Request-bound recovery cho owner, no operator plaintext, không ambient Support/Emergency decrypt |
| 9 product phases bị dùng thay 23 RM hoặc M01 | Giữ cả ba cấp; goals map chúng, không rename IDs hoặc cho M01 thành toàn R1 |

## Gate register

| Gate | Capability bị ảnh hưởng | Điều kiện trước khi mở / kết luận hoàn tất |
| --- | --- | --- |
| Implementation approval | Mọi application slice | PO/session approve bounded scope; không lấy lượt docs này làm code approval |
| P-H01 | MFA lost-device recovery/enrollment release | PO chốt policy + cập nhật contracts/AC/security review |
| P-H02 | Account restore/email reuse | Quyết định identity/owner recovery; không auto-reactivate/reuse |
| P-H03 | Sensitive sharing/support projections | Allowlist/consent/scope approved theo resource; safe-looking field không tự được phép |
| P-H04 | Vault owner portability/safe metadata | Policy approved, crypto/format/recovery contracts và security evidence |
| P-H05 | Advanced Finance và deletion semantics | Business rules được chốt; basic record completion không đóng advanced backlog |
| P-H06 | Subtask/recurrence/snooze/standalone reminder/attachments | PO scope + data/lifecycle/timezone contracts; flat Task core giữ nguyên |
| P-H07 | Backend ingestion/network tools/remote monitoring | Phạm vi outbound/provider/cost approved; không tự resume paused modules |
| Q-06/Q-07 Paused | FX30/34/35 | PO resume rõ ràng, rồi đóng rule/provider gates và verify; Allow/default-on không vượt pause |
| P-H08/Q-08 | Full R1, capacity, budget, RPO/RTO/SLA | Catalog accepted/rescoped, measured workload, ops ownership và PO commitments; không tự chọn số thành guarantee |
| Independent review | Security/data/lifecycle/cross-module changes theo AGENTS | Review thực của reviewer khác và disposition findings; thiếu thì Pending, không self-sign-off |
| Local Stable / deployment / public Go | RM18 / RM21 / RM22 | Ba cổng khác nhau theo roadmap; không merge/publish/provision nhờ docs approval |

## Validation của tài liệu

Bộ goals có 40 module files/120 module goals, 9 product-phase goals, 23 delivery-step goals, 5 M01 goals, 16 system goals và 15 cross-module goals. Tổng 188 goal IDs khác nhau; số lượng chỉ dùng kiểm tra không bỏ sót, không đo chất lượng hay runtime completion.

Kiểm tra trước PR: đúng FX01–40 một lần; link tương đối tồn tại; IDs không trùng; source AC IDs tồn tại; module/action/UX mappings đầy đủ; current gate counts lấy từ source catalog. Chạy baseline verifier vì root startup/routing được cập nhật. Không compile/test ứng dụng, tạo SQL/migration, thêm dependencies hoặc deploy trong task này. Kết quả kiểm tra thực tế và review disposition sẽ được ghi tại PR.

### Kết quả thực hiện trước PR

- PASS: 48 Markdown files, coverage đúng 40 FX, 188 goal definitions không trùng; relative links và referenced source AC IDs hợp lệ.
- PASS: `python3 .ai/scripts/verify-baseline.py` — cấu trúc package, 10 routing groups, 15 regression tests của agent gates. Đây không phải tests của app Nexora.
- PASS: `git diff --check`.
- Independent documentation reviewer: `review_goals`; phát hiện scope của Admin absent/Deny cần phân biệt User SELF và cần nhắc rõ Calendar ICS Task-projection exception. Đã sửa SYS03/FX02/X02 và FX13/RM08; reviewer kiểm tra lại, không còn material finding trong phạm vi review. Không phải penetration test hoặc runtime sign-off.
