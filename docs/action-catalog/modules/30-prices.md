# FX-30 — Price Tracking: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/30-shopee-price-tracking.md) — FX-30-BR-001, FX-30-BR-002, FX-30-BR-003, FX-30-BR-004, FX-30-BR-005, FX-30-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/30-price-tracking.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `prices` là stable logical key, bind installed ModuleId trong manifest. **Provider/Shopee policy Q-06; stale không price zero; không checkout**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="prices-tracker-read"></a>`prices.tracker.read` — Xem Product/variant tracker | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S01 |
| <a id="prices-tracker-create"></a>`prices.tracker.create` — Tạo Product/variant tracker | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S02 |
| <a id="prices-tracker-update"></a>`prices.tracker.update` — Sửa Product/variant tracker | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S02 |
| <a id="prices-tracker-pause"></a>`prices.tracker.pause` — Tạm dừng | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S01 |
| <a id="prices-tracker-resume"></a>`prices.tracker.resume` — Tiếp tục | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S01 |
| <a id="prices-tracker-remove"></a>`prices.tracker.remove` — Ngừng tracking | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S01 |
| <a id="prices-tracker-refresh"></a>`prices.tracker.refresh` — Refresh price | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S03 |
| <a id="prices-observation-read"></a>`prices.observation.read` — Xem price history | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S03 |
| <a id="prices-alert-read"></a>`prices.alert.read` — Xem Price alert rule | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S04 |
| <a id="prices-alert-create"></a>`prices.alert.create` — Tạo Price alert rule | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S04 |
| <a id="prices-alert-update"></a>`prices.alert.update` — Sửa Price alert rule | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S04 |
| <a id="prices-alert-enable"></a>`prices.alert.enable` — Bật alert | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S04 |
| <a id="prices-alert-disable"></a>`prices.alert.disable` — Tắt alert | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S04 |
| <a id="prices-alert-remove"></a>`prices.alert.remove` — Xóa alert | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S04 |
| <a id="prices-delivery-read"></a>`prices.delivery.read` — Xem alert history | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-06 | FX30-S05 |
| <a id="prices-worker-fetch"></a>`prices.worker.fetch` — Thu thập price observation | WORKER / SYSTEM | No | Normal (Normal) | Blocked Q-06 | Trusted worker/deployment; no user control |
| <a id="prices-worker-evaluate"></a>`prices.worker.evaluate` — Evaluate current alert rules | WORKER / SYSTEM | No | Normal (Normal) | Blocked Q-06 | Trusted worker/deployment; no user control |
| <a id="prices-support-read"></a>`prices.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `prices.tracker.read` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.tracker.create` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.tracker.update` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | `prices.tracker.read` |
| `prices.tracker.pause` | Exact product+variant; provider terms/cost/freshness Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.tracker.resume` | Exact product+variant; provider terms/cost/freshness Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.tracker.remove` | Exact product+variant; provider terms/cost/freshness Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.tracker.refresh` | Exact product+variant; provider terms/cost/freshness Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.observation.read` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.alert.read` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.alert.create` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.alert.update` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | `prices.alert.read` |
| `prices.alert.enable` | Rule version, cooldown/dedupe Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.alert.disable` | Rule version, cooldown/dedupe Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.alert.remove` | Rule version, cooldown/dedupe Q-06; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.delivery.read` | Provider/Shopee policy Q-06; stale không price zero; không checkout; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.worker.fetch` | Immutable observations + provider freshness; no CAPTCHA bypass; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.worker.evaluate` | Immutable observations + provider freshness; no CAPTCHA bypass; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |
| `prices.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Provider/Shopee policy Q-06; stale không price zero; không checkout | Common + dynamic source/provider guards |

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
