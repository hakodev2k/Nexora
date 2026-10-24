# Read Later

FX-23 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên. Theo `DEC-20260909-014`, phần Bookmark-reference local có đủ contract và đã được implement trên PR #4; News/body extraction và các capability chưa đủ contract vẫn gated. Đây là phạm vi slice, không phải tuyên bố toàn FX-23 hoàn tất.

## Phạm vi và tham chiếu

Hàng đợi đọc cho Bookmarks và News, read state và vị trí đọc.

[Instapaper](https://www.instapaper.com/docs): Lưu nội dung để đọc sau; tham chiếu cổng hướng dẫn, không kiểm thử tài khoản.

**Áp dụng cho Nexora:** Instapaper tham chiếu queue; không hứa full-text extraction/offline mọi nguồn.

**Màn hình:** `/read-later` cho queue Bookmark-reference local. Reader/body route, News source route và external-open vẫn chưa được implement.

## Luồng sử dụng

1. Save for later từ source → queue entry.
2. Unread/Reading/Read; sort savedAt desc, filter source/tag; mở nội dung được phép.
3. Mark read/unread, lưu vị trí; remove khỏi queue không xóa source.

## Dữ liệu và validation

- Unique User+sourceType+sourceId; state/savedAt/lastReadAt/position.
- Safe title/URL snapshot để nhận biết source unavailable.

## Hành vi và lifecycle

- **FX-23-BR-001:** Source Trash/disabled thì không trả cached body; queue ghi unavailable cho owner.
- **FX-23-BR-002:** Open external không tự mark Read; thiếu body không giả % đọc.
- **FX-23-BR-003:** News read state dùng chung qua contract, không tạo hai trạng thái mâu thuẫn.
- **FX-23-BR-004:** Remove queue không xóa article/bookmark; không notification cho mỗi lần đọc.
- **FX-23-BR-005:** Không tự fetch vượt nội dung nguồn cung cấp để hoàn thành progress.

### Local slice boundary (PR #4)

- Source type hiện được chấp nhận là `Bookmark` và phải là Bookmark còn đọc được trong cùng `PersonalSpace`. News chưa có source/provider contract runtime.
- Queue chỉ lưu `SourceType`, `SourceId`, `SafeTitleSnapshot`, `SafeUrlSnapshot`, state và position metadata trong SQL. Không lưu body, không follow URL và không tạo reader HTML.
- `Unread`, `Reading`, `Read` được cập nhật qua action riêng với `ETag`/`If-Match`, idempotency và audit. Remove chỉ gỡ queue reference; source không bị xóa.
- Khi Bookmark không còn active/archived hoặc module/read grant không còn, response chỉ đánh dấu `SourceAvailable=false`; snapshot không trở thành lối truy cập body.

## Quyền, API và tích hợp

- SaveForLater/RemoveFromQueue/SetReadState/SavePosition; SourceReadProvider.
- Không duplicate toàn article body trong queue.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gate, concurrency, idempotency, lỗi/loading/empty, phân trang và lifecycle. Support/Emergency chỉ read-only có grant; không thừa hưởng owner mutation, secret reveal hoặc export. API cụ thể phải theo command/query này và được chốt trong solution design.

## Tiêu chí nghiệm thu

- **FX-23-AC-001:** Save lặp một entry.
- **FX-23-AC-002:** Xóa source không rò cached body.
- **FX-23-AC-003:** Read position của User A không ảnh hưởng B.

Các AC nguồn và common gates vẫn bắt buộc; đây là các scenario bổ sung, không thay thế toàn bộ test specification.

## Traceability và phần còn mở

- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-RDL-001`, `P03-RDL-002`

Không phát sinh câu hỏi nghiệp vụ lớn riêng cho feature này. Các gate chung về security, capacity và solution design vẫn áp dụng.
