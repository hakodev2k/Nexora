# Phase 7 — Personal Assets, Digital Assets and Career/Learning

**Phase ID:** `NX-PH-07`  
**Outcome:** User quản lý vòng đời tài sản vật lý/số và hồ sơ nghề nghiệp/học tập bằng các capability đã ổn định: files, reminders, search, sharing, finance links và Vault references.  
**Status:** Toàn bộ domain Phase 7 là `PROPOSED`; phải đóng `DEC-PRD-014` trước khi cam kết scope.

## 1. Scope proposal

### P0 nếu domain được duyệt

- Personal Inventory/Devices/Electronics, purchase info, serial/model, warranty, invoice/accessories.
- Digital Assets: Domains, Hosting, VPS metadata, SSL certificates, Online Services, Software Licenses metadata, expiration tracking.
- Career: Job/Company/Interview Tracker, Resume Manager.
- Learning: Skills, Courses, Certifications; reminder/search/files.

### P1

Depreciation/maintenance, asset relations/components, automatic domain/certificate check, application analytics, resume versions/templates, learning progress, work log reports.

### Out of scope

Remote device control/MDM, password/credential duplication outside Vault, VPS shell/SSH execution, registrar/hosting write operations, automatic job application, candidate scraping, AI resume writing/matching, HR/payroll/time billing.

## 2. Shared boundary rules

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-BND-001` | P0 | Invoice/resume/certificate files use File Service; access follows owning resource. | Direct cross-user download fails; lifecycle relation correct. |
| `P07-BND-002` | P0 | Password/API key/private key/activation secret live in Vault; Asset stores only optional Vault reference. | Asset API/search/export never returns secret value; deleted/revoked reference handled. |
| `P07-BND-003` | P0 | Purchase/payment relation may reference Finance record but does not duplicate/edit Finance ledger implicitly. | Link/unlink leaves transaction unchanged; cross-owner reference blocked. |
| `P07-BND-004` | P0 | Expiry/due events emit Reminder/Notification intents; source module owns the expiry date and rule. | Date edit/cancel invalidates stale queued alert. |
| `P07-BND-005` | P0 | Sharing is read-only and field-selective for resources containing serial/contact/private notes. | Shared view excludes fields not in approved projection. |

## 3. Personal Inventory

### 3.1 Asset model

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-AST-001` | P0 | Asset có owner, name, category/type, manufacturer/model, status, notes và optional image/files. | Owner/private default; invalid type/state rejected. |
| `P07-AST-002` | P0 | Device/Electronics là asset types hoặc subtype đã quyết định, không duplicate record. | Type-specific fields round-trip; migration/change type guarded. |
| `P07-AST-003` | P0 | Serial number/identifier classification là `Sensitive`; masked in list/share/export by policy. | Search/result/log/audit do not expose full value without authorized detail. |
| `P07-AST-004` | P0 | Asset state proposal: `Active`, `Stored`, `Loaned`, `Repair`, `Sold`, `Disposed`, `Lost`, `Archived`. | Transition/history defined; sold/disposed does not delete purchase/warranty evidence. |
| `P07-AST-005` | P0 | Trash/restore/purge preserves or blocks dependencies according to aggregate policy. | Accessories/files/warranty not orphaned or silently purged. |
| `P07-AST-006` | P1 | Asset parent/component/accessory relation prevents cycles and cross-owner links. | Graph/cycle/reparent tests pass. |

