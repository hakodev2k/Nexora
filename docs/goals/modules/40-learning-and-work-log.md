# FX-40 — Skills, Courses, Certifications, Learning Plan và Work Log

Product phase: **P07** · Delivery: **RM15** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Specified; chưa approve implementation, chưa Implemented/Verified. Goals có điều kiện không là quyền code. FX-40 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX40-G01 | Learning progress và skill evidence không tự suy thành tích. | No milestones không 100%; proficiency không inferred từ hours; Complete explicit; unavailable source không fake done. |
| NXG-FX40-G02 | Certifications/plan links bảo toàn lịch sử và source authority. | Renew giữ prior expiry và một pending reminder; remove course/plan link không delete Tasks/Goals. |
| NXG-FX40-G03 | Work Log không double count hoặc trở thành payroll. | Time Entry link unique; gross duration/overlap minh bạch; archive read-only; credentialID/sensitive share giữ gate. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Tasks/Goals/Courses source contracts, Time Tracking, Notifications. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: P-H03 sensitive projections; không payroll/auto assessment.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/40-learning-and-work-log.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/40-learning.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 58 rows: Blocked: 2, Resolved delegated: 56. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/40-learning.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-40-AC-001`, `FX-40-AC-002`, `FX-40-AC-003`, `FX-40-AC-004`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
