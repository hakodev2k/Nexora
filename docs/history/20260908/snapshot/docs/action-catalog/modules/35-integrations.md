# FX-35 — Integrations / Webhooks / n8n: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/35-integrations-webhooks-and-n8n.md), [UX](../../ux-ui/modules/35-integrations-webhooks.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="integrations-connection-read"></a>`integrations.connection.read` — Xem Owner connection | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S01 |
| <a id="integrations-connection-create"></a>`integrations.connection.create` — Tạo Owner connection | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S02 |
| <a id="integrations-connection-update"></a>`integrations.connection.update` — Sửa Owner connection | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S02 |
| <a id="integrations-connection-test"></a>`integrations.connection.test` — Test connection | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S02 |
| <a id="integrations-connection-disable"></a>`integrations.connection.disable` — Disable connection | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S01 |
| <a id="integrations-connection-scopes"></a>`integrations.connection.scopes` — Đổi scopes | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S02 |
| <a id="integrations-connection-credential"></a>`integrations.connection.credential` — Thay credential reference | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S05 |
| <a id="integrations-webhook-read"></a>`integrations.webhook.read` — Xem Webhook | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-webhook-create"></a>`integrations.webhook.create` — Tạo Webhook | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-webhook-update"></a>`integrations.webhook.update` — Sửa Webhook | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-webhook-enable"></a>`integrations.webhook.enable` — Enable webhook | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-webhook-disable"></a>`integrations.webhook.disable` — Disable webhook | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-webhook-test"></a>`integrations.webhook.test` — Gửi test event | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-webhook-rotate"></a>`integrations.webhook.rotate` — Rotate signing reference | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S03 |
| <a id="integrations-delivery-read"></a>`integrations.delivery.read` — Xem delivery records | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S04 |
| <a id="integrations-delivery-retry"></a>`integrations.delivery.retry` — Retry delivery | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S04 |
| <a id="integrations-system-connection-read"></a>`integrations.system_connection.read` — Xem operational connection metadata | QUERY / ADMIN | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S01 |
| <a id="integrations-system-connection-configure"></a>`integrations.system_connection.configure` — Cấu hình system connection | COMMAND / SUPER | No | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX35-S01 |
| <a id="integrations-worker-receive"></a>`integrations.worker.receive` — Accept verified inbound webhook | WORKER / SYSTEM | No | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | Trusted worker/deployment only |
| <a id="integrations-worker-send"></a>`integrations.worker.send` — Dispatch authorized outbound event | WORKER / SYSTEM | No | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | Trusted worker/deployment only |
| <a id="integrations-support-read"></a>`integrations.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `integrations.connection.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.connection.create` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.connection.update` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | `integrations.connection.read` |
| `integrations.connection.test` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.connection.disable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.connection.scopes` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.connection.credential` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.webhook.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.webhook.create` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.webhook.update` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | `integrations.webhook.read` |
| `integrations.webhook.enable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.webhook.disable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.webhook.test` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.webhook.rotate` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.delivery.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.delivery.retry` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.system_connection.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.system_connection.configure` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.worker.receive` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.worker.send` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `integrations.support.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
