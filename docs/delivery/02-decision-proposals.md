# Quyết định đã xử lý và proposal cần Product Owner

2026-09-09 · User/Product Owner đã chấp nhận các option khuyến nghị trong implementation-readiness interview. File này giữ rationale/trade-off của các proposal cũ, nhưng trạng thái hiện hành nằm ở [PO decisions 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md) và [Decision status](../features/90-open-decisions.md).

## Resolved delegated — dùng cho M01

| ID | Quyết định | Tác động / căn cứ |
| --- | --- | --- |
| D-H01 | M01 là milestone nội bộ nền tảng, không phải R1 phát hành | Không giảm catalog R1; [scope](01-current-scope.md) |
| D-H02 | Cùng origin, cookie opaque server-side, SQL authority, CSRF; không browser bearer persistence | Tiếp nối ADR-D08, hiện thực request contract cụ thể |
| D-H03 | RFC 9457 errors; strong ETag/If-Match; operation-scoped idempotency | [API](milestone-01/02-api-contracts.md); không thay semantic lifecycle |
| D-H04 | Verify mới tạo PersonalSpace + grant snapshot trong một SQL transaction | Hợp nhất feature cũ với ADR-D09; email verification mới cho dùng |
| D-H05 | UI vi mặc định, chọn en; timezone IANA riêng; không tự suy tiền tệ | Đúng Q09; language gate đã đóng |
| D-H06 | Deleted account không reuse email/không tự phục hồi trong M01 | UQ giữ dữ liệu đúng owner; chính sách same-account restore/no-email-reuse đã được PO chốt tại DEC-20260909-002 |
| D-H07 | Không handler cho unknown/Paused/Blocked action; registration chỉ default installed+Ready+active scope | Hợp nhất all-modules-on với pause và module extension |
| D-H08 | Các version/toolchain dưới environment là research pins; compatibility và security recheck ở S00 | Không gọi chưa cài là tested |
| D-H09 | Basic Finance luôn nhập Currency rõ ràng; không tạo default VND từ vi | Amount cần đơn vị, advanced Finance vẫn gated |

## Product Owner approved — 2026-09-09

| Previous proposal | Current status | Decision summary | Still required before implementation/release |
| --- | --- | --- | --- |
| P-H01 — Mất Google Authenticator | Policy Approved | TOTP lost-device recovery dùng one-time recovery codes; email+password alone không reset MFA; mất cả TOTP/codes thì không có self-service recovery | MFA enrollment/recovery slice approval, schema/API/UX/AC, security review |
| P-H02 — Khôi phục account deleted/email reuse | Policy Approved | Account deletion soft-delete; no purge; no email reuse for new owner; restore returns same account/UserId/PersonalSpace | Restore UI/API slice approval and evidence; M01 only denies deleted accounts |
| P-H03 — Sensitive share/support projections | Policy Approved | Allowlist projection, default-hidden sensitive fields, Support safe metadata only, Emergency read-only/no export/copy secret | Concrete field projections per Finance/Vault/Assets/Career/Learning resource |
| P-H04 — Vault portability/recovery | Policy Approved | Hybrid owner encrypted portability + server-assisted recovery; no operator ambient plaintext/export | Crypto/key wrapping/package ADR, threat model, restore rehearsal |
| P-H05 — Finance nâng cao | Initial scope Approved; advanced remains gated | Finance starts as basic manual category/amount/explicit currency/date/optional note | Advanced ledger/accounts/transfers/budget/debt/interest/FX/deletion needs later PO approval |
| P-H06 — Task/Reminder mở rộng | Initial scope Approved; extensions remain gated | First Productivity slice keeps flat Tasks + one reminder | Subtask/recurrence/snooze/standalone reminder/Task attachment need later PO approval |
| P-H07 — Internal navigation/outbound ingestion | Boundary Approved | News/GitHub/Monitoring may use read-only public outbound after allowlist/SSRF/rate-limit/degraded-state contract; Monitoring probes only owner-configured target | Slice approval, network guard implementation/evidence; does not resume paused modules |
| P-H08 — R1/capacity/production | Sequencing Approved | Local Stable first; no provider/capacity/RPO/RTO/SLA commitment now | Production architecture, provider/budget/capacity and Go/No-Go after Local Stable evidence |

## Implementation approval

`DEC-20260909-001` approves **M01 + backend/frontend scaffold + local scripts**. This approval is intentionally narrow. It authorizes code for the exact M01 stories/contracts and the supporting local scaffold/scripts needed to prove them.

It does not authorize:

- Full Phase1/R1 implementation.
- Production deployment/public launch/provider spend/domains.
- Production data/secrets.
- Business modules outside M01.
- Price Tracking, Automation/Scheduler/Workflows or Integrations/Webhooks/n8n.
- OAuth/write/payment/executable third-party integration.

## Remaining PO decisions after this amendment

- Whether to offer manual support recovery if a User loses both TOTP device and recovery codes.
- Exact field-level allowlists for each sensitive resource before implementing sensitive share/support.
- Advanced Finance semantics when those capabilities are resumed.
- Task/Reminder extension semantics when those capabilities are resumed.
- Production provider, cost envelope, capacity target, RPO/RTO and SLA after Local Stable.
- Explicit resume of FX30/34/35 if Product Owner wants them active.

## Cách đóng tiếp

Mỗi future slice phải cập nhật source quote, current feature, DB, UX, action gate và acceptance; không chỉ đổi chữ Open. Chưa có câu trả lời thì giữ fail-closed đúng phần, không khóa toàn bộ hệ thống. [Readiness theo story](milestone-01/06-readiness-and-evidence.md).
