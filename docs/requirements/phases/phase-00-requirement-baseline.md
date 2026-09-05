# Phase 0 — Requirement Baseline and Product Discovery

**Phase ID:** `NX-PH-00`  
**Version:** `1.2-draft`  
**Outcome:** Có scope đủ rõ để thiết kế kiến trúc và triển khai Phase 1 mà không phải tự đoán product behavior.  
**Delivery type:** Documentation, review, prototype/spike khi cần; chưa xây business feature production.

## 1. Mục tiêu

- Khóa product charter, terminology và module boundary.
- Khóa personal ownership, cross-user isolation, external sharing và support/emergency-access boundary.
- Khóa developer-built Module Contract, lifecycle và enablement hierarchy.
- Tách `Committed`, `Proposed`, `Deferred`, `Excluded`.
- Xác nhận milestone/dependency order cho toàn bộ committed Release 1 catalog.
- Định nghĩa user journeys, permission/data scope và cross-cutting contracts.
- Đóng các quyết định chặn Phase 1; tạo owner/deadline cho quyết định phase sau.
- Thiết lập traceability từ requirement tới design, work item và test.

## 2. In scope

1. Product charter và non-goals.
2. Master Module Catalog + boundary map.
3. System roles, User module enablement, personal ownership, share/support/emergency data-scope baseline.
4. Data classification, security/privacy threat discovery.
5. Cross-module contracts: personal ownership, share/support, audit, trash, notification, file, search hook, event và job.
6. NFR target/profile, local development và Public SaaS production-readiness expectations.
7. Phase roadmap, dependency map, risk register và decision backlog.
8. Low-fidelity navigation/information architecture `PROPOSED` để kiểm tra module grouping.
9. Technical spikes chỉ khi cần loại bỏ rủi ro (authentication, encryption/key storage, Shopee/GitHub rate limit...), không được biến thành production implementation ngầm.

## 3. Out of scope

- Chốt database schema/API/component architecture chi tiết trước khi requirements liên quan được duyệt.
- Xây full UI hoặc business module.
- Chọn production hosting/provider trước khi architecture/options được đánh giá; Public SaaS outcome vẫn là committed.
- Bổ sung AI/LLM, billing/paid-plan hoặc team Workspace scope.

## 4. Required discovery artifacts

| ID | Pri | Artifact/requirement | Acceptance criteria |
|---|---:|---|---|
| `P00-001` | P0 | Product charter được Product Owner review. | Tầm nhìn, target users, constraints, non-goals và assumptions có trạng thái rõ. |
| `P00-002` | P0 | Module catalog được phân loại và gán phase. | Không còn module “candidate” nằm trong critical path mà thiếu quyết định. |
| `P00-003` | P0 | Boundary decisions cho Files, Notifications, Reminders, Tags/Collections, Read Later, Licenses/Warranty, Activity/Audit, Jobs/Automation. | Mỗi capability có đúng một source of truth và owner. |
| `P00-004` | P0 | Actor/persona và top-level journey map. | Có journey cho registration/email verification, Self data, module enablement, external sharing, User-granted support và emergency access. |
| `P00-005` | P0 | System role, User module enablement, action và owner/share/support/emergency scope model được duyệt. | Trả lời rõ effective access; Admin action không tự cấp private User data. |
| `P00-006` | P0 | Data classification matrix. | Mọi domain biết Private/Sensitive/Secret fields và prohibited sinks. |
| `P00-007` | P0 | NFR measurement profile ban đầu. | Chốt representative local-dev và production-like Public SaaS dataset/browser/network profile. |
| `P00-008` | P0 | Requirement ID/traceability workflow. | PR/work item/test template có chỗ tham chiếu requirement và evidence. |
| `P00-009` | P0 | Phase 1 decisions đóng. | `DEC-TEC-001..005`, `008..010`, `013..015`, `DEC-PRD-006/034`, `DEC-SEC-001/003/008/009` được quyết định hoặc có approved safe default. |
| `P00-010` | P1 | Wireframe/navigation prototype. | Desktop/mobile grouping được user review; không yêu cầu visual polish. |
| `P00-011` | P1 | Initial threat model. | Assets/trust boundaries/mitigations cho public registration, cross-user data, roles, modules, share/support/emergency, Vault, files, email/push và jobs. |
| `P00-012` | P1 | Data lifecycle matrix. | Create/active/archive/trash/purge/export/backup/restore rules cho resource types đã committed. |
| `P00-013` | P0 | Module Platform contract được duyệt. | Manifest, personal ownership, System/User enablement, Admin grants, dependencies, migrations, disable/upgrade và test kit rõ. |
| `P00-014` | P0 | Personal data/sharing/support specification được duyệt. | Owner isolation, three share modes, one-module support grant và emergency access rõ. |

