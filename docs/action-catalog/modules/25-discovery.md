# FX-25 — Search / Favorites / Command Palette: actions

Catalog v1 · 2026-09-07 · Source business decisions giữ nguyên; the current
PR #4 overlay implements only the bounded FX25-S01 Search and FX25-S03
Favorites rows locally. Remaining rows stay delegated/gated; Blocked rows
không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/25-search-favorites-and-command-palette.md) — FX-25-BR-001, FX-25-BR-002, FX-25-BR-003, FX-25-BR-004, FX-25-BR-005, FX-25-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/25-search-favorites-command-palette.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `discovery` là stable logical key, bind installed ModuleId trong manifest. **Current permission trên từng source result; index không authority**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="discovery-search-query"></a>`discovery.search.query` — Search across enabled providers | QUERY / SELF | Yes, gated | Normal (Normal) | **SLICE_IMPLEMENTED (local)** — bounded owner-scoped source query; runtime not run | FX25-S01 |
| <a id="discovery-command-read"></a>`discovery.command.read` — Liệt kê commands khả dụng | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S02 |
| <a id="discovery-command-execute"></a>`discovery.command.execute` — Thực thi command đã chọn | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S02 |
| <a id="discovery-favorite-read"></a>`discovery.favorite.read` — Xem favorites | QUERY / SELF | Yes, gated | Normal (Normal) | **SLICE_IMPLEMENTED (local)** — owner-scoped typed references; source access/lifecycle/trash rechecked; runtime not run | FX25-S03 |
| <a id="discovery.favorite-add"></a>`discovery.favorite.add` — Favorite source | COMMAND / SELF | Yes, gated | Normal (Normal) | **SLICE_IMPLEMENTED (local)** — SQL reference write with source recheck; runtime not run | FX25-S03 |
| <a id="discovery.favorite-remove"></a>`discovery.favorite.remove` — Unfavorite source | COMMAND / SELF | Yes, gated | Normal (Normal) | **SLICE_IMPLEMENTED (local)** — owner/ETag/idempotent delete; runtime not run | FX25-S03 |
| <a id="discovery.favorite-reorder"></a>`discovery.favorite.reorder` — Sắp xếp favorites | COMMAND / SELF | Yes, gated | Normal (Normal) | **SLICE_IMPLEMENTED (local)** — bounded rank with owner/ETag guard; runtime not run | FX25-S03 |
| <a id="discovery-recent-read"></a>`discovery.recent.read` — Xem recents | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S03 |
| <a id="discovery-recent-clear"></a>`discovery.recent.clear` — Xóa recents của mình | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S03 |
| <a id="discovery-saved-search-read"></a>`discovery.saved_search.read` — Xem Saved Search | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S04 |
| <a id="discovery-saved-search-create"></a>`discovery.saved_search.create` — Tạo Saved Search | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S04 |
| <a id="discovery-saved-search-update"></a>`discovery.saved_search.update` — Sửa Saved Search | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S04 |
| <a id="discovery-saved-search-delete"></a>`discovery.saved_search.delete` — Xóa Saved Search | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S04 |
| <a id="discovery-saved-search-run"></a>`discovery.saved_search.run` — Chạy lại Saved Search | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX25-S04 |
| <a id="discovery-index-refresh"></a>`discovery.index.refresh` — Cập nhật search index | WORKER / SYSTEM | No | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | Trusted worker/deployment; no user control |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `discovery.search.query` | Không secrets/history/private projection in preview; unknown/provider failed không empty; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.command.read` | Current permission trên từng source result; index không authority; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.command.execute` | Gọi canonical target action; command visibility không authorization; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.favorite.read` | Current permission trên từng source result; index không authority; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.favorite.add` | Own refs; source read; không bypass Archived/disabled; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.favorite.remove` | Own refs; source read; không bypass Archived/disabled; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.favorite.reorder` | Own refs; source read; không bypass Archived/disabled; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.recent.read` | Current permission trên từng source result; index không authority; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.recent.clear` | Current permission trên từng source result; index không authority; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.saved_search.read` | Owner query definition only; query params không embed secrets; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.saved_search.create` | Owner query definition only; query params không embed secrets; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.saved_search.update` | Owner query definition only; query params không embed secrets; Current permission trên từng source result; index không authority | `discovery.saved_search.read` |
| `discovery.saved_search.delete` | Current permission trên từng source result; index không authority; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |
| `discovery.saved_search.run` | Cùng search.query và current providers; Current permission trên từng source result; index không authority | `discovery.search.query` |
| `discovery.index.refresh` | Trusted provider/owner version; tombstone permission/lifecycle changes; không đọc index trực tiếp như API owner; Current permission trên từng source result; index không authority | Common + dynamic source/provider guards |

## Deny và UX contract

- Module off, grant missing/deny, resource wrong owner, disallowed lifecycle, current Q gate hoặc source dependency fail: không side effect; không dùng hidden button thay authorization.
- Before/after field diff được kiểm tra cho Save, import, version restore, bulk, scheduler và automation. Form không được gửi status/reveal/export/owner trong generic Update.
- Safe capability reason: ModuleUnavailable, ActionDenied, LifecycleLocked, DependencyUnavailable, DecisionBlocked hoặc StepUpRequired; unknown/wrong-owner resource trả unavailable chung để không enumerate.
- Implemented FX25-S03 endpoints map a missing/denied Favorites operation to `403 PermissionDenied`; unavailable FX25 or any hard dependency in its transitive chain maps to `409 ModuleUnavailable`. Source-read denial remains an unavailable projection.
- Grant không thay đổi state graph. Chỉ quyền đã cấp và hợp lệ mới xuất hiện enabled; permission editor có thể hiển thị blocked row để giải thích, không cho bật.
- Revocation và support/share/system contexts áp toàn bộ [common contract](../00-authorization-contract.md). Readonly projections không reuse full owner DTO.

## Acceptance tối thiểu

1. Với mỗi row: positive case đúng context/current state; wrong-owner và wrong-context negative; absent/deny Admin grant; module off; stale revision; lifecycle/Q gate.
2. COMMAND/COMPOSITE: request replay/idempotency, before-commit recheck; affected fields cần đủ action. QUERY: owner-scoped filtering trước count/pagination/projection, cache không rò source revoked.
3. LOCAL: keyboard/menu và tool entry cùng capability gate; không network/persist ngầm. SYSTEM: trusted caller, original authority và no UI grant.
4. Row nhạy cảm: no secret in response preview, toast, logs, URL, search, browser persistent storage; current recent-auth gate nếu required.
5. Nếu handler/source projection chưa có approved contract, action phải báo Blocked/Unavailable, không tự thực thi fallback rộng hơn.
