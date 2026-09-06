# Phase 3 — Documents, Global Search and Dashboard

**Phase ID:** `NX-PH-03`  
**Version:** `1.2-draft`  
**Outcome:** User lưu, tổ chức, chia sẻ read-only theo policy và tìm lại tri thức/tài liệu cá nhân; Dashboard tổng hợp dữ liệu của chính User mà không tạo source of truth mới.  
**Depends on:** Phase 1 personal ownership/Module Platform services; Phase 2 search projections/calendar/task read contracts.

## 1. Scope proposal

### P0

- Một `Documents` module với page types Document, Note và Knowledge.
- Files/attachments, Bookmarks, Snippets.
- Tags, typed Collections, Archive và basic version history.
- Unified Read Later cho URL/news/bookmark-compatible resources.
- Basic Global Search xuyên module đã hỗ trợ, có filters và access enforcement.
- Dashboard/Home với Today, recent content, counts/alerts và quick actions tối thiểu.
- Item/Collection read-only sharing cho resource được duyệt.
- Personal ownership theo module manifest.
- Versioning và conflict handling cho nhiều tab/session của cùng User; không có team comments/mentions/co-editing.

### P1

- Templates, saved/advanced search, favorites, recent items/history, command palette.
- Full document version comparison/restore, export formats bổ sung, customizable widgets.

### Deferred/out

Real-time co-editing/presence, formal approval workflow, public publishing site, AI summarization/semantic search, OCR, Office/Google Docs bidirectional editing, arbitrary executable snippets.

## 2. Approved Documents content model

Theo `DEC-PRD-004/DEC-KNW-006`, navigation chỉ có module `Documents`. Mọi page dùng chung một `ContentItem`/Document foundation:

- `ContentItemId`, owner, type, title/body, timestamps, version relation và common lifecycle metadata dùng chung.
- `DocumentType` tối thiểu gồm `Document`, `Note`, `Knowledge`.
- Mỗi type có thể có validation, organization, lifecycle, sharing, export và presentation policy riêng; dùng chung model không có nghĩa mọi type có cùng behavior.
- DocumentType bất biến sau khi tạo; client không được đổi type qua update/import/restore để bypass validation, permission, retention hoặc share rule.

UX tối thiểu của module:

- Trang Documents có nút Create/New và list các page đã tạo trước đó.
- Tạo page chọn DocumentType và EditorMode theo rule đã duyệt.
- Page mở trình soạn thảo kiểu Google Docs về trải nghiệm viết, nhưng vẫn dùng manual Save/version behavior đã chốt; không suy ra Google Docs autosave hoặc collaboration.
- Note và Knowledge chỉ là type/presentation preset trong Documents, không có navigation/module/data engine riêng.

Release 1 không có conversion/migration giữa DocumentType. Nếu bổ sung sau này phải có decision/migration explicit và không mất version/share/link.

Theo `DEC-KNW-001/003`, editor hỗ trợ cả Block editor và Markdown. User phải chọn một `EditorMode` khi tạo ContentItem; mode này bất biến và Release 1 không có conversion/switch editor flow. Canonical storage của từng mode là technical design nhưng phải giữ đúng round-trip semantics.

## 3. Documents

