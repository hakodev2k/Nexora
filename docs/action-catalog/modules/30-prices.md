# FX-30 — Price Tracking: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/30-shopee-price-tracking.md), [UX](../../ux-ui/modules/30-price-tracking.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="prices-tracker-read"></a>`prices.tracker.read` — Xem Product/variant tracker | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S01 |
| <a id="prices-tracker-create"></a>`prices.tracker.create` — Tạo Product/variant tracker | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S02 |
| <a id="prices-tracker-update"></a>`prices.tracker.update` — Sửa Product/variant tracker | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S02 |
| <a id="prices-tracker-pause"></a>`prices.tracker.pause` — Tạm dừng | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S01 |
| <a id="prices-tracker-resume"></a>`prices.tracker.resume` — Tiếp tục | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S01 |
| <a id="prices-tracker-remove"></a>`prices.tracker.remove` — Ngừng tracking | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S01 |
| <a id="prices-tracker-refresh"></a>`prices.tracker.refresh` — Refresh price | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S03 |
| <a id="prices-observation-read"></a>`prices.observation.read` — Xem price history | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S03 |
| <a id="prices-alert-read"></a>`prices.alert.read` — Xem Price alert rule | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S04 |
| <a id="prices-alert-create"></a>`prices.alert.create` — Tạo Price alert rule | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S04 |
| <a id="prices-alert-update"></a>`prices.alert.update` — Sửa Price alert rule | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S04 |
| <a id="prices-alert-enable"></a>`prices.alert.enable` — Bật alert | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S04 |
| <a id="prices-alert-disable"></a>`prices.alert.disable` — Tắt alert | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S04 |
| <a id="prices-alert-remove"></a>`prices.alert.remove` — Xóa alert | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S04 |
| <a id="prices-delivery-read"></a>`prices.delivery.read` — Xem alert history | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX30-S05 |
| <a id="prices-worker-fetch"></a>`prices.worker.fetch` — Thu thập price observation | WORKER / SYSTEM | No | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | Trusted worker/deployment only |
| <a id="prices-worker-evaluate"></a>`prices.worker.evaluate` — Evaluate current alert rules | WORKER / SYSTEM | No | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | Trusted worker/deployment only |
| <a id="prices-support-read"></a>`prices.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `prices.tracker.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.tracker.create` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.tracker.update` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | `prices.tracker.read` |
| `prices.tracker.pause` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.tracker.resume` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.tracker.remove` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.tracker.refresh` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.observation.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.alert.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.alert.create` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.alert.update` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | `prices.alert.read` |
| `prices.alert.enable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.alert.disable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.alert.remove` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.delivery.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.worker.fetch` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.worker.evaluate` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `prices.support.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
