# FX-34 — Automation: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/34-automation-and-scheduler.md), [UX](../../ux-ui/modules/34-automation.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="automation-definition-read"></a>`automation.definition.read` — Xem definitions | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01, FX34-S03 |
| <a id="automation-definition-create"></a>`automation.definition.create` — Tạo definition | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01, FX34-S02 |
| <a id="automation-definition-save"></a>`automation.definition.save` — Save immutable definition version | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S02 |
| <a id="automation-definition-validate"></a>`automation.definition.validate` — Validate definition | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S02 |
| <a id="automation-definition-enable"></a>`automation.definition.enable` — Enable definition | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01 |
| <a id="automation-definition-disable"></a>`automation.definition.disable` — Disable definition | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01 |
| <a id="automation-definition-trash"></a>`automation.definition.trash` — Đưa definition vào Thùng rác | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01 |
| <a id="automation-definition-restore"></a>`automation.definition.restore` — Khôi phục definition từ Thùng rác | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01 |
| <a id="automation-definition-purge"></a>`automation.definition.purge` — Xóa vĩnh viễn definition | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S01 |
| <a id="automation-definition-history"></a>`automation.definition.history` — Xem lịch sử definition | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S03 |
| <a id="automation-run-read"></a>`automation.run.read` — Xem runs/step results | QUERY / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S04 |
| <a id="automation-run-start"></a>`automation.run.start` — Chạy approved definition | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S05 |
| <a id="automation-run-cancel"></a>`automation.run.cancel` — Yêu cầu dừng run | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S05 |
| <a id="automation-run-retry-step"></a>`automation.run.retry_step` — Retry eligible failed step | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S05 |
| <a id="automation-run-dry-run"></a>`automation.run.dry_run` — Preview simulated run | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S05 |
| <a id="automation-schedule-update"></a>`automation.schedule.update` — Đổi schedule | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S02 |
| <a id="automation-definition-import"></a>`automation.definition.import` — Import definition | COMPOSITE / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S02 |
| <a id="automation-definition-export"></a>`automation.definition.export` — Export definition không secrets | COMPOSITE / SELF | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX34-S02 |
| <a id="automation-step-dispatch"></a>`automation.step.dispatch` — Execute one authorized step | WORKER / SYSTEM | No | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | Trusted worker/deployment only |
| <a id="automation-support-read"></a>`automation.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Paused | DEC-20260907-Q06/Q07: Paused by Product Owner | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `automation.definition.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.create` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.save` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.validate` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.enable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.disable` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.trash` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.restore` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.purge` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.history` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.run.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.run.start` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.run.cancel` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.run.retry_step` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.run.dry_run` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.schedule.update` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.import` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.definition.export` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.step.dispatch` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |
| `automation.support.read` | No active product handler/provider/worker/enablement; preserve definition/history only. Resume requires PO; core notification infrastructure is separate | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
