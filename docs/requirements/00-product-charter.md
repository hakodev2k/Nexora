# Nexora Product Charter

**Document ID:** `NX-PRD-000`  
**Version:** `1.1-draft`  
**Status:** Product direction partially confirmed; module details tiếp tục refinement  
**Last updated:** `2026-09-04`

## 1. Tầm nhìn

Nexora là một **multi-user Modular Digital Operating System / Super Website** hợp nhất các công cụ, thông tin và workflow thường ngày trong một web application duy nhất. Mỗi User có Personal Space và có thể tham gia nhiều Team Workspace. Sản phẩm được thiết kế như một nền tảng có module: mỗi module giải quyết một domain cụ thể nhưng tái sử dụng chung identity, space ownership, permissions, collaboration, sharing, notification, search, audit, file và background processing.

Module mới do trusted Nexora developers phát triển và ship bằng code. Admin/User không tạo module; họ enable, configure và sử dụng module theo system/workspace/role/user policy.

Nexora trước hết phục vụ sử dụng thực tế ở local; chỉ đánh giá phương án production deployment sau khi chức năng, dữ liệu và security controls đã ổn định.

## 2. Mục tiêu sản phẩm

| ID | Mục tiêu | Chỉ dấu thành công ở mức sản phẩm |
|---|---|---|
| `GOAL-001` | Một điểm truy cập thống nhất | User truy cập các domain đã bật từ một application shell và một account. |
| `GOAL-002` | Dữ liệu riêng tư theo thiết kế | Dữ liệu mới luôn có owner; user không thể đọc dữ liệu không được cấp quyền. |
| `GOAL-003` | Module dùng chung platform capabilities | Module không tự xây phiên bản riêng của sharing, audit, notifications, files hoặc trash. |
| `GOAL-004` | Local-first nhưng không khóa đường mở rộng | Toàn bộ scope đã duyệt chạy được local; deployment topology không bị đóng cứng vào một máy. |
| `GOAL-005` | Giá trị sử dụng tăng dần theo phase | Mỗi phase có vertical slice dùng được, dữ liệu có thể tiếp tục được sử dụng ở phase sau. |
| `GOAL-006` | An toàn với dữ liệu nhạy cảm | Secret có thể khôi phục không được lưu plaintext; action nhạy cảm được kiểm soát và audit. |
| `GOAL-007` | Team collaboration từ nền tảng | Personal và Workspace data có ownership rõ; members cộng tác bất đồng bộ mà không cross-workspace leak. |
| `GOAL-008` | Mở rộng bằng developer-built modules | Module mới tích hợp qua contract chuẩn và có thể enable/disable mà không sửa Platform Kernel hoặc mất dữ liệu. |

## 3. Đối tượng sử dụng và vai trò

### 3.1 User

Người dùng cuối quản lý Personal Space, tham gia Team Workspace, sử dụng module được bật, cộng tác theo Workspace role/permission, chia sẻ resource được phép chia sẻ và cấu hình preference của chính mình.

### 3.2 Admin

Người vận hành được SuperAdmin cấp quyền theo `module.action`. Admin không mặc nhiên có toàn quyền và không được tự nâng quyền.

### 3.3 SuperAdmin

Vai trò đặc quyền cao nhất, có quyền quản trị toàn hệ thống và truy cập dữ liệu khi cần vận hành. Mọi truy cập dữ liệu user bằng đặc quyền phải có audit trail. Hệ thống phải ngăn việc vô hiệu hóa, hạ quyền hoặc xóa SuperAdmin cuối cùng.

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
| `PRIN-009` | Space ownership from day one | Resource thuộc đúng một Personal Space hoặc Team Workspace; creator/editor không đồng nghĩa owner. |
| `PRIN-010` | Asynchronous collaboration first | Assignment, comments, mentions, activity, notifications, versions và conflict detection là baseline; realtime co-editing được defer. |
| `PRIN-011` | Developer-built modules | Chỉ trusted developers tạo/ship module; enablement không đồng nghĩa permission và disable không đồng nghĩa xóa data. |

