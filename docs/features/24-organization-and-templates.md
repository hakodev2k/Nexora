# Tags, Collections và Templates

FX-24 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Các building blocks phân loại và mẫu tạo resource.

[Notion Templates](https://www.notion.com/help/database-templates): Template cung cấp cấu trúc/nội dung khởi tạo dùng lại.

[Raindrop.io](https://help.raindrop.io/quickstart): Lưu bookmark và tổ chức bằng collections/tags.

**Áp dụng cho Nexora:** Notion tham chiếu khởi tạo từ template; không no-code database/module builder.

**Màn hình:** `/collections, /templates, module tag pickers`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Tạo/rename Tag đúng namespace module.
2. Collection thêm/bỏ ResourceRefs; mở resolve permission từng item.
3. Template từ resource/mẫu hệ thống → preview → điền required fields → create resource mới.

## Dữ liệu và validation

- Tag1–50 trim, unique case-insensitive User+namespace; color optional.
- Collection name 1–100, description, ordered member refs; không collection nesting.
- Template name/module/type/schemaVersion/seed fields sanitized.

## Hành vi và lifecycle

- **FX-24-BR-001:** Project/Task chung tag catalog; Documents riêng tối đa một Tag/page; các module khác riêng.
- **FX-24-BR-002:** Documents template không preselect type/editor hoặc bypass Folder/Parent immutable.
- **FX-24-BR-003:** Template không copy owner/IDs/share/lifecycle/history/secrets hoặc reminders đã gửi; no live binding về bản mẫu.
- **FX-24-BR-004:** Delete collection không delete members. Documents Tag delete blocked nếu current/Archived/Trash page dùng.
- **FX-24-BR-005:** Collection sharing chỉ provider approved projection; không lộ count/title resource thiếu quyền.

## Quyền, API và tích hợp

- TagProvider/CollectionMemberProvider/TemplateProvider versioned; create validate lại tại server.
- Template instantiate idempotent, không bypass module action gate.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-24-AC-001:** Sửa template không sửa resource đã tạo.
- **FX-24-AC-002:** Task template vẫn cần required times/active Project.
- **FX-24-AC-003:** Collection không cấp quyền resource bên trong.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-ORG-001`, `P03-ORG-002`, `P03-ORG-003`, `P03-ORG-004`, `P03-ORG-005`, `P03-ORG-006`, `P03-ORG-007`, `P03-ORG-008`, `P03-ORG-009`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
