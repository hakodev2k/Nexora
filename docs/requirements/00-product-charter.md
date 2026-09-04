# Nexora Product Charter

**Document ID:** `NX-PRD-000`  
**Version:** `1.2-draft`  
**Status:** Product direction confirmed; module details tiếp tục requirement discovery  
**Last updated:** `2026-09-04`

## 1. Tầm nhìn

Nexora là một **multi-user Modular Digital Operating System / Super Website** được cung cấp dưới dạng **Public SaaS** do chủ sở hữu Nexora vận hành. Sản phẩm hợp nhất công việc, kiến thức, dữ liệu cá nhân và các workflow thường ngày trong một web application duy nhất cho mọi đối tượng người dùng có thể tự đăng ký.

Mỗi User là một cá nhân độc lập, tự nhập và quản lý dữ liệu của chính mình. Hệ thống có thể dùng một Personal Space/internal owner boundary để cô lập dữ liệu, nhưng Release 1 không có Team Workspace, membership, group-owned resource hoặc team collaboration.

Module mới do trusted Nexora developers phát triển và ship bằng code. Admin/User không tạo module hoặc upload executable plugin. Mọi module thuộc catalog Release 1 được bật mặc định cho User mới; SuperAdmin có thể giới hạn module theo từng User và module/action permission theo từng Admin.

## 2. Mục tiêu sản phẩm

| ID | Mục tiêu | Chỉ dấu thành công ở mức sản phẩm |
|---|---|---|
| `GOAL-001` | Một điểm truy cập thống nhất | User truy cập các domain đã bật từ một application shell và một account. |
| `GOAL-002` | Dữ liệu riêng tư theo thiết kế | Dữ liệu mới luôn có owner; user không thể đọc dữ liệu không được cấp quyền. |
| `GOAL-003` | Module dùng chung platform capabilities | Module không tự xây phiên bản riêng của sharing, audit, notifications, files hoặc trash. |
| `GOAL-004` | Public SaaS có thể vận hành và phát triển an toàn | Production deployment phục vụ nhiều User, đồng thời developer vẫn có môi trường local tái lập được. |
| `GOAL-005` | Giá trị sử dụng tăng dần theo phase | Mỗi phase có vertical slice dùng được, dữ liệu có thể tiếp tục được sử dụng ở phase sau. |
| `GOAL-006` | An toàn với dữ liệu nhạy cảm | Secret có thể khôi phục không được lưu plaintext; action nhạy cảm được kiểm soát và audit. |
| `GOAL-007` | Cô lập dữ liệu cá nhân | User không thể đọc, tìm kiếm, export hoặc suy ra dữ liệu của User khác ngoài một read-only share grant hoặc privileged-support path hợp lệ. |
| `GOAL-008` | Mở rộng bằng developer-built modules | Module mới tích hợp qua contract chuẩn và có thể enable/disable mà không sửa Platform Kernel hoặc mất dữ liệu. |

## 3. Đối tượng sử dụng và vai trò

### 3.1 User

Người dùng cuối tự đăng ký, xác minh email, sử dụng module được bật, quản lý dữ liệu cá nhân và tạo link chỉ-đọc cho resource được SuperAdmin/module policy cho phép.

### 3.2 Admin

Người vận hành được SuperAdmin cấp quyền theo `module.action`. Admin không mặc nhiên có toàn quyền, không được tự nâng quyền và không được xem dữ liệu cá nhân của User nếu chưa có support grant hợp lệ do User cấp cho đúng một module.

### 3.3 SuperAdmin

Vai trò đặc quyền cao nhất, có quyền quản trị toàn hệ thống. Truy cập dữ liệu User khi chưa có consent chỉ được thực hiện qua emergency/break-glass flow, phải nhập lý do, ghi audit bất biến và thông báo ngay cho User. Hệ thống phải ngăn việc vô hiệu hóa, hạ quyền hoặc xóa SuperAdmin cuối cùng.

## 4. Product principles

| ID | Nguyên tắc | Hệ quả bắt buộc |
|---|---|---|
| `PRIN-001` | Private by default | Không có public visibility hoặc share link nếu owner chưa chủ động tạo. |
| `PRIN-002` | Multi-user from day one | Mọi business record phù hợp có ownership; mọi query kiểm tra access scope. |
| `PRIN-003` | Manual input first | Business flow cốt lõi phải dùng được mà không phụ thuộc integration bên ngoài. |
| `PRIN-004` | Shared platform capabilities | Sharing, audit, notification, files, trash, settings và jobs có contract dùng chung. |
| `PRIN-005` | Security is a feature | Security acceptance criteria là điều kiện release, không phải backlog tùy chọn. |
| `PRIN-006` | Progressive delivery | Candidate module không được làm tăng critical path của phase trước khi được duyệt. |
| `PRIN-007` | Explicit decisions | Điểm chưa chốt được ghi `TBD`/decision ID; developer không tự biến assumption thành product behavior. |
| `PRIN-008` | No AI dependency | Core functionality không yêu cầu AI/LLM; “AI News” chỉ là chủ đề nội dung. |
| `PRIN-009` | Personal ownership from day one | Mỗi resource thuộc đúng một User/Personal Space; mọi data path phải chống cross-user access. |
| `PRIN-010` | Sharing không phải collaboration | Share grant chỉ cấp quyền xem; không tạo quyền edit/comment/assign và không chuyển ownership. |
| `PRIN-011` | Developer-built modules | Chỉ trusted developers tạo/ship module; enablement không đồng nghĩa permission và disable không đồng nghĩa xóa data. |

## 5. Ràng buộc đã xác nhận

