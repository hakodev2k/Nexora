# Phase 4 — Finance and Vault

**Phase ID:** `NX-PH-04`  
**Version:** `1.2-draft`  
**Outcome:** User quản lý dữ liệu tài chính thủ công và secret cá nhân có thể khôi phục, bằng controls phù hợp với dữ liệu `Sensitive`/`Secret`.  
**Depends on:** Phase 1 personal ownership/Module Platform/security/support access; Phase 3 files/search (metadata only for Vault); approved encryption/key/backup design.

## 1. Release safety gate

Không bắt đầu lưu real Vault secret trước khi `DEC-SEC-002` được duyệt và có automated tests chứng minh encryption, tamper detection, key separation, redaction và restore. Không đóng Phase 4 nếu chỉ chứng minh CRUD mà chưa chứng minh backup + key recovery.

Finance và Vault Release 1 đều Personal-only theo `DEC-PRD-024`:

- Không có team Finance, Team Vault hoặc shared secret.
- Mọi aggregate/export/job phải scope theo owner User + module permission/context.
- Mọi relation Finance/Vault phải cùng owner; generic update không được đổi owner.
- Admin support grant không tự cấp `vault.reveal/copy/export`; sensitive action vẫn cần dedicated permission/control.

## 2. Scope proposal

### Finance P0

- Accounts, opening balance.
- Income, Expense, Transfer (`Transfer` cần `DEC-PRD-007`).
- Categories, transaction list/filter/edit/trash/restore.
- Bills, payments, recurring payments và subscriptions cơ bản.
- Monthly/category/cash-flow summaries và budget cơ bản.
- Manual input; CSV import/export chỉ P1 hoặc sau format decision.

### Finance P1

Split transactions, savings goals, debt/loans, attachments/receipts, recurring templates nâng cao, multi-currency conversion, advanced reports.

### Vault P0

Password, Secure Note, API Key, Token, SSH Credential, Database Credential, Recovery Codes, Software License Secret, Generic Secret; create/view masked/update/delete/restore/reveal/copy; folders/tags metadata; audit; encryption/key rotation readiness.

### Deferred/out

Bank/Open Banking sync, investment/portfolio/tax/accounting, payment initiation, AI categorization, credential autofill/browser extension, secret auto-rotation, public Vault sharing, plaintext bulk export, team vault.

## 3. Finance domain model và rules

### 3.1 Accounts

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-ACC-001` | P0 | User tạo Account với name, type, currency và opening balance/date. | Owner/creator server-side; invalid currency/precision rejected; duplicate name policy rõ. |
| `P04-ACC-002` | P0 | Type baseline: `Cash`, `Bank`, `Wallet`, `Other`; type không tự quyết định external integration. | CRUD/filter consistent; type migration không mất transactions. |
| `P04-ACC-003` | P0 | Current balance được tính từ opening balance + posted transactions theo rule duy nhất. | Ledger test cases khớp UI/report; không tin balance client gửi. |
| `P04-ACC-004` | P0 | Account currency không đổi tùy tiện sau khi có transaction. | Update bị block hoặc có explicit migration workflow được test. |
| `P04-ACC-005` | P0 | Archive Account giữ historical/report data; delete/purge bị block nếu dependencies chưa xử lý. | Không orphan transaction/bill; archived account không nhận input mới ngoài explicit restore. |
| `P04-ACC-006` | P1 | Balance reconciliation/adjustment nếu có dùng explicit transaction type và audit, không sửa history âm thầm. | Before/after/reason preserved. |

### 3.2 Transactions

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-TXN-001` | P0 | Transaction có type, account(s), amount > 0, currency, transaction date, optional category/payee/note/tags. | Money uses decimal; owner/account/category scopes validated. |
| `P04-TXN-002` | P0 | Income làm tăng và Expense làm giảm account balance; sign convention không được thay đổi giữa API/UI/report. | Golden ledger suite pass. |
| `P04-TXN-003` | P0 | Transfer là một logical operation liên kết hai legs, atomic và không tính là income/expense mặc định. | Fault giữa hai legs rollback/compensate; report không double-count. |
| `P04-TXN-004` | P0 | Edit/delete/restore transaction cập nhật balance/report nhất quán và concurrency-safe. | Cache/read model invalidated; repeated delete/restore idempotent. |
| `P04-TXN-005` | P0 | Transaction date là date/time semantics được duyệt; created-at/audit time tách biệt. | Backdated entry xuất hiện đúng period; timezone không đổi calendar date ngoài rule. |
| `P04-TXN-006` | P0 | List/filter theo account/type/category/date/amount/payee và sort/pagination. | Aggregate/filter totals access-scoped và khớp rows. |
| `P04-TXN-007` | P1 | Split transaction có tổng split bằng amount theo rounding rule. | Invalid sum rejected; category reports không double-count. |
| `P04-TXN-008` | P1 | CSV import có mapping/preview/validation/idempotency và duplicate strategy. | Re-import không tạo duplicate ngoài explicit choice; row errors visible. |
| `P04-TXN-009` | P0/P1 | Export có period/filter/currency/schema label, audit và explicit sensitive-data warning. | Export chỉ data actor authorized; no hidden deleted data unless selected. |

