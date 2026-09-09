# FX-27 — Finance — current basic manual records

Product phase: **P04** · Delivery: **RM12** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Specified; chưa approve implementation, chưa Implemented/Verified. Goals có điều kiện không là quyền code. FX-27 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX27-G01 | Finance basic là category và manual amount/currency/date/note. | Tạo record không cần ledger account; 0 hợp lệ, negative/overflow/ambiguous float denied; duplicate retry một record. |
| NXG-FX27-G02 | Edits và totals đúng owner, decimal và từng currency. | Foreign category denied; stale edit conflict; vi/en/timezone không đổi amount/currency/date-only; không cộng currency khác. |
| NXG-FX27-G03 | Không ngầm triển khai ledger hoặc deletion chưa chốt. | Used category delete denied; record delete unavailable; không accounts/transfer/budget/debt/FX hoặc sensitive share khi quyết định liên quan chưa được duyệt. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Owner SQL, category/record concurrency, projection policies. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: P-H05 advanced Finance và record deletion; P-H03 sensitive projections. Core manual records riêng.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/27-finance.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/27-finance.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 53 rows: Blocked: 45, Resolved delegated: 8. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/27-finance.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX27-MAN-AC01`, `FX27-MAN-AC02`, `FX27-MAN-AC03`, `FX27-MAN-AC04`, `FX27-MAN-AC05`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