### 3.2 Purchase and warranty

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-PUR-001` | P0 | Purchase info includes seller, date, amount/currency, optional order/reference, invoice and Finance transaction link. | Money/date semantics correct; no automatic ledger write. |
| `P07-WAR-001` | P0 | Warranty has provider, start/end or duration, terms/notes/files, status derived/manual policy. | Expiry boundary/timezone correct; missing end supported if lifetime/unknown selected. |
| `P07-WAR-002` | P0 | User configures lead-time reminder; repeated warning/cooldown behavior clear. | Update/renew/cancel suppresses stale reminders; dedupe pass. |
| `P07-WAR-003` | P1 | Repair/claim history records dates, provider, cost/Finance link, result and attachments. | Does not mutate warranty terms silently; history retained. |

## 4. Digital Assets and Infrastructure

### 4.1 Common digital asset

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-DIG-001` | P0 | Digital asset has type, name/provider, URL/identifier, lifecycle status, renewal/expiry, cost/currency optional, contacts/notes classification. | Validation per type; no credential in normal fields. |
| `P07-DIG-002` | P0 | Renewal/expiry reminder supports lead times and auto-renew flag as information only. | Auto-renew flag does not perform payment; message labels action required. |
| `P07-DIG-003` | P0 | Credential/private connection data stored by Vault reference; UI offers authorized jump, never embeds plaintext. | API/search/export/share controls pass. |
| `P07-DIG-004` | P0 | Archive/delete asset does not delete referenced Vault/Finance record. | Link removed or retained per policy; source record untouched. |

### 4.2 Domains

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-DOM-001` | P0 | Domain stores normalized internationalized domain representation, registrar, registered/expiry date, auto-renew info and nameserver notes. | Unicode/punycode/confusable display safe; URL validation correct. |
| `P07-DOM-002` | P1 | Automated WHOIS/RDAP/DNS check only uses approved provider, labels source/fetched-at and does not overwrite manual data without policy. | Provider failure/rate limit degraded; history/source provenance kept. |

### 4.3 Hosting/VPS

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-INF-001` | P0 | Hosting/VPS record stores provider/plan/region/public endpoint/renewal/cost/status and Vault reference. | Private IP/connection string fields classified; no remote execution. |
| `P07-INF-002` | P1 | Availability/expiry monitoring if added uses bounded approved network checks and distinct observed/manual fields. | SSRF policy pass; result timestamp/source/status clear. |

### 4.4 Certificates

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-CER-001` | P0 | Certificate record stores subject/SAN/issuer/serial fingerprint/not-before/not-after/status/source; private key never here. | Parse/time/hostname semantics labeled; secret scan pass. |
| `P07-CER-002` | P0 | Expiration alerts use not-after instant and user lead times. | Boundary/renewal/replacement invalidates stale alerts. |
| `P07-CER-003` | P1 | Remote TLS inspection requires host/port allow policy and cannot access prohibited network ranges. | SSRF/DNS rebinding/timeouts/certificate mismatch tests pass. |

### 4.5 Online Services and Software Licenses

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-LIC-001` | P0 | License metadata includes product/vendor/edition/quantity/purchase/renewal/expiry/device association; key is Vault reference. | Export/share excludes key; delete metadata does not delete Vault item. |
| `P07-SVC-001` | P0 | Online Service tracks plan/account label/contact/billing/renewal/status; password/token remains Vault. | Subscription link to Finance optional and scoped. |

## 5. Career — Job and Company Tracker

### 5.1 Jobs

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-JOB-001` | P0 | Job opportunity has title, company, source URL, location/work mode, employment type, salary text/structured policy, description/notes, discovered/applied dates and status. | URL/content safe; date/status validation; private owner. |
| `P07-JOB-002` | P0 | Status proposal: `Saved`, `Preparing`, `Applied`, `Screening`, `Interviewing`, `Offer`, `Accepted`, `Rejected`, `Withdrawn`, `Closed`. | Valid transitions/history; status timestamp server-side. |
| `P07-JOB-003` | P0 | Job can link resume version, interviews, contacts/notes, reminders and files. | Cross-owner links blocked; delete behavior explicit. |
| `P07-JOB-004` | P0 | List/board filters status/company/date/location/tags and uses access-scoped counts. | Drag status respects transition/concurrency; mobile alternative exists. |
| `P07-JOB-005` | P1 | Application activity/history tracks meaningful events without becoming Security Audit Log. | User can edit notes; privileged access still audited separately. |

### 5.2 Companies and interviews

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-COM-001` | P0 | Company has name, URL, industry/location/notes and related opportunities; duplicate/merge policy clear. | Merge preserves links/history; external content sanitized. |
| `P07-INT-001` | P0 | Interview has job, round/type, start/end/timezone, location/link, participants as user-entered text/contact refs, notes and status. | Calendar/reminder link idempotent; reschedule/cancel invalidates stale event/reminder. |
| `P07-INT-002` | P0 | Interview feedback/private notes excluded from share by default. | Shared job projection does not include hidden fields. |