## 5. Ràng buộc đã xác nhận

| ID | Requirement | Trạng thái |
|---|---|---|
| `CON-001` | Frontend sử dụng ReactJS. | `CONFIRMED` |
| `CON-002` | Backend sử dụng .NET. | `CONFIRMED` |
| `CON-003` | Persistent database thuộc nhóm SQL. | `CONFIRMED` |
| `CON-004` | Redis thuộc technology direction. Use case cụ thể phải được quyết định trong architecture. | `CONFIRMED` |
| `CON-005` | Initial deployment là local. | `CONFIRMED` |
| `CON-006` | Desktop và mobile web là target bắt buộc; tablet không được unusable. | `CONFIRMED` |
| `CON-007` | Sản phẩm là multi-user với `SuperAdmin`, `Admin`, `User`. | `CONFIRMED` |
| `CON-008` | Core functionality không sử dụng AI/LLM. | `CONFIRMED` |
| `CON-009` | Hỗ trợ Team Workspace và collaboration ngay từ thiết kế ban đầu. | `CONFIRMED` |
| `CON-010` | Collaboration v1 là bất đồng bộ. | `CONFIRMED` |
| `CON-011` | Module mới chỉ do trusted developers phát triển bằng code; Admin/User chỉ enable và sử dụng. | `CONFIRMED` |

## 6. Ngoài phạm vi baseline hiện tại

| ID | Hạng mục | Lý do/điều kiện xem xét lại |
|---|---|---|
| `OUT-001` | Commercial SaaS features: billing, plans, tenant subscription, metering | Chỉ xem xét sau khi sản phẩm local ổn định. |
| `OUT-002` | Native iOS/Android app | Responsive web là target hiện tại. |
| `OUT-003` | AI chat, agent, generation, summarization, semantic assistant | Bị loại khỏi policy hiện tại. |
| `OUT-004` | Live presence/cursor và real-time multi-author co-editing | Collaboration v1 là bất đồng bộ; Workspace editing và external read-only sharing vẫn được hỗ trợ. |
| `OUT-005` | GitHub OAuth và thao tác ghi lên GitHub | GitHub Discovery chỉ đọc public data. |
| `OUT-006` | Bank sync/Open Banking | Finance bắt đầu bằng manual entry. |
| `OUT-007` | Production cloud/Kubernetes/CDN topology cụ thể | Là quyết định của Phase 8, không phải baseline ban đầu. |
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
| `MET-007` | Cross-workspace isolation test pass rate | 100% test cases |
| `MET-008` | Developer-built module contract/disable/upgrade test pass rate | 100% P0 contract tests |

## 8. Roadmap theo capability

| Phase | Capability outcome |
|---:|---|
| 0 | Scope, module boundary, requirement baseline và decision backlog được duyệt. |
| 1 | Có Personal/Workspace shell, membership, Module Platform và platform services tối thiểu an toàn. |
| 2 | Members cộng tác bất đồng bộ trên công việc, dự án, lịch và nhắc việc. |
| 3 | Members cộng tác trên tri thức/tài liệu; search/dashboard luôn space-scoped. |
| 4 | User quản lý tài chính thủ công và secret theo security controls nâng cao. |
| 5 | User đọc feeds, theo dõi sản phẩm/giá và nhận cảnh báo. |
| 6 | User dùng developer utilities, khám phá GitHub và vận hành automation có kiểm soát. |
| 7 | User quản lý tài sản cá nhân/số và hành trình nghề nghiệp/học tập. |
| 8 | Hệ thống được harden, backup/restore, vận hành và sẵn sàng quyết định deployment. |

## 9. Điều kiện thay đổi charter

Thay đổi một mục `CONFIRMED`, thêm domain mới vào critical path hoặc đưa commercial SaaS/AI vào scope phải có decision record, phân tích tác động tới security/data/roadmap và Product Owner approval.
