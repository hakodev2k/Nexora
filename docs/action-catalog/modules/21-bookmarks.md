# FX-21 — Bookmarks: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/21-bookmarks.md) — FX-21-BR-001, FX-21-BR-002, FX-21-BR-003, FX-21-BR-004, FX-21-BR-005. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/21-bookmarks.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `bookmarks` là stable logical key, bind installed ModuleId trong manifest. **Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="bookmarks-bookmark-read"></a>`bookmarks.bookmark.read` — Xem Bookmark | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S01, FX21-S03 |
| <a id="bookmarks-bookmark-create"></a>`bookmarks.bookmark.create` — Tạo Bookmark | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S02 |
| <a id="bookmarks-bookmark-update"></a>`bookmarks.bookmark.update` — Sửa Bookmark | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S02 |
| <a id="bookmarks-bookmark-refresh"></a>`bookmarks.bookmark.refresh` — Lấy lại URL metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 for outbound execution | FX21-S03 |
| <a id="bookmarks-bookmark-archive"></a>`bookmarks.bookmark.archive` — Archive bookmark | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-unarchive"></a>`bookmarks.bookmark.unarchive` — Unarchive bookmark | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-trash"></a>`bookmarks.bookmark.trash` — Đưa bookmark vào Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-restore"></a>`bookmarks.bookmark.restore` — Khôi phục bookmark từ Thùng rác | COMMAND / SELF | Yes, gated | Lifecycle (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-purge"></a>`bookmarks.bookmark.purge` — Xóa vĩnh viễn bookmark | COMMAND / SELF | Yes, gated | Destructive (Administrative) | Resolved delegated: action contract; source business rules unchanged | FX21-S01 |
| <a id="bookmarks-bookmark-share"></a>`bookmarks.bookmark.share` — Quản lý link chỉ-đọc của bookmark | COMPOSITE / SELF | Yes, gated | Disclosure (Sensitive) | Resolved delegated: action contract; source business rules unchanged | FX21-S03 |
| <a id="bookmarks-bookmark-open-external"></a>`bookmarks.bookmark.open_external` — Mở URL nguồn | LOCAL / SELF | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX21-S03 |
| <a id="bookmarks-support-read"></a>`bookmarks.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `bookmarks.bookmark.read` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.create` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.update` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | `bookmarks.bookmark.read` |
| `bookmarks.bookmark.refresh` | Approved fetch provider + SSRF limits; failure giữ bản trước và freshness; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.archive` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; ngoài Trash, chưa Archived; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.unarchive` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; Archived, khôi phục previous state; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.trash` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; preview aggregate, không purge; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.restore` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; đúng deletion cohort, parent hợp lệ; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.purge` | Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit; chỉ Trash, preview pins/dependencies, xác nhận không thể hoàn tác; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.bookmark.share` | Source eligible, SharingEnabled, projection chính xác; kết hợp sharing.link.*; không history/reason/secret; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | `sharing.link.read` |
| `bookmarks.bookmark.open_external` | Safe URL scheme; disclosure external navigation; không credential forwarding; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |
| `bookmarks.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own bookmark; saved metadata khác fetched metadata, không overwrite owner edit | Common + dynamic source/provider guards |

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
