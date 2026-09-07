# FX-36 — Monitoring / Job Operations: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/36-monitoring-and-job-operations.md) — FX-36-BR-001, FX-36-BR-002, FX-36-BR-003, FX-36-BR-004, FX-36-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/36-monitoring-jobs.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `monitoring` là stable logical key, bind installed ModuleId trong manifest. **User monitor scope tách operational jobs; không job payload private in admin lists**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="monitoring-monitor-read"></a>`monitoring.monitor.read` — Xem Monitor | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX36-S01 |
| <a id="monitoring-monitor-create"></a>`monitoring.monitor.create` — Tạo Monitor | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX36-S02 |
| <a id="monitoring-monitor-update"></a>`monitoring.monitor.update` — Sửa Monitor | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX36-S02 |
| <a id="monitoring-monitor-pause"></a>`monitoring.monitor.pause` — Tạm dừng | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07/Q-08 for probing | FX36-S01 |
| <a id="monitoring-monitor-resume"></a>`monitoring.monitor.resume` — Tiếp tục | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07/Q-08 for probing | FX36-S01 |
| <a id="monitoring-monitor-remove"></a>`monitoring.monitor.remove` — Ngừng monitor | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07/Q-08 for probing | FX36-S01 |
| <a id="monitoring-monitor-check"></a>`monitoring.monitor.check` — Kiểm tra ngay | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07/Q-08 for probing | FX36-S03 |
| <a id="monitoring-observation-read"></a>`monitoring.observation.read` — Xem observations | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX36-S03 |
| <a id="monitoring-incident-read"></a>`monitoring.incident.read` — Xem incidents | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX36-S03 |
| <a id="monitoring-probe-run"></a>`monitoring.probe.run` — Thực thi probe | WORKER / SYSTEM | No | Normal (Normal) | Blocked Q-07/Q-08 | Trusted worker/deployment; no user control |
| <a id="monitoring-job-read"></a>`monitoring.job.read` — Xem operational job metadata | QUERY / ADMIN | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX36-S04, FX36-S05 |
| <a id="monitoring-job-retry"></a>`monitoring.job.retry` — Retry eligible job | COMMAND / ADMIN | Yes, gated | Operational (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX36-S05 |
| <a id="monitoring-job-cancel"></a>`monitoring.job.cancel` — Cancel queued/running job cooperatively | COMMAND / ADMIN | Yes, gated | Operational (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX36-S05 |
| <a id="monitoring-support-read"></a>`monitoring.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `monitoring.monitor.read` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.create` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.update` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | `monitoring.monitor.read` |
| `monitoring.monitor.pause` | Destination/interval/provider limits Q-07/Q-08; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.resume` | Destination/interval/provider limits Q-07/Q-08; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.remove` | Destination/interval/provider limits Q-07/Q-08; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.check` | Destination/interval/provider limits Q-07/Q-08; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.observation.read` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.incident.read` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.probe.run` | Approved destinations/agent only; no intranet scanner; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.job.read` | Explicit Admin grant; type/status/retry metadata, no business body/secrets; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.job.retry` | Separate action grants; declared safe retry/cancel contract; original owner authority rechecked; cannot unsend accepted effect; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.job.cancel` | Separate action grants; declared safe retry/cancel contract; original owner authority rechecked; cannot unsend accepted effect; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |

## Deny và UX contract

- Module off, grant missing/deny, resource wrong owner, disallowed lifecycle, current Q gate hoặc source dependency fail: không side effect; không dùng hidden button thay authorization.
- Before/after field diff được kiểm tra cho Save, import, version restore, bulk, scheduler và automation. Form không được gửi status/reveal/export/owner trong generic Update.
- Safe capability reason: ModuleUnavailable, ActionDenied, LifecycleLocked, DependencyUnavailable, DecisionBlocked hoặc StepUpRequired; unknown/wrong-owner resource trả unavailable chung để không enumerate.
- Grant không thay đổi state graph. Chỉ quyền đã cấp và hợp lệ mới xuất hiện enabled; permission editor có thể hiển thị blocked row để giải thích, không cho bật.
- Revocation và support/share/system contexts áp toàn bộ [common contract](../00-authorization-contract.md). Readonly projections không reuse full owner DTO.

## Acceptance tối thiểu

1. Với mỗi row: positive case đúng context/current state; wrong-owner và wrong-context negative; absent/deny Admin grant; module off; stale revision; lifecycle/Q gate.
2. COMMAND/COMPOSITE: request replay/idempotency, before-commit recheck; affected fields cần đủ action. QUERY: owner-scoped filtering trước count/pagination/projection, cache không rò source revoked.
3. LOCAL: keyboard/menu và tool entry cùng capability gate; không network/persist ngầm. SYSTEM: trusted caller, original authority và no UI grant.
4. Row nhạy cảm: no secret in response preview, toast, logs, URL, search, browser persistent storage; current recent-auth gate nếu required.
5. Nếu handler/source projection chưa có approved contract, action phải báo Blocked/Unavailable, không tự thực thi fallback rộng hơn.
