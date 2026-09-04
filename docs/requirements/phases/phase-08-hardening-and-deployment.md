# Phase 8 — Hardening, Recovery and Deployment Readiness

**Phase ID:** `NX-PH-08`  
**Version:** `1.1-draft`  
**Outcome:** Nexora có bằng chứng về security, reliability, recoverability, operability và upgrade path để Product Owner quyết định có triển khai ngoài môi trường local hay không.  
**Important:** Phase này chuẩn bị và đánh giá; không tự động quyết định cloud/VPS/Kubernetes hoặc thực hiện production deployment.

## 1. Entry criteria

- Scope/module catalog cho target release đã locked.
- Module manifests, dependencies, `supportedSpaces` và system/workspace enablement matrix đã locked.
- Các phase được chọn đã đạt exit criteria hoặc có accepted exception.
- Không còn unresolved Personal/Workspace ownership, membership, permission, collaboration hoặc encryption semantics.
- Data migrations và supported upgrade source version được liệt kê.
- Deployment target candidates, expected users/data/jobs và risk appetite có owner.

## 2. Deployment decision package

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-DPL-001` | P0 | So sánh local-only, private LAN, single VPS, managed cloud/container và other viable topology theo cost, security, operations, recovery, scaling. | Decision record nêu options/trade-offs; không mặc định Kubernetes. |
| `P08-DPL-002` | P0 | Chốt environment topology: frontend/backend/SQL/Redis/files/jobs/reverse proxy/TLS/backup/key store. | Data/trust/network boundary diagram và responsibility matrix approved. |
| `P08-DPL-003` | P0 | Chốt domain/DNS/TLS/certificate renewal và HTTP→HTTPS behavior nếu network-exposed. | Automated TLS/security-header checks; renewal failure alert/runbook. |
| `P08-DPL-004` | P0 | Chốt secret/config injection and rotation per environment; no production secret in image/source. | Image/repo/config scan pass; rotate rehearsal for representative secret. |
| `P08-DPL-005` | P0 | Chốt persistence/volume/managed-service lifecycle; app restart/redeploy không mất data. | Redeploy/restart test preserves SQL/files/keys and handles Redis loss. |
| `P08-DPL-006` | P1 | Zero/minimal downtime requirement chỉ được cam kết nếu target cần; migration strategy tương ứng. | Measured deployment rehearsal meets approved target. |

## 3. Environment and release management

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-REL-001` | P0 | Tách Development/Test/Staging/Production-equivalent config/data/secrets theo selected topology. | Không dùng production data/secret ở test; connection guard prevents wrong target. |
| `P08-REL-002` | P0 | Build artifact/image immutable, versioned, reproducible và trace tới commit/dependency manifest. | Same source/config build yields equivalent artifact; version visible. |
| `P08-REL-003` | P0 | Release pipeline gates build, tests, migrations check, vulnerability/secret/license scans và approval. | Failed gate blocks promotion; evidence retained. |
| `P08-REL-004` | P0 | Rollback/roll-forward strategy accounts for database schema and irreversible data migration. | Rehearsal from target previous version; no unsafe app/schema mismatch. |
| `P08-REL-005` | P0 | Feature/module enablement has dependency validation and safe default. | Cannot enable module without migrations/config; disable doesn't corrupt data. |
| `P08-REL-006` | P1 | Release notes include breaking behavior, migration, security changes, known limits and rollback notes. | Operator can identify required action before deploy. |
| `P08-REL-007` | P0 | Module install/upgrade/disable/uninstall orchestration kiểm tra manifest version, dependencies, migrations, contributions, active jobs và rollback compatibility. | Contract/migration rehearsal không mất module data; stale contribution/route/job không còn executable. |

## 4. Backup and restore

### 4.1 Decisions

