# Phase 3 — Knowledge, Documents, Global Search and Dashboard

**Phase ID:** `NX-PH-03`  
**Outcome:** User lưu, tổ chức, chia sẻ read-only và tìm lại tri thức/tài liệu của mình; Dashboard tổng hợp dữ liệu đã phát hành mà không tạo source of truth mới.  
**Depends on:** Phase 1 platform services; Phase 2 search projections/calendar/task read contracts.

## 1. Scope proposal

### P0

- Notes, Knowledge Base Articles, Documents sau khi đóng content-model decision.
- Files/attachments, Bookmarks, Snippets.
- Tags, typed Collections, Archive và basic version history.
- Unified Read Later cho URL/news/bookmark-compatible resources.
- Basic Global Search xuyên module đã hỗ trợ, có filters và access enforcement.
- Dashboard/Home với Today, recent content, counts/alerts và quick actions tối thiểu.
- Item/Collection read-only sharing cho resource được duyệt.

### P1

- Templates, saved/advanced search, favorites, recent items/history, command palette.
- Full document version comparison/restore, export formats bổ sung, customizable widgets.

### Deferred/out

Real-time co-editing, comments/approval workflow, public publishing site, AI summarization/semantic search, OCR, Office/Google Docs bidirectional editing, arbitrary executable snippets.

## 2. Content model decision gate

`DEC-PRD-004` phải chọn một trong các hướng trước implementation:

1. `Note`, `KnowledgeArticle`, `Document` là resource/domain model riêng; hoặc
2. một `ContentItem` chung với typed behavior/policy.

Bất kể implementation, UX semantics tối thiểu:

- **Note:** capture nhanh, cấu trúc nhẹ, private mặc định.
- **Knowledge Article:** nội dung được tổ chức trong KB/category/collection; có lifecycle/versioning/search.
- **Document:** nội dung dài/formal hơn, attachment/export/share/versioning.

Migration giữa type, nếu hỗ trợ, phải explicit và không mất version/share/link.

## 3. Notes, Knowledge Articles và Documents

### 3.1 Common content requirements

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-CNT-001` | P0 | User tạo content với title/body theo type; owner/private default server-side. | Invalid length/format rejected; User B cannot access direct ID. |
| `P03-CNT-002` | P0 | Storage/editor format phải versioned, documented và không khóa export/migration. | Round-trip content giữ semantic; unsupported construct được cảnh báo. |
| `P03-CNT-003` | P0 | Rich content từ user/external source được sanitize/encode; embedded URL/media theo allowlist. | XSS corpus không chạy ở editor, preview, share view hoặc search highlight. |
| `P03-CNT-004` | P0 | Autosave/manual save behavior, dirty-state và conflict handling rõ. | Navigation/session expiry không âm thầm mất content; two-tab conflict được detect. |
| `P03-CNT-005` | P0 | Content hỗ trợ tags, attachments, archive, trash/restore và search projection theo capability. | Lifecycle giữ owner/links hợp lệ; archived khác trashed. |
| `P03-CNT-006` | P0 | View/update timestamps và author/editor metadata không thể spoof bởi client. | Server authoritative; timezone display đúng. |
| `P03-CNT-007` | P0 | Read-only share hiển thị chỉ field được policy cho phép, không lộ internal metadata/history mặc định. | Anonymous/authenticated/password share matrix pass. |
| `P03-CNT-008` | P1 | Export format tối thiểu được quyết định theo type; export preserve encoding and attachments manifest. | Schema/round-trip test; access/audit pass. |

### 3.2 Knowledge Base

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-KB-001` | P0 | User có một hoặc nhiều Knowledge Base/space theo decision; Article thuộc đúng một KB. | Cross-owner assignment bị chặn; delete KB có child policy explicit. |
| `P03-KB-002` | P0 | Article có organizational metadata: category/collection/tags theo model đã chọn. | Move/category delete không orphan; duplicate names xử lý rõ. |
| `P03-KB-003` | P0/P1 | Article lifecycle `Draft/Published/Archived` chỉ tồn tại nếu publish semantics được duyệt. | `knowledge.publish` tách khỏi update; shared viewer thấy version/state đúng. |
| `P03-KB-004` | P0 | KB navigation hỗ trợ browse/search/filter trên desktop/mobile. | Large tree/list paginated/lazy; keyboard accessible. |

