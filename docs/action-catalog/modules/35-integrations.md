# FX-35 — Integrations / Webhooks / n8n: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/35-integrations-webhooks-and-n8n.md) — FX-35-BR-001, FX-35-BR-002, FX-35-BR-003, FX-35-BR-004, FX-35-BR-005, FX-35-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/35-integrations-webhooks.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `integrations` là stable logical key, bind installed ModuleId trong manifest. **Q-07 provider scopes/cost/network; credentials by Vault reference**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="integrations-connection-read"></a>`integrations.connection.read` — Xem Owner connection | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S01 |
| <a id="integrations-connection-create"></a>`integrations.connection.create` — Tạo Owner connection | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S02 |
| <a id="integrations-connection-update"></a>`integrations.connection.update` — Sửa Owner connection | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S02 |
| <a id="integrations-connection-test"></a>`integrations.connection.test` — Test connection | COMMAND / SELF | Yes, gated | Security (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S02 |
| <a id="integrations-connection-disable"></a>`integrations.connection.disable` — Disable connection | COMMAND / SELF | Yes, gated | Security (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S01 |
| <a id="integrations-connection-scopes"></a>`integrations.connection.scopes` — Đổi scopes | COMMAND / SELF | Yes, gated | Security (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S02 |
| <a id="integrations-connection-credential"></a>`integrations.connection.credential` — Thay credential reference | COMMAND / SELF | Yes, gated | Security (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S05 |
| <a id="integrations-webhook-read"></a>`integrations.webhook.read` — Xem Webhook | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-webhook-create"></a>`integrations.webhook.create` — Tạo Webhook | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-webhook-update"></a>`integrations.webhook.update` — Sửa Webhook | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-webhook-enable"></a>`integrations.webhook.enable` — Enable webhook | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-webhook-disable"></a>`integrations.webhook.disable` — Disable webhook | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-webhook-test"></a>`integrations.webhook.test` — Gửi test event | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-webhook-rotate"></a>`integrations.webhook.rotate` — Rotate signing reference | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S03 |
| <a id="integrations-delivery-read"></a>`integrations.delivery.read` — Xem delivery records | QUERY / SELF | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S04 |
| <a id="integrations-delivery-retry"></a>`integrations.delivery.retry` — Retry delivery | COMMAND / SELF | Yes, gated | Network (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S04 |
| <a id="integrations-system-connection-read"></a>`integrations.system_connection.read` — Xem operational connection metadata | QUERY / ADMIN | Yes, gated | Normal (Normal) | Blocked Q-07; Vault references Q-04 | FX35-S01 |
| <a id="integrations-system-connection-configure"></a>`integrations.system_connection.configure` — Cấu hình system connection | COMMAND / SUPER | No | Security (Administrative) | Blocked Q-07; Vault references Q-04 | FX35-S01 |
| <a id="integrations-worker-receive"></a>`integrations.worker.receive` — Accept verified inbound webhook | WORKER / SYSTEM | No | Network (Administrative) | Blocked Q-07; Vault references Q-04 | Trusted worker/deployment; no user control |
| <a id="integrations-worker-send"></a>`integrations.worker.send` — Dispatch authorized outbound event | WORKER / SYSTEM | No | Network (Administrative) | Blocked Q-07; Vault references Q-04 | Trusted worker/deployment; no user control |
| <a id="integrations-support-read"></a>`integrations.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `integrations.connection.read` | Q-07 provider scopes/cost/network; credentials by Vault reference; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.connection.create` | Q-07 provider scopes/cost/network; credentials by Vault reference; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.connection.update` | Q-07 provider scopes/cost/network; credentials by Vault reference; Q-07 provider scopes/cost/network; credentials by Vault reference | `integrations.connection.read` |
| `integrations.connection.test` | Diff of scopes/effects; Vault reference purpose validation; not reveal plaintext; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.connection.disable` | Diff of scopes/effects; Vault reference purpose validation; not reveal plaintext; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.connection.scopes` | Diff of scopes/effects; Vault reference purpose validation; not reveal plaintext; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.connection.credential` | Diff of scopes/effects; Vault reference purpose validation; not reveal plaintext; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.webhook.read` | Q-07 provider scopes/cost/network; credentials by Vault reference; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.webhook.create` | Q-07 provider scopes/cost/network; credentials by Vault reference; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.webhook.update` | Q-07 provider scopes/cost/network; credentials by Vault reference; Q-07 provider scopes/cost/network; credentials by Vault reference | `integrations.webhook.read` |
| `integrations.webhook.enable` | Current endpoint/scope/signature policy, preview network effects; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.webhook.disable` | Current endpoint/scope/signature policy, preview network effects; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.webhook.test` | Current endpoint/scope/signature policy, preview network effects; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.webhook.rotate` | Current endpoint/scope/signature policy, preview network effects; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.delivery.read` | Redacted payload/headers; no secrets; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.delivery.retry` | Idempotent adapter + current owner permissions + known failure; no blind replay; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.system_connection.read` | Operational grant; không user credentials; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.system_connection.configure` | SuperAdmin-only; environment policy + audit; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.worker.receive` | Signature/replay defense, source event projection and owner target permissions; no actor from payload; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.worker.send` | Signature/replay defense, source event projection and owner target permissions; no actor from payload; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |
| `integrations.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Q-07 provider scopes/cost/network; credentials by Vault reference | Common + dynamic source/provider guards |

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
