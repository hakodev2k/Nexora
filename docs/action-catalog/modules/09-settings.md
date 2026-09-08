# FX-09 — Settings / Shell: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/09-settings-and-app-shell.md), [UX](../../ux-ui/modules/09-settings-app-shell.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="settings-preference-read"></a>`settings.preference.read` — Xem preferences/navigation | QUERY / SELF | Yes when active | Resolved delegated | DEC-20260907-Q09 / INTERNAL | FX09-S01, FX09-S04 |
| <a id="settings-preference-update"></a>`settings.preference.update` — Lưu theme/nav/display preferences | COMMAND / SELF | Yes when active | Resolved delegated | DEC-20260907-Q09 / INTERNAL | FX09-S02 |
| <a id="settings-module-read"></a>`settings.module.read` — Xem module settings | COMPOSITE / SELF | Yes when active | Resolved delegated | DEC-20260907-Q09 / INTERNAL | FX09-S03 |
| <a id="settings-module-update"></a>`settings.module.update` — Lưu owner module settings | COMPOSITE / SELF | Yes when active | Resolved delegated | DEC-20260907-Q09 / INTERNAL | FX09-S03 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `settings.preference.read` | Own nonsensitive preferences; no arbitrary JSON business data; Own nonsensitive preferences; no arbitrary JSON business data; UI vi default, explicit en preference; no currency/timezone/content translation; no external navigation | Common + dynamic source/provider guards |
| `settings.preference.update` | Không đổi quyền/module grants; approved default views giữ nguyên; Own nonsensitive preferences; no arbitrary JSON business data; UI vi default, explicit en preference; no currency/timezone/content translation; no external navigation | Common + dynamic source/provider guards |
| `settings.module.read` | Kèm target module/action; không plaintext credentials; Own nonsensitive preferences; no arbitrary JSON business data; UI vi default, explicit en preference; no currency/timezone/content translation; no external navigation | Common + dynamic source/provider guards |
| `settings.module.update` | Target declared setting action required; không system policy/self-grant; Own nonsensitive preferences; no arbitrary JSON business data; UI vi default, explicit en preference; no currency/timezone/content translation; no external navigation | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