### 3.3 Categories

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-CAT-001` | P0 | Category là owned, có Income/Expense/both applicability theo decision. | Invalid type use blocked; normalization consistent. |
| `P04-CAT-002` | P0 | Rename giữ historical relation; delete có replace/uncategorized/block choice. | Transaction không orphan; report history predictable. |
| `P04-CAT-003` | P1 | Hierarchy nếu hỗ trợ có depth/cycle rules và roll-up definition. | No cycles; parent totals không double-count. |

## 4. Bills, Payments, Recurring Payments và Subscriptions

### 4.1 Boundary

- `Bill` là nghĩa vụ cần thanh toán với due date/status.
- `Payment` là link từ Bill/Subscription tới Transaction đã ghi nhận.
- `Recurring Payment`/`Subscription` là schedule/template; không phải Transaction cho đến khi posted/confirmed theo policy.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-BIL-001` | P0 | Bill có title/payee, amount/currency, due date, status, optional category/account/reminder. | Status/amount/date validation; owner scope. |
| `P04-BIL-002` | P0 | Bill state đề xuất: `Upcoming`, `Due`, `Overdue`, `PartiallyPaid` (P1), `Paid`, `Cancelled`. | Derived due/overdue uses user timezone; paid status linked to payments. |
| `P04-BIL-003` | P0 | Mark paid có thể tạo/link Transaction atomically; retry không duplicate. | Bill/payment/transaction remain consistent after fault. |
| `P04-BIL-004` | P0 | Reminder intent phát theo configured lead time; cancel/paid/reschedule invalidates stale job. | No alert after paid/cancelled; retry idempotent. |
| `P04-REC-001` | P0 | Recurring template có amount/currency/category/account/schedule/timezone/start/end. | Month-end/DST/missed-run behavior test. |
| `P04-REC-002` | P0 | Generated occurrence default `PROPOSED` là pending Bill/Transaction draft cần confirm, không âm thầm chuyển tiền. | Duplicate scheduler run creates one occurrence; user can edit instance without corrupting series. |
| `P04-SUB-001` | P0 | Subscription có service/name, cost, billing cycle, next date, payment account optional, status và reminder. | Pause/cancel prevents future reminders/drafts; historical payments retained. |
| `P04-SUB-002` | P1 | Price change history/renewal term/trial end supported nếu approved. | Current vs historical cost and notification rule clear. |

