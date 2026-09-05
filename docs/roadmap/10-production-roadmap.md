# Production Roadmap — Sau Local Stable

**Status:** PLANNED LATER; chưa chọn cloud/VPS/OS/container/hosting, chưa provision/deploy/public website.
**Hard dependency:** [RM18 LOCAL STABLE RELEASE](09-local-stable-release.md) phải đạt và được approve trước RM19.

[Master specifications RM19–RM22](00-master-implementation-roadmap.md) chứa Objective/Input/Tasks/Technical Decisions/Dependencies/Deliverables/DoD/Next Step của từng phase. Tài liệu này bổ sung decision package và evidence.

## 1. Phân biệt production outcome và provider choice

Public SaaS do Product Owner vận hành đã Approved. Local-only không phải sản phẩm cuối cùng. Hosting/provider/cost/region/OS/scale topology chỉ được chọn từ measured local capacity và operational constraints sau RM18. Không mặc định Azure, AWS, VPS, Docker production hosting hoặc Kubernetes.

Local architecture cần giữ replaceable file/key/email/cache/network adapters, externalized configuration, migrations, scoped DB/Redis access, durable jobs và health/observability. Không cần xây cloud-specific services trước.

## 2. RM19 — Production Architecture decision package

| Workstream | Inputs phải có khi phase tương lai bắt đầu | Decision/output |
|---|---|---|
| Capacity | Measured Users/concurrent sessions/records/files/search/job/provider rates | Target profile, growth assumptions, bottlenecks, budget/SLO còn cần PO review |
| Trust/network | Approved personal/sharing/support/emergency and secret controls | Browser/API/worker/SQL/Redis/files/key/provider boundaries; no public data services |
| App topology | Local measured CPU/I/O/worker contention, module dependencies | Single service vs separate worker process nếu cần; không microservices mặc định |
| Persistence | SQL schema/edition features, backups/files/keys, migration chain | SQL host/edition/storage/recovery choices; Redis cache lifecycle; file/key durability |
| Operations | Operator capability/on-call, incidents/restore evidence | Ownership, alert destinations/runbooks, audit/retention/policy |
| Delivery | Artifact/version/schema compatibility, rollback evidence | Environment promotion/migration/rollback design |
| Security | Final attack surface/secret handling/SSRF/browser policy | Threat model update, MFA/recent-auth/headers/recovery controls |
| External transports | Local real Email/Push/provider adapter evidence | Production delivery credentials/domains/rate/abuse and provider-failure plan |

Architecture options chỉ so sánh dựa trên criteria trên khi phase bắt đầu; không cung cấp “provider mặc định tốt nhất” trong task này.

## 3. RM20 — Hosting Selection và detailed deployment design

| Quyết định cần chọn sau RM19 | Review criteria |
|---|---|
| Hosting platform/location | Cost envelope, support/operation effort, latency, region/data constraints, availability/recovery |
| Windows/Linux | .NET/SQL/provider compatibility, operational familiarity, supported editions/features |
| Containers hay native services | Reproducibility, persistent storage, patching, local-to-prod parity; Docker không mặc định |
| Frontend/API/worker/reverse proxy | TLS, proxy headers/cookies/CSRF/CORS, service lifecycle, backpressure và safe health endpoints |
| SQL Server | Edition/license suitability, patch/backup/storage/auth/encryption, restore performance |
| Redis | Private access/TLS/auth, bounded memory/eviction, cache flush/recovery; no irreplaceable state |
| File/object storage/CDN | Authorization-aware downloads, old version references, backup, malware scanning; public CDN không serve private files ngầm |
| Key/secret store | Separation/access/rotation/versioning/authorized recovery; no raw key in source/image/config |
| Domain/DNS/HTTPS | Registration/renewal, cert automation/failure alert, HTTP policy |
| Monitoring/alerts | Metrics/logs/traces collection, retention/redaction, ownership, alert noise and incident response |
| CI/CD | Immutable reproducible artifact, checks, migrations/compatibility, manual approval gates, rollout/rollback |
| Backup/retention | SQL/files/config/key dependency consistency, RPO/RTO/off-site/protection, expiry/deletion constraints |
| Abuse controls | Public registration/resend/recovery/share traffic, query limits, upload quotas, provider spam |
| Policy choices | MFA/recent-auth, audit/export retention, account lifecycle, production capacity/SLO |

