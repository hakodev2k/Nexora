# Nexora Documentation

> Current specification · reconciled 2026-09-09. [Previous version](history/20260908/snapshot/docs/README.md) is historical evidence, not implementation input.

Bộ tài liệu này là nguồn yêu cầu chính thức (single source of truth) cho Nexora. Nội dung được tái cấu trúc từ bản `Super Website — Product Requirements Draft v0.2` thành các yêu cầu có mã định danh, tiêu chí nghiệm thu và cổng quyết định theo từng phase.

## Trạng thái tài liệu

| Thuộc tính | Giá trị |
|---|---|
| Phiên bản | `1.3-draft` |
| Ngày lập baseline | `2026-09-04` |
| Trạng thái | Requirement discovery tiếp tục theo module; M01 + scaffold đã được PO approve để implement local-first |
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
10. [PO implementation-readiness decisions 2026-09-09](requirements/11-owner-decisions-20260909-implementation-readiness.md) — approval boundary cho M01 + scaffold và các quyết định còn chặn e2e.

## Product decisions đã chốt trong v1.3

- Nexora là Public SaaS do Product Owner vận hành; bất kỳ ai cũng có thể đăng ký.
- Account được kích hoạt sau khi xác minh email và được dùng ngay, không cần Admin phê duyệt.
- Release 1 là personal-only: mỗi User sở hữu dữ liệu riêng; không có Team Workspace, membership hoặc team collaboration.
- Toàn bộ module đã có requirement hiện tại thuộc Release 1 và phải hoàn thành theo acceptance criteria đã duyệt; có thể chia milestone nội bộ.
- Module mới chỉ do trusted Nexora developers phát triển và ship bằng code.
- Mọi module mặc định bật cho User mới; SuperAdmin có thể enable/disable module theo User và quản lý module/action permission của Admin.
- External sharing luôn chỉ-đọc, theo resource/module policy; Calendar Event không được chia sẻ.
- Admin chỉ xem dữ liệu User khi User cấp quyền hỗ trợ read-only cho đúng một module; SuperAdmin emergency access phải có lý do, audit và thông báo ngay.
- Project, Task và Calendar đã có state/field/view/trash/history/reminder/import-export rules chi tiết tại Phase 2.
- M01 + backend/frontend scaffold + local scripts đã được PO approve để implement local-first; production, public launch và full R1 vẫn cần gate riêng.
- Account deletion là soft-delete; deleted email không được reuse cho owner mới; restore sau này phải restore cùng account/UserId/PersonalSpace.
- TOTP lost-device recovery dùng recovery codes một lần; email + password không đủ reset MFA; mất cả TOTP và recovery codes thì không có self-service recovery theo quyết định hiện tại.
- Vault đi theo hướng hybrid recoverability/owner encrypted portability; operator/SuperAdmin không có quyền plaintext thường trực hoặc export secret của User khác.
- Sensitive share/support dùng projection allowlist; field nhạy cảm mặc định bị ẩn, Support chỉ xem safe metadata/error/debug state, Emergency vẫn read-only và không export/copy secret.
- Finance slice đầu là manual records: category, amount, explicit currency, date và optional note; ledger/budget/debt/interest/FX/transfers còn gated.
- Productivity slice đầu giữ flat Tasks + một reminder; subtask/recurrence/snooze/standalone reminder/Task attachment còn gated.
- News/GitHub Discovery/Monitoring có thể dùng outbound read-only public metadata/feed sau contract được duyệt và network guards; không resume Price/Automation/Integrations.
- Price Tracking, Automation/Scheduler/Workflows và Integrations/Webhooks/n8n vẫn Paused, chưa chuyển R2.
- Local Stable đi trước production; chưa cam kết provider, capacity, RPO/RTO hoặc SLA.
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

## Feature specifications — 2026-09-06

Đọc [docs/features](features/README.md) để xem40 đặc tả hành vi theo module/capability, sản phẩm tham chiếu, luồng màn hình, fields/validation, lifecycle, quyền, commands và acceptance scenarios. Có [coverage](features/92-coverage-and-decisions.md), [requirement routing](features/93-requirement-routing.md) và [12 nhóm quyết định lớn](features/90-open-decisions.md).

Chi tiết nhỏ được PM/Technical chốt theo DEC-GOV-001; không hỏi lại từng thao tác. Proposal còn chưa Approved chỉ được implement khi có PO decision tương ứng; với M01 + scaffold, đọc quyết định 2026-09-09 trước khi code.

## Design review 2026-09-07 — documentation only

- [Database: tables, fields, types, relations, module evolution](design-database/README.md)
- [Architecture review and upgraded design](architecture/README.md)
- [UX/UI detailed screens and common interaction contracts](ux-ui/README.md)
- [Cross-layer consistency, decisions and readiness report](design-review/README.md)

Các tài liệu này không tuyên bố đã implement. Current Product Owner decisions > approved requirements > resolved delegated decisions > features > these technical/UX designs > historical roadmap/reference products.

## Current action catalog

[733 documented action contracts /40 feature scopes](action-catalog/README.md), [screen bindings](action-catalog/06-screen-bindings.md), and [database binding](design-database/16-action-catalog-binding.md). Docs-only; implementation chỉ được phép cho bounded slice đã có PO approval.

## Current PO decision revision

[2026-09-09 implementation readiness](requirements/11-owner-decisions-20260909-implementation-readiness.md) · [2026-09-07 decision source](requirements/10-owner-decisions-20260907.md) · [Current Q status](features/90-open-decisions.md) · [Physical delta:4new tables](design-database/17-owner-decision-delta.md) · [Security/recovery ADR](architecture/07-owner-decisions-security-and-recovery.md) · [Capacity policy](architecture/08-capacity-and-verification-policy.md) · [Action catalog v1.1](action-catalog/README.md). Historical181tables/197screens/714actions counts remain prior snapshots; current documented inventory includes185 table specs,202screens,733contracts with inactive scopes counted explicitly.

## Current milestone handoff

Start from [current delivery specification](delivery/README.md). M01 scope, API/DB/UX/acceptance and evidence gates are linked there. M01 + scaffold is approved for implementation by DEC-20260909-001. A local/internal milestone is not Release1 completion. Runtime evidence remains required and must be generated by actual implementation/testing.

## Goals cho agents

[Goals xuyên phase và từng module](goals/README.md): 9 product phases, 23 delivery steps, M01, 40 module goals và cross-module test/evidence contracts. Đọc trước khi lập plan/code/test; goals không tự mở paused scope, không thay PO decisions và không thay runtime evidence.
