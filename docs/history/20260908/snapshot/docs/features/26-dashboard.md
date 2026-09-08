# Dashboard và Widgets

FX-26 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Dashboard cá nhân cấu hình widgets và drill-down.

[ClickUp Dashboards](https://clickup.com/features/dashboards): Dashboard gồm các card tổng hợp có thể cấu hình.

**Áp dụng cho Nexora:** ClickUp tham chiếu cards; không team dashboard hoặc user viết query/code widget.

**Màn hình:** `/dashboard`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Default Task due/overdue, Calendar today, recent Documents, unread notifications nếu module enabled.
2. Add widget từ registry → filters/size/order → Save layout.
3. Click metric mở source cùng filters; quick capture mở form source đầy đủ.

## Dữ liệu và validation

- One default dashboard/User, layoutVersion, stable widget IDs/providerVersion/config.
- Widget count/list/chart/calendar; mỗi widget loading/empty/error/stale/refreshedAt riêng.

## Hành vi và lifecycle

- **FX-26-BR-001:** Metric nêu denominator/timezone/status; Skipped không tính Completed; active-work widget loại closed Project.
- **FX-26-BR-002:** Finance không cộng khác currency; Vault không secret/sensitive title.
- **FX-26-BR-003:** Một provider lỗi không làm toàn Dashboard thất bại; refresh coalesced.
- **FX-26-BR-004:** Disable module giữ layout nhưng không payload; enable lại restore config hợp lệ.
- **FX-26-BR-005:** Responsive và keyboard reorder; quick capture không bypass required dates/type/editor.

## Quyền, API và tích hợp

- DashboardContribution ValidateConfig/Query/GetDrilldown; current access mỗi provider.
- QuickCaptureContribution mở native creation workflow.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-26-AC-001:** Finance disable không lộ balance từ cache.
- **FX-26-AC-002:** Widget lỗi riêng vẫn dùng widget khác.
- **FX-26-AC-003:** Count/drilldown cùng snapshot/filter semantics.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-DSH-001`, `P03-DSH-002`, `P03-DSH-003`, `P03-DSH-004`, `P03-DSH-005`, `P03-DSH-006`, `P03-DSH-007`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