## 5. User journey questions phải được trả lời

### 5.1 Bootstrap và onboarding

- Ai chạy deployment/bootstrap setup?
- SuperAdmin đầu tiên được tạo bằng flow nào mà không dùng credential mặc định?
- Public self-registration và email verification đã Approved; cần chốt verification token lifetime, resend/rate limits và recovery provider behavior.
- Email provider outage/degraded mode và bootstrap SuperAdmin flow hoạt động thế nào?

### 5.2 Multi-user và administration

- Admin nhìn thấy danh sách User metadata nào theo permission?
- Quyền `module.action` kết hợp `self/shared-link/support-grant/emergency` scope thế nào?
- Support grant có cho xem Trash/history hay Vault metadata không?
- Disable/delete User xử lý Personal data, shares, reminders, jobs, notifications và sessions thế nào?

### 5.3 Module Platform

- Mọi module Release 1 là Personal; SuperAdmin system-enable/User-enable và grant module/action cho Admin.
- Registration Default thay đổi áp dụng cho User mới hay cả User hiện tại?
- Disable module xử lý routes/search/widgets/jobs/events/data thế nào?
- Module migration/dependency/version compatibility và failure recovery thế nào?

### 5.4 Sharing và privileged support

- Module nào được share ở từng phase?
- Expiration default/maximum của share link và authenticated viewer policy có cần khác theo module không?
- Expiration default và maximum duration có cần policy không?
- Existing share xử lý thế nào khi SuperAdmin tắt sharing của module?
- Support access có thấy Trash/history không; Vault support có bị cấm hay chỉ metadata-safe?

### 5.5 Content/productivity/finance

- Project/Task/Calendar đã được chốt tại Phase 2; còn task subtasks/attachments/recurrence và remaining Productivity modules.
- Documents mặc định Grid; Grid/Table hiển thị Title, DocumentType, Tag; lọc DocumentType/Tag/ngày tạo, tìm Title/Tag và sắp xếp cập nhật mới nhất trước. Còn chốt navigation/Archived visibility, cover format/size và Tag reference trong Trash/version history.
- Finance transfer/split/currency/budget semantics.
- Vault sharing/export prohibition hay supported flow.

## 6. Prioritization rule

Mỗi feature vẫn được sắp theo dependency, security/data risk, delivery size và learning value để tạo milestone nội bộ. Tuy nhiên `DEC-PRD-025` đã committed toàn bộ module có requirement vào Release 1; prioritization không được âm thầm đổi module thành Deferred hoặc placeholder.

## 7. Review cadence và approvals

- Product review: scope, behavior, priority, wording và acceptance.
- Security review: identity, permission, sensitive/secret data, sharing, network integrations.
- Architecture review: feasibility, boundary, migrations, NFR, operational risks.
- Mỗi review kết thúc bằng approved changes hoặc decision IDs; không chỉ ghi meeting notes.

## 8. Exit criteria

Phase 0 hoàn thành khi:

- `P00-001` đến `P00-009`, `P00-013` và `P00-014` được duyệt;
- P0 scope Phase 1 không còn product decision mở;
- privacy/authorization model có negative test matrix;
- NFR/security release gates và Definition of Done được chấp nhận;
- các phase sau có decision backlog, không cần đóng toàn bộ;
- tài liệu versioned và có reviewer/approval record.

## 9. Handoff sang Phase 1

Đầu vào bắt buộc: approved charter/catalog, authentication/email decision, personal ownership, System roles, Module Contract, share/support/emergency boundary, local-dev + Public SaaS target, SQL/Redis/file/email/push strategy, security controls và Phase 1 acceptance map.
