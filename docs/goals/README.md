# Nexora — Goals cho code, test và phát hành

Ngày: 2026-09-09 · Nguồn chốt đọc: `e95a4b0c20f5677bada6a65ffbc1acbfd8735523` trên `main`.

**Mục đích:** giữ outcome và logic nhất quán từ discovery tới Public SaaS, để agent biết mình phải đạt điều gì, không được đổi điều gì và cần bằng chứng nào. Đây là tài liệu tổng hợp/traceability theo yêu cầu PO; **không cấp approval implement, mở paused scope, mua hạ tầng hoặc deploy**. Chưa có goal runtime nào được đánh dấu hoàn thành.

## Đọc theo task

1. [Goals toàn hệ thống](01-system-goals.md): các invariants kế thừa bởi mọi phase/module.
2. [Product phases P00–P08](02-product-phase-goals.md): outcome và cổng hoàn tất của 9 phase sản phẩm.
3. [Delivery RM00–RM22 và M01](03-delivery-step-goals.md): 23 bước từ requirements tới public release; không nhầm M01 với Phase 1 hoặc R1.
4. [Goals 40 module](modules/README.md): 120 goal cụ thể, source contracts, test outcomes và blocked scope.
5. [Cross-module verification](04-cross-module-verification.md): nơi lỗi authority, lifecycle và consistency xuất hiện giữa module.
6. [Task/evidence template](05-task-and-evidence-template.md): dùng khi lập plan, review code, chạy test và handoff.
7. [Coverage 40 FX](module-coverage.csv), [source review và gate register](06-source-review-and-gates.md).

## Authority và tránh lệch scope

Current explicit PO decisions → approved requirements → resolved delegated decisions trong phạm vi → current features/action/UX/DB/delivery contracts. Goals tổng hợp các nguồn đó, không đứng trên chúng. Nếu có conflict, áp dụng precedence của root AGENTS và giải quyết phần bị ảnh hưởng trước code. Không lấy một câu trong goals làm lý do bỏ requirement nguồn, AC chi tiết, action guard hoặc story gate. Các sai khác lịch sử đã nhận diện ở source review.

`GOAL-001…008` trong product charter giữ nguyên ID. Bộ này dùng namespace mới `NXG-SYS-*`, `NXG-P00…P08-*`, `NXG-RM00…RM22-*`, `NXG-FX01…FX40-G01…G03`, `NXG-X-*`; không tái sử dụng requirement/action/AC ID. ID đã dùng không đổi nghĩa: retire + link thay thế nếu cần.

Phase sản phẩm P00–P08 nhóm **giá trị**; roadmap RM00–RM22 nhóm **thứ tự delivery**. Không phải hai roadmap cạnh tranh. Một FX có thể xuất hiện ở nhiều RM; 40 FX không có nghĩa bắt buộc 40 assemblies, databases hoặc DbContexts. Thứ tự trong roadmap không cho phép bỏ dependency hoặc scope gate. Parallel/reorder chỉ theo điều kiện nguồn và quyết định được ghi nhận.

## Workflow bắt buộc cho agent

- Intake: ghi user authorization đã có, branch/source revision, outcome, exact slice, goal IDs, source requirement/BR/AC và exact action keys. Không xin lại approval đã có; docs approval không tự thành code approval.
- Before code: đọc current PO/delivery/action status, module goal + dependencies, API/DB/UX/acceptance. Ngoài M01, phải có story contract đủ field/state/request/response/error/transaction trước implement; goal không thay contract này.
- Before test: chuyển mỗi goal liên quan thành assertions quan sát được và expected result có nguồn. Test cả allowed và denied paths, lifecycle, races/faults có liên quan. Không viết test chỉ xác nhận implementation đang làm gì.
- Before handoff: nối `goal → source requirement/AC → story/action → code → test/evidence → reviewed revision`. Cập nhật cả module nguồn và consumer nếu boundary đổi. Scope ngoài task không được tự thêm từ tên module.
- Nếu source/gate chưa rõ: hoàn tất phần độc lập đã được phép, ghi đúng capability bị chặn; không giả quyết định hoặc làm placeholder rồi gọi Done.

## Trạng thái không được gộp

| Trạng thái | Bằng chứng để ghi nhận |
| --- | --- |
| Specified | Goal và nguồn/expected outcome có tài liệu |
| Approved to implement | PO/session approval rõ exact slice |
| Implemented | Code tồn tại, có commit/PR và scope trace |
| Runtime verified | AC/tests thực sự chạy pass trên revision được báo cáo |
| Accepted module/phase | Toàn bộ committed scope được đối chiếu; required review và sign-off hoàn tất |
| Production approved | Go/No-Go, topology/security/ops/capacity evidence và approval riêng |

`Paused`, `Blocked`, `Conditional`, `Not run` không phải Passed, N/A hoặc Done. Chỉ dùng N/A khi requirement thực sự không áp dụng và có rationale/source; không dùng N/A để giấu committed feature còn thiếu. Snapshot thiết kế không chứng minh endpoint đang tồn tại; 120 goals không thay coverage đầy đủ của tất cả action contracts.
