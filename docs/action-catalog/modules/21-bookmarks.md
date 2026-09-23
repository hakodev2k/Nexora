# FX-21 — Bookmarks: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/21-bookmarks.md), [UX](../../ux-ui/modules/21-bookmarks.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Manual metadata subset is `SLICE_IMPLEMENTED` on PR #4; advanced actions remain contract-gated.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="bookmarks-bookmark-read"></a>`bookmarks.bookmark.read` — Xem Bookmark | QUERY / SELF | Yes when active | SLICE_IMPLEMENTED (local) | Owner-scoped manual metadata list/search; URL remains inert | FX21-S01, FX21-S03 |
| <a id="bookmarks-bookmark-create"></a>`bookmarks.bookmark.create` — Tạo Bookmark | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | Owner-scoped HTTP(S) URL/title/description; no outbound fetch | FX21-S02 |
| <a id="bookmarks-bookmark-update"></a>`bookmarks.bookmark.update` — Sửa Bookmark | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | ETag/If-Match and idempotent owner update | FX21-S02 |
| <a id="bookmarks-bookmark-refresh"></a>`bookmarks.bookmark.refresh` — Lấy lại URL metadata | COMMAND / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX21-S03 |
| <a id="bookmarks-bookmark-archive"></a>`bookmarks.bookmark.archive` — Archive bookmark | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | Active → Archived with ETag/If-Match and audit | FX21-S01 |
| <a id="bookmarks-bookmark-unarchive"></a>`bookmarks.bookmark.unarchive` — Unarchive bookmark | COMMAND / SELF | Yes when active | SLICE_IMPLEMENTED (local) | Archived → Active with ETag/If-Match and audit | FX21-S01 |
| <a id="bookmarks-bookmark-trash"></a>`bookmarks.bookmark.trash` — Đưa bookmark vào Thùng rác | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-restore"></a>`bookmarks.bookmark.restore` — Khôi phục bookmark từ Thùng rác | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-purge"></a>`bookmarks.bookmark.purge` — Xóa vĩnh viễn bookmark | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-share"></a>`bookmarks.bookmark.share` — Quản lý link chỉ-đọc của bookmark | COMPOSITE / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX21-S03 |
| <a id="bookmarks-bookmark-open-external"></a>`bookmarks.bookmark.open_external` — Mở URL nguồn | LOCAL / SELF | No | Superseded | DEC-20260907-INTERNAL: external navigation excluded | FX21-S03 |
| <a id="bookmarks-support-read"></a>`bookmarks.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Resolved delegated | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `bookmarks.bookmark.read` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.create` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.update` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | `bookmarks.bookmark.read` |
| `bookmarks.bookmark.refresh` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `bookmarks.bookmark.archive` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; ngoài Trash, chưa Archived; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.unarchive` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Archived, khôi phục previous state; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.trash` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; preview aggregate, không purge; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.restore` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; đúng deletion cohort, parent hợp lệ; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.purge` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | `sharing.link.read` |
| `bookmarks.bookmark.open_external` | Keep URL as inert metadata; no external navigation/fetch/iframe | Common + dynamic source/provider guards |
| `bookmarks.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
