# Search, Saved Search, Favorites và Command Palette

FX-25 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Global Search xuyên module, saved queries, favorite/recent và commands.

[Notion Search](https://www.notion.com/help/search): Tìm page và thu hẹp kết quả bằng bộ lọc.

[Raindrop.io Search](https://help.raindrop.io/using-search): Tìm bookmark bằng nội dung truy vấn và bộ lọc.

[Notion Sidebar](https://www.notion.com/help/navigate-with-the-sidebar): Sidebar phục vụ điều hướng và truy cập nhanh.

**Áp dụng cho Nexora:** Notion/Raindrop tham chiếu search/navigation; không thay local Documents Title/Tag-only search.

**Màn hình:** `/search, /favorites, Ctrl/Cmd+K`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Query → grouped results → filter module/type/tag/date → mở source.
2. Advanced form sinh typed query → preview → Save Search; chạy lại trên current data.
3. Favorite/reorder; Recent; command palette chỉ hiện action hợp lệ.

## Dữ liệu và validation

- Query≤500, SavedSearch name≤100/schemaVersion/filter/sort.
- Hit resource/type/title/safe snippet/path/updatedAt; favorites rank; recent lastOpenedAt.

## Hành vi và lifecycle

- **FX-25-BR-001:** Permission/lifecycle/module check tại query và open; index/cache không cấp quyền.
- **FX-25-BR-002:** Không index Vault payload/support view/audit reasons/credentials; share link không đưa data người khác vào global index của viewer.
- **FX-25-BR-003:** Trash excluded; Archived chỉ khi Include Archived; disabled module excluded ngay.
- **FX-25-BR-004:** Relevance default stable tie ID; page25; debounce250ms; cancel stale responses.
- **FX-25-BR-005:** Recent cap100 và clear recent delegated, không xóa Audit/resource; saved query không snapshot kết quả.
- **FX-25-BR-006:** Destructive command mở confirm/form, không thực thi ngay khi chọn search result.

## Quyền, API và tích hợp

- SearchContribution typed fields + authorization filters + idempotent projection.
- CommandContribution permission/confirmation; SavedSearch migration báo incompatibility không widen query ngầm.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-25-AC-001:** Revoked access không trả snippet/count dù index còn.
- **FX-25-AC-002:** Body-only Documents match không xuất local list nhưng có thể Global Search authorized.
- **FX-25-AC-003:** Unknown saved-query version báo cần migrate.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-SRC-001`, `P03-SRC-002`, `P03-SRC-003`, `P03-SRC-004`, `P03-SRC-005`, `P03-SRC-006`, `P03-SRC-007`, `P03-SRC-008`, `P03-SRC-009`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