### 3.1 Common content requirements

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-CNT-001` | P0 | User tạo content với title/body theo type; owner và creator được set server-side, mặc định private. | Invalid length/format rejected; User khác không access qua direct ID. |
| `P03-CNT-002` | P0 | Content editor hỗ trợ cả Block editor và Markdown; storage/editor format phải versioned, documented và không khóa export/migration. | Mỗi mode save/load round-trip giữ semantic; unsupported construct được cảnh báo. |
| `P03-CNT-003` | P0 | Rich content từ user/external source được sanitize/encode; embedded URL/media theo allowlist. | XSS corpus không chạy ở editor, preview, share view hoặc search highlight. |
| `P03-CNT-004` | P0 | Không autosave. User chủ động bấm Save; mỗi Save thành công tạo một version mới sau optimistic-concurrency check. | Dirty-state/navigation/session-expiry warning ngăn mất content; two-tab stale save bị detect, không silent overwrite hoặc tạo version sai base. |
| `P03-CNT-005` | P0 | Content hỗ trợ tags, attachments, archive, trash/restore và search projection theo capability. | Lifecycle giữ owner/links hợp lệ; archived khác trashed. |
| `P03-CNT-006` | P0 | View/update timestamps và author/editor metadata không thể spoof bởi client. | Server authoritative; timezone display đúng. |
| `P03-CNT-007` | P0 | External read-only share chỉ hiển thị field được policy cho phép, không lộ internal metadata/history mặc định. | Public/authenticated/restricted-user matrix pass; share không cấp edit access. |
| `P03-CNT-008` | P1 | Export format tối thiểu được quyết định theo type; export preserve encoding and attachments manifest. | Schema/round-trip test; access/audit pass. |
| `P03-CNT-009` | P0 | Create ContentItem bắt buộc chọn EditorMode `Block` hoặc `Markdown`; field này không được thay đổi sau khi tạo. | Missing/unknown mode bị từ chối; update/restore/import không được đổi mode hoặc bypass editor validation. |

### 3.2 Document library, types và lifecycle

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-DOC-007` | P0 | Module Documents có Create/New và hai view Card/Grid, Table; mặc định Card/Grid. Mỗi Card và dòng Table chỉ hiển thị Title, DocumentType, Tag. | Hai view dùng cùng access/data scope; đổi view không mutation. Tag trống hợp lệ; không tự thêm Status, ngày, Folder/page cha, Icon/cover vào Card/Table; không thêm Tree View riêng. Filter/search/sort theo `DEC-KNW-037`, navigation/Archived visibility theo `DEC-KNW-038`; empty/loading/error/pagination states rõ, không lộ dữ liệu User khác hoặc module-disabled. |
| `P03-DOC-008` | P0 | Mỗi page có immutable DocumentType `Document`, `Note` hoặc `Knowledge`; không có Knowledge Base/Knowledge Article resource riêng. | Create/list/detail/search hiển thị type rõ; update/import/restore không đổi type hoặc nhận legacy KB/Article owner relation. |
| `P03-DOC-009` | P0 | Page mới mặc định Draft; Draft ↔ Published; Draft/Published → Archived; Archived → previous Draft/Published state. | Unknown/other transition bị từ chối; archive lưu previous state; retry idempotent. |
| `P03-DOC-010` | P0 | Published không làm Document public; chỉ active share grant mới cho viewer khác xem. | Publish không tạo share/token/index public hoặc thay owner/access. |
| `P03-DOC-011` | P0 | Published Document vẫn editable và mỗi Save tạo version như Draft. | Update/Save được phép theo owner/module policy; share view đọc current saved version. |
| `P03-DOC-012` | P0 | Archived Document read-only nhưng User có thể unarchive về đúng Draft/Published state trước khi Archive. | Save/content mutation khi Archived bị chặn; unarchive không tự publish/unpublish khác previous state. |
| `P03-DOC-013` | P0 | Documents hỗ trợ Folder tối đa hai cấp, Tag và page cha-con dạng single-parent tree tối đa hai cấp. | Không tạo Folder/page cấp 3 hoặc cycle; mọi relation owner-scoped. |
| `P03-DOC-014` | P0 | DocumentType là immutable business field sau create. | Direct update, version restore, import và forged payload không thay đổi type. |
| `P03-DOC-015` | P0 | Root/parent page chọn optional Folder membership `0..1` khi tạo và không được thay đổi sau đó; child page không có Folder riêng, kế thừa effective Folder từ parent. | Root không đổi/gỡ/gắn thêm Folder sau create, kể cả ban đầu không thuộc Folder; cross-user Folder/multi-folder/direct child Folder bị chặn. Update/import/version restore không bypass immutable membership. |
| `P03-DOC-016` | P0 | Một Document page có tối đa một parent page và page hierarchy có tối đa hai cấp. | Root page có thể tạo child page mới; child page không nhận child page khác; create không tạo cycle/cấp 3; attach/detach/reparent sau create bị chặn theo `P03-DOC-020`. |
| `P03-DOC-017` | P0 | Xóa parent page là aggregate delete, đưa parent và toàn bộ child pages vào Trash. | Không promote/orphan child; list/search/direct/share access không trả aggregate đã trash; retry idempotent. |
| `P03-DOC-018` | P0 | Restore parent page là aggregate restore, khôi phục parent và toàn bộ child pages thuộc aggregate tại thời điểm xóa. | Không selective restore hoặc tạo duplicate relation/version; page tree khôi phục đúng cấu trúc; retry idempotent. |
| `P03-DOC-019` | P0 | User có thể xóa riêng child page của parent đang active sau explicit confirmation warning. | Confirmation nêu rõ child sẽ vào Trash; parent/siblings không đổi; cancel không mutation; confirmed retry idempotent. |
| `P03-DOC-020` | P0 | Root/child relation được xác định khi tạo và không được thay đổi sau đó. | `ParentPageId` immutable; update, version restore, import hoặc forged payload không attach/detach/reparent page. |
| `P03-DOC-021` | P0 | Child page bị xóa riêng chỉ được restore khi original parent đang active. | Parent ở Trash trả conflict yêu cầu restore parent trước; parent permanent delete chặn restore; không promote child thành root. |
| `P03-DOC-022` | P0 | Chỉ Published Document được tạo share link; chuyển Published về Draft suspend mọi link còn tồn tại. | Draft không tạo share mới và suspended link không truy cập được; publish lại chỉ re-activate link chưa expired/revoked. |
| `P03-DOC-023` | P0 | Active share link vẫn cho phép xem Archived Document read-only. | Archive từ Published không revoke/suspend active link; Archive không cho tạo link mới; suspended/expired/revoked link không được kích hoạt bởi Archive. |
| `P03-DOC-024` | P0 | Create page bắt buộc `DocumentType`, `EditorMode` và non-blank `Title`; content body được phép trống. Optional metadata gồm Tags, Folder cho root, Parent cho child, Icon hoặc cover image. | Missing/blank required field bị từ chối; root không chọn Parent, child không chọn Folder riêng; không tự thêm Description/Summary. |
| `P03-DOC-025` | P0 | Document Title được phép trùng trong cùng Folder, page tree và toàn bộ module. | Không có unique-title constraint hoặc auto-rename; list/search/breadcrumb dùng type/path/stable ID để phân biệt. |
| `P03-DOC-026` | P0 | Mỗi page có tối đa một Tag; User có thể chọn Tag hiện có hoặc tạo Tag ngay trong create/edit form. | Không gắn nhiều Tag; inline-create owner-scoped, validate/normalize như Documents Tag catalog và cho phép tái sử dụng. |
| `P03-DOC-027` | P0 | Visual metadata của page là optional exclusive choice: một Icon hoặc một cover image, không đồng thời cả hai. | Payload có cả Icon và cover bị từ chối; absent visual hợp lệ; source theo `DEC-KNW-029`, file limits theo `DEC-KNW-032`, crop theo `DEC-KNW-033`. |
| `P03-DOC-028` | P0 | Mỗi create flow bắt buộc User chọn rõ DocumentType và EditorMode, không có default hoặc remembered value. | Form không preselect/silent infer; thiếu một lựa chọn không submit; cả hai immutable sau create. |
| `P03-DOC-029` | P0 | Icon được chọn từ Emoji/thư viện Icon có sẵn; cover chỉ từ file ảnh User upload qua File Service. | Không có custom Icon upload hoặc external-URL Icon/cover; invalid icon reference bị từ chối; cover áp dụng owner/file/share access và upload validation chung. Format/size còn mở, crop theo `P03-DOC-031`. |
| `P03-DOC-030` | P0 | Draft/Published page cho thay đổi Tag và Icon/Cover; Folder membership cố định; Archived vẫn read-only. | Save Tag/visual tuân thủ manual Save/versioning, tối đa một Tag và không đồng thời Icon/cover; thay visual không đổi Folder/parent/type/editor; Archived chặn metadata mutation cho tới Unarchive. |
| `P03-DOC-031` | P0 | User có thể cắt cover đã upload và chọn vùng hiển thị; thay đổi được lưu qua manual Save/versioning. | Vùng crop hợp lệ nằm trong ảnh; preview thể hiện vùng đã chọn, reload sau Save giữ kết quả. Cancel không thay cover đã lưu; Archived và read-only viewers không được crop; không ghi đè dữ liệu cover của version cũ. |
| `P03-DOC-032` | P0 | Grid/Table Documents có bộ lọc DocumentType, Tag và khoảng ngày tạo. | Không thêm Status hoặc khoảng ngày cập nhật vào bộ lọc của trang Documents; lọc theo created timestamp, không nhầm updated timestamp; ngày hiển thị theo User timezone. Hai view trả cùng tập kết quả trong scope trực tiếp tại vị trí đang mở theo `DEC-KNW-039`; không lộ dữ liệu User khác. |
| `P03-DOC-033` | P0 | Tìm kiếm tại trang Documents chỉ tìm trong Title và Tag. | Page chỉ khớp content body mà không khớp Title/Tag không xuất hiện vì từ khóa đó; page không có Tag vẫn tìm được qua Title. Áp dụng cùng search scope cho Grid/Table: chỉ nội dung trực tiếp tại vị trí đang mở theo `DEC-KNW-039`; không thay đổi Global Search. |
| `P03-DOC-034` | P0 | Danh sách Documents mặc định sắp xếp theo thời điểm cập nhật giảm dần. | Page mới cập nhật đứng trước; cùng timestamp có thứ tự phụ ổn định để phân trang không trùng/thiếu trên tập dữ liệu ổn định. Sort áp dụng cho kết quả tìm kiếm/lọc ở cả hai view, không tự thêm cột ngày cập nhật. |
| `P03-DOC-035` | P0 | Danh sách Documents ban đầu hiển thị Folder cấp 1 và root pages không thuộc Folder. | Page xuất hiện tại đây phải không có ParentPageId/FolderId và không ở Archived/Trash; không flatten child page, page trong Folder hoặc Folder cấp 2. Nội dung khi mở Folder và phạm vi search/filter theo `DEC-KNW-039`. |
| `P03-DOC-036` | P0 | Khi owner mở parent page, child pages được truy cập từ sidebar của page. | Mở child không đổi ParentPageId/FolderId hoặc content; sidebar không phải một Tree View mới của trang Documents và không tự cấp quyền cho share viewer. Child Archived vẫn hiển thị trong sidebar với nhãn Archived và mở read-only, áp dụng khi parent Draft/Published hoặc Archived; không lẫn child trong Trash. Archive cả cây theo `DEC-KNW-040`; sidebar không tự cho Unarchive child khi parent còn Archived. |
| `P03-DOC-037` | P0 | Archived pages được hiển thị trong mục Archived riêng bên trong Documents, tách khỏi danh sách page thường. | Mục Archived liệt kê phẳng từng page Archived của current User, gồm cả parent và child; mỗi page là mục riêng, không gom hoặc giấu child dưới parent. Không trộn Trash hoặc parent đang Draft/Published chỉ vì có child Archived. ParentPageId giữ nguyên. Mở page vẫn read-only; Unarchive theo previous-state và parent/cohort rules. Việc ẩn khỏi danh sách thường không revoke share link hợp lệ. |
| `P03-DOC-038` | P0 | Mở một Folder chỉ liệt kê Folder con trực tiếp và root pages có FolderId đúng Folder đang mở. | Không flatten page của Folder con hoặc child page vào danh sách; Folder cấp 2 không có Folder con cấp 3. Page Archived/Trash không xuất hiện trong danh sách thường. Grid/Table dùng cùng phạm vi và owner/module access checks. |
| `P03-DOC-039` | P0 | Search và filter tại trang Documents chỉ áp dụng cho nội dung trực tiếp của vị trí hiện tại. | Tại gốc, chỉ xét nội dung danh sách gốc đã duyệt; trong Folder A, page ở Folder B bên trong A hoặc child page không thành kết quả dù khớp Title/Tag/type/ngày tạo. Không tự mở rộng scope khi không có kết quả; Global Search không bị thay đổi. Quy tắc filter/render cho Folder không có metadata DocumentType/Tag cần refinement riêng, không giả lập metadata page cho Folder. |
| `P03-DOC-040` | P0 | Khi owner Archive parent page, parent và toàn bộ child pages thuộc cây được Archive cùng thao tác. | Parent/child Draft hoặc Published chuyển Archived/read-only; giữ previous state riêng của từng page theo lifecycle đã duyệt, không dùng state của parent cho tất cả child. Child đã Archived không bị ghi đè previous state bằng Archived. Không thay parent/Folder/type/editor, nội dung hoặc lịch sử đã lưu. Link từng page tiếp tục áp dụng expiry/revoke/suspended rules; không tạo hoặc mở rộng share. Retry không lặp transition; kết quả thành công không để cây bị Archive dở dang. Unarchive theo `P03-DOC-041..043`; sidebar/Archived listing theo `DEC-KNW-042`; tương tác với Trash theo `DEC-KNW-043` và `P03-DOC-045..047` (Resolved delegated). |
| `P03-DOC-041` | P0 | Unarchive parent khôi phục parent và chỉ các child đã bị Archive cùng đợt với parent. | Mỗi page về đúng previous Draft/Published state riêng; child vốn Archived trước đợt đó giữ nguyên Archived và previous state. Dùng thông tin đợt/nguồn Archive rõ ràng, không gom mọi child Archived hoặc suy theo timestamp. Không thay content, immutable metadata hoặc history; không tạo share hay hồi sinh link expired/revoked. Retry không lặp transition và không báo thành công một phần; child trong Trash/purged bị bỏ qua theo `P03-DOC-046`, không khôi phục ngầm. |
| `P03-DOC-042` | P0 | User được Archive riêng một child khi parent đang Draft hoặc Published. | Chỉ child chuyển Archived/read-only và giữ previous state riêng; parent và sibling không đổi state/content. Đợt Archive riêng không trở thành đợt của parent khi parent được Archive về sau; link child theo share policy đã duyệt. Owner/module/access và concurrency checks vẫn bắt buộc. |
| `P03-DOC-043` | P0 | Không cho Unarchive riêng child khi parent còn Archived; phải Unarchive parent trước. | UI thể hiện điều kiện; request trực tiếp bị chặn và không đổi child/parent. Khi parent đã Draft/Published, child vẫn Archived được Unarchive riêng về previous state của nó. Kiểm tra lại state parent tại lúc thực hiện, gồm tình huống hai tab cạnh tranh; không dùng Unarchive để vượt Trash/owner/module gate. |
| `P03-DOC-044` | P0 | User được xóa trực tiếp page Archived vào Trash mà không cần Unarchive trước. | Delete parent Archived đưa parent và toàn bộ child vào Trash theo aggregate rule; không xóa riêng parent hoặc để child còn truy cập được. Delete riêng child Archived dùng warning/confirmation đã duyệt, giữ nguyên parent/sibling. Không có transition trung gian sang Draft/Published; không Save/Unarchive ngầm. Page đã Trash biến mất khỏi Archived/sidebar/list/search và share bị chặn theo Trash policy; không purge content/version history. Archived vẫn chặn content/metadata edit. Restore và retention theo `P03-DOC-045..047` (Resolved delegated). |
| `P03-DOC-045` | P0 | Documents page/Folder trong Trash được giữ tới khi User chủ động xóa vĩnh viễn; không auto-purge theo tuổi. | Resolved delegated theo DEC-GOV-001/KNW-043. Retention/cleanup không xóa content hoặc full history trước purge. Purge yêu cầu xác nhận rõ phạm vi và không thể khôi phục trong app; dùng aggregate/owner policy hiện có. Không suy thêm billing/quota hoặc đổi backup/audit policy. |
| `P03-DOC-046` | P0 | Archive/Unarchive parent bỏ qua child trong Trash hoặc đã purge, không tự khôi phục và không chặn thao tác parent chỉ vì child đã xóa. | Resolved delegated. Chỉ áp dụng transition lên page hiện hữu ngoài Trash; Unarchive vẫn chỉ xét child thuộc đúng đợt của parent. Child Trash giữ state/history/cohort tại lúc xóa; child purge không được tái tạo. Không đưa child đó vào Archived/sidebar/search/share. Request retry và multi-tab checks giữ kết quả nhất quán. |
| `P03-DOC-047` | P0 | Restore riêng child từ Trash yêu cầu original parent hiện hữu, ngoài Trash và không Archived. | Resolved delegated. Parent Trash phải restore trước; parent Archived phải Unarchive trước; parent purge chặn vĩnh viễn, không promote child. Child được restore về state tại lúc xóa: Archived vẫn Archived và chỉ Unarchive qua thao tác riêng hợp lệ; Draft/Published không bị tự đổi. Giữ original ParentPageId/Folder/history và provenance; không gán lại đợt Archive cũ vào đợt mới của parent. Sau khi đợt cũ đã được Unarchive, restore child không chạy lại đợt đó. Kiểm tra parent state tại commit để chống race. |


