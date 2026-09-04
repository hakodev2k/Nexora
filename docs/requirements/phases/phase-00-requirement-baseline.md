# Phase 0 — Requirement Baseline and Product Discovery

**Phase ID:** `NX-PH-00`  
**Version:** `1.1-draft`  
**Outcome:** Có scope đủ rõ để thiết kế kiến trúc và triển khai Phase 1 mà không phải tự đoán product behavior.  
**Delivery type:** Documentation, review, prototype/spike khi cần; chưa xây business feature production.

## 1. Mục tiêu

- Khóa product charter, terminology và module boundary.
- Khóa Personal Space/Team Workspace ownership, membership và asynchronous-collaboration boundary.
- Khóa developer-built Module Contract, lifecycle và enablement hierarchy.
- Tách `Committed`, `Proposed`, `Deferred`, `Excluded`.
- Xác nhận thứ tự phase và P0/P1/P2 cho release path đầu tiên.
- Định nghĩa user journeys, permission/data scope và cross-cutting contracts.
- Đóng các quyết định chặn Phase 1; tạo owner/deadline cho quyết định phase sau.
- Thiết lập traceability từ requirement tới design, work item và test.

## 2. In scope

1. Product charter và non-goals.
2. Master Module Catalog + boundary map.
3. System role, Workspace role, membership, module enablement và resource data-scope baseline.
4. Data classification, security/privacy threat discovery.
5. Cross-module contracts: Space ownership, collaboration, share, audit, trash, notification, file, search hook, event và job.
6. NFR target/profile và local deployment expectations.
7. Phase roadmap, dependency map, risk register và decision backlog.
8. Low-fidelity navigation/information architecture `PROPOSED` để kiểm tra module grouping.
9. Technical spikes chỉ khi cần loại bỏ rủi ro (authentication, encryption/key storage, Shopee/GitHub rate limit...), không được biến thành production implementation ngầm.

## 3. Out of scope

- Chốt database schema/API/component architecture chi tiết trước khi requirements liên quan được duyệt.
- Xây full UI hoặc business module.
- Cam kết production hosting/provider.
- Bổ sung AI/LLM hoặc commercial SaaS scope.

## 4. Required discovery artifacts

| ID | Pri | Artifact/requirement | Acceptance criteria |
|---|---:|---|---|
| `P00-001` | P0 | Product charter được Product Owner review. | Tầm nhìn, target users, constraints, non-goals và assumptions có trạng thái rõ. |
| `P00-002` | P0 | Module catalog được phân loại và gán phase. | Không còn module “candidate” nằm trong critical path mà thiếu quyết định. |
| `P00-003` | P0 | Boundary decisions cho Files, Notifications, Reminders, Tags/Collections, Read Later, Licenses/Warranty, Activity/Audit, Jobs/Automation. | Mỗi capability có đúng một source of truth và owner. |
| `P00-004` | P0 | Actor/persona và top-level journey map. | Có journey cho bootstrap, Personal Space, Workspace create/invite/join/leave, Workspace module enablement, collaboration, external sharing và privileged access. |
| `P00-005` | P0 | System/Workspace role, module enablement, membership, action và resource-scope model được duyệt. | Trả lời rõ effective access và không trộn System Admin với Workspace Admin. |
| `P00-006` | P0 | Data classification matrix. | Mọi domain biết Private/Sensitive/Secret fields và prohibited sinks. |
| `P00-007` | P0 | NFR measurement profile ban đầu. | Chốt representative local hardware/dataset/browser và target nào là gate. |
| `P00-008` | P0 | Requirement ID/traceability workflow. | PR/work item/test template có chỗ tham chiếu requirement và evidence. |
| `P00-009` | P0 | Phase 1 decisions đóng. | `DEC-TEC-001..005`, `010`, `013..015`, `DEC-PRD-006/018/020`, `DEC-SEC-001/003/008/009` được quyết định hoặc có approved safe default. |
| `P00-010` | P1 | Wireframe/navigation prototype. | Desktop/mobile grouping được user review; không yêu cầu visual polish. |
| `P00-011` | P1 | Initial threat model. | Assets/trust boundaries/mitigations cho identity, cross-workspace, invitations, roles, modules, share, Vault, files và jobs. |
| `P00-012` | P1 | Data lifecycle matrix. | Create/active/archive/trash/purge/export/backup/restore rules cho resource types đã committed. |
| `P00-013` | P0 | Module Platform contract được duyệt. | Manifest, supported Space, enablement, dependency, contributions, migration, disable/upgrade và test kit rõ. |
| `P00-014` | P0 | Workspace/async collaboration specification được duyệt. | Ownership, roles, invite/removal, assignment, comments, mentions, versions/conflicts và external-share boundary rõ. |

## 5. User journey questions phải được trả lời

### 5.1 Bootstrap và onboarding

- Ai chạy first-run setup?
- SuperAdmin đầu tiên được tạo bằng flow nào mà không dùng credential mặc định?
- User được SuperAdmin/Admin tạo, tự đăng ký hay invite? Self-registration mặc định là `TBD`.
- Email có bắt buộc không khi deployment local? Verification/password recovery hoạt động thế nào nếu không có email provider?

### 5.2 Multi-user và administration

- Admin nhìn thấy danh sách user hay cả dữ liệu module?
- Workspace role/default visibility và Guest access cụ thể là gì?
- Ai được tạo Workspace và ai được invite/remove/leave?
- Quyền `module.action` kết hợp `personal/workspace/restricted/all` scope thế nào?
- Privileged access có yêu cầu reason/recent-auth không?
- Disable/delete/remove user xử lý Personal data, Workspace assignments, shares, jobs và sessions thế nào?

### 5.3 Module Platform

- Module nào hỗ trợ Personal, Workspace hoặc cả hai?
- Ai được system-enable, Workspace-enable và assign module?
- Disable module xử lý routes/search/widgets/jobs/events/data thế nào?
- Module migration/dependency/version compatibility và failure recovery thế nào?

### 5.4 Sharing và collaboration

- Module nào được share ở từng phase?
- Password-protected và anyone-with-link có được phép khi host ở public network không?
- Expiration default và maximum duration có cần policy không?
- Comment edit/delete/moderation, mention và assignment rules cụ thể là gì?
- External read-only share phải tách khỏi Workspace edit/comment thế nào?

### 5.5 Content/productivity/finance

- Task/Project/Calendar state và recurrence semantics.
- Notes/Document/Knowledge boundary và editor format.
- Finance transfer/split/currency/budget semantics.
- Vault sharing/export prohibition hay supported flow.

## 6. Prioritization rule

Mỗi feature được chấm tối thiểu theo: user value, dependency leverage, security/data risk, external dependency, delivery size và learning value. P0 phải là scope nhỏ nhất tạo được phase outcome; không đưa “toàn bộ candidate list” vào P0 chỉ vì có trong catalog.

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

Đầu vào bắt buộc: approved charter/catalog, authentication decision, Personal/Workspace ownership, System/Workspace roles, Module Contract, async-collaboration boundary, local setup target, SQL/Redis/file strategy, security controls và Phase 1 acceptance map.
