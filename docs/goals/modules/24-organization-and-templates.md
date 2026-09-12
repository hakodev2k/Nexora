# FX-24 — Tags, Collections và Templates

Product phase: **P03** · Delivery: **RM09** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** FX24-S01 Tag catalog `SLICE_IMPLEMENTED (local)` trên PR #4; assignment, Collections và Templates vẫn gated, runtime chưa verify. Goals có điều kiện không là quyền code. FX-24 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX24-G01 | Tag/Collection tôn trọng namespace và resource authority. | Project/Task chung tags, Documents riêng một Tag/page; collection không cấp quyền hoặc lộ count/title ẩn. |
| NXG-FX24-G02 | Template tạo dữ liệu mới hợp lệ, không copy authority/history. | Không copy owner/IDs/shares/secrets/sent reminders; Documents vẫn chọn mode/type; Task vẫn cần active Project/dates. |
| NXG-FX24-G03 | Xóa/sửa cấu trúc tổ chức không làm mất nguồn. | Template update không sửa resource cũ; collection delete giữ members; Documents tag đang current/Archived/Trash bị chặn xóa. |

## Slice overlay trên PR #4

FX24-S01 hiện thực mục tiêu Tag catalog ở phạm vi local: tag owner-scoped,
namespace cố định (`projects`, `documents`, `bookmarks`, `snippets`), tên unique
case-insensitive, màu tùy chọn, usage projection và ETag/If-Match. `TagInUse`
chặn xóa khi `ResourceTag` có tham chiếu. Không suy ra rằng goal đã hoàn tất
cho assignment, Collections hoặc Templates; các phần đó cần slice riêng.

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Per-module creation/summary contracts. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: Sensitive collection projections theo action/provider gate, không tự serialize members.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/24-organization-and-templates.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/24-organization.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 23 rows: Blocked: 1, Resolved delegated: 22. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/24-organization-tags-templates.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-24-AC-001`, `FX-24-AC-002`, `FX-24-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