Các requirement `P03-KB-001..004` của model Knowledge Base container trước đây bị `DEC-KNW-006` supersede và không được implement.

State transitions:

| From | To | Rule |
|---|---|---|
| Draft | Published | Cho phép; Document vẫn private nếu chưa có share; link suspended còn hiệu lực được kích hoạt lại. |
| Published | Draft | Cho phép; mọi share link được suspend nhưng giữ nguyên token/configuration. |
| Draft | Archived | Cho phép; lưu Draft làm previous state, chuyển page sang read-only; không kích hoạt suspended link. |
| Published | Archived | Cho phép; lưu Published làm previous state, chuyển page sang read-only; active link tiếp tục cho xem. |
| Archived | Previous Draft/Published | Cho phép qua Unarchive; không được chọn một state khác previous state. |

Archive parent áp dụng cho cả cây theo `P03-DOC-040`; Unarchive parent chỉ khôi phục child cùng đợt theo `P03-DOC-041`. Bảng trên mô tả previous state riêng của từng page. Archive/Unarchive riêng child phải qua điều kiện parent tại `P03-DOC-042/043`; không tự dùng state của parent làm state đích của child.

### 3.3 Versioning and sharing

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-DOC-001` | P0 | Document Sharing là capability confirmed và dùng Sharing Engine, không custom token system. | Item/Collection share lifecycle/expiration/revoke tests pass. |
| `P03-DOC-002` | P0 | Mỗi explicit Save thành công tạo version với version number, actor và timestamp; change note hiện vẫn chưa chốt. | Concurrent saves không tạo duplicate/gap bất hợp lý; history owner-scoped; không có autosave version. |
| `P03-DOC-003` | P0 | Restore version tạo current version mới từ selected version và không xóa/rewrite lịch sử. | New version ghi source-version reference/actor/time; share policy không bị reset/bypass. |
| `P03-DOC-004` | P1 | Compare version nếu có phải render untrusted content an toàn. | Diff view sanitized; large content bounded. |
| `P03-DOC-005` | P0 | Team comments/replies/mentions/follows không thuộc Release 1; Document vẫn có personal Activity/History theo policy. | Không expose collaboration UI/API; history owner-scoped. |
| `P03-DOC-006` | P0 | Save Document dựa trên current version/precondition; conflict trả metadata an toàn để reload/merge/retry. | Hai tab/session save stale base không silent overwrite; accepted save tạo attribution/version đúng. |

## 4. Versioning

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-VER-001` | P0 | Mỗi lần User bấm Save và request thành công phải tạo đúng một immutable version, kể cả khi editor mode là Block hoặc Markdown; không autosave. | Retry idempotent không tạo version trùng; mỗi successful distinct Save command tăng version đúng một. |
| `P03-VER-002` | P0 | Historical version immutable với business user; purge theo retention riêng. | No update endpoint; restore tạo version mới và giữ actor/owner attribution. |
| `P03-VER-003` | P0 | Version không copy secret/external embedded credential vào audit/log/search. | Redaction/classification test pass. |
| `P03-VER-004` | P0 | Giữ toàn bộ versions tới khi ContentItem bị permanent delete; không auto-expire, cap theo số lượng hoặc cho User xóa riêng version. | Retention/cleanup job không xóa version của item chưa purge; Trash/restore giữ nguyên full history. |
| `P03-VER-005` | P0 | Restore một historical version tạo đúng một current version mới với cùng EditorMode của ContentItem. | Toàn bộ history cũ còn nguyên; content/source-version trace đúng; retry idempotent không tạo nhiều restored versions. |

