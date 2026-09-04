# Phase 5 — News/Feeds and Shopping/Price Tracking

**Phase ID:** `NX-PH-05`  
**Version:** `1.2-draft`  
**Outcome:** User theo dõi nguồn tin và sản phẩm/giá cá nhân, lưu nội dung cần đọc/mua và nhận cảnh báo đáng tin cậy khi điều kiện đã cấu hình xảy ra.  
**Depends on:** Module Platform/personal access, notifications, scheduler/jobs, safe external HTTP policy, unified Read Later, search and files.

## 1. External-provider gate

- `DEC-PRD-009/010/011`, `DEC-TEC-008/009`, `DEC-SEC-007` phải đóng cho P0.
- Shopee acquisition method phải được kiểm tra về tính khả thi, Terms/policy và độ ổn định trước khi cam kết background tracking.
- Provider outage, rate limit, schema change hoặc blocked access phải tạo degraded state; không được bịa price/article data.
- News/Shopping Release 1 là Personal-only; read/unread, follow, tracker, alert và purchase data thuộc current User.
- Background fetch có thể dùng cache public chung nhưng subscription/tracker/rule/result projection luôn re-check owner/module access.

## 2. Scope proposal

### News/Feeds P0

RSS/Atom feed sources, fetch/parse/deduplicate, AI News và Tech News categories, article list/detail/external link, Saved, unified Read Later, read/unread/history controls, manual refresh và scheduled refresh cơ bản.

### Shopping P0

Shopee product URL tracking, selected variant/price definition, current/previous/lowest price, snapshots/history, absolute target alert, manual refresh, scheduled check, in-app notification, wishlist relation.

### P1

Topic Watch không dùng LLM, additional price-drop rules, product comparison, purchase/order/warranty records, seller/shop tracking, email/browser alerts, multiple marketplace adapters.

### Out of scope

AI-generated summary/classification, paywall bypass/full copyrighted article mirroring, automated checkout/purchase, Shopee account login/order sync, price guarantee, seller messaging, crawling vượt policy/provider controls.

## 3. Feed Sources

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-FED-001` | P0 | User tạo feed source bằng supported HTTP(S) URL; URL normalized và SSRF validated. | Loopback/private/link-local/unsafe scheme/credential URL blocked per policy. |
| `P05-FED-002` | P0 | Source có name, URL, category, active state, refresh policy, last success/attempt/error metadata. | UI phân biệt never fetched, healthy, stale, failed, disabled. |
| `P05-FED-003` | P0 | Connection test/fetch có timeout, redirect, size, content-type và parse bounds. | Redirect loop/zip bomb/huge/malformed feed fails safely. |
| `P05-FED-004` | P0 | Same normalized source của cùng User xử lý duplicate theo policy; User khác không chia sẻ subscription ownership. | Add retry không duplicate; cached public fetch có owner-safe projections. |
| `P05-FED-005` | P0 | Disable/delete source dừng future fetch; historical saved/read items theo retention decision. | Queued job rechecks active state; no data loss beyond explicit choice. |
| `P05-FED-006` | P1 | Admin-managed shared catalog nếu có phải tách subscription của từng user khỏi source definition. | User preference/read state remains private. |

## 4. Feed ingestion và Articles

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-ART-001` | P0 | Parser hỗ trợ RSS/Atom subset được document; lưu canonical URL/source ID/title/published/updated/author/summary-content policy. | Fixture suite với namespaces/date formats/missing optional fields pass. |
| `P05-ART-002` | P0 | External content luôn sanitize/encode trước render; script/style/unsafe URL không chạy. | Malicious feed corpus safe ở list/detail/share/search. |
| `P05-ART-003` | P0 | Dedup dùng stable provider ID/canonical URL/fallback fingerprint theo thứ tự rõ; update không tạo article mới nếu cùng item. | Repeat fetch idempotent; collision handling observable. |
| `P05-ART-004` | P0 | Không tuyên bố lưu full article nếu feed chỉ cấp excerpt; luôn giữ source attribution/link. | UI labels excerpt/source; no fabricated content. |
| `P05-ART-005` | P0 | Published/updated times preserve source value + fetch time; invalid date có fallback explicit. | Sort stable; timezone/date parsing tests pass. |
| `P05-ART-006` | P0 | Image/enclosure remote content không được proxy/download ngầm nếu chưa có security/storage policy. | Unsafe URL not fetched server-side; broken media degrades gracefully. |
| `P05-ART-007` | P0 | User mark read/unread, save/unsave và add/remove unified Read Later. | State private per user; retry idempotent. |
| `P05-ART-008` | P0 | Article search/filter theo source/category/read/saved/date và safe text fields. | Access/read-state filters correct; malicious highlight safe. |
| `P05-ART-009` | P1 | Reading history có retention/clear controls; opening external link behavior được ghi nhận rõ, không theo dõi ngoài site. | Clear affects only user history; audit not incorrectly deleted. |

