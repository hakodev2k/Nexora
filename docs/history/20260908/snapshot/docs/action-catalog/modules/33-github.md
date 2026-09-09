# FX-33 — GitHub Discovery: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/33-github-discovery.md), [UX](../../ux-ui/modules/33-github-discovery.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="github-repository-search"></a>`github.repository.search` — Tìm public repositories | QUERY / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX33-S01 |
| <a id="github-repository-read"></a>`github.repository.read` — Xem repository metadata | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S02 |
| <a id="github-repository-refresh"></a>`github.repository.refresh` — Refresh public metadata | COMMAND / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX33-S02 |
| <a id="github-saved-read"></a>`github.saved.read` — Xem saved repositories | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-saved-save"></a>`github.saved.save` — Lưu repository | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-saved-notes"></a>`github.saved.notes` — Sửa own notes | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-saved-remove"></a>`github.saved.remove` — Bỏ lưu | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-read"></a>`github.query.read` — Xem Saved discovery query | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-create"></a>`github.query.create` — Tạo Saved discovery query | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-update"></a>`github.query.update` — Sửa Saved discovery query | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-delete"></a>`github.query.delete` — Xóa query | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S03 |
| <a id="github-query-run"></a>`github.query.run` — Chạy saved query | COMMAND / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX33-S03 |
| <a id="github-snapshot-read"></a>`github.snapshot.read` — Xem snapshots | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S04 |
| <a id="github-snapshot-capture"></a>`github.snapshot.capture` — Chụp discovery snapshot | COMMAND / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX33-S04 |
| <a id="github-snapshot-compare"></a>`github.snapshot.compare` — So sánh snapshots | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX33-S04 |
| <a id="github-provider-fetch"></a>`github.provider.fetch` — Fetch public metadata | WORKER / SYSTEM | No | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | Trusted worker/deployment only |
| <a id="github-support-read"></a>`github.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Resolved delegated | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `github.repository.search` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `github.repository.read` | Approved public provider; freshness/rate-limit surfaced; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.repository.refresh` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `github.saved.read` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.save` | Own reference metadata; không GitHub write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.notes` | Own reference metadata; không GitHub write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.saved.remove` | Own reference metadata; không GitHub write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.read` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.create` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.update` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | `github.query.read` |
| `github.query.delete` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.query.run` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `github.snapshot.read` | Public repository discovery only; không OAuth, star/fork/issues/write; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.snapshot.capture` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `github.snapshot.compare` | Same query/week/rule compatibility; unknown/stale shown explicitly; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |
| `github.provider.fetch` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `github.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Public repository discovery only; không OAuth, star/fork/issues/write | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
