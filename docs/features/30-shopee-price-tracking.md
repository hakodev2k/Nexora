# Shopee Price Tracking

FX-30 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Product/variant resolution, observations/history, price targets và stock/drop alerts.

[camelcamelcamel](https://camelcamelcamel.com/support/first_price_watch): Price watch/history/alert; chỉ xác minh qua snippet chính thức, trang trả 403. Sản phẩm tham chiếu theo dõi Amazon, không chứng minh Shopee API.

**Áp dụng cho Nexora:** camelcamelcamel tham chiếu price watch trên Amazon; provider Shopee chưa được xác nhận, Q-06 là gate.

**Màn hình:** `/shopping/tracking`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Nhập Shopee URL → resolve shop/product/variant → preview → Watch.
2. Detail chart series đúng variant/currency/source/time; target/rules.
3. Refresh/pause/resume; error/stale hiển thị last valid.
4. Crossing target/percentage drop/new low/back-in-stock → logical notification.

## Dữ liệu và validation

- Product/shop/external IDs/URL/title; variant ID/options.
- Observation itemPrice/listPrice optional/currency/availability InStock/OutOfStock/Unknown/time/provider.
- Rule target>0, percent1–100, newLow/backInStock flags; enabled.

## Hành vi và lifecycle

- **FX-30-BR-001:** Proposal Q-06 so item price công khai đúng variant, trừ shipping/vouchers/member discounts; không coi giá này đã Approved.
- **FX-30-BR-002:** Failed fetch không ghi0/OutOfStock; immutable observations không trộn manual/provider hoặc currencies.
- **FX-30-BR-003:** Target crossing >target→≤target; initial matching một alert; rearm khi lên trên, cooldown24h delegated.
- **FX-30-BR-004:** Percentage so comparable previous valid point; new low≥2valid points; back-stock cần prior explicitOutOfStock.
- **FX-30-BR-005:** Polling6h proposal phụ thuộc provider/cost Q-06; no bypass login/CAPTCHA, no automated purchase.
- **FX-30-BR-006:** Provider unavailable không được hạ thành demo rồi nói module R1 complete.

## Quyền, API và tích hợp

- PriceProvider Resolve/GetObservation versioned; scheduled fetch idempotent.
- PriceObserved→EvaluateRules→all 3 Notification; Wishlist/Compare dùng normalized ProductRef.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-30-AC-001:** Variant/currency khác không chung chart.
- **FX-30-AC-002:** Timeout hiển thị stale không alert0.
- **FX-30-AC-003:** Retry point không duplicate crossing alert; Unknown không fake restock.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-05-news-and-shopping.md](../requirements/phases/phase-05-news-and-shopping.md): `P05-ALT-001`, `P05-ALT-002`, `P05-ALT-003`, `P05-ALT-004`, `P05-ALT-005`, `P05-ALT-006`, `P05-PHS-001`, `P05-PHS-002`, `P05-PHS-003`, `P05-PHS-004`, `P05-PHS-005`, `P05-PRD-001`, `P05-PRD-002`, `P05-PRD-003`, `P05-PRD-004`, `P05-PRD-005`, `P05-PRD-006`, `P05-PRD-007`

Quyết định lớn cần PO: [Q-06](90-open-decisions.md#q-06). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
