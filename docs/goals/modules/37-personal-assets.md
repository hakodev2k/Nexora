# FX-37 — Personal Assets, Inventory và Devices

Product phase: **P07** · Delivery: **RM15** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Specified; chưa approve implementation, chưa Implemented/Verified. Goals có điều kiện không là quyền code. FX-37 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX37-G01 | Assets quản lý inventory/device bằng một owner và graph hợp lệ. | Device subtype không duplicate asset; cycle/cross-owner component denied; Loaned không chuyển owner. |
| NXG-FX37-G02 | Purchase/warranty/maintenance history tồn tại qua trạng thái tài sản. | Sold/Disposed giữ invoice/history; renewal invalidates stale reminder; refs không ghi Finance tự động. |
| NXG-FX37-G03 | Delete và projections không phá nguồn hoặc lộ sensitive fields. | Accessory independent refs không cascade purge; no Vault dereference; serial/contact/invoice share giữ P-H03. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Files/Warranty, Finance/Vault refs, expiry Notifications. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: P-H03 sensitive projections; P-H05 nếu thêm finance effects.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/37-personal-assets.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/37-assets.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 29 rows: Blocked: 2, Resolved delegated: 27. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/37-personal-assets.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-37-AC-001`, `FX-37-AC-002`, `FX-37-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
