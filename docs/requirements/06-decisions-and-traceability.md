# Decisions, Assumptions and Traceability

**Document ID:** `NX-GOV-001`  
**Version:** `1.1-draft`  
**Status:** Active requirement governance

## 1. Requirement status lifecycle

`Proposed → Reviewed → Approved → Implementing → Verified → Released`

Ngoài luồng chính có `Blocked`, `Deferred`, `Rejected`, `Deprecated`. Chỉ Product Owner (product behavior/scope) hoặc owner được ủy quyền (technical target trong guardrail đã duyệt) mới chuyển requirement thành `Approved`.

## 2. Product decisions đã xác nhận

| ID | Decision | Status | Consequence |
|---|---|---|---|
| `DEC-PRD-015` | Team Workspace và collaboration phải được thiết kế ngay từ đầu. | Approved | Phase 1 phải có Personal/Workspace ownership, membership, roles và isolation. |
| `DEC-PRD-016` | Module mới chỉ do trusted developers phát triển/ship bằng code; Admin/User chỉ enable/configure/use. | Approved | Module Contract/Registry là core; no-code builder và executable upload được defer. |
| `DEC-PRD-017` | Collaboration v1 là bất đồng bộ. | Approved | Assignment, comments, mentions, activity, notifications, versions và conflict detection P0; live cursor/CRDT/OT out. |

## 3. Baseline assumptions cần xác nhận

| ID | Assumption | Tác động nếu sai |
|---|---|---|
| `ASM-001` | Nexora ban đầu dùng bởi một nhóm nhỏ trên môi trường local/private. | Security topology, capacity và onboarding phải đổi nếu public Internet sớm. |
| `ASM-002` | Một account tương ứng một user identity; User có một Personal Space và có thể thuộc nhiều Team Workspace. | Identity federation/multiple identities sẽ cần decision mới nếu phát sinh. |
| `ASM-003` | User tự nhập dữ liệu Finance/Tasks/Knowledge/Vault ở phase đầu. | Import/sync sẽ trở thành critical path nếu manual không đủ. |
| `ASM-004` | External share link là read-only; Workspace members cộng tác/edit/comment bất đồng bộ theo permission. | Realtime co-editing hoặc edit-through-share cần security/concurrency architecture mới. |
| `ASM-005` | GitHub Discovery chỉ dùng public data và không cần GitHub login. | OAuth/token storage/rate limit/privacy scope đổi nếu cần account features. |
| `ASM-006` | “Top weekly popular” nghĩa repository tạo trong tuần hiện tại, sort tổng stars giảm dần. | Ranking/data snapshot thay đổi nếu metric là stars gained. |
| `ASM-007` | Không có AI/LLM feature; AI News chỉ là category. | Architecture/cost/privacy/UX mở rộng nếu policy đổi. |
| `ASM-008` | Phase plan trong bộ tài liệu này là đề xuất, chưa phải release commitment. | Timeline/resourcing cần cập nhật sau prioritization. |

## 4. Open decision backlog

### 3.1 Product decisions

