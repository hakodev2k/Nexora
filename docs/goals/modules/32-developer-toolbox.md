# FX-32 — Developer Toolbox

Product phase: **P06** · Delivery: **RM14** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** FX32-S01/S02 pure local subset implemented on PR #4; runtime/functional verification remains owner-owned. Goals có điều kiện không là quyền code. FX-32 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

The implemented subset is tracked in [developer-toolbox-pure-slice](../../implementation/developer-toolbox-pure-slice.md). XML/YAML/CSV, advanced formatting, QR/certificate, history/favorites, Save-to-Snippet and network actions remain gated.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX32-G01 | Pure tools xử lý deterministic, bounded và không thực thi input. | XXE/regex pathological không network/hang; SQL/code formatter không execute; lossy convert có warning. |
| NXG-FX32-G02 | Security labels và dữ liệu người dùng trung thực. | JWT decode không verified; certificate parse không chain-trusted; QR không navigate; epoch units explicit. |
| NXG-FX32-G03 | Tool history và network không mở rộng scope. | Input không persist mặc định, secret-like không persistent history; SaveAsSnippet explicit; HTTP/DNS actions giữ gate. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Bounded pure parsers/formatters, Snippets explicit save. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: P-H07/Q-07 network tools chưa chạy; thuần local có scope riêng.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/32-developer-toolbox.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/32-toolbox.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 33 rows: Paused: 2, Resolved delegated: 31. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/32-developer-toolbox.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-32-AC-001`, `FX-32-AC-002`, `FX-32-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
