# Decision status — cập nhật theo Product Owner 2026-09-09

> Current specification · reconciled 2026-09-09. [Previous version](../history/20260908/snapshot/docs/features/90-open-decisions.md) is historical evidence, not implementation input.

[Biên bản lời PO 2026-09-07](../requirements/10-owner-decisions-20260907.md) và [PO implementation-readiness decisions 2026-09-09](../requirements/11-owner-decisions-20260909-implementation-readiness.md) là nguồn quyết định hiện hành. Các proposal trước đây không tự trở thành Approved; các mục dưới đây chỉ Approved đúng phạm vi được nêu. [DEC-20260909-014](../requirements/12-owner-decisions-20260909-local-e2e.md) approves full documented R1 local implementation, with complete contracts and simulated providers; production remains unapproved.

<a id="q-01"></a>
## Q-01 — Resolved for account deletion/recovery policy

**Đã xác nhận:** Account delete = soft delete; không purge tài khoản/dữ liệu do thao tác này. Deleted email không được reuse cho owner mới. Khi sau này có restore, restore phải dùng cùng account/UserId/PersonalSpace; password reset không auto-reactivate deleted account.

**Còn lại / giới hạn:** Self-service restore UI/API không nằm trong M01. Không có permanent purge path được duyệt.

Nguồn: DEC-20260907-Q01, DEC-20260909-002.

<a id="q-02"></a>
## Q-02 — Resolved policy; implementation staged

**Đã xác nhận:** Google Authenticator TOTP là phương thức MFA được duyệt. Lost-device recovery dùng one-time recovery codes tạo khi enrollment, plaintext chỉ hiển thị một lần và lưu hash. Email + password không đủ reset MFA. Nếu mất cả TOTP và recovery codes thì không có self-service recovery theo quyết định hiện tại.

**Còn lại / giới hạn:** M01 vẫn password-only; TOTP enrollment/recovery không ship trong M01. MFA-enabled fixtures phải fail closed, không bypass bằng password-only. Manual support recovery cần PO decision riêng nếu muốn thêm.

Nguồn: DEC-20260907-Q02, DEC-20260908-Q02-TOTP, DEC-20260909-003.

<a id="q-03"></a>
## Q-03 — Resolved policy; module projections still required

**Đã xác nhận:** Tắt sharing hoặc xóa nguồn làm link cũ bị loại bỏ vĩnh viễn. Sensitive share/support dùng projection allowlist, không serialize toàn owner DTO. Field nhạy cảm mặc định bị ẩn; owner phải preview/chọn rõ trước khi exposed. Support chỉ xem safe metadata/error/debug state. Emergency vẫn read-only và không export/copy secret.

**Còn lại / giới hạn:** Finance/Assets/Career/Learning/Vault vẫn cần field projection contracts và acceptance tests riêng trước khi release share/support cho các resource nhạy cảm.

Nguồn: DEC-20260907-Q03, DEC-20260909-005.

<a id="q-04"></a>
## Q-04 — Resolved product policy; crypto design still gated

**Đã xác nhận:** Vault theo hướng hybrid recoverability: owner-controlled encrypted portability + server-assisted recovery workflow. Operators/SuperAdmin có thể authorize recovery theo workflow nhưng không được có plaintext thường trực, ambient decrypt, hoặc export secret của User khác.

**Còn lại / giới hạn:** Vault không thuộc M01. Key wrapping, encrypted package format, rehearsal import/export, restore evidence và threat/security review vẫn phải được chốt trong technical ADR trước Vault release.

Nguồn: DEC-20260907-Q04, DEC-20260909-004.

<a id="q-05"></a>
## Q-05 — Resolved initial Finance scope; advanced Finance gated

**Đã xác nhận:** Finance initial implementation là manual records: category, amount, explicit currency, date và optional note. UI language không quyết định tiền tệ.

**Còn lại / giới hạn:** Ledger/account/transfer/budget/debt/interest/FX/advanced deletion semantics chưa được duyệt. Không tự coi các tính năng nâng cao đã approved hoặc bị hủy khỏi R1 catalog.

Nguồn: DEC-20260907-Q05, DEC-20260909-006.

<a id="q-06"></a>
## Q-06 — Local Price Tracking simulation approved

DEC-20260909-014 resumes Price Tracking for local simulated implementation after complete contracts. Real provider calls remain disabled. Simulated observations and alerts require explicit labeling, authorization and durable execution.

