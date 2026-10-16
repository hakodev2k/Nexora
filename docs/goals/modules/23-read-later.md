# FX-23 — Read Later

Product phase: **P03** · Delivery: **RM09** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Bookmark-reference local subset đã `SLICE_IMPLEMENTED` trên PR #4 theo `DEC-20260909-014`; News/body extraction, reader and sensitive projections remain gated. Goals không tự thay thế contract/evidence và không là tuyên bố toàn FX-23 hoàn tất.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX23-G01 | Read Later là queue tham chiếu Bookmark, không bản sao nguồn. | Save retry một entry; remove không xóa Bookmark; không tự fetch/copy body. |
| NXG-FX23-G02 | Reading state và position metadata nhất quán theo owner/source contract. | User A không ảnh hưởng B; state `Unread/Reading/Read` và position 0–1 qua SQL/ETag; không fake percent khi thiếu body. |
| NXG-FX23-G03 | Nguồn unavailable không để snapshot thành lối truy cập body khác. | Bookmark thiếu/disabled hoặc mất read grant chỉ trả safe snapshot + `SourceAvailable=false`; remove vẫn chỉ gỡ queue reference. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Bookmarks source/read contract đã đủ cho local slice; News source/read-state contract chưa có runtime implementation. Chỉ truy cập module khác qua source contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: External-open bị loại theo PO INTERNAL; không autofetch thêm nội dung.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/23-read-later.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/23-reading.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 7 rows: 6 local slice actions và 1 support action còn gated. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/23-read-later.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-23-AC-001`, `FX-23-AC-002`, `FX-23-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
