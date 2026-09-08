# FX-29 — News / Feeds: action catalog v1.1

Source [PO decisions](../../requirements/10-owner-decisions-20260907.md), [feature](../../features/29-news-and-feeds.md), [UX](../../ux-ui/modules/29-news-feeds.md), [global authorization](../00-authorization-contract.md), [changes](../08-owner-decision-changes.md). Docs-only; no implementation approved.

New PO rules override former Q proposals. Paused/Blocked/Superseded rows cannot be enabled via grant/defaults. AdminGrantable describes eligibility of action class, not authorization while inactive. All operations additionally check current account.IsDeleted, owner scope, source/lifecycle/read-projection, dependencies, policy revision and semantic field diff; no mutation response can leak denied read data.

| Action | Kind / context | Admin-grantable | Current scope | Gate | UI entry |
| --- | --- | --- | --- | --- | --- |
| <a id="news-source-read"></a>`news.source.read` — Xem sources | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-follow"></a>`news.source.follow` — Theo dõi source | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S02 |
| <a id="news-source-update"></a>`news.source.update` — Sửa subscription metadata | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S02 |
| <a id="news-source-pause"></a>`news.source.pause` — Tạm dừng | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-resume"></a>`news.source.resume` — Tiếp tục | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-unfollow"></a>`news.source.unfollow` — Bỏ theo dõi | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-source-refresh"></a>`news.source.refresh` — Yêu cầu refresh source | COMMAND / SELF | Yes when active | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | FX29-S01 |
| <a id="news-article-read"></a>`news.article.read` — Xem article/reader | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S03, FX29-S04 |
| <a id="news-article-mark-read"></a>`news.article.mark_read` — Đánh dấu đã đọc | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-article-mark-unread"></a>`news.article.mark_unread` — Đánh dấu chưa đọc | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-article-mark-all-read"></a>`news.article.mark_all_read` — Đánh dấu tất cả trong phạm vi đã xem | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-article-save"></a>`news.article.save` — Lưu vào Read Later | COMPOSITE / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S04 |
| <a id="news-history-read"></a>`news.history.read` — Xem reading history | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-history-clear"></a>`news.history.clear` — Xóa reading history | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S03 |
| <a id="news-category-read"></a>`news.category.read` — Xem Feed category | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-category-create"></a>`news.category.create` — Tạo Feed category | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-category-update"></a>`news.category.update` — Sửa Feed category | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-category-remove"></a>`news.category.remove` — Xóa category container | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S01 |
| <a id="news-watch-read"></a>`news.watch.read` — Xem Topic watch | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-create"></a>`news.watch.create` — Tạo Topic watch | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-update"></a>`news.watch.update` — Sửa Topic watch | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-enable"></a>`news.watch.enable` — Bật watch | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-disable"></a>`news.watch.disable` — Tắt watch | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-watch-delete"></a>`news.watch.delete` — Xóa watch | COMMAND / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-match-read"></a>`news.match.read` — Xem watch matches | QUERY / SELF | Yes when active | Resolved delegated | Resolved delegated: action contract; source business rules unchanged | FX29-S05 |
| <a id="news-worker-fetch"></a>`news.worker.fetch` — Fetch/update article snapshots | WORKER / SYSTEM | No | Blocked | DEP-EXT-01: outbound ingestion boundary needs clarification | Trusted worker/deployment only |
| <a id="news-worker-match"></a>`news.worker.match` — Evaluate topic watches | WORKER / SYSTEM | No | Resolved delegated | Local evaluation only; outbound fetch unavailable | Trusted worker/deployment only |
| <a id="news-support-read"></a>`news.support.read` — Xem safe support projection của module | QUERY / SUPPORT | Yes when active | Resolved delegated | Resolved delegated: diagnostic projection only; private body excluded unless source scope explicitly permits | FX05-S03, FX05-S05 |

| Action | Exact guard / effect | Additional prerequisites |
| --- | --- | --- |
| `news.source.read` | Own subscriptions/state; fetched articles bất biến từ provider, không editor; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.follow` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.update` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.pause` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.resume` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.unfollow` | Own subscription; unfollow không xóa saved references ngoài scope; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |
| `news.source.refresh` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
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
| `news.worker.fetch` | No automatic upstream call/fetch/refresh. Existing safe owned snapshots may be read through separate read actions; report unavailable/stale, not fake live results | Common + dynamic source/provider guards |
| `news.worker.match` | Current owned existing articles/watch definitions only; no remote fetch implicit in match | Common + dynamic source/provider guards |
| `news.support.read` | Qualified Admin + support.session.open + current one-module consent OR authorized SuperAdmin Emergency; target enabled; approved redacted projection only; no owner history/export/reveal/linked-module expansion; Own subscriptions/state; fetched articles bất biến từ provider, không editor | Common + dynamic source/provider guards |

## Acceptance

Check each active row: correct context/owner, Admin Allow/Deny/absent, deleted account, module off, stale version, protected-field diff, source dependencies and response projection. Paused/Blocked/Superseded denies even with Allow; no active UI/worker. Recovery needs SuperAdmin request-bound authorization and no operator plaintext; revoked link cannot revive after restore; internal flows must not auto-follow provider URLs. UI and keyboard call same source actions. Source BR/AC remain authoritative where not superseded by PO decisions.