### 3.3 Documents

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-DOC-001` | P0 | Document Sharing là capability confirmed và dùng Sharing Engine, không custom token system. | Item/Collection share lifecycle/expiration/revoke tests pass. |
| `P03-DOC-002` | P0 | Document version metadata lưu version number, author, timestamp và change note optional. | Concurrent saves không tạo duplicate/gap bất hợp lý; history scoped. |
| `P03-DOC-003` | P1 | Restore version tạo current version mới, không xóa lịch sử. | Restore auditable; old share policy không bị reset/bypass. |
| `P03-DOC-004` | P1 | Compare version nếu có phải render untrusted content an toàn. | Diff view sanitized; large content bounded. |

## 4. Versioning

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-VER-001` | P0 | Version creation policy (`every save`, interval, meaningful save) được quyết định và consistent. | Automated test chứng minh expected version count. |
| `P03-VER-002` | P0 | Historical version immutable với business user; purge theo retention riêng. | No update endpoint; restore tạo version mới. |
| `P03-VER-003` | P0 | Version không copy secret/external embedded credential vào audit/log/search. | Redaction/classification test pass. |
| `P03-VER-004` | P1 | Retention/max versions/compaction không được làm mất version đang cần cho active share/legal requirement đã duyệt. | Purge preview và referential checks pass. |

## 5. Files và attachments

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-FIL-001` | P0 | Standalone file và attachment đều dùng File Service; resource relation khai báo owner/access. | Không duplicate binary khi reuse được policy cho phép; không cross-user reuse. |
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
| `P03-RDL-002` | P0 | Read state, added/read timestamps và remove/archive behavior riêng theo user. | User B activity không ảnh hưởng User A; source delete có fallback rõ. |

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
| `P03-ORG-001` | P0 | Tag belongs to user; normalized name/color optional; relation typed. | Cross-user tags blocked; rename propagates without rewriting content. |
| `P03-ORG-002` | P0 | Collection có owner, type và supported resource types; item relation không thay đổi ownership. | Invalid type mix bị chặn; collection share composition rõ. |
| `P03-ORG-003` | P0 | Share Collection snapshot/live behavior phải quyết định; default `PROPOSED` là live membership subject to access. | Add/remove item reflected đúng; private/unsupported item không leak. |
| `P03-ORG-004` | P1 | Template tạo copy mới với owner mới/current user; không giữ share/secret/history của source. | Instantiate idempotency/field reset tests pass. |
| `P03-ORG-005` | P0 | Archive là non-destructive active-but-hidden state; khác Trash và không vô hiệu access trừ policy. | Archived item tìm được bằng filter; restore/unarchive đúng. |

## 9. Global Search

### 9.1 Result and access semantics

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-SRC-001` | P0 | Search xuyên Task, Project, Event, Note/Article/Document, Bookmark, Snippet, File metadata và resource đã đăng ký. | Unsupported module không làm query fail; result type/label/link rõ. |
| `P03-SRC-002` | P0 | Query, filters, pagination/cursor và deterministic tie-break được định nghĩa. | Repeated page không missing/duplicate dưới stable dataset. |
| `P03-SRC-003` | P0 | Search chỉ trả resource actor được phép xem tại query time. | Owner/share/admin/revoke/trash matrix 100% pass; count/facet không leak. |
| `P03-SRC-004` | P0 | Result chứa safe title/snippet/highlight; không chứa Secret hoặc hidden field. | XSS/redaction tests; Vault plaintext never indexed. |
| `P03-SRC-005` | P0 | Index/update/delete có consistency target và repair/reindex path. | Create/update visible trong bound; delete/revoke blocked immediately by access check; reindex idempotent. |
| `P03-SRC-006` | P0 | Filters tối thiểu: resource type, date range, tags; module-specific filters optional. | Filter combination and no-result state correct. |
| `P03-SRC-007` | P1 | Ranking strategy documented; exact/title/recent relevance behavior không phụ thuộc personalized AI. | Golden query suite versioned và pass threshold. |
| `P03-SRC-008` | P1 | Saved Search lưu query/filter cho owner, không snapshot unauthorized results. | Permission change affects results; share of saved search absent unless specified. |
| `P03-SRC-009` | P1 | Search history/favorites/recent items là private user data với clear/delete control. | Cross-user isolation and retention pass. |

