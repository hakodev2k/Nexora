# Wishlist, Comparison, Orders, Sellers và Warranty

FX-31 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Nhập tay wishlist/comparison/orders/purchases/sellers và bằng chứng bảo hành.

[AnyList](https://www.anylist.com/features): Danh sách item và thao tác tổ chức nhu cầu mua sắm; không là nguồn cho order/warranty.

**Áp dụng cho Nexora:** AnyList tham chiếu shopping lists; order/warranty lifecycle là Nexora đề xuất, không gán cho AnyList.

**Màn hình:** `/shopping/wishlist, /compare, /orders, /sellers`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Add manual hoặc tracked product vào wishlist; quantity/priority/notes.
2. Chọn2–4 items compare attributes/price/currency/source date.
3. Tạo Order seller/lines/costs → status → delivery/invoice.
4. Explicit Create Asset draft hoặc Link Finance, không tự ghi sổ.

## Dữ liệu và validation

- Wishlist name/URL/quantity>0/desiredPrice/status Wanted/Purchased/Archived.
- Order date/seller/currency, lines qty>0 unitPrice≥0, shipping/tax/discount≥0, total computed.
- Draft/Ordered/Shipped/Delivered/Cancelled/Returned; return quantity≤delivered.
- Seller name/contact/URL; Warranty provider/start/end hoặc Lifetime/Unknown/terms/files.

## Hành vi và lifecycle

- **FX-31-BR-001:** Tracked price đổi không sửa actual orderprice; khác currency không tự quy đổi compare.
- **FX-31-BR-002:** Transitions lưu history; cancellation/return không tự refund/void ledger.
- **FX-31-BR-003:** Duplicate order reference cảnh báo nhưng allowconfirmed vì không bảo đảm external unique.
- **FX-31-BR-004:** Delete aggregate Trash giữ external Finance/Asset refs; purge dependency preview.
- **FX-31-BR-005:** Seller merge explicit giữ refs và historical purchase label; warranty dùng shared provider reference với Assets.
- **FX-31-BR-006:** Share projection loại addresses/contact/invoices/serial/private orderreference tới Q-03.

## Quyền, API và tích hợp

- OrderAggregate/SellerMerge/ComparisonQuery/WarrantyProvider.
- CreateAssetFromPurchase explicit idempotent draft; Finance link read-only reference.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-31-AC-001:** Giá live thay đổi không sửa hóa đơn cũ.
- **FX-31-AC-002:** Retry CreateAsset một draft.
- **FX-31-AC-003:** Return không tự ghi refund; seller cross-owner denied.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-05-news-and-shopping.md](../requirements/phases/phase-05-news-and-shopping.md): `P05-CMP-001`, `P05-ORD-001`, `P05-SEL-001`, `P05-WAR-001`, `P05-WIS-001`

Quyết định lớn cần PO: [Q-03](90-open-decisions.md#q-03). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