## 5. AI News và Tech News

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-NEW-001` | P0 | `AI News` và `Tech News` là category/source grouping, không gọi AI/LLM. | Network/dependency audit shows no LLM; UI wording không imply AI summary. |
| `P05-NEW-002` | P0 | Category assignment là manual/source-rule deterministic theo decision. | Same input/rule gives deterministic category; user can see source/category. |
| `P05-NEW-003` | P1 | Topic Watch chỉ dùng deterministic keyword/filter baseline; case/language matching documented. | No semantic/LLM claim; false-positive limitation visible. |
| `P05-NEW-004` | P1 | Topic alert có dedupe/cooldown và links tới source article. | Re-fetch/update không spam duplicate alert. |

## 6. Feed refresh jobs

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-RFS-001` | P0 | Manual refresh trả acknowledgement/run state, không block request đến hết long fetch. | User thấy queued/running/success/partial/failure và last updated. |
| `P05-RFS-002` | P0 | Scheduled refresh có per-source/global concurrency, timeout, retry/backoff và rate limit. | Provider 429/outage không retry storm; app remains responsive. |
| `P05-RFS-003` | P0 | Concurrent manual/scheduled refresh coalesce hoặc serialize cùng source. | No duplicate items/races; one logical outcome. |
| `P05-RFS-004` | P0 | HTTP validators (`ETag`, `Last-Modified`) được dùng nếu architecture hỗ trợ và không làm sai freshness. | 304 handled as success/no change; last attempt/success semantics clear. |
| `P05-RFS-005` | P0 | Parse/fetch error summary safe; raw response không log toàn bộ nếu chứa data nhạy cảm. | Logs redacted/bounded; diagnostic correlation available. |

## 7. Shopee tracked product model

Một tracked item phải chỉ rõ **listing + selected variant/price definition**. “Current Price” không đủ rõ nếu listing có price range, voucher, shipping hoặc member-only price.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-PRD-001` | P0 | User thêm Shopee URL; adapter parse/normalize listing identity và reject unsupported/invalid URL. | Equivalent URL không duplicate cho cùng User ngoài explicit choice. |
| `P05-PRD-002` | P0 | Tracked product có owner, creator, provider/listing ID, URL, name, shop, image URL policy, selected variant, currency, current/previous/lowest, last checked/status. | Unknown fields are null/labeled, never fabricated; owner/creator set server-side. |
| `P05-PRD-003` | P0 | Price definition ghi rõ included/excluded: base/sale/voucher/shipping/tax/member price. | UI/history/alert use same definition; tooltip/source timestamp visible. |
| `P05-PRD-004` | P0 | Variant selection change starts new comparable series hoặc marks discontinuity. | Lowest/percentage không compare apples-to-oranges silently. |
| `P05-PRD-005` | P0 | Listing unavailable/private/deleted/anti-bot/parse-changed có distinct status. | Failure không overwrite last valid price với 0; user sees stale/error. |
| `P05-PRD-006` | P0 | User pause/resume/delete/restore tracking; queued checks re-evaluate state. | Paused/deleted item not fetched/alerted; restore schedule per policy. |
| `P05-PRD-007` | P1 | Seller/shop metadata tracked only from public permitted source and never treated as identity verification. | UI labels source; missing/changed shop handled. |

## 8. Price snapshots và history

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-PHS-001` | P0 | Successful comparable check stores normalized price, currency, availability, checked-at, provider timestamp/source metadata và adapter version. | Same run retry idempotent; amount precision correct. |
| `P05-PHS-002` | P0 | Current = latest valid comparable snapshot; Previous = previous distinct/check per decision; Lowest excludes invalid/unavailable/other variant. | Golden history suite computes exact values. |
| `P05-PHS-003` | P0 | Failed check creates job/status evidence, không tạo fake zero snapshot hoặc change alert. | Outage leaves last valid data labeled stale. |
| `P05-PHS-004` | P0 | History chart/table labels currency, price definition, timestamp/timezone, missing periods và discontinuity. | Accessible table; no interpolated factual price unless explicitly labeled. |
| `P05-PHS-005` | P1 | Snapshot retention/downsampling preserves alert evidence và lowest semantics. | Retention job idempotent; aggregate traceable. |

