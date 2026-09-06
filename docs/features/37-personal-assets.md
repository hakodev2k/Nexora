# Personal Assets, Inventory và Devices

FX-37 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Tài sản/thiết bị, accessories/components, purchase/warranty/repair và evidence.

[Snipe-IT](https://snipe-it.readme.io/docs/overview): Asset và các thông tin quản lý liên quan được tổ chức thành records.

**Áp dụng cho Nexora:** Snipe-IT tham chiếu asset records; không company checkout/member assignment. Loaned chỉ record thủ công của owner.

**Màn hình:** `/assets, /assets/:id`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Create name/type/model/status → optional purchase/files.
2. Detail tabs information/purchase/warranty/components/history; list filter type/status/tag, search name/model.
3. Record repair/claim/loan/sale/disposal; configure expiry reminder.
4. Trash preview dependencies → restore aggregate hoặc purge khi hợp lệ.

## Dữ liệu và validation

- Name1–200/type/category/manufacturer/model/status/notes; serial Sensitive masked.
- Active/Stored/Loaned/Repair/Sold/Disposed/Lost/Archived; Loaned borrower text private không User permission.
- Purchase seller/date/amount/currency/invoice/FinanceRef; warranty provider/start/end/Lifetime/Unknown/terms.
- Parent/component/accessory same-owner refs acyclic; repair date/provider/cost/result/files.

## Hành vi và lifecycle

- **FX-37-BR-001:** Type Device/Electronics là subtype asset, không duplicate inventory record.
- **FX-37-BR-002:** Loan/sold/disposed không chuyển owner hoặc xóa purchase evidence; transitions manual có history.
- **FX-37-BR-003:** Accessory links không cascade purge asset độc lập; deletion preview phân biệt component owned record và independentAssetRef.
- **FX-37-BR-004:** Warranty source dùng shared provider, renewal date change invalidates stale alert; default30day leadtime delegated configurable.
- **FX-37-BR-005:** Finance/Vault refs không dereference secret/ghi ledger; delete asset không delete source.
- **FX-37-BR-006:** Sharing safe name/type/model/status only proposed Q-03, serial/contact/invoice excluded.

## Quyền, API và tích hợp

- AssetAggregate/LinkComponent/RecordRepair/WarrantyProvider; Files evidence.
- AssetChanged/ExpiryIntent; CreateFromPurchase idempotent; graph cycle checks.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-37-AC-001:** Cycle/cross-owner component denied.
- **FX-37-AC-002:** Sold giữ invoice/history; edit serial không leak search.
- **FX-37-AC-003:** Warranty renew làm reminder cũ stale; Assetdelete không xóa Finance.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-07-assets-and-career.md](../requirements/phases/phase-07-assets-and-career.md): `P07-AST-001`, `P07-AST-002`, `P07-AST-003`, `P07-AST-004`, `P07-AST-005`, `P07-AST-006`, `P07-BND-001`, `P07-BND-002`, `P07-BND-003`, `P07-BND-004`, `P07-BND-005`, `P07-PUR-001`, `P07-WAR-001`, `P07-WAR-002`, `P07-WAR-003`

Quyết định lớn cần PO: [Q-03](90-open-decisions.md#q-03). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