## 5. Files và attachments

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-FIL-001` | P0 | Standalone file và attachment đều dùng File Service; resource relation khai báo owner/access. | Không duplicate binary khi reuse được policy cho phép; không cross-user reuse/access. |
| `P03-FIL-002` | P0 | Preview chỉ bật cho safe supported types; fallback là controlled download. | Active content không chạy same-origin trái policy; MIME spoof test pass. |
| `P03-FIL-003` | P0 | Replace file tạo version/relation mới theo decision, không overwrite object âm thầm. | Link/reference behavior documented; checksum/metadata update correct. |
| `P03-FIL-004` | P0 | Folder/collection nếu có là logical organization, không dùng untrusted path làm physical path. | Rename/move không di chuyển/overwrite ngoài storage root. |

## 6. Bookmarks và unified Read Later

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-BMK-001` | P0 | Bookmark có URL normalized, title/description/notes/tags và optional fetched metadata. | Invalid/unsafe scheme bị từ chối; duplicates theo policy rõ. |
| `P03-BMK-002` | P0 | Metadata fetch là untrusted network operation và áp dụng SSRF/timeout/size/redirect/content rules. | Private/link-local/loopback target bị chặn; HTML sanitized. |
| `P03-BMK-003` | P0 | Fetch failure không ngăn manual bookmark; user thấy last fetch/status. | Provider/URL failure degrade rõ, không mất input. |
| `P03-RDL-001` | P0 | Chỉ có một Read Later queue dùng typed reference hoặc captured URL; source module vẫn sở hữu item. | Một news article/bookmark không tạo hai queue records trùng do retry. |
| `P03-RDL-002` | P0 | Read state, added/read timestamps và remove/archive behavior thuộc current User. | User B activity không ảnh hưởng User A; source delete/revoke có fallback rõ. |

