# Nexora — Current delivery specification

Ngày review: 2026-09-08. Baseline: `1ca717a241cb084263de97adfba949d924ebf1ec`. **Docs-only; implementation chưa được Product Owner duyệt.**

Đọc theo thứ tự: [phạm vi hiện hành](01-current-scope.md) → [milestone đầu tiên](milestone-01/README.md) → stories → API → DB/transactions → UX/acceptance → môi trường/evidence. Một action có thiết kế không đồng nghĩa có handler, endpoint đang chạy, hoặc được phép phát hành.

| Câu hỏi | Nguồn hiện hành |
| --- | --- |
| Sản phẩm và Release 1 gồm gì? | [Scope](01-current-scope.md), [Product Owner record](../requirements/10-owner-decisions-20260907.md) |
| Milestone đầu tiên được làm gì sau approval? | [M01 handoff](milestone-01/README.md) |
| Điểm nào đã tự chốt, điểm nào cần PO? | [Decision proposals](02-decision-proposals.md) |
| Mẫu tham khảo nào, áp dụng đến đâu? | [Evidence register](03-reference-evidence.md) |
| Bản cũ ở đâu? | [Historical snapshot](../history/20260908/README.md) |

## Quy tắc duy trì

- Current feature/UX/DB body phải mô tả cùng một hành vi. Không thêm một amendment rồi để đoạn mâu thuẫn tiếp tục đóng vai trò chỉ dẫn.
- Khi thay đổi, chuyển phiên bản cũ sang history hoặc dùng commit permalink; cập nhật câu cũ tại chỗ, action gate, story, request/response và acceptance bị ảnh hưởng.
- History là bằng chứng, không là input implementation. Không index history vào danh sách requirement/action đang active.
- Requirement ID giữ ổn định. ID bị thay thế được ghi retired + trỏ tới contract mới, không tái sử dụng ID cho nghĩa khác.
- `Approved` chỉ cho lời PO; `Resolved delegated` cho quyết định trong quyền đã giao; `Proposed` cho thay đổi business/security còn cần PO; `Blocked` có phạm vi cụ thể; `Paused` chỉ resume khi PO yêu cầu.
- Specification-ready, implementation-approved, implemented, runtime-verified và production-approved là năm trạng thái độc lập.

## Kết quả của lượt này

Chuẩn hóa các mâu thuẫn đã phát hiện (Documents formats, account/space activation, locale, soft-delete, sharing và các gate cũ); cụ thể hóa M01. Không tuyên bố toàn bộ 40 FX đã có API story-ready. Proposal còn lại được cô lập khỏi M01; R1 và production không được kết luận hoàn tất từ M01.

## Goals và acceptance trace

[Goals](../goals/README.md) nối current scope/M01 với P00–P08, RM00–RM22 và từng FX; [task/evidence template](../goals/05-task-and-evidence-template.md) yêu cầu trace goal tới source AC/action và actual test evidence. Không thay story contracts hoặc approval.