## 5. Budget, savings, debt và reports

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-BUD-001` | P0 | Budget có period, category/allocation, amount/currency và rollover rule explicit. | Actual/spent calculation uses posted Expense only per decision; transfer excluded. |
| `P04-BUD-002` | P0 | Budget progress/remaining/over-budget không silently mix currencies. | Unsupported multi-currency shows separated totals or requires conversion decision. |
| `P04-SAV-001` | P1 | Savings goal có target/current calculation/source accounts/deadline; manual progress vs computed behavior rõ. | No double counting; update history retained. |
| `P04-DEB-001` | P1 | Debt/Loan model có principal, direction, interest/repayment semantics only after dedicated refinement. | Không ship calculator nếu chưa có rounding/schedule tests. |
| `P04-RPT-001` | P0 | Reports tối thiểu: account balance, monthly income/expense, category breakdown, cash flow. | Totals match golden ledger; filters/timezone/currency labeled. |
| `P04-RPT-002` | P0 | Charts có table/text equivalent và không tải unbounded transaction set client-side. | Accessibility/performance tests pass. |
| `P04-RPT-003` | P0 | Report generation/export rechecks owner, module và privileged scope; privileged export audited. | User khác/Admin-no-support cannot infer totals. |

## 6. Vault item model

### 6.1 Common fields

Metadata có thể gồm: title, type, username/account label, URL/host, tags/folder, favorite, notes classification, created/updated. Secret payload theo type được serialize/version/encrypt như một protected envelope. Không đưa plaintext payload vào normal list/search.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-VLT-001` | P0 | User tạo item thuộc allowed type; secret payload encrypted trước persistence. | DB/cache/log/index/backup inspection không có plaintext marker. |
| `P04-VLT-002` | P0 | Type-specific validation không làm lộ secret trong error. | Invalid SSH/DB/token fields return safe field errors; raw value absent logs. |
| `P04-VLT-003` | P0 | List/search chỉ dùng approved metadata và trả masked preview. | API payload never contains encrypted key material/plaintext secret. |
| `P04-VLT-004` | P0 | Detail mặc định vẫn masked; `Reveal` và `Copy` là dedicated endpoints/actions. | `vault.view` alone cannot decrypt; network response contains value only for authorized action. |
| `P04-VLT-005` | P0 | Update protected payload creates new encrypted version/audit metadata; old plaintext not retained accidentally. | Ciphertext version changes; history/backup policy documented. |
| `P04-VLT-006` | P0 | Trash/restore keeps ciphertext decryptable; purge handles versions/attachments/keys per crypto-erasure/data policy. | Restore/reveal works; purge does not leave retrievable active references. |
| `P04-VLT-007` | P0 | Folder/tag/favorite are owned metadata and do not lower secret classification. | Sharing/search/favorites do not expose payload. |
| `P04-VLT-008` | P1 | Password generator nếu có chạy bằng cryptographically secure randomness và không persist generated value trước explicit save. | Statistical/API implementation review; generated marker absent logs/history. |

### 6.2 Type semantics

| Type | Minimum protected fields đề xuất | Notes |
|---|---|---|
| Password | password; optional recovery answers | URL/username may be metadata based on classification decision. |
| Secure Note | note body | Title may remain metadata; user warning about searchable fields. |
| API Key/Token | key/token; optional client secret/refresh token | Expiry/scope/provider can be metadata. |
| SSH Credential | private key/passphrase/password | Public key/fingerprint can be metadata. |
| Database Credential | password/connection secret | Host/database/username classification configurable. |
| Recovery Codes | codes list | Consumed state may be protected metadata/event without code in audit. |
| Software License Secret | license/activation key | Purchase/expiry belongs Digital Assets in Phase 7. |
| Generic Secret | field/value pairs | Field name itself may be sensitive; classification required. |

## 7. Reveal, Copy và privileged access

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-VAC-001` | P0 | Reveal/Copy trong baseline chỉ dành cho owner ở Self context với `vault.reveal/copy` và contextual control phù hợp. | Share/support/emergency context hoặc stale recent-auth bị từ chối; policy privileged Vault khác cần decision riêng. |
| `P04-VAC-002` | P0 | Every reveal/copy attempt success/failure creates redacted audit event. | Actor, target ID, action, outcome present; secret absent. |
| `P04-VAC-003` | P0 | Client masks by default, auto-remasks và không persist plaintext in local/session storage/cache. | Browser storage/network cache inspection pass. |
| `P04-VAC-004` | P0 | SuperAdmin emergency context không tự cấp `vault.reveal` hoặc `vault.copy`; policy xem Vault metadata/secret trong emergency còn phải được chốt riêng. | Ordinary Vault route luôn owner-scoped; chưa có approved Vault emergency policy thì reveal/copy của User khác bị từ chối. |
| `P04-VAC-005` | P0 | Public/anonymous share prohibited; authenticated Vault sharing/export remain disabled until `DEC-PRD-008`. | Share registry rejects type; no bulk plaintext export route. |

## 8. Encryption, keys, rotation và recovery

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P04-CRY-001` | P0 | Cipher envelope contains algorithm/key version/nonce/tag/associated context needed for safe decrypt and migration. | Tamper/swap ciphertext between owner/item/context fails. |
| `P04-CRY-002` | P0 | Key access is least privilege and separate from SQL/file backup artifact. | App without authorized key cannot decrypt; repository/config has no key. |
| `P04-CRY-003` | P0 | Rotation supports old/new key versions, resumable batch và idempotent retry. | Interrupted rotation resumes; all items decrypt; progress/audit has no secret. |
| `P04-CRY-004` | P0 | Backup/restore includes documented key dependency and checksum/integrity verification. | Isolated restore reveals test secret correctly; wrong/missing key fails explicitly, not data overwrite. |
| `P04-CRY-005` | P0 | Key loss/compromise procedures distinguish availability loss and confidentiality incident. | Runbook/tabletop exists; no fake “password reset recovers master key” claim. |

