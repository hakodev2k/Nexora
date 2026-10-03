# FX-22 — Code Snippets

Product phase: **P03** · Delivery: **RM09** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Text/version subset `SLICE_IMPLEMENTED (local)`; history/export/share/advanced lifecycle chưa Implemented/Verified. Goals có điều kiện không là quyền code. FX-22 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX22-G01 | Snippet là code dưới dạng dữ liệu, không execution. | Script trong preview/diff/share không chạy; không gửi tới AI/lint service; credential warning không log payload. |
| NXG-FX22-G02 | Save/restore phiên bản không silent overwrite. | Retry một revision; stale restore conflict; Archive read-only, history giữ tới purge. |
| NXG-FX22-G03 | Copy/export/share đúng scope và định dạng. | Unicode/newline giữ nguyên, filename safe; no history/private metadata ngoài projection; cross-owner search denied. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Version/history, Sharing, syntax renderer. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: Không có quyết định riêng mới từ bộ goals; vẫn phải qua current action gates, story DoR và implementation approval.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/22-snippets.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/22-snippets.md) — exact action keys, contexts, prerequisites, status/gate; five text/version rows are `SLICE_IMPLEMENTED (local)`, other rows remain gated. Đây là implementation status, không phải runtime coverage.
- [Text/version slice evidence](../../implementation/snippets-text-slice.md) — API/DB/security/UI boundary and verification ownership.
- [UX/screens](../../ux-ui/modules/22-snippets.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-22-AC-001`, `FX-22-AC-002`, `FX-22-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
