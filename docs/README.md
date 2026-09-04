# Nexora Documentation

Bộ tài liệu này là nguồn yêu cầu chính thức (single source of truth) cho Nexora. Nội dung được tái cấu trúc từ bản `Super Website — Product Requirements Draft v0.2` thành các yêu cầu có mã định danh, tiêu chí nghiệm thu và cổng quyết định theo từng phase.

## Trạng thái tài liệu

| Thuộc tính | Giá trị |
|---|---|
| Phiên bản | `1.0-draft` |
| Ngày lập baseline | `2026-09-04` |
| Trạng thái | Requirement baseline chờ Product Owner review |
| Phạm vi hiện tại | Local-first, multi-user web application |
| Công nghệ đã định hướng | ReactJS, .NET, SQL, Redis |

## Bắt đầu từ đâu

1. [Product charter](requirements/00-product-charter.md) — tầm nhìn, mục tiêu, nguyên tắc và ranh giới sản phẩm.
2. [Scope và module catalog](requirements/01-scope-and-module-catalog.md) — module đã xác nhận, module đề xuất và cách xử lý phần giao nhau.
3. [Cross-cutting requirements](requirements/02-cross-cutting-requirements.md) — ownership, sharing, audit, notification, trash, file, settings và dữ liệu dùng chung.
4. [Security và privacy](requirements/03-security-and-privacy.md) — authentication, authorization, encryption, Vault và quản trị đặc quyền.
5. [Non-functional requirements](requirements/04-non-functional-requirements.md) — UX, accessibility, performance, reliability, observability và quality gates.
6. [Role/permission matrix](requirements/05-role-and-permission-matrix.md) — mô hình quyền theo `module.action`.
7. [Decision log và traceability](requirements/06-decisions-and-traceability.md) — giả định, câu hỏi mở, quy trình thay đổi và Definition of Done.

## Delivery phases

| Phase | Chủ đề | Tài liệu |
|---:|---|---|
| 0 | Requirement baseline & product discovery | [Phase 0](requirements/phases/phase-00-requirement-baseline.md) |
| 1 | Core Platform & application shell | [Phase 1](requirements/phases/phase-01-core-platform.md) |
| 2 | Productivity | [Phase 2](requirements/phases/phase-02-productivity.md) |
| 3 | Knowledge, Documents, Search & Dashboard | [Phase 3](requirements/phases/phase-03-knowledge-search-dashboard.md) |
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
