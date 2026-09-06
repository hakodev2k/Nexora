# Automation, Scheduler và Workflows

FX-34 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Triggers/actions/schedules, definitions, executions/logs/retry/cancel và failure alerts.

[n8n](https://n8n.io/features/): Workflow nối trigger/actions và theo dõi execution; không sao chép toàn bộ node catalog.

**Áp dụng cho Nexora:** n8n tham chiếu workflow/execution; chỉ trusted module actions, graph depth/data egress còn Q-07.

**Màn hình:** `/automation, /automation/:id/runs`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Draft → Manual/Schedule/ModuleEvent/Webhook trigger → steps → validate/next runs → Enable.
2. Run pinned definitionversion; xem step status/redacted output.
3. Disable stops new/queued work; edit tạo version; retry/cancel theo action contract.

## Dữ liệu và validation

- Definition name/version/status/trigger/actions configs/VaultRefs.
- Schedule timezone/start/end/frequency hoặc cron/missed policy.
- Run Queued/Running/Succeeded/PartiallySucceeded/Failed/Cancelled/TimedOut/Skipped; step attempts/timestamps.

## Hành vi và lifecycle

- **FX-34-BR-001:** Proposal Q-07 acyclic≤20steps linear+condition, no loops/customcode; không đánh dấu proposed graph là approved.
- **FX-34-BR-002:** Default one run/definition; busy queue latest; missed schedule skip, catch-up explicit; DST nonexistent skip/repeated occurrence once.
- **FX-34-BR-003:** Transient max3 retries/backoff; permission/validation failures no retry; timeout30s proposed admin bounds.
- **FX-34-BR-004:** Recheck current owner/module/action/resource/secret trước mỗi side effect; disabled queued→Skipped, running stop trước next effect.
- **FX-34-BR-005:** Cancel không giả rollback external effect; final failure một logical alert all 3, safe bounded logs.
- **FX-34-BR-006:** JSON definition import/export no secrets; importedDraft+rebind; DryRun chỉ khi mọi action supports simulation.

## Quyền, API và tích hợp

- TriggerRegistry/ActionRegistry/Validate/Run/Cancel/RetryStep, outbox/idempotency.
- Module upgrade dependency preflight; breaking actionversion migrate hoặc disable rõ reason.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-34-AC-001:** Revoke giữa steps chặn step sau.
- **FX-34-AC-002:** Duplicate trigger không double side effect.
- **FX-34-AC-003:** DryRun unsupported không gọi request thật.
- **FX-34-AC-004:** Retry final failure không spam alerts.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-06-developer-and-automation.md](../requirements/phases/phase-06-developer-and-automation.md): `P06-AUT-001`, `P06-AUT-002`, `P06-AUT-003`, `P06-AUT-004`, `P06-AUT-005`, `P06-AUT-006`, `P06-AUT-007`, `P06-AUT-008`, `P06-AUT-009`, `P06-AUT-010`, `P06-AUT-011`, `P06-AUT-012`, `P06-AUT-013`, `P06-AUT-014`

Quyết định lớn cần PO: [Q-07](90-open-decisions.md#q-07), [Q-08](90-open-decisions.md#q-08). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
