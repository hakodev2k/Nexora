# GitHub Discovery

FX-33 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Public repos mới/weekly ranking/detail, filters, saved repos/queries và snapshots.

[GitHub Search API](https://docs.github.com/en/rest/search/search): Tìm repository theo qualifiers và sort; kết quả có giới hạn và có thể incomplete.

**Áp dụng cho Nexora:** GitHub Search tham chiếu public queries; không OAuth User/private repos/write/star/fork.

**Màn hình:** `/developer/github`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Top 10 New sort createdAt desc; Top 10 Weekly lấy repo tạo trong tuần hiện tại, sort tổng stars desc; mỗi bảng lấy tối đa10 kết quả sau filter.
2. Filter language/topic/created range/min stars; xem definition/window/freshness.
3. Detail → Open GitHub/Copy repo URL/Copy clone URL.
4. Save local repo/query; capture ranking snapshot immutable.

## Dữ liệu và validation

- Repo publicID/fullName/owner/avatar/description/URLs/metrics/language/topics/license/defaultBranch/dates.
- Week Monday00:00UTC→nextMonday exclusive delegated; tie stars→createdAt desc→ID asc.
- Snapshot query/ruleVersion/window/capturedAt/rank/metrics; saved filter owner-scoped.

## Hành vi và lifecycle

- **FX-33-BR-001:** Weekly không phải stars gained tuần: dùng CREATED currentweek và totalstars theo requirement nguồn.
- **FX-33-BR-002:** Không hidden fork/archive/template exclusions; filters explicit.
- **FX-33-BR-003:** API incomplete/result caps/rate limit báo partial/stale; không gọi ranking exhaustive khi nguồn bị giới hạn.
- **FX-33-BR-004:** Watchers dùng subscribers semantics khi dữ liệu có, không gán stargazers thành watchers.
- **FX-33-BR-005:** Cache15min keyed query/window/version; refresh coalesced; lastAttempt khác lastSuccess.
- **FX-33-BR-006:** Snapshot không bị live metrics rewrite; missing repo giữ historical record. System token optional secret read-only.

## Quyền, API và tích hợp

- GitHubPublicReadAdapter Search/GetDetails; sanitize external fields.
- SavedRepository/SavedQuery/CaptureSnapshot; no GitHub mutation endpoint.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-33-AC-001:** Repo cũ tăng stars không lọt Weekly.
- **FX-33-AC-002:** Cache khác filter không lẫn.
- **FX-33-AC-003:** Snapshot giữ metrics cũ sau refresh; partial result có nhãn.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Snapshot deltas

Trend delta chỉ so hai snapshots có cùng query/filter/rule version và window tương thích. Hiển thị delta total stars/forks/rank cùng hai capturedAt; không đổi nhãn thành “stars gained this week”. Repository thiếu ở một snapshot hiển thị New/Unavailable, không giả prior stars=0. Top10 bị giới hạn nguồn phải ghi partial; không tính rank toàn GitHub từ mẫu Top10.

## Traceability và phần còn mở

- [phase-06-developer-and-automation.md](../requirements/phases/phase-06-developer-and-automation.md): `P06-GHA-001`, `P06-GHA-002`, `P06-GHA-003`, `P06-GHA-004`, `P06-GHD-001`, `P06-GHD-002`, `P06-GHD-003`, `P06-GHD-004`, `P06-GHD-005`, `P06-GHD-006`, `P06-GHR-001`, `P06-GHR-002`, `P06-GHR-003`, `P06-GHR-004`, `P06-GHR-005`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.