## 7. Snippets

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-SNP-001` | P0 | Snippet lưu code/command/query/config/text với language/type, title, body, tags. | Body rendered as text/code, không tự execute. |
| `P03-SNP-002` | P0 | Copy action không execute hoặc interpolate content; audit chỉ cần nếu snippet được classified sensitive. | Shell/HTML markers không chạy; clipboard response scoped. |
| `P03-SNP-003` | P0 | UI cảnh báo không lưu password/API key trong normal Snippet và cung cấp link sang Vault. | Secret pattern detection nếu có chỉ là warning, không AI dependency. |
| `P03-SNP-004` | P1 | Snippet template/variables không được thực thi server-side nếu chưa có sandbox/security spec. | Baseline supports plain substitution only hoặc feature absent. |

## 8. Tags, Collections, Templates và Archive

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-ORG-001` | P0 | Tag thuộc đúng User/module catalog; normalized name/color optional; relation typed. | Cross-user tags blocked; rename propagates without rewriting content. |
| `P03-ORG-002` | P0 | Collection có owner, type và supported resource types; item relation không thay đổi ownership. | Invalid/cross-user type mix bị chặn; collection share composition rõ. |
| `P03-ORG-003` | P0 | Share Collection snapshot/live behavior phải quyết định; default `PROPOSED` là live membership subject to access. | Add/remove item reflected đúng; private/unsupported item không leak. |
| `P03-ORG-004` | P1 | Template tạo copy mới cho current User; không giữ share/secret/history của source. | Instantiate idempotency/field reset/cross-user authorization tests pass. |
| `P03-ORG-005` | P0 | Archive là non-destructive active-but-hidden state; khác Trash và không vô hiệu access trừ policy. | Documents dùng mục Archived riêng theo `P03-DOC-037`, không tự thêm Status filter; các resource khác theo policy module. Restore/unarchive đúng. |
| `P03-ORG-006` | P0 | Folder hierarchy có tối đa hai cấp và không có cycle. | Root Folder có thể chứa child Folder; child Folder không nhận child Folder khác; move concurrent không tạo cycle/cấp 3. |
| `P03-ORG-007` | P0 | Xóa Folder là aggregate delete: đưa Folder, toàn bộ child Folder và mọi page thuộc cây Folder vào Trash. | Không orphan hoặc tự chuyển page lên root; list/search/direct/share access không trả aggregate đã trash; retry idempotent. |
| `P03-ORG-008` | P0 | Restore Folder là aggregate restore, khôi phục toàn bộ Folder tree và page contents đúng cấu trúc tại thời điểm xóa. | Folder/child Folder/page membership được restore cùng nhau; không selective restore hoặc tạo duplicate relation; retry idempotent. |
| `P03-ORG-009` | P0 | Xóa Tag trong Documents bị chặn khi còn page sử dụng; chỉ được xóa khi không còn page sử dụng Tag. | Request bị chặn giữ nguyên Tag/page/relation và báo lý do; kiểm tra tại lúc commit để không tạo dangling reference khi gắn Tag đồng thời. Không tự gỡ/thay Tag hoặc xóa page; Trash/history reference policy theo `DEC-KNW-036`. |

