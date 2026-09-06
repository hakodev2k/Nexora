# Monitoring và Job Operations

FX-36 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Jobs operational visibility và public endpoint/expiry checks thuộc Digital Assets.

[UptimeRobot](https://uptimerobot.com/): Monitoring hiển thị trạng thái kiểm tra và sự cố.

**Áp dụng cho Nexora:** UptimeRobot tham chiếu status/incidents; không tự thêm private network, browser synthetic hoặc cron heartbeat module mới.

**Màn hình:** `/admin/jobs, /monitoring`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Admin operational permission xem queue/failures/redacted error; retry/cancel audited.
2. Owner cấu hình Asset public endpoint monitor sau Q-07; preview target/method/interval.
3. Observations → incident sau failures → recovery sau successes; history chart.

## Dữ liệu và validation

- Job type/opaque ownerID/correlation/state/lease/attempt/timestamps/errorcode.
- Monitor asset/target/type HTTPstatus hoặc TLSexpiry/interval; observation time/status/latency.
- Proposed interval5min/timeout10s/incident3fails/recovery2successes; capacity Q-08.

## Hành vi và lifecycle

- **FX-36-BR-001:** Admin job detail không private payload; support read grant riêng nếu cần moduledata.
- **FX-36-BR-002:** Lease chỉ một active attempt; retry biết idempotency; Cancel không rollback giả.
- **FX-36-BR-003:** Egress guard; observed values tách manual Assetfields.
- **FX-36-BR-004:** Incident/recovery mỗi loại một logicalnotification all 3, không mỗi failed probe.
- **FX-36-BR-005:** Disable/delete asset stops newchecks; missing samples không được tính uptime thành công.

## Quyền, API và tích hợp

- JobStatus/RetryJob/CancelJob authorization; aggregate metrics safe.
- MonitorProvider→Observation→Incident→Notification; provider costs Q-08.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-36-AC-001:** Worker restart không double side effect.
- **FX-36-AC-002:** Three failures mở một incident; recovery dedupe.
- **FX-36-AC-003:** Operational logs không lộ Documentbody/Vaultvalue.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [02-cross-cutting-requirements.md](../requirements/02-cross-cutting-requirements.md): `JOB-001`, `JOB-002`, `JOB-003`, `JOB-004`
- [04-non-functional-requirements.md](../requirements/04-non-functional-requirements.md): `OBS-001`, `OBS-002`, `OBS-003`, `OBS-004`, `OBS-005`
- [phase-01-core-platform.md](../requirements/phases/phase-01-core-platform.md): `P01-PLT-006`
- [phase-07-assets-and-career.md](../requirements/phases/phase-07-assets-and-career.md): `P07-CER-003`, `P07-INF-002`

Quyết định lớn cần PO: [Q-07](90-open-decisions.md#q-07), [Q-08](90-open-decisions.md#q-08). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