### 9.2 Command Palette

P1 Command Palette có thể search navigation/allowed actions và data results. Nó không được bypass permission, auto-execute destructive action hoặc expose hidden admin routes.

## 10. Dashboard/Home

### P0 widget proposal

- Today: Tasks, Events, Reminders.
- Overdue/Upcoming counts.
- Recent Knowledge/Documents.
- Unread Notifications.
- Quick actions: new Task, Event, Note/Document.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P03-DSH-001` | P0 | Dashboard chỉ dùng read contracts/source modules; không duplicate business state. | Edit từ quick action gọi module workflow; refresh phản ánh source. |
| `P03-DSH-002` | P0 | Mỗi widget access-scoped và fail độc lập. | Widget provider fail hiển thị degraded state; widget khác vẫn usable. |
| `P03-DSH-003` | P0 | Time-sensitive widget dùng cùng timezone/business definitions với source module. | Today/overdue count khớp Task/Calendar list. |
| `P03-DSH-004` | P0 | Responsive order/priority giữ alerts/actions quan trọng accessible trên mobile. | No horizontal overflow; keyboard/screen-reader labels. |
| `P03-DSH-005` | P1 | User có thể hide/reorder selected widgets; config per-user, validated. | Unknown/disabled widget ignored safely; no permission bypass. |
| `P03-DSH-006` | P1 | Chart có accessible text/table equivalent và source/time range label. | No misleading stale/unlabeled aggregate. |

## 11. Permissions và audit

- Namespaces: `knowledge`, `documents`, `files`, `search`, `sharing` và relevant actions trong matrix.
- User owner policy áp dụng list/detail/version/file/search/share.
- Admin `view` không tự cấp `access_all`; privileged content/history/export access được audit.
- Audit bắt buộc: share lifecycle/access theo policy, permanent delete, export, restore version, admin access, unsafe upload/network rejection quan trọng.
- Normal edit/version creation có thể là Activity History; không log content body.

## 12. Edge cases

- Large document, rapid autosaves, concurrent tabs và network interruption.
- Malicious rich text/Markdown/link preview/file MIME.
- Shared collection membership thay đổi; item trash/revoke/ownership transfer.
- Search index stale hoặc full rebuild; Redis/search unavailable.
- Rename/move/delete tag/category/collection có references.
- File missing/corrupt/quarantined; attachment referenced bởi nhiều resources.
- External URL redirect loop, huge response, private IP hoặc credential in URL.
- Dashboard source module disabled/permission revoked/timeout.

## 13. Phase verification scenarios

1. User tạo Article có malicious markup; editor/share/search highlight đều không chạy script.
2. User share Document bằng cả ba mode, expiry/revoke/trash chặn đúng.
3. Admin thiếu `access_all` không tìm thấy/export/view version content User khác.
4. Reindex sau crash tạo cùng logical index và không đưa Secret vào index.
5. Bookmark metadata fetch bị redirect tới loopback và bị chặn; bookmark manual vẫn được lưu.
6. Restore document version tạo version mới, không thay share permission.
7. Dashboard widget fail/timeout nhưng shell và widget khác hoạt động.

## 14. Exit criteria

- `DEC-PRD-004/005`, `DEC-TEC-006/007`, `DEC-SEC-006` đóng cho scope P0.
- Content round-trip, sanitization, conflict, version/lifecycle tests pass.
- Search access/count/facet/highlight negative tests 100% pass.
- Sharing Item/Collection modes và file access pass trên desktop/mobile.
- Dashboard definitions khớp source modules và degraded states pass.
- Export/migration/restore strategy cho content được chứng minh ở scope P0.
- Không có Critical/High security/data-loss finding mở.
