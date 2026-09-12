# FX-21 — Bookmarks

Product phase: **P03** · Delivery: **RM09** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Specified; chưa approve implementation, chưa Implemented/Verified. Goals có điều kiện không là quyền code. FX-21 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX21-G01 | Bookmark quản lý URL/metadata thủ công mà không tự outbound. | Lưu URL khi không có fetch; không Open external hoặc autofetch; network chỉ sau quyết định liên quan. |
| NXG-FX21-G02 | Metadata và duplicate handling bảo toàn user edits. | Khác query không tự merge; manual title không bị refresh ghi đè; escaped content không execute. |
| NXG-FX21-G03 | Collections/lifecycle/share không cấp quyền ngoài resource. | Xóa collection không xóa bookmarks; Trash không search/share; projection theo action gate và owner. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Collections/Tags, safe resource providers. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: P-H07/Q-07: metadata fetching; external-open bị loại theo PO INTERNAL.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/21-bookmarks.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/21-bookmarks.md) — exact action keys, contexts, prerequisites, status/gate; five manual metadata rows are `SLICE_IMPLEMENTED (local)`, one is Superseded and the remaining rows stay gated. This is implementation status, not runtime coverage.
- [Manual slice evidence](../../implementation/bookmarks-manual-slice.md) — API/DB/security/UI boundary and verification ownership.
- [UX/screens](../../ux-ui/modules/21-bookmarks.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-21-AC-001`, `FX-21-AC-002`, `FX-21-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
