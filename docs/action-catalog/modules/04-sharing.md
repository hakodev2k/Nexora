# FX-04 — Read-only Sharing: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/04-read-only-sharing.md), [UX](../../ux-ui/modules/04-sharing.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="sharing-link-read"></a>`sharing.link.read` — Xem own links | QUERY / SELF | Yes when active | Resolved delegated | DEC-20260907-Q03; sensitive projection gates still apply | FX04-S01, FX04-S02 |
| <a id="sharing-link-create"></a>`sharing.link.create` — Tạo link | COMMAND / SELF | Yes when active | Resolved delegated | DEC-20260907-Q03; sensitive projection gates still apply | FX04-S01 |
| <a id="sharing-link-update"></a>`sharing.link.update` — Đổi audience/expiry/allowlist | COMMAND / SELF | Yes when active | Resolved delegated | DEC-20260907-Q03; sensitive projection gates still apply | FX04-S01 |
| <a id="sharing-link-revoke"></a>`sharing.link.revoke` — Thu hồi link | COMMAND / SELF | Yes when active | Resolved delegated | DEC-20260907-Q03; sensitive projection gates still apply | FX04-S02 |
| <a id="sharing-link-resolve"></a>`sharing.link.resolve` — Đọc approved shared projection | QUERY / LINK | No | Resolved delegated | DEC-20260907-Q03; sensitive projection gates still apply | FX04-S03, FX04-S04 |
| <a id="sharing-link-copy-created"></a>`sharing.link.copy_created` — Copy URL vừa tạo | LOCAL / SELF | No | Resolved delegated | DEC-20260907-Q03; sensitive projection gates still apply | FX04-S01 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `sharing.link.read` | Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; deleted owner/source/link or stale SharingEpoch →404; re-enable/restore cannot revive old token; active link management omits invalidated links | Common + dynamic source/provider guards |
| `sharing.link.create` | Kèm source *.share; create Published Documents only; không đổi source/pinned Resume version bằng Update; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; deleted owner/source/link or stale SharingEpoch →404; re-enable/restore cannot revive old token; active link management omits invalidated links | Common + dynamic source/provider guards |
| `sharing.link.update` | Kèm source *.share; create Published Documents only; không đổi source/pinned Resume version bằng Update; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; deleted owner/source/link or stale SharingEpoch →404; re-enable/restore cannot revive old token; active link management omits invalidated links | Common + dynamic source/provider guards |
| `sharing.link.revoke` | Kèm source *.share; create Published Documents only; không đổi source/pinned Resume version bằng Update; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; deleted owner/source/link or stale SharingEpoch →404; re-enable/restore cannot revive old token; active link management omits invalidated links | Common + dynamic source/provider guards |
| `sharing.link.resolve` | Token + mode/auth/allowlist + owner module + source lifecycle + field projection; không owner.read hoặc viewer module grant bypass; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; deleted owner/source/link or stale SharingEpoch →404; re-enable/restore cannot revive old token; active link management omits invalidated links | Common + dynamic source/provider guards |
| `sharing.link.copy_created` | Chỉ plaintext còn trong authorized creation-result memory; hash-only không reconstruct URL cũ; Cùng owner resource; link mode/expiry/revoke và source projection hiện tại; deleted owner/source/link or stale SharingEpoch →404; re-enable/restore cannot revive old token; active link management omits invalidated links | `sharing.link.create` |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