Technical recommendation không tự ký budget, acquire services/domains, publish secrets hoặc deploy. Output phải là concrete approved topology/runbook và cost/operations/recovery responsibility matrix trước RM21.

## 4. RM21 — Deployment and qualification plan

Future execution sau explicit deployment authorization:

- Tạo environments/config/data/secrets riêng; no production data ở test, no demo seed ở production.
- Build immutable/versioned artifact trace tới source + dependency manifest; build/test/migration/security/license checks là gate.
- Configure private SQL/Redis/files/keys/jobs; backend/worker restart không mất persistent data; no public admin/DB ports.
- Deploy theo approved migration/compatibility sequence; failed migration safe-disabled; rollback/roll-forward có data-aware plan.
- Bootstrap SuperAdmin securely; register/verify/login bằng production delivery; protect setup replay.
- Configure DNS/TLS/proxy/cookies/CORS/CSRF/security headers/rate limits; never copy TrustServerCertificate local accommodation as production policy.
- Run deployed auth/isolation/share/support/emergency/modules/file/SSRF/redaction/transport suites; topology differences có thể tạo lỗi dù local pass.
- Load/soak/backpressure, restart/outage, backup/key/restore/rollback rehearsals on selected target.
- Independent security review/penetration test theo P08-SEC-008; resolve Critical/High release blockers.
- Monitoring/alert/runbook/tabletop: credential leak/key compromise, failed backup, stuck worker, SQL/Redis/file/provider/TLS failure.

Không publish Public Website vì app vừa boot thành công. RM21 phải tạo evidence; RM22 mới Go/No-Go.

## 5. Production source mapping

| Requirement groups | Phase / evidence |
|---|---|
| P08-DPL-*; DEC-TEC-012 | RM19–RM20 option comparison, topology/trust responsibility, DNS/TLS/secrets/persistence |
| P08-REL-* | RM20–RM21 immutable artifacts, promotion checks, rollback/migrations/module upgrade rehearsal |
| P08-BKP-*; BKP-* | Local baseline RM12/RM17 rồi production RM21 consistent encrypted backup/isolated restore/RPO/RTO/replay controls |
| P08-SEC-*; WEBSEC-*; ADMSEC-* | RM19 threat model then RM21 deployed hardening/independent review |
| P08-CAP-*; PERF-*; CAP-* | RM19 approved target profile; RM21 load/soak/fault evidence |
| P08-OPS-*; OBS-*; LOGSEC-*; IR-* | RM20–RM21 operational stack/alerts/redaction/runbooks/tabletops |
| P08-DAT-* | RM20–RM21 all-module retention/account lifecycle/portability/backup deletion limits |
| SAA-* | RM21–RM22 public endpoint abuse, cross-user isolation, three-channel failure independence and go-live |
| MOD-*; MNT-*; PDS-* | Re-run deployed versions of contract/isolation/revocation/lifecycle tests |

Exact requirement-ID mappings nằm trong 01; table này không thay detailed trace.

## 6. RM22 — Go/No-Go và public release

| Gate | Required evidence |
|---|---|
| Scope | All approved Release 1 modules complete; no unapproved defer; exclusions explicit |
| Security/data | Current role/owner/module/grant checks, encryption/key recovery, vulnerability handling pass |
| Operations | Backup restore proof, monitoring/alerts, incident/support owners và tested runbooks |
| Capacity | Approved SLO/load profile measured; no unsupported availability claim |
| Delivery | Correct artifact/schema, rollback path, provider/email/push/TLS production checks |
| Product | Known limitations reviewed; documentation/training/support/readiness |
| Approval | Product Owner + Technical/Security go-live record with candidate/environment/evidence |
| Post-exposure | Controlled registration/module flows, safe logs and actionable monitoring verified |

No-Go hoãn release, không đổi product model thành local-only. Record blockers/owner/recovery action; chỉ tiếp tục khi conditions được thỏa. Không tạo assumed calendar date cho production trong roadmap hiện tại.

## 7. Sau release

Theo dõi measured errors/latency/provider failures/backup/credential-expiry qua operational controls đã được duyệt; sửa lỗi/upgrade/modules mới qua change control. Không tự thêm billing/AI/team collaboration/native apps/third-party marketplace. Không có deployment, monitoring automation hoặc external message nào được thực hiện trong task lập roadmap này.
