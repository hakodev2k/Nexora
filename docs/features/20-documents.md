# Documents, Note và Knowledge Pages

FX-20 · Feature specification · 2026-09-06 · Baseline requirements: d0d8418

**Trạng thái:** yêu cầu đã xác nhận được giữ nguyên; chi tiết bổ sung bên dưới là **Resolved (delegated)** theo DEC-GOV-001. Mục Q còn mở là proposal, chưa được duyệt. Tài liệu không cấp phép implement.

## Phạm vi và tham chiếu

Một Documents module, types Document/Note/Knowledge; page editor, folders/hierarchy, metadata/version/lifecycle.

[Google Docs](https://support.google.com/docs/answer/190843?hl=en): Xem lịch sử và khôi phục nội dung phiên bản trước.

[Notion](https://www.notion.com/help/writing-and-editing-basics): Nội dung page được tổ chức qua các block và thao tác soạn thảo.

**Áp dụng cho Nexora:** Google Docs tham chiếu version/editor, Notion tham chiếu block/page navigation. Nexora manual Save, no collaboration/autosave/no-code database.

**Màn hình:** `/documents, /documents/folders/:id, /documents/pages/:id, /documents/archived`. Routes là thiết kế đề xuất; không phải endpoint đã implement.

## Luồng sử dụng

1. Trang Documents mặc định Grid, có Table; chỉ hiển thị Title/DocumentType/Tag. Gốc liệt kê Folder cấp1 và root pages ngoài Folder.
2. Mở Folder chỉ thấy Folder con trực tiếp và root pages trực tiếp. Child pages truy cập từ sidebar của parent. Archived là danh sách phẳng riêng.
3. Mỗi lần tạo phải chọn DocumentType và EditorMode, không preselect/remember; Title bắt buộc, body optional.
4. User bấm Save tạo immutable version cho mỗi command thành công. Restore phiên bản cũ tạo version mới, giữ toàn bộ lịch sử.
5. Draft ↔ Published; cả hai Archive được. Archive parent xử lý children; Unarchive chỉ children cùng đợt. Trash/restore theo deletion provenance.

## Dữ liệu và validation

- Title1–200, trùng mọi nơi được phép; delegated cho sửa Title ở Draft/Published. Body tối đa1MiB dạng canonical Block JSON hoặc Markdown.
- EditorMode và DocumentType bất biến. Root có0..1 Folder cố định kể cả initial none; ParentPageId cố định. Folder và page tree tối đa hai cấp.
- Tối đa một Tag, tạo ngay trong form. Visual optional: Icon hoặc cover, không cả hai. Icon từ emoji/library; cover JPG/PNG/WebP≤5MiB/25MP, crop không ghi đè binary.
- Blocks: paragraph, H1–H3, lists/checklist, quote, divider, link, code, image, simple table. Markdown CommonMark+tables/tasklists/strikethrough; raw HTML escaped, không arbitrary embed/script.

## Hành vi và lifecycle

- **FX-20-BR-001:** Local search chỉ Title/Tag; filter Type/Tag/CreatedDate trong vị trí trực tiếp hiện tại; sort updatedAt desc rồi stable ID. Folder giữ vai trò navigation khi page filters active, không giả metadata page cho Folder.
- **FX-20-BR-002:** Child kế thừa Folder của parent. Folder rename được phép, move bị chặn để giữ membership cố định. Template/version restore không bypass immutable fields.
- **FX-20-BR-003:** Save snapshot Title/body/Tag/visual; có dirty warning và stale conflict. Change note optional. Lifecycle ghi Activity, không tự tạo content Save version.
- **FX-20-BR-004:** Tag delete chặn khi current/Archived/Trash page dùng. History-only giữ snapshot label; restore metadata cần preview rebind hoặc tạo lại Tag, không mất Tag âm thầm.
- **FX-20-BR-005:** Published vẫn private tới khi tạo share. Draft suspend link; Archive giữ link Published đang active, không tạo mới hoặc hồi sinh expired/revoked link. Share page không tự bao gồm children.
- **FX-20-BR-006:** Archive parent atomic với children ngoài Trash, giữ previous state riêng. Child đã Archived giữ cohort cũ. Unarchive chỉ cùng cohort; bỏ qua Trash/purged child. Không Unarchive child khi parent Archived.
- **FX-20-BR-007:** Archived chỉ-đọc nhưng xóa vào Trash được. Sidebar hiển thị child Archived có nhãn. Xóa parent kéo cả cây; xóa child riêng cần cảnh báo. Restore riêng child yêu cầu original parent tồn tại, ngoài Trash và không Archived.
- **FX-20-BR-008:** Trash giữ tới owner purge. Restore đúng deletion batch, không hồi sinh child đã xóa riêng trước đó. Version history giữ tới page purge; import/export Office/PDF/Markdown theo Q-11.

## Quyền, API và tích hợp

- SavePage, RestoreVersion, ArchiveTree, UnarchiveCohort, TrashTree, RestoreTree; concurrency và atomic aggregate.
- Editor schema version/migration giữ semantic và mode; Files giữ historical cover refs; Search đọc current saved projection.

Áp dụng [hợp đồng chung](00-shared-behavior.md): owner isolation, module/action gates, concurrency, idempotency, loading/empty/error và lifecycle. Support/Emergency chỉ read-only trong grant; không owner mutations hoặc export.

## Tiêu chí nghiệm thu

- **FX-20-AC-001:** Mỗi lần tạo phải chọn mode/type. Forged restore không đổi Folder/Parent/Type/Mode.
- **FX-20-AC-002:** Parent và child Published được Archive rồi Unarchive đúng state; child Archived riêng từ trước vẫn Archived.
- **FX-20-AC-003:** Unarchive parent không hồi sinh child Trash; restore child khi parent Archived bị từ chối.
- **FX-20-AC-004:** Retry Save một version; distinct Save không đổi nội dung vẫn tạo version mới. Paste độc hại không execute.
- **FX-20-AC-005:** Body-only match không xuất local Documents list; Global Search có provider và scope riêng.

AC nguồn và common acceptance gates vẫn bắt buộc.

## Traceability và phần còn mở

- [06-decisions-and-traceability.md](../requirements/06-decisions-and-traceability.md): `DEC-KNW-001`, `DEC-KNW-002`, `DEC-KNW-003`, `DEC-KNW-004`, `DEC-KNW-005`, `DEC-KNW-006`, `DEC-KNW-007`, `DEC-KNW-008`, `DEC-KNW-009`, `DEC-KNW-010`, `DEC-KNW-011`, `DEC-KNW-012`, `DEC-KNW-013`, `DEC-KNW-014`, `DEC-KNW-015`, `DEC-KNW-016`, `DEC-KNW-017`, `DEC-KNW-018`, `DEC-KNW-019`, `DEC-KNW-020`, `DEC-KNW-021`, `DEC-KNW-022`, `DEC-KNW-023`, `DEC-KNW-024`, `DEC-KNW-025`, `DEC-KNW-026`, `DEC-KNW-027`, `DEC-KNW-028`, `DEC-KNW-029`, `DEC-KNW-030`, `DEC-KNW-031`, `DEC-KNW-032`, `DEC-KNW-033`, `DEC-KNW-034`, `DEC-KNW-035`, `DEC-KNW-036`, `DEC-KNW-037`, `DEC-KNW-038`, `DEC-KNW-039`, `DEC-KNW-040`, `DEC-KNW-041`, `DEC-KNW-042`, `DEC-KNW-043`
- [phase-03-knowledge-search-dashboard.md](../requirements/phases/phase-03-knowledge-search-dashboard.md): `P03-CNT-001`, `P03-CNT-002`, `P03-CNT-003`, `P03-CNT-004`, `P03-CNT-005`, `P03-CNT-006`, `P03-CNT-007`, `P03-CNT-008`, `P03-CNT-009`, `P03-DOC-001`, `P03-DOC-002`, `P03-DOC-003`, `P03-DOC-004`, `P03-DOC-005`, `P03-DOC-006`, `P03-DOC-007`, `P03-DOC-008`, `P03-DOC-009`, `P03-DOC-010`, `P03-DOC-011`, `P03-DOC-012`, `P03-DOC-013`, `P03-DOC-014`, `P03-DOC-015`, `P03-DOC-016`, `P03-DOC-017`, `P03-DOC-018`, `P03-DOC-019`, `P03-DOC-020`, `P03-DOC-021`, `P03-DOC-022`, `P03-DOC-023`, `P03-DOC-024`, `P03-DOC-025`, `P03-DOC-026`, `P03-DOC-027`, `P03-DOC-028`, `P03-DOC-029`, `P03-DOC-030`, `P03-DOC-031`, `P03-DOC-032`, `P03-DOC-033`, `P03-DOC-034`, `P03-DOC-035`, `P03-DOC-036`, `P03-DOC-037`, `P03-DOC-038`, `P03-DOC-039`, `P03-DOC-040`, `P03-DOC-041`, `P03-DOC-042`, `P03-DOC-043`, `P03-DOC-044`, `P03-DOC-045`, `P03-DOC-046`, `P03-DOC-047`, `P03-VER-001`, `P03-VER-002`, `P03-VER-003`, `P03-VER-004`, `P03-VER-005`

Quyết định lớn cần PO: [Q-11](90-open-decisions.md#q-11). Các hành vi phụ thuộc chúng chưa đạt Definition of Ready.
