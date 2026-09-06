# Code Snippets

FX-22 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Code/text cá nhân có highlighting, copy, history và tìm kiếm.

[GitHub Gists](https://docs.github.com/en/rest/gists/gists): Snippet/file và revisions là tài nguyên có thể quản lý riêng.

[DevToys](https://devtoys.app/): Các công cụ nhỏ chuyển đổi/định dạng dữ liệu; đây là sản phẩm desktop, chỉ tham chiếu hành vi tool.

**Áp dụng cho Nexora:** Gist tham chiếu snippet/revision; không thực thi code, Git hosting hoặc sync Gist.

**Màn hình:** `/snippets`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. New Title/language/code → Save.
2. List tìm title/tag/code; detail highlight/copy/download plain text.
3. Save revision, diff, restore thành revision mới; archive/Trash/restore.

## Dữ liệu và validation

- Title 1–200, code required≤1MiB, language enum gồm plaintext; tags/description optional.
- Version immutable body/language/title; sourceVersionId khi restore.

## Hành vi và lifecycle

- **FX-22-BR-001:** Code là dữ liệu escaped ở preview/diff/share, không execute.
- **FX-22-BR-002:** Cảnh báo mẫu giống credential, đề nghị Vault; không gửi code đến AI/lint service.
- **FX-22-BR-003:** Share owner preview gồm code/language/title, không history/private metadata.
- **FX-22-BR-004:** Archive read-only có unarchive; versions tới purge; clipboard explicit không ghi payload vào audit.
- **FX-22-BR-005:** Text export giữ Unicode/newlines, filename safe; source code không tự tạo executable attachment.

## Quyền, API và tích hợp

- SaveSnippet/RestoreVersion/ExportText; common concurrency/idempotency.
- Search body allowed theo permission; no secret indexing.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-22-AC-001:** Script trong snippet không chạy.
- **FX-22-AC-002:** Retry Save một revision; stale restore conflict.
- **FX-22-AC-003:** Cross-user code search không trả snippet.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-SNP-001`, `P03-SNP-002`, `P03-SNP-003`, `P03-SNP-004`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
