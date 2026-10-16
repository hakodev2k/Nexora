# FX-26 — Dashboard và Widgets

Product phase: **P03** · Delivery: **RM11** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** FX26-S01 read-only attention slice implemented locally on PR #4; layout/quick-create/provider widgets remain gated and runtime is not run. Goals có điều kiện không là quyền code. FX-26 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Current implementation overlay

The local slice implements `NXG-FX26-G01`/`G02` for four independent
owner-scoped source widgets through `getDashboard`. It does not claim the full
layout lifecycle in `NXG-FX26-G03` or any provider/quick-create behavior.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX26-G01 | Dashboard tổng hợp từ source providers theo metric rõ. | Count/drilldown cùng filter/snapshot; Skipped không Completed; không cộng nhiều currency hoặc expose Vault. |
| NXG-FX26-G02 | Provider lỗi/disabled chỉ ảnh hưởng widget tương ứng. | Một widget lỗi không làm hỏng toàn trang; module disabled không lộ cached balance; config giữ để re-enable hợp lệ. |
| NXG-FX26-G03 | Layout và quick capture dùng được và không bypass contracts. | Keyboard/mobile reorder, stale layout conflict; quick-create vẫn yêu cầu dates/type/editor. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Per-module widget/summary/quick-create contracts. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: Không có quyết định riêng mới từ bộ goals; vẫn phải qua current action gates, story DoR và implementation approval.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/26-dashboard.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/26-dashboard.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 8 rows: Resolved delegated: 8. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/26-dashboard.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-26-AC-001`, `FX-26-AC-002`, `FX-26-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
