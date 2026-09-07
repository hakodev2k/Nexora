# FX-29 — News / Feeds: actions

Catalog v1 · 2026-09-07 · Docs-only. New key decomposition = Resolved delegated; source business decisions giữ nguyên; Blocked rows không được kích hoạt bằng grant.

## Sources và phạm vi

- [Feature / validation / state graph](../../features/29-news-and-feeds.md) — FX-29-BR-001, FX-29-BR-002, FX-29-BR-003, FX-29-BR-004, FX-29-BR-005, FX-29-BR-006. Các BR này áp cho feature; không gán sai một BR duy nhất cho mọi row.
- [UX specification](../../ux-ui/modules/29-news-feeds.md); [exact screen bindings](../06-screen-bindings.md).
- [Authorization contract](../00-authorization-contract.md), [semantic field guards](../01-composition-and-field-guards.md), [SDK contract](../04-module-action-contract.md).
- Namespace `news` là stable logical key, bind installed ModuleId trong manifest. **Own subscriptions/state; fetched articles bất biến từ provider, không editor**.

## Catalog

“All prerequisites” ở row bao gồm explicit Requires **và** source/dynamic dependencies trong guard; danh sách Requires trống không có nghĩa bỏ owner/module/lifecycle checks. Every action denies unknown fields and unapproved semantic changes. Source state matrix luôn kiểm tra ở handler, không suy quyền từ verb hoặc tên button.

| Action key / hành vi | Kind / context | Admin checkbox? | Risk (DB mapping) | Status / gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="news-source-read"></a>`news.source.read` — Xem sources | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-follow"></a>`news.source.follow` — Theo dõi source | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S02 |
| <a id="news-source-update"></a>`news.source.update` — Sửa subscription metadata | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S02 |
| <a id="news-source-pause"></a>`news.source.pause` — Tạm dừng | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-resume"></a>`news.source.resume` — Tiếp tục | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-unfollow"></a>`news.source.unfollow` — Bỏ theo dõi | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-refresh"></a>`news.source.refresh` — Yêu cầu refresh source | COMMAND / SELF | Yes, gated | Normal (Normal) | Blocked Q-07 for outbound execution | FX29-S01 |
| <a id="news-article-read"></a>`news.article.read` — Xem article/reader | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S03, FX29-S04 |
| <a id="news-article-mark-read"></a>`news.article.mark_read` — Đánh dấu đã đọc | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-article-mark-unread"></a>`news.article.mark_unread` — Đánh dấu chưa đọc | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-article-mark-all-read"></a>`news.article.mark_all_read` — Đánh dấu tất cả trong phạm vi đã xem | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-article-save"></a>`news.article.save` — Lưu vào Read Later | COMPOSITE / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S04 |
| <a id="news-history-read"></a>`news.history.read` — Xem reading history | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-history-clear"></a>`news.history.clear` — Xóa reading history | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-category-read"></a>`news.category.read` — Xem Feed category | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-category-create"></a>`news.category.create` — Tạo Feed category | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-category-update"></a>`news.category.update` — Sửa Feed category | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-category-remove"></a>`news.category.remove` — Xóa category container | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-watch-read"></a>`news.watch.read` — Xem Topic watch | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-create"></a>`news.watch.create` — Tạo Topic watch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-update"></a>`news.watch.update` — Sửa Topic watch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-enable"></a>`news.watch.enable` — Bật watch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-disable"></a>`news.watch.disable` — Tắt watch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-delete"></a>`news.watch.delete` — Xóa watch | COMMAND / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-match-read"></a>`news.match.read` — Xem watch matches | QUERY / SELF | Yes, gated | Normal (Normal) | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-worker-fetch"></a>`news.worker.fetch` — Fetch/update article snapshots | WORKER / SYSTEM | No | Normal (Normal) | Blocked Q-07 for network fetch; local match follows approved definition | Trusted worker/deployment; no user control |
| <a id="news-worker-match"></a>`news.worker.match` — Evaluate topic watches | WORKER / SYSTEM | No | Normal (Normal) | Blocked Q-07 for network fetch; local match follows approved definition | Trusted worker/deployment; no user control |
| <a id="news-support-read"></a>`news.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes, gated | Sensitive (Sensitive) | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

## Điều kiện riêng theo operation

| Action | Guard / validation / effects | Explicit dependencies bổ sung |
| --- | --- | --- |
| `news.source.read` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.follow` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.update` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.pause` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.resume` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.unfollow` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.refresh` | Approved fetch adapter + SSRF/network policy; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.article.read` | Current source visibility; failed parser giữ external-open fallback; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.article.mark_read` | Own read state; mark-all snapshot/watermark, không cả nguồn mới đến; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.article.mark_unread` | Own read state; mark-all snapshot/watermark, không cả nguồn mới đến; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.article.mark_all_read` | Own read state; mark-all snapshot/watermark, không cả nguồn mới đến; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.article.save` | No duplicated body, reading.item.save + source read; Own subscriptions/state; fetched articles bất biến từ provider, không editor | `reading.item.save` |
| `news.history.read` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.history.clear` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.category.read` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.category.create` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.category.update` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | `news.category.read` |
| `news.category.remove` | Giữ subscriptions, reassign/unfiled preview; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.watch.read` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.watch.create` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.watch.update` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | `news.watch.read` |
| `news.watch.enable` | Own query definition; không AI/paid crawling; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.watch.disable` | Own query definition; không AI/paid crawling; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.watch.delete` | Own query definition; không AI/paid crawling; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.match.read` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.worker.fetch` | Trusted scoped provider; no private URL/secret fetch; current enabled/consent policy; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.worker.match` | Trusted scoped provider; no private URL/secret fetch; current enabled/consent policy; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |

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
