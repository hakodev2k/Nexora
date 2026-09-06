# Integrations, Webhooks và n8n

FX-35 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Connection registry/auth refs, inbound/outbound contracts/delivery history và n8n adapter.

[n8n](https://n8n.io/features/): Workflow nối trigger/actions và theo dõi execution; không sao chép toàn bộ node catalog.

**Áp dụng cho Nexora:** n8n tham chiếu integration; không cấp DB/master-key hoặc cài executable nodes vào Nexora.

**Màn hình:** `/settings/integrations, /automation/webhooks`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Chọn supported connector/endpoint/VaultRef → test có mô tả side effect → Save Disabled.
2. Chọn allowed events/actions/projection Q-07 → Enable → delivery history.
3. Rotate/revoke/disconnect; retry sau current authorization checks.

## Dữ liệu và validation

- Connection owner/systemscope/provider/version/endpoint/VaultRef/status.
- MessageID/schemaVersion/occurredAt/signature/timestamp, body≤256KiB proposal.
- Delivery eventID/projectionhash/status Pending/Sending/Delivered/Failed/Cancelled/attempt/responsecode.

## Hành vi và lifecycle

- **FX-35-BR-001:** Proposal Q-07 outbound selected events + inbound trusted commands; không sync toàn User data mặc định.
- **FX-35-BR-002:** Signature replay window5min, messageID dedupe, rotation overlap bounded; invalid signature no mutation.
- **FX-35-BR-003:** SSRF/DNS/redirect/port/size/timeout guard; no secret headers/logs/responsecookies.
- **FX-35-BR-004:** Retry transient max3, permanent4xx no retry except provider429; disconnect chặn queued trước send; delivered không unsend.
- **FX-35-BR-005:** n8n down degraded, core CRUD vẫn hoạt động; final failure all 3.
- **FX-35-BR-006:** Vault payload excluded; Finance/Documents external egress cần Q-07, support không tạo integration grant.

## Quyền, API và tích hợp

- ConnectorManifest AuthMode/DataClassification/Test/Actions; versioned DeliverEvent/ReceiveCommand.
- Inbound gọi owner-authorized applicationcommands, không trust payload OwnerUserId.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-35-AC-001:** Replay/alteredsignature không lặp mutation.
- **FX-35-AC-002:** Disconnect trước retry chặn gửi.
- **FX-35-AC-003:** n8n outage không chặn Task Save; credential absent logs.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `INT-001`, `INT-002`
- [phase-06-developer-and-automation.md](../requirements/phases/phase-06-developer-and-automation.md): `P06-N8N-001`, `P06-N8N-002`, `P06-WHK-001`, `P06-WHK-002`

Quyết định lớn cần PO: [Q-07](90-open-decisions.md#q-07). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