| ID | Decision cần chốt | Chặn phase | Owner đề xuất |
|---|---|---:|---|
| `DEC-PRD-001` | Module P0/P1/P2 cuối cùng và phần nào của candidate catalog bị defer/remove/merge | 0 | Product Owner |
| `DEC-PRD-002` | Tasks–Projects cardinality, status model và recurring task behavior | 2 | Product Owner |
| `DEC-PRD-003` | Calendar recurrence, invitation/attendee/shared calendar scope | 2 | Product Owner |
| `DEC-PRD-004` | Notes, Knowledge Article và Document là resource riêng hay content types trên cùng engine | 3 | Product + Architecture |
| `DEC-PRD-005` | Dashboard widget set và customization level | 3 | Product Owner |
| `DEC-PRD-006` | UI language, locale, timezone default, currency set và first day of week | 1 | Product Owner |
| `DEC-PRD-007` | Finance transfer, split transaction, budget period và debt workflow | 4 | Product Owner |
| `DEC-PRD-008` | Vault sharing/import/export có được phép không | 4 | Product + Security |
| `DEC-PRD-009` | News ingestion: RSS only hay thêm curated/manual sources | 5 | Product Owner |
| `DEC-PRD-010` | Shopee acquisition method và legal/operational constraints | 5 | Product + Legal/Tech |
| `DEC-PRD-011` | Price alert rule: absolute target, percentage drop, lowest-price và cooldown | 5 | Product Owner |
| `DEC-PRD-012` | Developer Toolbox P0 tool list và server-side network tools có được bật không | 6 | Product + Security |
| `DEC-PRD-013` | Automation v1 là scheduler đơn giản hay workflow graph; n8n scope | 6 | Product + Architecture |
| `DEC-PRD-014` | Module nào trong Personal/Digital Assets/Career thực sự committed | 7 | Product Owner |
| `DEC-PRD-018` | Workspace role defaults, ai được tạo Workspace, invite policy và Guest visibility | 0/1 | Product Owner |
| `DEC-PRD-019` | Module nào hỗ trợ Personal, Workspace hoặc cả hai; đặc biệt Finance/Vault | Theo module | Product + Security |
| `DEC-PRD-020` | Member removal: unassign/reassign Tasks, pending approvals và notification history | 1/2 | Product Owner |
| `DEC-PRD-021` | Comment edit/delete window, moderation và reply depth | 2/3 | Product Owner |

### 3.2 Technical/security decisions

| ID | Decision cần chốt | Chặn phase |
|---|---|---:|
| `DEC-TEC-001` | React framework/build tool, routing, state/data-fetching conventions | 1 |
| `DEC-TEC-002` | .NET target version, architecture/module boundaries, API style | 1 |
| `DEC-TEC-003` | SQL engine, ORM/data access, migration ownership | 1 |
| `DEC-TEC-004` | Authentication/session implementation | 1 |
| `DEC-TEC-005` | Redis use cases, fallback behavior và key isolation | 1 |
| `DEC-TEC-006` | File storage provider abstraction và local implementation | 1/3 |
| `DEC-TEC-007` | Search implementation/index consistency strategy | 3 |
| `DEC-TEC-008` | Background job/scheduler engine | 2/5 |
| `DEC-TEC-009` | Notification channel providers | 5/8 |
| `DEC-TEC-010` | Logging/metrics/tracing stack | 1 |
| `DEC-TEC-011` | Backup format/tool, retention, RPO/RTO | 8 |
| `DEC-TEC-012` | Docker/reverse proxy/hosting/cloud/domain/TLS/CDN topology | 8 |
| `DEC-TEC-013` | Module registry/manifest/package boundaries, versioning và migration orchestration | 1 |
| `DEC-TEC-014` | Workspace/Personal ownership representation và query isolation strategy | 1 |
| `DEC-TEC-015` | Optimistic concurrency/conflict response/revalidation strategy cho async collaboration | 1/2 |
| `DEC-SEC-001` | Password hashing scheme và parameter upgrade policy | 1 |
| `DEC-SEC-002` | Secret encryption envelope, key store, versioning và rotation | 4 |
| `DEC-SEC-003` | Admin privileged data scope/reason capture workflow | 1/4 |
| `DEC-SEC-004` | MFA/recent-auth requirements cho Admin/SuperAdmin/Vault | 4/8 |
| `DEC-SEC-005` | Audit retention/integrity/export strategy | 1/8 |
| `DEC-SEC-006` | Upload malware scanning, quotas và unsafe content policy | 3/8 |
| `DEC-SEC-007` | SSRF policy cho HTTP Client, webhook, feed và crawler | 5/6 |
| `DEC-SEC-008` | Workspace Guest/restricted-resource permission và anti-enumeration policy | 1 |
| `DEC-SEC-009` | Invitation token, local delivery và member revocation propagation | 1 |

## 5. Decision record rule

Mỗi decision khi đóng phải ghi: context, considered options, decision, rationale, consequences, security/data/migration impact, owner, date và link tới requirement bị ảnh hưởng. Architecture decision không được thay đổi product behavior đã approved nếu chưa qua change control.

## 6. Traceability chain

Mỗi P0/P1 requirement phải trace được theo chuỗi:

`Goal → Requirement ID → User story/use case → Design/ADR → Work item/PR → Test case → Verification evidence → Release`

Minimum metadata cho work item/PR:

