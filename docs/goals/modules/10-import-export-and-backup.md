# FX-10 — Import, Export và Backup/Restore

Product phase: **P01** · Delivery: **RM06/RM09/RM21** · Phase 8/RM16–RM22 kiểm chứng tích hợp và phát hành.

**Trạng thái:** Specified; chưa approve implementation, chưa Implemented/Verified. Goals có điều kiện không là quyền code. FX-10 phải đọc cùng [goals toàn hệ thống](../01-system-goals.md), [hợp đồng dùng goals](../README.md) và sources hiện hành.

## Mục tiêu và bằng chứng chấp nhận

| Goal ID | Outcome phải giữ khi code | Bằng chứng test tối thiểu khi thực thi |
| --- | --- | --- |
| NXG-FX10-G01 | Portability theo format và policy từng module. | DOCX/MD và Calendar ICS hợp lệ; Project/Task generic export denied; unsupported/lossy import có preview/report. |
| NXG-FX10-G02 | Import/export không bypass owner, immutable fields hoặc current access. | Preview không mutation; retry commit không duplicate; download sau revoke bị chặn; output không audit/secret. |
| NXG-FX10-G03 | Backup/restore chứng minh consistency và phục hồi thật. | Khôi phục isolated SQL/files/keys đúng manifest; thiếu key/corruption không Success; không replay side effects cũ. |

## Ownership, dependencies và giới hạn

- Contracts phụ thuộc: Files, format providers, operations/key recovery. Chỉ truy cập module khác qua contract, không trực tiếp table/DbContext/private store.
- Phạm vi/gate: P-H04 Vault portability; P-H08 production backup/RPO/RTO. DOCX/MD và ICS đã chốt.
- Mọi source lifecycle/field/action/AC vẫn bắt buộc; ba goals là điểm kiểm soát outcome, không thu hẹp chức năng nguồn. Không tự lấy feature của sản phẩm tham chiếu, numeric proposal hoặc historical roadmap làm scope.
- Dữ liệu tổng hợp/cache không là authority; direct API, response, file, search/count và queued job đều theo current actor/context/owner/lifecycle. Các goal không cấp quyền cho PUBLIC/CONTROL/LOCAL ngoài handler đã đăng ký.

## Traceability và kiểm chứng

- [Feature/BR/AC nguồn](../../features/10-import-export-and-backup.md) — đọc toàn bộ field/state/validation và trace requirements tại đó.
- [Action contracts](../../action-catalog/modules/10-transfer.md) — exact action keys, contexts, prerequisites, status/gate; catalog có 13 rows: Blocked: 6, Resolved delegated: 7. Đây là inventory, không phải coverage đã pass.
- [UX/screens](../../ux-ui/modules/10-import-export-backup.md) — luồng màn hình, disabled/loading/error và interaction contracts.
- [DB binding](../../design-database/16-action-catalog-binding.md), [DB integrity tests](../../design-database/13-query-and-invariant-tests.md), [field classification](../../design-database/15-field-classification.md).
- AC hiện hành cần kế thừa: `FX-10-AC-001`, `FX-10-AC-002`, `FX-10-AC-003`. Cộng source requirement AC, POAC liên quan và [cross-module journeys](../04-cross-module-verification.md); danh sách này không thay test plan đầy đủ.

Trước code, bind từng action/story được approve với **một hoặc nhiều** goal ở trên và exact source requirement/AC, DTO/DB/UX contract. Chọn test layer theo rủi ro: unit cho formula/state; real SQL cho constraint/transaction/race; API cho quyền/errors; browser cho interaction; integration cho file/job/provider boundaries. Không suy test coverage từ việc link tồn tại.

## Điều kiện đóng module

Toàn bộ committed capability của FX có implementation + passing evidence đúng revision, hoặc quyết định PO đổi scope tường minh. Mọi blocked/paused/conditional capability phải có disposition, không được đánh dấu Passed bằng placeholder/mock. Agent điền [evidence template](../05-task-and-evidence-template.md), ghi rõ subset đã hoàn tất, phần còn chặn và review độc lập khi bắt buộc. **Hoàn tất ba mục tiêu cho active subset chưa đủ kết luận toàn FX hoặc Release 1 hoàn tất.**
