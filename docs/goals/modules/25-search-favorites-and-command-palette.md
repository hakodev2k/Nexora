# FX-25 — Search, Saved Search, Favorites và Command Palette

Product phase: **P03** · Delivery: **RM11** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** FX25-S01 bounded source-query search and FX25-S03 owner-scoped Favorites slices are implemented locally on PR #4; Recents, Saved Search and Command Palette remain gated and runtime is not run. Goals có điều kiện không là quyền code. FX-25 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Current implementation overlay

The local slices implement `NXG-FX25-G01` and the access/failure boundary in
`NXG-FX25-G02` through source-query Search and typed Favorites. Favorites do
not create source authority, copy source payloads or bypass current lifecycle;
Recents, Saved Search, Command Palette and persisted index behavior remain
gated.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX25-G01 | Search/favorites/recents không tạo authority ngoài current policy. | Stale index/cache sau revoke không lộ count/snippet; Vault payload/audit reasons không index; Trash/disabled excluded. |
| NXG-FX25-G02 | Search semantics và saved queries có version rõ. | Local Documents Title/Tag khác global provider; unknown saved-query version báo cần migrate; cancel stale response. |
| NXG-FX25-G03 | Command palette gọi đúng workflow hiện hành. | Destructive command mở preview/confirm; quick-create không bỏ required fields; clear recent không xóa Audit/resource. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Per-module safe search providers and access evaluator. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: Không có quyết định riêng mới từ bộ goals; vẫn phải qua current action gates, story DoR và implementation approval.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/25-search-favorites-and-command-palette.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/25-discovery.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 15 rows with the current local Search/Favorites overlays. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/25-search-favorites-command-palette.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-25-AC-001`, `FX-25-AC-002`, `FX-25-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
