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
| `P03-DOC-007` | P0 | Module Documents có Create/New action và list các page current User đã tạo. | Empty/loading/error/pagination states rõ; User khác và module-disabled data không xuất hiện. |
| `P03-DOC-008` | P0 | Mỗi page có immutable DocumentType `Document`, `Note` hoặc `Knowledge`; không có Knowledge Base/Knowledge Article resource riêng. | Create/list/detail/search hiển thị type rõ; update/import/restore không đổi type hoặc nhận legacy KB/Article owner relation. |
| `P03-DOC-009` | P0 | Page mới mặc định Draft; Draft ↔ Published; Draft/Published → Archived; Archived → previous Draft/Published state. | Unknown/other transition bị từ chối; archive lưu previous state; retry idempotent. |
| `P03-DOC-010` | P0 | Published không làm Document public; chỉ active share grant mới cho viewer khác xem. | Publish không tạo share/token/index public hoặc thay owner/access. |
| `P03-DOC-011` | P0 | Published Document vẫn editable và mỗi Save tạo version như Draft. | Update/Save được phép theo owner/module policy; share view đọc current saved version. |
| `P03-DOC-012` | P0 | Archived Document read-only nhưng User có thể unarchive về đúng Draft/Published state trước khi Archive. | Save/content mutation khi Archived bị chặn; unarchive không tự publish/unpublish khác previous state. |
| `P03-DOC-013` | P0 | Documents hỗ trợ Folder tối đa hai cấp, Tag và page cha-con. | Không tạo Folder cấp 3/cycle; cross-user relation/cardinality behavior theo `DEC-KNW-013/016/017`. |
| `P03-DOC-014` | P0 | DocumentType là immutable business field sau create. | Direct update, version restore, import và forged payload không thay đổi type. |

Các requirement `P03-KB-001..004` của model Knowledge Base container trước đây bị `DEC-KNW-006` supersede và không được implement.

State transitions:

| From | To | Rule |
|---|---|---|
| Draft | Published | Cho phép; Document vẫn private nếu chưa có share. |
| Published | Draft | Cho phép; share behavior theo `DEC-KNW-018`. |
| Draft hoặc Published | Archived | Cho phép; lưu previous state, chuyển page sang read-only; share behavior theo `DEC-KNW-014`. |
| Archived | Previous Draft/Published | Cho phép qua Unarchive; không được chọn một state khác previous state. |

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
| `P03-ORG-005` | P0 | Archive là non-destructive active-but-hidden state; khác Trash và không vô hiệu access trừ policy. | Archived item tìm được bằng filter; restore/unarchive đúng. |
| `P03-ORG-006` | P0 | Folder hierarchy có tối đa hai cấp và không có cycle. | Root Folder có thể chứa child Folder; child Folder không nhận child Folder khác; move concurrent không tạo cycle/cấp 3. |

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

## 14. Exit criteria

- `DEC-PRD-004`, `DEC-KNW-001..009`, `DEC-KNW-011/012/015` đã Approved; `DEC-PRD-005`, `DEC-KNW-010/013/014/016..018`, `DEC-TEC-006/007` và `DEC-SEC-006` phải đóng cho scope P0.
- Content round-trip, sanitization, conflict, version/lifecycle tests pass.
- Search access/count/facet/highlight negative tests 100% pass.
- Sharing Item/Collection modes và file access pass trên desktop/mobile.
- Dashboard definitions khớp source modules và degraded states pass.
- Personal history/activity/version-conflict scenarios pass.
- Cross-user, revoked-support và disabled-module negative matrix pass.
- Export/migration/restore strategy cho content được chứng minh ở scope P0.
- Không có Critical/High security/data-loss finding mở.
