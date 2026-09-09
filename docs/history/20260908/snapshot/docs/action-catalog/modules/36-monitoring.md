# FX-36 — Monitoring / Job Operations: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/36-monitoring-and-job-operations.md), [UX](../../ux-ui/modules/36-monitoring-jobs.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="monitoring-monitor-read"></a>`monitoring.monitor.read` — Xem Monitor | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S01 |
| <a id="monitoring-monitor-create"></a>`monitoring.monitor.create` — Tạo Monitor | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S02 |
| <a id="monitoring-monitor-update"></a>`monitoring.monitor.update` — Sửa Monitor | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S02 |
| <a id="monitoring-monitor-pause"></a>`monitoring.monitor.pause` — Tạm dừng | COMMAND / SELF | Yes when active | Resolved delegated | Local configuration only; network execution paused | FX36-S01 |
| <a id="monitoring-monitor-resume"></a>`monitoring.monitor.resume` — Tiếp tục | COMMAND / SELF | Yes when active | Resolved delegated | Local configuration only; network execution paused | FX36-S01 |
| <a id="monitoring-monitor-remove"></a>`monitoring.monitor.remove` — Ngừng monitor | COMMAND / SELF | Yes when active | Resolved delegated | Local configuration only; network execution paused | FX36-S01 |
| <a id="monitoring-monitor-check"></a>`monitoring.monitor.check` — Kiểm tra ngay | COMMAND / SELF | Yes when active | Paused | DEC-20260907-Q07 / DEP-EXT-01 | FX36-S03 |
| <a id="monitoring-observation-read"></a>`monitoring.observation.read` — Xem observations | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S03 |
| <a id="monitoring-incident-read"></a>`monitoring.incident.read` — Xem incidents | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S03 |
| <a id="monitoring-probe-run"></a>`monitoring.probe.run` — Thực thi probe | WORKER / SYSTEM | No | Paused | DEC-20260907-Q07 / DEP-EXT-01 | Trusted worker/deployment only |
| <a id="monitoring-job-read"></a>`monitoring.job.read` — Xem operational job metadata | QUERY / ADMIN | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S04, FX36-S05 |
| <a id="monitoring-job-retry"></a>`monitoring.job.retry` — Retry eligible job | COMMAND / ADMIN | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S05 |
| <a id="monitoring-job-cancel"></a>`monitoring.job.cancel` — Cancel queued/running job cooperatively | COMMAND / ADMIN | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX36-S05 |
| <a id="monitoring-support-read"></a>`monitoring.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Resolved delegated | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `monitoring.monitor.read` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.create` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.monitor.update` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | `monitoring.monitor.read` |
| `monitoring.monitor.pause` | Owner metadata state change only; resume preference cannot start probes before product scope resumes | Common + dynamic source/provider guards |
| `monitoring.monitor.resume` | Owner metadata state change only; resume preference cannot start probes before product scope resumes | Common + dynamic source/provider guards |
| `monitoring.monitor.remove` | Owner metadata state change only; resume preference cannot start probes before product scope resumes | Common + dynamic source/provider guards |
| `monitoring.monitor.check` | Network execution unavailable while integration scope paused; local tools/admin core-job operations remain independent | Common + dynamic source/provider guards |
| `monitoring.observation.read` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.incident.read` | User monitor scope tách operational jobs; không job payload private in admin lists; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.probe.run` | Network execution unavailable while integration scope paused; local tools/admin core-job operations remain independent | Common + dynamic source/provider guards |
| `monitoring.job.read` | Explicit Admin grant; type/status/retry metadata, no business body/secrets; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.job.retry` | Separate action grants; declared safe retry/cancel contract; original owner authority rechecked; cannot unsend accepted effect; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.job.cancel` | Separate action grants; declared safe retry/cancel contract; original owner authority rechecked; cannot unsend accepted effect; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |
| `monitoring.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; User monitor scope tách operational jobs; không job payload private in admin lists | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