`DEC-TEC-011` must define RPO, RTO, frequency, retention, encryption, location, immutability/off-device need, key escrow/recovery and deletion.

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-BKP-001` | P0 | Backup is consistent across SQL, File Storage, configuration metadata and encryption-key dependency. | Restore point does not reference missing file/key beyond documented tolerance. |
| `P08-BKP-002` | P0 | Backup encrypted/integrity-protected with access/audit/retention controls. | Unauthorized reader cannot inspect; corruption detected before restore. |
| `P08-BKP-003` | P0 | Automated backup job reports start/outcome/size/duration/restore-point ID and alerts final failure. | Simulated destination/quota/network failure visible and retried within policy. |
| `P08-BKP-004` | P0 | Restore targets explicit isolated environment by default and requires privileged confirmation. | Wrong-environment/overwrite guard test pass; event audited. |
| `P08-BKP-005` | P0 | Full restore rehearsal validates login, Spaces/memberships, module enablement, ownership, comments/activity, files, search rebuild, jobs, Finance ledger and Vault decrypt. | Approved checklist passes within RTO; data point meets RPO. |
| `P08-BKP-006` | P0 | Restore/rebuild does not replay historical notifications/webhooks/jobs unintentionally. | Queued/run state reconciliation tests pass. |
| `P08-BKP-007` | P1 | Periodic restore verification and backup expiry/deletion are automated with safe reports. | At least one scheduled rehearsal/verification cadence approved. |

## 5. Security hardening

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-SEC-001` | P0 | Threat model refreshed against final topology/module scope. | Every material trust boundary/threat has mitigation/test/risk owner. |
| `P08-SEC-002` | P0 | MFA/recent-auth/admin access policy closed and enabled as required for exposure level. | Privileged flow/recovery/revocation tests pass. |
| `P08-SEC-003` | P0 | External attack surface minimized; DB/Redis/files/job admin endpoints not publicly exposed unless explicitly protected. | Network scan/config review matches topology. |
| `P08-SEC-004` | P0 | TLS, cookies, CSRF, CORS, CSP/security headers, rate limits and error handling configured for deployment. | Automated DAST/config suite pass; exception approved. |
| `P08-SEC-005` | P0 | Dependency/container/OS patch policy and vulnerability response SLA defined. | No unresolved Critical/High release-blocking issue. |
| `P08-SEC-006` | P0 | Key/credential rotation, compromise and user/session revocation rehearsed. | Runbook execution produces expected audit/access outcomes. |
| `P08-SEC-007` | P0 | SSRF/upload/rich-content/webhook/integration security suites run against deployed topology. | Network/storage/proxy differences do not bypass controls. |
| `P08-SEC-008` | P1 | Independent security review/penetration test for Internet exposure is `PROPOSED`. | Findings triaged; Critical/High closed before go-live. |

## 6. Performance and capacity qualification

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-CAP-001` | P0 | Production-like capacity profile states users, Workspaces/members, concurrent sessions, records/module, comments/activity, file size/volume, search index, schedules/webhooks and external rates. | Load dataset/script versioned; no vague “fast enough”. |
| `P08-CAP-002` | P0 | Load/soak/spike tests verify approved latency/error/resource budgets and no cross-user/cross-workspace data leak. | Report includes percentiles, saturation, bottleneck and pass/fail. |
| `P08-CAP-003` | P0 | Job concurrency/backpressure prevents background work starving interactive traffic. | Price/feed/search/backup burst scenario remains within approved bounds. |
| `P08-CAP-004` | P0 | SQL/index/query/cache tuning based on measured workload; Redis loss/restart behavior tested. | No unbounded query/N+1 on P0 flows; cache rebuild safe. |
| `P08-CAP-005` | P1 | Scaling trigger/runbook defined if target requires growth. | Operator knows metric/threshold/action and stateful dependency constraint. |

## 7. Observability and operations

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-OPS-001` | P0 | Metrics/logs/traces/health cover frontend/backend/SQL/Redis/files/jobs/integrations/reverse proxy. | Known failure can be localized from alert to correlation/run ID. |
| `P08-OPS-002` | P0 | Alert set covers authentication/security anomalies, error/latency/resource saturation, job/provider failure, backup/restore, storage/key/certificate expiry. | Each alert has threshold, owner, severity, destination and runbook. |
| `P08-OPS-003` | P0 | Logs/telemetry retention, access and redaction validated with production config. | Marker secrets/private samples absent; authorized operator access only. |
| `P08-OPS-004` | P0 | Runbooks cover deploy/rollback, restart, migration failure, DB/Redis/file outage, provider rate limit, stuck jobs, key issue, backup restore, account recovery. | Tabletop/game-day executes priority runbooks. |
| `P08-OPS-005` | P0 | SLO/availability only published after measurement and ownership; local-only mode may use operational objectives instead. | No unsupported SLA claim. |