## 9. Search, notification, files và sharing integration

- Finance can index safe metadata such as account/category/payee/transaction note only per classification; result/totals access-scoped.
- Vault indexes only explicitly approved metadata; protected values and secure-note body excluded.
- Bill/subscription reminders use shared scheduler/Notification Center.
- Finance receipts use File Service with Sensitive classification.
- Vault attachments are P1 and require same encryption/access policy as payload, not generic public preview.
- Finance item sharing is `PROPOSED` P1 read-only; Vault public sharing is out.
- Team comments/mentions và group access không thuộc Release 1. External Finance sharing vẫn cần field-level projection; Vault public sharing là out.

## 10. Permissions và audit

Namespaces/actions theo permission matrix. Finance `export`, `purge`; Vault `reveal`, `copy`, `export`, `purge`; support/emergency access, permission/key/security configuration là sensitive.

Audit bắt buộc: privileged Finance view/export, transaction purge/restore/adjustment, import/export, Vault create metadata (không secret), reveal/copy/update/delete/restore/purge/export attempt, key rotation, decrypt/tamper failure, security-setting change, backup/restore.

## 11. Critical edge cases

- Currency decimal/rounding, backdated transactions, transfer across currencies.
- Edit/delete one transfer leg, duplicate payment request, partial recurring job failure.
- Category/account archived/deleted with dependencies.
- Scheduler restart at billing boundary; user timezone changes.
- Ciphertext tamper/swap/version unknown; key unavailable/rotation interrupted.
- Two-tab secret update; reveal then logout/session revoke; browser back/cache.
- Admin permission revoked during export/reveal.
- Backup contains DB but missing files/key, or key exists but wrong environment.
- User/module/support permission bị disable/revoke khi report/export/job đang chạy.
- Cross-user account/category/transaction/file/Vault reference hoặc ciphertext context swap.

## 12. Verification scenarios

1. Golden ledger: income/expense/transfer/edit/delete/restore yields exact balances/reports.
2. Duplicate “mark bill paid” request creates one logical payment/transaction.
3. User A cannot infer User B Finance totals via dashboard/search/export/direct ID; Admin without active grant cannot view them.
4. Secret marker never appears in SQL plaintext, Redis, search, logs, audit, browser storage or unencrypted backup.
5. Ciphertext tamper and item/owner swap fail closed without corrupting data.
6. Interrupted key rotation resumes and every test item remains decryptable.
7. Restore into isolated environment requires correct key and reproduces finance + Vault integrity.
8. SuperAdmin privileged reveal meets re-auth/reason/audit policy; ordinary route cannot bypass.
9. Finance personal-owner policy chặn cross-user account/transaction/report relation và stale Admin support access.
10. Vault Personal-only policy chặn tạo/move/link secret sang User khác; support view không tự reveal/copy/export.

## 13. Exit criteria

- `DEC-PRD-007/008`, `DEC-SEC-002/003/004` và relevant backup decisions closed.
- Finance golden ledger/rounding/currency/report tests pass.
- Vault encryption/redaction/access/rotation/backup-restore tests pass 100%.
- No plaintext Secret in prohibited sink; no Critical/High security finding.
- Mobile/desktop manual-entry, bill/reminder, reveal/copy/trash/restore journeys pass.
- Admin/SuperAdmin privileged access and audit evidence pass.
- Personal ownership của Finance/Vault được khóa; cross-user, revoked-support và module-disabled tests pass.
- Known limits (no bank sync, no public Vault share, currency behavior) visible/documented.
