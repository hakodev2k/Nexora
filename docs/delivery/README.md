# Nexora — Current delivery specification

Ngày review: 2026-09-09. Baseline: `1ca717a241cb084263de97adfba949d924ebf1ec`. **Full documented R1 local E2E implementation is approved by [DEC-20260909-014](../requirements/12-owner-decisions-20260909-local-e2e.md), delivered slice-by-slice with complete contracts.**

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
- `DEC-20260909-014` expands local implementation beyond M01, including simulated formerly paused modules. Real providers, production deployment, real secrets/data and paid services remain unapproved.
- Trước khi code, mọi action chạm tới phải được phân loại bằng [paused/blocked/gated register](04-paused-blocked-gate-register.md). Nếu module file và register mâu thuẫn, dùng rule chặt hơn và sửa docs trước khi code capability đó.

## Kết quả của lượt này

Chuẩn hóa các quyết định PO ngày 2026-09-09 để unblock implementation cho M01 + scaffold. Các gate lớn đã được phân loại lại: một số policy đã Approved, một số advanced/module-specific contracts vẫn phải có ADR/field projection/story package trước khi code từng slice. R1 và production không được kết luận hoàn tất từ M01.

## Goals và acceptance trace

[Goals](../goals/README.md) nối current scope/M01 với P00–P08, RM00–RM22 và từng FX; [task/evidence template](../goals/05-task-and-evidence-template.md) yêu cầu trace goal tới source AC/action và actual test evidence. Goals không thay story contracts, không cấp quyền ngoài `DEC-20260909-014`, không biến `Not run` thành `Pass`.
