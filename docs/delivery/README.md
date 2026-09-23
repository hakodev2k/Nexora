# Nexora — Current delivery specification

> **Current local implementation approval:** [DEC-20260909-014](../requirements/12-owner-decisions-local-e2e-implementation.md) supersedes older M01-only/future-slice approval and local-code pause statements below. Full local E2E is approved with contracts first; real providers/production remain unapproved. Business rules and retired actions are unchanged.


Ngày review: 2026-09-10. **DEC-20260909-014** mở rộng approval cho full local Release 1 implementation slice-by-slice khi contract đủ; production, provider thật, secrets/data thật và paid services vẫn bị chặn. Current run là code-only: functional testing/QA/fixtures/runtime verification do owner thực hiện.

Đọc theo thứ tự: [phạm vi hiện hành](01-current-scope.md) → [PO implementation-readiness decisions](../requirements/11-owner-decisions-20260909-implementation-readiness.md) → [paused/blocked/gated register](04-paused-blocked-gate-register.md) → [milestone đầu tiên](milestone-01/README.md) → stories → API → DB/transactions → UX/acceptance → môi trường/evidence. Một action có thiết kế không đồng nghĩa có handler, endpoint đang chạy, hoặc được phép phát hành.

| Câu hỏi | Nguồn hiện hành |
| --- | --- |
| Sản phẩm và Release 1 gồm gì? | [Scope](01-current-scope.md), [Product Owner record 2026-09-07](../requirements/10-owner-decisions-20260907.md), [PO implementation-readiness 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md) |
| Milestone đầu tiên được làm gì sau approval? | [M01 handoff](milestone-01/README.md), [M01 implementation handoff prompt](milestone-01/07-implementation-handoff.md) |
| Paused/Blocked/Gated hiện nghĩa là gì trước khi code? | [Paused/blocked/gated register](04-paused-blocked-gate-register.md) |
| Điểm nào đã tự chốt, điểm nào cần PO? | [Decision status](../features/90-open-decisions.md), [Decision proposals](02-decision-proposals.md) |
| Mẫu tham khảo nào, áp dụng đến đâu? | [Evidence register](03-reference-evidence.md) |
| Bản cũ ở đâu? | [Historical snapshot](../history/20260908/README.md) |

## Quy tắc duy trì

- Current feature/UX/DB body phải mô tả cùng một hành vi. Không thêm một amendment rồi để đoạn mâu thuẫn tiếp tục đóng vai trò chỉ dẫn.
- Khi thay đổi, chuyển phiên bản cũ sang history hoặc dùng commit permalink; cập nhật câu cũ tại chỗ, action gate, story, request/response và acceptance bị ảnh hưởng.
- History là bằng chứng, không là input implementation. Không index history vào danh sách requirement/action đang active.
- Requirement ID giữ ổn định. ID bị thay thế được ghi retired + trỏ tới contract mới, không tái sử dụng ID cho nghĩa khác.
- `Approved` chỉ cho lời PO; `Resolved delegated` cho quyết định trong quyền đã giao; `Proposed` cho thay đổi business/security còn cần PO; `Blocked` có phạm vi cụ thể; `Paused` chỉ resume khi PO yêu cầu.
- Specification-ready, implementation-approved, implemented, runtime-verified và production-approved là năm trạng thái độc lập.
- `DEC-20260909-014` supersedes the older M01-only implementation boundary for local code. Không lấy approval này để deploy production, bật real provider calls, dùng production secrets/data hoặc làm external destructive actions.
- Trước khi code, mọi action chạm tới phải được phân loại bằng [paused/blocked/gated register](04-paused-blocked-gate-register.md). Nếu module file và register mâu thuẫn, dùng rule chặt hơn và sửa docs trước khi code capability đó.

## Kết quả của lượt này

Chuẩn hóa các quyết định PO ngày 2026-09-09 và DEC-014 để unblock local implementation theo dependency order. Advanced/module-specific capability chỉ được code sau khi API/DB/UX/AC/security/evidence contract đủ; R1 và production không được kết luận hoàn tất chỉ từ code.

## Goals và acceptance trace

[Goals](../goals/README.md) nối current scope với P00–P08, RM00–RM22 và từng FX; [task/evidence template](../goals/05-task-and-evidence-template.md) yêu cầu trace goal tới source AC/action và actual evidence. Goals không thay story contracts, không biến `Not run` thành `Pass`; code-only run hiện tại không thêm test/mock/demo data.