## 6. Resume Manager

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-RES-001` | P0 | Resume record has name, version, source file/document reference, language, created/updated and status. | File access scoped; version immutable/replace behavior clear. |
| `P07-RES-002` | P0 | Resume sharing uses read-only Sharing Engine with field/file controls and expiration/revoke. | All three share modes pass if enabled; link does not reveal job tracker notes. |
| `P07-RES-003` | P0 | Linking resume to application records exact version used. | Later resume update does not rewrite historical application reference. |
| `P07-RES-004` | P1 | Template/export conversion only after format/fidelity/privacy requirements; no AI generation. | Output matches approved fixture; metadata/hidden content review. |

## 7. Skills, Learning, Courses, Certifications and Work Log

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P07-SKL-001` | P0/P1 | Skill has name/category/proficiency scale/evidence/last used; scale definition consistent and user-controlled. | No computed claim without evidence; duplicate/merge policy. |
| `P07-CRS-001` | P0/P1 | Course has provider/URL, start/end, status, progress rule, notes/files and optional cost/Finance link. | Progress range/rounding validated; completion history kept. |
| `P07-CERF-001` | P0/P1 | Certification has issuer, credential ID/URL, issued/expiry, files and renewal reminder. | Credential ID masked/share policy; expiry alert deduped. |
| `P07-LRN-001` | P1 | Learning plan links goals/tasks/courses without copying their state. | Deleting link leaves source; progress definition transparent. |
| `P07-WRK-001` | P1 | Work Log records date/duration/category/project/employer label/notes with privacy controls; not payroll by default. | Timezone/duration/concurrency/report totals correct. |

## 8. Search, sharing, permissions and audit

- Search safe metadata/content; serials, credential IDs, personal contacts, salary/notes follow field classification.
- Sharing uses explicit projection per resource type, not serialize-entire-entity.
- Namespace: `personal_assets`, `digital_assets`, `career`; action + scope model applies.
- Privileged access/export/purge/share lifecycle/credential-link change/network monitoring configuration audited.
- Normal status/progress/maintenance events can be Activity History.

## 9. Edge cases

- Warranty/certificate/domain has unknown/lifetime expiry; timezone boundary; renewal changes identifier/date.
- Asset sold/disposed while warranty/reminder/Finance/Vault references exist.
- IDN/punycode/confusable domain; invalid certificate chain/clock; provider lookup stale.
- Resume file replaced after application; job/company merge; interview rescheduled across timezone.
- Vault/Finance/file reference deleted/revoked or belongs another user.
- Sharing projection accidentally includes private notes/serial/salary/contact.

## 10. Verification scenarios

1. Asset with invoice, warranty, Finance link and Vault ref survives archive/trash/restore without source corruption.
2. Shared asset/resume omits serial/private note/credential/job-feedback fields by default.
3. Domain/certificate expiry edit cancels stale notification and schedules exact new instant.
4. Job application keeps exact resume version after resume update.
5. Admin action without `access_all` cannot search/export User career/assets.
6. Remote domain/certificate check (if P1) passes SSRF/rate-limit/degraded-state tests.

## 11. Exit criteria

- Product Owner explicitly chooses committed Phase 7 modules; omitted candidates are marked Deferred.
- Boundaries with Vault/Finance/Files/Notifications/Search approved and tested.
- Sensitive field/search/share/export projections pass negative tests.
- Expiry/reminder idempotency/timezone/update tests pass.
- Responsive/accessibility P0 journeys pass; no Critical/High finding remains.