## 9. Global Search

### 9.1 Result and access semantics

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-SRC-001` | P0 | Search xuyên Task, Project, Event, Document pages theo DocumentType, Bookmark, Snippet, File metadata và resource đã đăng ký. | Unsupported module không làm query fail; result type/label/link rõ. |
| `P03-SRC-002` | P0 | Query, filters, pagination/cursor và deterministic tie-break được định nghĩa. | Repeated page không missing/duplicate dưới stable dataset. |
| `P03-SRC-003` | P0 | Search chỉ trả resource current User được phép xem trong owner/module context. | Owner/share/support/revoke/trash/disabled-module matrix 100% pass; count/facet không leak. |
| `P03-SRC-004` | P0 | Result chứa safe title/snippet/highlight; không chứa Secret hoặc hidden field. | XSS/redaction tests; Vault plaintext never indexed. |
| `P03-SRC-005` | P0 | Index/update/delete có consistency target và repair/reindex path. | Create/update visible trong bound; delete/revoke blocked immediately by access check; reindex idempotent. |
| `P03-SRC-006` | P0 | Filters tối thiểu: resource type, date range, tags; module-specific filters optional. | Filter combination and no-result state correct. |
| `P03-SRC-007` | P1 | Ranking strategy documented; exact/title/recent relevance behavior không phụ thuộc personalized AI. | Golden query suite versioned và pass threshold. |
| `P03-SRC-008` | P1 | Saved Search là per-user, không snapshot unauthorized results. | Permission/module/share change affects results; share of saved search absent unless specified. |
| `P03-SRC-009` | P1 | Search history/favorites/recent items là private user data với clear/delete control. | Cross-user isolation and retention pass. |

### 9.2 Command Palette

P1 Command Palette có thể search navigation/allowed actions và data results. Nó không được bypass permission, auto-execute destructive action hoặc expose hidden admin routes.

## 10. Dashboard/Home

### P0 widget proposal

- Today: Tasks, Events, Reminders.
- Overdue/Upcoming counts.
- Recent Documents, có thể phân biệt DocumentType.
- Unread Notifications.
- Quick actions: new Task, Event hoặc Document page với type đã chọn.
- Current User/module context; không có Space switcher hoặc Workspace activity.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-DSH-001` | P0 | Dashboard chỉ dùng read contracts/source modules; không duplicate business state. | Edit từ quick action gọi module workflow; refresh phản ánh source. |
| `P03-DSH-002` | P0 | Mỗi widget access-scoped và fail độc lập. | Widget provider fail hiển thị degraded state; widget khác vẫn usable. |
| `P03-DSH-003` | P0 | Time-sensitive widget dùng cùng timezone/business definitions với source module. | Today/overdue count khớp Task/Calendar list. |
| `P03-DSH-004` | P0 | Responsive order/priority giữ alerts/actions quan trọng accessible trên mobile. | No horizontal overflow; keyboard/screen-reader labels. |
| `P03-DSH-005` | P1 | User có thể hide/reorder selected widgets; config per-user, validated. | Unknown/disabled widget ignored safely; no permission bypass. |
| `P03-DSH-006` | P1 | Chart có accessible text/table equivalent và source/time range label. | No misleading stale/unlabeled aggregate. |
| `P03-DSH-007` | P0 | Dashboard query và quick action luôn gắn current User owner context. | Không trộn count/recent/notification từ User khác; support context không biến thành User dashboard. |