## 9. Price alert rules

### P0 rule

`Current comparable price <= target amount` cho same currency/variant.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P05-ALT-001` | P0 | Alert rule có owner/product, creator, type, threshold/currency, active state, cooldown/re-arm policy và channel preference. | Invalid threshold/currency blocked; cross-user product blocked. |
| `P05-ALT-002` | P0 | Evaluate chỉ trên valid new snapshot sau commit; notification intent idempotent. | Job retry sends one logical alert; no alert on parse/fetch failure. |
| `P05-ALT-003` | P0 | Repeated price dưới target không spam; cooldown/one-shot/re-arm semantics phải được Product Owner chọn. | Golden sequence test exact alert count. |
| `P05-ALT-004` | P0 | Notification nêu product/variant, observed price, target, checked-at và link; không khẳng định checkout price guaranteed. | Message source data accurate and labeled. |
| `P05-ALT-005` | P0 | Pause/delete product/rule hoặc user disable suppresses future alert. | Queued delivery rechecks state. |
| `P05-ALT-006` | P1 | Percentage drop, new-low, back-in-stock hoặc seller rule chỉ ship sau baseline/cooldown definition. | No divide-by-zero/variant/currency mismatch; sequence tests pass. |

## 10. Shopping records P1

| ID | Pri | Requirement |
|---|---:|---|
| `P05-WIS-001` | P0/P1 | Wishlist là owned collection/reference; adding tracked product is idempotent and does not duplicate tracker. |
| `P05-CMP-001` | P1 | Comparison only compares explicitly selected comparable attributes/variants/currency; missing data labeled. |
| `P05-ORD-001` | P1 | Order/Purchase is manual record with seller, item, quantity, amount/currency, dates/status and optional invoice; no Shopee account sync baseline. |
| `P05-WAR-001` | P1 | Warranty belongs Purchase/Asset, has start/end/provider/terms/files; expiration emits notification via shared engine. |
| `P05-SEL-001` | P1 | Seller tracking stores public shop reference/status only; no automated messaging or trust guarantee. |

## 11. Permissions, privacy và audit

- Feed subscription/tracking resource thuộc User; read state, follow, notification preference và delivery là private data.
- Public provider data cache may be shared internally only if per-user state/access remains isolated.
- Admin namespace `news.*`, `shopping.*`; action `view` không tự cho xem subscription/tracking của User khác nếu thiếu active support/emergency context.
- Audit: source/tracker/rule configuration by admin scope, manual refresh/automation execution as appropriate, export/purge, provider/security rejection; logs must not store credential-bearing URLs.
- Sharing saved article/product is P1 and never grants access to private alert/purchase/history metadata unless explicit.

## 12. Edge/failure cases

- Malformed/malicious feed, duplicate GUID reused, future/invalid dates, HTML tracking content.
- Provider timeout/429/403/captcha/schema change/partial response/redirect.
- Shopee URL changes, listing ID reused, variant removed, currency changes, price range/voucher expiry.
- Two refreshes concurrently, scheduler downtime, stale queued job after pause/delete.
- Price oscillates around threshold; repeated identical snapshot; notification channel failure.
- Source/product belongs to deleted/disabled user; notification permission revoked.
- User/module/permission bị disable/revoke sau khi job queued nhưng trước fetch/evaluate/deliver.

## 13. Verification scenarios

1. Malicious RSS item is safe in list/detail/search and retains source attribution.
2. Repeated/parallel feed refresh creates one article/user state and honors 304/429.
3. AI News works with deterministic category/source rules and no LLM dependency.
4. Shopee adapter receives listing with variants/range/voucher; UI/alert uses selected price definition only.
5. Failed check never writes zero or false price-drop; last valid price labeled stale.
6. Golden price sequence validates current/previous/lowest/cooldown alert counts.
7. User A và Admin-no-support không thấy feeds, read state, trackers, alerts hoặc purchase records của User B.
8. Disabled User/module hoặc revoked support không nhận alert, mở tracker hay suy ra aggregate qua stale job/cache.

## 14. Exit criteria

- External acquisition/legal/security decisions closed and adapter contract versioned.
- Feed fixture/security/dedup/idempotency suites pass.
- Shopee price-definition/variant/history/alert golden suites pass.
- Provider outage/rate-limit/schema-change degraded behavior is observable and non-destructive.
- Notifications are accurate, deduplicated and not overclaimed.
- Manifest personal ownership, cross-user isolation và User/module-disable behavior được test.
- Responsive/accessibility core journeys pass; no Critical/High finding remains.
