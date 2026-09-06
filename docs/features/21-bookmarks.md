# Bookmarks

FX-21 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Lưu URL cá nhân với tags, collections và metadata.

[Raindrop.io](https://help.raindrop.io/quickstart): Lưu bookmark và tổ chức bằng collections/tags.

**Áp dụng cho Nexora:** Áp dụng lưu link/collection của Raindrop; không archive toàn website hoặc bypass paywall.

**Màn hình:** `/bookmarks`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Paste URL → fetch metadata an toàn hoặc nhập tay → Save.
2. Grid/List, tìm Title/URL/Tag; lọc collection/tag/domain; savedAt giảm dần.
3. Open external, edit, archive, Trash/restore; duplicate URL hiện lựa chọn mở cũ hoặc lưu riêng.

## Dữ liệu và validation

- URL http/https và Title 1–200 required; description≤20k/tags/collections optional.
- URL gốc giữ query có ý nghĩa; duplicate suggestion không tự gộp.
- Fetched title/icon/description có provenance/time; manual override riêng.

## Hành vi và lifecycle

- **FX-21-BR-001:** Fetch qua egress guard; failure không chặn lưu thủ công; không gửi browser cookies.
- **FX-21-BR-002:** Metadata refresh không ghi đè User edits; HTML/scripts escaped.
- **FX-21-BR-003:** Archive ẩn list thường; Trash chặn direct/share/search.
- **FX-21-BR-004:** Sharing safe projection title/URL/description/tags owner preview; không private notes/fetch logs.
- **FX-21-BR-005:** Collection deletion không xóa bookmarks; restored bookmark không tái tạo collection đã purge.

## Quyền, API và tích hợp

- CreateBookmark/UpdateBookmark/FetchMetadata/Archive/Trash/Restore; Search/Sharing/Collections providers.
- BookmarkSaved event chỉ safe metadata.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-21-AC-001:** Fetch timeout vẫn lưu URL hợp lệ thủ công.
- **FX-21-AC-002:** Khác query không tự gộp; duplicate suggestion không cross-user.
- **FX-21-AC-003:** Malicious metadata không chạy trong share.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-BMK-001`, `P03-BMK-002`, `P03-BMK-003`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
