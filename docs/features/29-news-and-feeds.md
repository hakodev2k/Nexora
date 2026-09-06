# News, RSS và Topic Watch

FX-29 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

RSS/Atom sources/categories/articles/read history/read later/topic alerts.

[Feedly](https://docs.feedly.com/article/288-how-to-follow-a-feed-in-your-feedly-account): Theo dõi nguồn feed để đọc bài trong một nơi.

**Áp dụng cho Nexora:** Feedly tham chiếu following feeds; không full-body scraping/paywall bypass.

**Màn hình:** `/news, /news/sources, /news/topics`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Add URL → validate/fetch preview → follow/category.
2. Unread/All/Saved, publishedAt desc fallback fetchedAt; search title/source/topic.
3. Read sanitized content hoặc external original; mark read/unread/save later.
4. Topic keywords/source filters → preview → enable future-match alerts.

## Dữ liệu và validation

- Feed URL/title override/status/etag/lastModified/lastSuccess/error.
- Article GUID+feed identity, URL/title/dates/body sanitized; UserReadState riêng.
- TopicWatch include/exclude literal phrases/source IDs/queryVersion.

## Hành vi và lifecycle

- **FX-29-BR-001:** Egress guarded, XML external entities off; bounded bytes/items; failure isolated source.
- **FX-29-BR-002:** Dedup GUID scoped feed, fallback URL+fingerprint; updates giữ read state.
- **FX-29-BR-003:** MarkAllRead watermark; article đến sau vẫn unread.
- **FX-29-BR-004:** Topic alerts chỉ new article sau enable; dedupe watch+article, all 3; query edit không bắn toàn history.
- **FX-29-BR-005:** Unfollow không tự xóa User saved-later refs; no arbitrary copyrighted mirror.
- **FX-29-BR-006:** Poll30min+jitter/backoff delegated; source stale labeled, không promise realtime.

## Quyền, API và tích hợp

- FeedFetch/ArticleUpsert/ReadState/TopicMatch; public feed cache tách User preferences.
- ReadLater shared provider; Notification safe excerpt không full external body.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-29-AC-001:** XXE/private-IP redirect blocked.
- **FX-29-AC-002:** GUID update không duplicate article/alert.
- **FX-29-AC-003:** User unfollow không xóa saved state của User khác.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Source và reading details bổ sung

- Cung cấp categories ban đầu AI News và Tech News theo phase source; owner thêm/đổi category. Category không là classification bằng AI.
- Same normalized feed URL trong cùng User trả existing subscription khi retry; manual add duplicate mở source đã theo dõi. User khác có subscription/read state độc lập.
- Manual refresh coalesced và có lastAttempt/lastSuccess; nguồn chưa fetch, healthy, stale, failed, disabled phải phân biệt.
- Reading History lưu lần mở/read-state của owner, clear history không xóa security audit hoặc tự unfollow. Saved và ReadLater dùng shared relation, không lưu hai bản article gây lệch trạng thái.
- Admin-managed shared catalog là conditional extension, không tự cấp Admin quyền read history của User.

## Traceability và phần còn mở

- [phase-05-news-and-shopping.md](../requirements/phases/phase-05-news-and-shopping.md): `P05-ART-001`, `P05-ART-002`, `P05-ART-003`, `P05-ART-004`, `P05-ART-005`, `P05-ART-006`, `P05-ART-007`, `P05-ART-008`, `P05-ART-009`, `P05-FED-001`, `P05-FED-002`, `P05-FED-003`, `P05-FED-004`, `P05-FED-005`, `P05-FED-006`, `P05-NEW-001`, `P05-NEW-002`, `P05-NEW-003`, `P05-NEW-004`, `P05-RFS-001`, `P05-RFS-002`, `P05-RFS-003`, `P05-RFS-004`, `P05-RFS-005`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.