<a id="q-07"></a>
## Q-07 — Local Automation/Integrations simulation approved

DEC-20260909-014 resumes FX34/35 for local/simulated/integration-safe implementation. Complete API/DB/UX/AC/security/evidence contracts first. Real external webhooks, provider writes, OAuth and n8n execution remain disabled. Core workers retain durable state, bounded retries, leases and idempotency.

<a id="q-08"></a>
## Q-08 — Resolved sequencing; measurable production targets deferred

**Đã xác nhận:** Local Stable đi trước production. Chưa cam kết provider, capacity guarantee, RPO/RTO hoặc SLA. News/GitHub Discovery/Monitoring có thể dùng backend read-only outbound public metadata/feed sau khi có slice contract, allowlist, SSRF protection, redirect/payload limits, timeout, retry/rate limits và degraded states. Monitoring HTTP probing chỉ được phép sau khi owner cấu hình target.

**Còn lại / giới hạn:** Production workload/concurrency/storage/email budget/RPO/RTO/SLA sẽ chốt sau Local Stable evidence. Không dùng M01/local evidence làm production approval.

Nguồn: DEC-20260907-Q08, DEC-20260909-008, DEC-20260909-010.

<a id="q-09"></a>
## Q-09 — Resolved for language/timezone/currency handling

**Đã xác nhận:** UI mặc định tiếng Việt; User chuyển sang tiếng Anh trong Settings. Timezone là IANA/browser-detected và User đổi được. Finance amount phải có explicit currency; không suy currency từ language.

**Còn lại / giới hạn:** Mọi module có monetary value phải giữ currency rõ ràng trong field/schema/API. Default display preference có thể là UX detail, nhưng không thay currency của record.

Nguồn: DEC-20260907-Q09, DEC-20260909-006.

<a id="q-10"></a>
## Q-10 — Resolved initial Productivity scope; extensions gated

**Đã xác nhận:** Giữ các Task/Project/Reminder rules đã chốt. First Productivity implementation giữ flat Tasks: mỗi Task thuộc một Project và có tối đa một reminder theo scope đã duyệt.

**Còn lại / giới hạn:** Subtask, recurring task, snooze, standalone reminder và Task attachment không thuộc first Productivity slice; cần PO decision + data/lifecycle/timezone contracts riêng nếu muốn thêm.

Nguồn: DEC-20260907-Q10, DEC-20260909-007.

<a id="q-11"></a>
## Q-11 — Resolved scope; delegated fidelity contract

**Đã xác nhận:** Document/Resume formats cơ bản DOCX và Markdown (.md).

**Còn lại / giới hạn:** Supported subset + preview/loss report được đặc tả kỹ thuật; không hứa Word round-trip hoàn hảo. PDF/HTML product export không thuộc baseline mới; ICS Calendar và định dạng import riêng module khác giữ scope riêng.

Nguồn: DEC-20260907-Q11.

<a id="q-12"></a>
## Q-12 — Resolved workflow; delegated ownership

**Đã xác nhận:** Chỉ tạo/liên kết Event trong Nexora và thông báo.

**Còn lại / giới hạn:** Calendar sở hữu Personal Event; Career chỉ reference. Không phỏng vấn trực tuyến, conference URL, provider invite hoặc external interview workflow.

Nguồn: DEC-20260907-Q12.

## Implementation approval status

`DEC-20260909-014` approves full documented R1 local implementation in contract-complete slices. Agents continue after reading `docs/delivery/milestone-01/**`, `docs/delivery/01-current-scope.md`, goals and AGENTS instructions. Every change must trace to story/action/AC and actual test evidence.

## Các việc còn cần PO quyết định tiếp

- Manual support recovery khi User mất cả TOTP device và recovery codes, nếu muốn hỗ trợ ngoài mặc định no self-service.
- Concrete field-level projections cho từng sensitive module trước khi mở share/support của các resource đó.
- Vault crypto package/key wrapping/import-export ADR trước Vault release.
- Advanced Finance semantics nếu muốn vượt basic manual records.
- Task/Reminder extensions nếu muốn vượt flat Task + one reminder.
- Provider/cost/capacity/RPO/RTO/SLA sau Local Stable.
- Real provider execution requires later explicit approval. FX30/34/35 local simulation is already approved.

Các chi tiết kỹ thuật còn lại do technical owner tự chốt thành ADR/acceptance có thể kiểm chứng trong phạm vi đã được PO approve. DEC-20260909-014 supplies local resume approval; real providers remain disabled.