## 11. Permissions và audit

- Namespaces: `documents`, `files`, `search`, `sharing` và relevant actions trong matrix; không có `knowledge` namespace riêng.
- Personal owner policy hoặc explicit read-only share/support context áp dụng list/detail/version/file/search/share.
- Admin action permission không tự cấp quyền xem dữ liệu User khác; support/emergency access được audit.
- Audit bắt buộc: share lifecycle/access theo policy, permanent delete, export, restore version, admin access, unsafe upload/network rejection quan trọng.
- Normal edit/version/share/support event có thể là Activity History; không log content body.

## 12. Edge cases

- Large document, rapid explicit Saves, concurrent tabs và network interruption.
- Malicious rich text/Markdown/link preview/file MIME.
- Shared collection membership thay đổi; item trash/revoke/ownership transfer.
- Search index stale hoặc full rebuild; Redis/search unavailable.
- Rename/move/delete tag/category/collection có references.
- File missing/corrupt/quarantined; attachment referenced bởi nhiều resources.
- External URL redirect loop, huge response, private IP hoặc credential in URL.
- Dashboard source module disabled/permission revoked/timeout.
- User/module/support permission bị revoke khi đang xem hoặc có queued notification/index job.
- Resource relation/tag/collection/file từ User khác.
- Hai tab/session edit trên cùng base version và offline client quay lại trước khi Save.
- Client cố đổi EditorMode qua update/restore/import hoặc gửi payload của mode khác.
- ContentItem ở Trash được restore cùng toàn bộ versions; permanent delete purge content/version theo retention/audit boundary.

## 13. Phase verification scenarios

1. User tạo Knowledge-type Document có malicious markup; editor/share/search highlight đều không chạy script.
2. User share Document bằng cả ba mode, expiry/revoke/trash chặn đúng.
3. Admin không có active support grant hoặc emergency context không tìm thấy/export/view version content User khác.
4. Reindex sau crash tạo cùng logical index và không đưa Secret vào index.
5. Bookmark metadata fetch bị redirect tới loopback và bị chặn; bookmark manual vẫn được lưu.
6. Restore document version tạo version mới, không thay share permission.
7. Dashboard widget fail/timeout nhưng shell và widget khác hoạt động.
8. Document không expose team collaboration API; personal history/activity đúng owner.
9. Hai tab/session save cùng base version; một save conflict, không mất nội dung và attribution đúng.
10. Search/reindex/dashboard không lộ count, snippet hoặc file giữa User A/B hay từ module đã disable.
11. Root page tạo trong Folder A không thể chuyển sang Folder B hoặc bỏ Folder; root tạo ngoài Folder không thể gắn Folder về sau; child vẫn kế thừa parent. Kiểm thử cả update/import/version restore.
12. Draft/Published page sửa Tag/visual qua Save và tạo version; không cho lưu hai Tag hoặc đồng thời Icon/cover; Archived từ chối thay đổi.
13. Icon picker chỉ dùng Emoji/thư viện có sẵn; cover chỉ nhận upload theo File Service. Không nhận URL ngoài, custom Icon upload hoặc cover file của User khác.
14. Xóa Tag đang được page sử dụng bị chặn mà không thay Tag/page/relation; kiểm thử race giữa gắn Tag và xóa Tag không tạo tham chiếu hỏng.
15. Crop/chọn vùng cover rồi Save và mở lại giữ kết quả; cancel giữ cover đã lưu; version cũ không bị ghi đè; Archived/read-only share không cho chỉnh cover.
16. Mở Documents mặc định Grid; Card và Table chỉ hiển thị Title, DocumentType, Tag, kể cả khi page có cover/Icon hoặc không có Tag. Chuyển Grid/Table dùng cùng scope dữ liệu, không thay đổi page/Folder/parent và không lộ dữ liệu User khác.
17. Kiểm thử bộ lọc DocumentType/Tag/ngày tạo trên cả Grid/Table; thay đổi ngày cập nhật không làm page khớp một khoảng ngày tạo khác. Query chỉ khớp body không được tính là kết quả tìm kiếm tại trang Documents.
18. Kết quả Documents mặc định theo updated timestamp giảm dần, có tie-break ổn định; áp dụng sau search/filter và vẫn giữ đúng ba field hiển thị Title, DocumentType, Tag.
19. Với Folder hai cấp và page cha-con, mở Documents chỉ thấy Folder cấp 1/root page ngoài Folder trong danh sách ban đầu; child page được mở từ sidebar parent, không thay đổi cấu trúc đã cố định.
20. Archive page làm page chuyển khỏi danh sách thường sang mục Archived; mở tại đó vẫn read-only và share đang hợp lệ không bị thu hồi chỉ vì thay nơi hiển thị. Trash và dữ liệu User khác không xuất hiện trong mục Archived.

