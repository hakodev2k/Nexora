# Nexora Documentation

Bộ tài liệu này là nguồn yêu cầu chính thức (single source of truth) cho Nexora. Nội dung được tái cấu trúc từ bản `Super Website — Product Requirements Draft v0.2` thành các yêu cầu có mã định danh, tiêu chí nghiệm thu và cổng quyết định theo từng phase.

## Trạng thái tài liệu

| Thuộc tính | Giá trị |
|---|---|
| Phiên bản | `1.2-draft` |
| Ngày lập baseline | `2026-09-04` |
| Trạng thái | Requirement discovery đang tiếp tục; Project/Task/Calendar đã được Product Owner làm rõ |
| Mô hình sản phẩm | Public SaaS, self-registration, dữ liệu cá nhân độc lập, không có Workspace trong Release 1 |
| Phạm vi Release 1 | Toàn bộ module đã có requirement hiện tại; mỗi module phải hoàn thành theo scope đã duyệt |
| Công nghệ đã định hướng | ReactJS, .NET, SQL, Redis |

## Bắt đầu từ đâu

1. [Product charter](requirements/00-product-charter.md) — tầm nhìn, mục tiêu, nguyên tắc và ranh giới sản phẩm.
2. [Scope và module catalog](requirements/01-scope-and-module-catalog.md) — module đã xác nhận, module đề xuất và cách xử lý phần giao nhau.
3. [Cross-cutting requirements](requirements/02-cross-cutting-requirements.md) — ownership, sharing, audit, notification, trash, file, settings và dữ liệu dùng chung.
4. [Security và privacy](requirements/03-security-and-privacy.md) — authentication, authorization, encryption, Vault và quản trị đặc quyền.
5. [Non-functional requirements](requirements/04-non-functional-requirements.md) — UX, accessibility, performance, reliability, observability và quality gates.
6. [Role/permission matrix](requirements/05-role-and-permission-matrix.md) — mô hình quyền theo `module.action`.
7. [Decision log và traceability](requirements/06-decisions-and-traceability.md) — giả định, câu hỏi mở, quy trình thay đổi và Definition of Done.
8. [Module Platform](requirements/07-module-platform.md) — contract, lifecycle và enablement cho module do developer phát triển.
9. [Personal ownership, sharing và support access](requirements/08-workspaces-and-collaboration.md) — cô lập dữ liệu cá nhân, link chỉ-đọc, quyền hỗ trợ có đồng ý và emergency access.

## Product decisions đã chốt trong v1.2

- Nexora là Public SaaS do Product Owner vận hành; bất kỳ ai cũng có thể đăng ký.
- Account được kích hoạt sau khi xác minh email và được dùng ngay, không cần Admin phê duyệt.
- Release 1 là personal-only: mỗi User sở hữu dữ liệu riêng; không có Team Workspace, membership hoặc team collaboration.
- Toàn bộ module đã có requirement hiện tại thuộc Release 1 và phải hoàn thành theo acceptance criteria đã duyệt; có thể chia milestone nội bộ.
- Module mới chỉ do trusted Nexora developers phát triển và ship bằng code.
- Mọi module mặc định bật cho User mới; SuperAdmin có thể enable/disable module theo User và quản lý module/action permission của Admin.
- External sharing luôn chỉ-đọc, theo resource/module policy; Calendar Event không được chia sẻ.
- Admin chỉ xem dữ liệu User khi User cấp quyền hỗ trợ read-only cho đúng một module; SuperAdmin emergency access phải có lý do, audit và thông báo ngay.
- Project, Task và Calendar đã có state/field/view/trash/history/reminder/import-export rules chi tiết tại Phase 2.
- No-code Module Builder và executable third-party marketplace được defer.

## Delivery phases

| Phase | Chủ đề | Tài liệu |
|---:|---|---|
| 0 | Requirement baseline & product discovery | [Phase 0](requirements/phases/phase-00-requirement-baseline.md) |
| 1 | Core Platform & application shell | [Phase 1](requirements/phases/phase-01-core-platform.md) |
| 2 | Productivity | [Phase 2](requirements/phases/phase-02-productivity.md) |
| 3 | Documents, Search & Dashboard | [Phase 3](requirements/phases/phase-03-knowledge-search-dashboard.md) |
| 4 | Finance & Vault | [Phase 4](requirements/phases/phase-04-finance-and-vault.md) |
| 5 | News/Feeds & Shopping/Price Tracking | [Phase 5](requirements/phases/phase-05-news-and-shopping.md) |
| 6 | Developer Toolbox, GitHub Discovery & Automation | [Phase 6](requirements/phases/phase-06-developer-and-automation.md) |
| 7 | Personal Assets, Digital Assets & Career/Learning | [Phase 7](requirements/phases/phase-07-assets-and-career.md) |
| 8 | Hardening, backup/restore & deployment readiness | [Phase 8](requirements/phases/phase-08-hardening-and-deployment.md) |

Thứ tự phase là đề xuất lập kế hoạch, không phải cam kết ngày phát hành. Mỗi phase chỉ được bắt đầu khi exit criteria của phase trước đã đạt hoặc có quyết định chấp nhận rủi ro được ghi nhận.

## Quy ước

| Nhãn | Ý nghĩa |
|---|---|
| `CONFIRMED` | Đã có trong baseline đầu vào và được coi là yêu cầu đã xác nhận. |
| `PROPOSED` | Đề xuất để biến ý tưởng thành yêu cầu có thể xây dựng; Product Owner cần duyệt. |
| `TBD` | Chưa đủ quyết định, không được tự suy diễn trong implementation. |
| `OUT` | Ngoài phạm vi hiện tại. |
| `MUST` / P0 | Bắt buộc để phase được nghiệm thu. |
| `SHOULD` / P1 | Giá trị cao nhưng có thể dời bằng quyết định được ghi nhận. |
| `COULD` / P2 | Tùy chọn, không chặn release. |

Mã yêu cầu không được tái sử dụng. Khi bỏ một yêu cầu, giữ nguyên mã và chuyển trạng thái thành `Deprecated` để bảo toàn traceability.