| ID | Requirement | Trạng thái |
|---|---|---|
| `CON-001` | Frontend sử dụng ReactJS. | `CONFIRMED` |
| `CON-002` | Backend sử dụng .NET. | `CONFIRMED` |
| `CON-003` | Persistent database thuộc nhóm SQL. | `CONFIRMED` |
| `CON-004` | Redis thuộc technology direction. Use case cụ thể phải được quyết định trong architecture. | `CONFIRMED` |
| `CON-005` | Nexora được vận hành dưới dạng Public SaaS do chủ sở hữu quản lý tập trung. | `CONFIRMED` |
| `CON-006` | Desktop và mobile web là target bắt buộc; tablet không được unusable. | `CONFIRMED` |
| `CON-007` | Sản phẩm là multi-user với `SuperAdmin`, `Admin`, `User`. | `CONFIRMED` |
| `CON-008` | Core functionality không sử dụng AI/LLM. | `CONFIRMED` |
| `CON-009` | Release 1 là personal-only; không có Team Workspace, membership hoặc team collaboration. | `CONFIRMED` |
| `CON-010` | Public self-registration yêu cầu xác minh email; account được dùng ngay sau verification, không cần Admin duyệt. | `CONFIRMED` |
| `CON-011` | Module mới chỉ do trusted developers phát triển bằng code; Admin/User không author hoặc upload module. | `CONFIRMED` |
| `CON-012` | Toàn bộ module đã có requirement hiện tại thuộc Release 1 và phải hoàn thành theo scope/acceptance đã duyệt. | `CONFIRMED` |
| `CON-013` | User mới mặc định có toàn bộ module Release 1; SuperAdmin có thể giới hạn module theo User và Admin. | `CONFIRMED` |
| `CON-014` | Dữ liệu chủ yếu do User nhập thủ công; import file chỉ có ở module/format đã được duyệt. | `CONFIRMED` |

## 6. Ngoài phạm vi baseline hiện tại

| ID | Hạng mục | Lý do/điều kiện xem xét lại |
|---|---|---|
| `OUT-001` | Billing, paid plans, metering và subscription commerce | Public SaaS Release 1 chưa có mô hình thương mại được duyệt. |
| `OUT-002` | Native iOS/Android app | Responsive web là target hiện tại. |
| `OUT-003` | AI chat, agent, generation, summarization, semantic assistant | Bị loại khỏi policy hiện tại. |
| `OUT-004` | Team Workspace, member collaboration, assignment giữa User, comments/mentions nhiều người và real-time co-editing | Release 1 personal-only; external read-only sharing vẫn được hỗ trợ theo policy. |
| `OUT-005` | GitHub OAuth và thao tác ghi lên GitHub | GitHub Discovery chỉ đọc public data. |
| `OUT-006` | Bank sync/Open Banking | Finance bắt đầu bằng manual entry. |
| `OUT-007` | Self-hosted/customer-managed distribution | Release 1 chỉ là Public SaaS do Nexora vận hành; topology production cụ thể vẫn là technical decision. |
| `OUT-008` | No-code Module Builder cho User/Admin | Module do developer triển khai theo confirmed direction. |
| `OUT-009` | User/Admin upload executable plugin hoặc migration | Chỉ trusted build/deployment pipeline được ship module. |
| `OUT-010` | Third-party executable module marketplace | Chỉ xem xét sau khi có sandbox, provenance và supply-chain model riêng. |

## 7. Success metrics đề xuất

Các chỉ số sau là `PROPOSED`; Product Owner cần xác nhận trước khi dùng làm KPI:

| ID | Metric | Target đề xuất |
|---|---|---|
| `MET-001` | Ownership isolation test pass rate | 100% test cases |
| `MET-002` | P0 acceptance criteria hoàn thành khi đóng phase | 100% |
| `MET-003` | Critical/High security findings chưa xử lý khi release | 0 |
| `MET-004` | Core flows sử dụng được trên desktop và mobile viewport | 100% P0 flows |
| `MET-005` | Background job quan trọng có history và failure visibility | 100% |
| `MET-006` | Restore rehearsal thành công trước production readiness | 100% kịch bản đã duyệt |
| `MET-007` | Cross-user isolation test pass rate | 100% test cases |
| `MET-008` | Developer-built module contract/disable/upgrade test pass rate | 100% P0 contract tests |

## 8. Roadmap theo capability

| Phase | Capability outcome |
|---:|---|
| 0 | Scope, module boundary, requirement baseline và decision backlog được duyệt. |
| 1 | Có Public SaaS account lifecycle, personal-data boundary, Module Platform và platform services tối thiểu an toàn. |
| 2 | User quản lý Project, Task, Calendar/Event và Reminder theo business rules đã duyệt. |
| 3 | User quản lý tri thức/tài liệu; Search/Dashboard luôn owner-scoped. |
| 4 | User quản lý tài chính thủ công và secret theo security controls nâng cao. |
| 5 | User đọc feeds, theo dõi sản phẩm/giá và nhận cảnh báo. |
| 6 | User dùng developer utilities, khám phá GitHub và vận hành automation có kiểm soát. |
| 7 | User quản lý tài sản cá nhân/số và hành trình nghề nghiệp/học tập. |
| 8 | Hệ thống được harden, backup/restore, vận hành và đạt production-readiness cho Public SaaS. |

## 9. Điều kiện thay đổi charter

Thay đổi một mục `CONFIRMED`, thêm domain/module ngoài catalog Release 1 hoặc đưa billing/AI/team collaboration vào scope phải có decision record, phân tích tác động tới security/data/roadmap và Product Owner approval.