21. Folder A chứa Folder B, root page P và child C của P; B chứa root page Q. Mở A chỉ hiển thị B và P, không hiển thị C hoặc Q; mở B mới hiển thị Q. Tại A, search/filter chỉ khớp Q/C không được trả Q/C hoặc tự mở rộng scope. Kiểm tra cả Grid/Table và kết quả rỗng.
22. Parent Draft có child Published và child đã Archived: Archive parent làm parent và child Published thành Archived/read-only; giữ previous state tương ứng, không ghi đè previous state của child vốn Archived. Kiểm tra share hợp lệ/suspended/expired độc lập trên từng page, retry và lỗi giữa thao tác không tạo thành công một phần. Unarchive theo các scenarios 23–25 bên dưới.

23. Parent P Draft, child A Published, child B đã Archived riêng từ Draft. Archive P rồi Unarchive P: P về Draft, A về Published, B vẫn Archived với previous state Draft. Đợt Archive của B không bị thay bằng đợt của P; retry không đổi kết quả.
24. Khi P Draft/Published, Archive riêng A chỉ đổi A; P và sibling không đổi. Tiếp tục Archive/Unarchive P không tự khôi phục A vì A đã Archived từ trước đợt của P.
25. Khi P Archived, Unarchive riêng A bị chặn ở UI/API và không đổi dữ liệu. Sau khi Unarchive P, child còn Archived có thể Unarchive riêng về previous state của chính nó. Kiểm tra hai tab Archive parent/Unarchive child cạnh tranh để không chấp nhận child Unarchive khi parent còn Archived.

26. Parent Draft/Published có child Archived: sidebar vẫn hiện child với nhãn Archived; mở child read-only. Archive parent rồi mở lại vẫn thấy child Archived; nút Unarchive riêng child không vượt điều kiện parent. Child trong Trash không xuất hiện.
27. Archived parent P và hai child A/B tạo ba mục riêng trong Archived. Parent đang Draft/Published với child Archived riêng không được thêm vào Archived; child vẫn là mục riêng. Không sửa ParentPageId hoặc quyền và không đưa Trash vào danh sách.
28. Xóa parent từ Archived đưa cả cây vào Trash trực tiếp, không Unarchive trung gian; xóa riêng child Archived sau warning chỉ đưa child vào Trash. Kiểm tra cancel không mutation, retry không xóa lặp, full history còn nguyên, share/list/sidebar không trả page đã Trash.

29. Page/Folder nằm Trash hơn 90 ngày vẫn còn dữ liệu/full history, có thể restore theo parent policy; chỉ purge đã xác nhận mới loại khỏi khả năng khôi phục trong app.
30. Archive parent có child Trash hoặc đã purge vẫn thực hiện với phần cây hiện hữu; Unarchive parent chỉ khôi phục child cùng đợt ngoài Trash. Child đã xóa không được xuất hiện lại, không mất history còn trong Trash hoặc bị thay đợt Archive.
31. Restore child khi parent Archived bị chặn không mutation. Unarchive parent rồi restore child: child trước xóa Archived vẫn Archived, child trước xóa Draft/Published giữ state tương ứng; không tự Publish/Unarchive hoặc gán sang đợt Archive mới. Parent Trash/purge tiếp tục chặn theo rule riêng.

## 14. Exit criteria

- `DEC-PRD-004`, `DEC-KNW-001..031`, `DEC-KNW-033..035`, `DEC-KNW-037..042` đã Approved; `DEC-PRD-005`, `DEC-KNW-032/036`, `DEC-TEC-006/007` và `DEC-SEC-006` phải đóng cho scope P0.
- `DEC-KNW-043` đã Resolved (delegated) theo `DEC-GOV-001`; kiểm chứng acceptance DOC-045..047 trước sign-off. Các chi tiết UX/validation thông thường còn lại do PM/Technical resolve theo governance, không chuyển thành chuỗi câu hỏi Product Owner. Approval implementation vẫn riêng.
- Content round-trip, sanitization, conflict, version/lifecycle tests pass.
- Search access/count/facet/highlight negative tests 100% pass.
- Sharing Item/Collection modes và file access pass trên desktop/mobile.
- Dashboard definitions khớp source modules và degraded states pass.
- Personal history/activity/version-conflict scenarios pass.
- Cross-user, revoked-support và disabled-module negative matrix pass.
- Export/migration/restore strategy cho content được chứng minh ở scope P0.
- Không có Critical/High security/data-loss finding mở.