## 8. Data lifecycle, privacy and portability

| ID | Pri | Requirement | Acceptance criteria |
|---|---:|---|---|
| `P08-DAT-001` | P0 | Retention/purge rules defined for active/trash/audit/activity/notifications/jobs/search cache/backups/files/import/export artifacts. | Automated jobs/dry-run reports match policy; legal/operational exceptions documented. |
| `P08-DAT-002` | P0 | User/Workspace/member disable/delete/leave/transfer/export workflow covers every released module and external side effect. | Data inventory reconciliation leaves no orphan resource/comment/share/job/secret/file. |
| `P08-DAT-003` | P0 | Export schemas/versioning and sensitive-field controls documented; generated artifact expires/cleans safely. | Cross-user/cross-workspace/privacy tests pass; download token bounded. |
| `P08-DAT-004` | P0 | Permanent delete versus backup retention limitation is disclosed and implemented per approved policy. | UI/docs do not promise immediate erasure from immutable backup if untrue. |

## 9. Upgrade and disaster scenarios

Mandatory rehearsals:

1. Clean install and first SuperAdmin bootstrap.
2. Upgrade from oldest supported version through all migrations.
3. Failed migration with safe rollback/restore.
4. SQL unavailable/slow; Redis wiped/unavailable; File Storage partially unavailable.
5. Job worker crash mid-side-effect; duplicate/replayed webhook/trigger.
6. Search index lost and rebuilt without permission leak.
7. Encryption key rotation interrupted; old key retired only after proof.
8. Full environment loss and restore from backup within approved RPO/RTO.
9. Credential compromise/session revoke/security incident tabletop.
10. TLS certificate/provider outage/rate-limit and alert/runbook execution.
11. Member removal/role downgrade while editing, mentioned, assigned hoặc automation queued.
12. Workspace archive/delete/restore với resources, comments, files, shares, jobs và module settings.
13. Module upgrade/disable/uninstall với migrations, search/dashboard contributions và queued jobs.
14. Concurrent async edits verify conflict response; không yêu cầu live cursor/CRDT.

## 10. Go-live decision checklist

Product Owner and technical/security owner must explicitly decide:

- target users/network exposure and whether production deployment proceeds;
- accepted/deferred modules and known limitations;
- Workspace/collaboration limits và module enablement/rollback matrix;
- capacity/SLO/RPO/RTO/cost/operations ownership;
- authentication/MFA/recovery/admin access policy;
- backup/key escrow and incident contacts;
- outstanding risks/exceptions with expiry and owner;
- rollback/disable path.

## 11. Exit criteria

Phase 8 is complete when:

- selected topology decision and responsibility matrix are approved;
- clean deploy/upgrade/rollback/full restore/security incident rehearsals pass;
- performance/capacity and observability evidence meet approved targets;
- Critical/High security/reliability/data-loss findings are closed;
- runbooks, alerts, retention, backup and key rotation are operational;
- cross-workspace, membership-revocation, async-conflict và module-lifecycle suites pass;
- Product Owner records `Go`, `No-Go` hoặc `Remain Local` — cả ba đều là kết quả hợp lệ nếu có rationale.
