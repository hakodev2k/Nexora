# FX-38 — Digital Assets: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/38-digital-assets.md), [UX](../../ux-ui/modules/38-digital-assets.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="digital-asset-read"></a>`digital.asset.read` — Xem Digital Asset | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S01, FX38-S03 |
| <a id="digital-asset-create"></a>`digital.asset.create` — Tạo Digital Asset | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S02 |
| <a id="digital-asset-update"></a>`digital.asset.update` — Sửa Digital Asset | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S02 |
| <a id="digital-asset-cancel"></a>`digital.asset.cancel` — Đánh dấu canceled metadata | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S03 |
| <a id="digital-asset-archive"></a>`digital.asset.archive` — Archive asset | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-unarchive"></a>`digital.asset.unarchive` — Unarchive asset | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-trash"></a>`digital.asset.trash` — Đưa asset vào Thùng rác | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-restore"></a>`digital.asset.restore` — Khôi phục asset từ Thùng rác | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-purge"></a>`digital.asset.purge` — Xóa vĩnh viễn asset | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S01 |
| <a id="digital-asset-history"></a>`digital.asset.history` — Xem lịch sử asset | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S04 |
| <a id="digital-renewal-record"></a>`digital.renewal.record` — Ghi nhận renewal | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S04 |
| <a id="digital-credential-reference"></a>`digital.credential.reference` — Gắn/thay Vault reference | COMMAND / SELF | Yes when active | Blocked | Blocked Q-04 for Vault resolution | FX38-S02 |
| <a id="digital-observation-inspect"></a>`digital.observation.inspect` — Lấy public domain/TLS observation | COMMAND / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX38-S03 |
| <a id="digital-certificate-parse"></a>`digital.certificate.parse` — Parse public certificate local | LOCAL / SELF | No | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S02 |
| <a id="digital-asset-set-reminder"></a>`digital.asset.set_reminder` — Đặt expiry reminder | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX38-S03 |
| <a id="digital-asset-share"></a>`digital.asset.share` — Quản lý link chỉ-đọc của asset | COMPOSITE / SELF | Yes when active | Blocked | Blocked Q-03 | FX38-S03 |
| <a id="digital-support-read"></a>`digital.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Blocked | Blocked Q-03; domain gates also apply | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `digital.asset.read` | Metadata tracking only; không infrastructure control plane/payment renewal; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.create` | Metadata tracking only; không infrastructure control plane/payment renewal; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.update` | Metadata tracking only; không infrastructure control plane/payment renewal; Metadata tracking only; không infrastructure control plane/payment renewal | `digital.asset.read` |
| `digital.asset.cancel` | Không hủy service bên ngoài; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.archive` | Metadata tracking only; không infrastructure control plane/payment renewal; ngoài Trash, chưa Archived; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.unarchive` | Metadata tracking only; không infrastructure control plane/payment renewal; Archived, khôi phục previous state; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.trash` | Metadata tracking only; không infrastructure control plane/payment renewal; preview aggregate, không purge; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.restore` | Metadata tracking only; không infrastructure control plane/payment renewal; đúng deletion cohort, parent hợp lệ; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.purge` | Metadata tracking only; không infrastructure control plane/payment renewal; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.history` | Owner-only history, cùng owner/module; không share/support; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.renewal.record` | Explicit new expiry/cost metadata; no charge or provider write; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.credential.reference` | Same owner + purpose validation; không reveal từ Digital Assets; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.observation.inspect` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `digital.certificate.parse` | No private key input; local-only bounded parser; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.set_reminder` | Current source expiry/version; no auto-renew; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |
| `digital.asset.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Metadata tracking only; không infrastructure control plane/payment renewal | `sharing.link.read` |
| `digital.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Metadata tracking only; không infrastructure control plane/payment renewal | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