- requirement ID(s);
- decision/ADR link nếu có;
- acceptance criteria được đáp ứng;
- security/data migration impact;
- test evidence và known limitations.

## 7. Change control

1. Người đề xuất nêu requirement hiện tại, thay đổi mong muốn và lý do.
2. Phân tích tác động đến scope/phase, permission, privacy/security, data migration, API/UI, test và docs.
3. Product Owner duyệt behavior/scope; technical/security owner duyệt guardrails tương ứng.
4. Cập nhật requirement/decision ID trước hoặc cùng implementation.
5. Không sửa lịch sử để làm như decision mới luôn tồn tại; ghi version/changelog.

## 8. Definition of Ready cho feature

Feature chỉ sẵn sàng development khi có:

- user/actor và problem statement;
- in-scope/out-of-scope;
- happy path, alternate/error/empty/loading states;
- business rules và state transitions;
- Personal/Workspace ownership, module enablement, membership/permission/share policy;
- data fields, classification, validation, retention và migration impact;
- audit/notification/search/file/job integration;
- measurable acceptance criteria;
- dependencies/decisions/risks đã đóng hoặc có owner/date.

## 9. Definition of Done cho feature/phase

- P0 acceptance criteria pass; P1 defer có approval.
- Code review, build, automated tests và security checks pass.
- Authorization negative tests và cross-user/cross-workspace isolation tests pass.
- Responsive/accessibility/error-state QA hoàn thành.
- Migration, backup/restore impact và rollback path được chứng minh nếu liên quan.
- Logging/audit/notification không lộ sensitive data.
- Docs, API contract, runbook/operational note được cập nhật.
- Known limitations/remaining risk có owner và không mâu thuẫn release gate.

## 10. Initial risk register

| ID | Risk | Mức | Mitigation/decision |
|---|---|---|---|
| `RSK-001` | Catalog quá rộng làm sản phẩm không có usable vertical slice | High | Phase gate, P0 scope cap, defer candidate modules. |
| `RSK-002` | Retrofit ownership/permission gây data leak | Critical | Phase 1 trước business module; mandatory negative tests. |
| `RSK-003` | Vault key management sai làm lộ hoặc mất secret | Critical | Security design/review/rotation/restore rehearsal trước Phase 4 exit. |
| `RSK-004` | Scraping Shopee không ổn định hoặc vi phạm điều kiện provider | High | `DEC-PRD-010`, adapter, rate limit, degraded/manual fallback. |
| `RSK-005` | Search/cache trả stale permission data | High | Query-time access enforcement + invalidation bound tests. |
| `RSK-006` | Automation retry tạo duplicate side effect | High | Idempotency key, run history, retry/compensation requirements. |
| `RSK-007` | Direct network tools gây SSRF | Critical | Disabled-by-default server egress hoặc strict policy; security tests. |
| `RSK-008` | Local deployment bị hiểu nhầm là không cần security | High | Same security baseline; only availability/capacity targets differ. |
| `RSK-009` | Backup có nhưng không restore được hoặc thiếu key/files | Critical | Full inventory + isolated restore rehearsal. |
| `RSK-010` | Duplicate concepts (Read Later, Files, Licenses, Warranty) phân mảnh data | Medium | Boundary decisions trong module catalog. |
| `RSK-011` | Workspace membership/cache/search/job sai scope gây cross-workspace leak | Critical | Space context server-side, query-time checks, revocation bound và mandatory matrix. |
| `RSK-012` | Module disable/upgrade làm mất data hoặc để job/route hoạt động | High | Module lifecycle contract, dependency/migration checks và contract tests. |
| `RSK-013` | Async concurrent edits silent overwrite | High | Optimistic concurrency, version history và conflict-resolution UX. |
| `RSK-014` | System role và Workspace role bị trộn tạo privilege escalation | Critical | Hai role layers độc lập, default deny và bidirectional escalation tests. |

## 11. Review checklist dành cho Product Owner

Ưu tiên xác nhận tiếp: Workspace role/invite/default visibility, module supported Space, member-removal policy, comment moderation, phase order, committed module list, locale/currency, Productivity scope, Knowledge content model, Finance/Vault Workspace support, Shopee/Automation và Phase 7 modules.
