# M01 readiness and evidence gates

## Current status

> **Authority update:** The M01-only approval below is retained for historical traceability. DEC-20260909-014 is the current authority for local R1 implementation beyond M01; it does not turn any slice into runtime-verified or production-ready. This code-only run adds no tests/fixtures; owner QA must produce runtime evidence.

M01 + backend/frontend scaffold + local scripts are approved for implementation by `DEC-20260909-001`. No application has yet been compiled, DB created, migration executed, browser journey tested, capacity measured or production service approved by this documentation amendment. Runtime evidence must be produced by the implementation PRs.

| Gate | M01 effect | Current state / next owner |
| --- | --- | --- |
| G01 Product Owner implementation approval | Unblocks code execution for M01 + scaffold only | **Satisfied for bounded M01 package** by DEC-20260909-001; does not approve full Phase1/R1/production |
| G02 Exact package/toolchain compatibility | S00 technical work; blocks merging scaffold/runtime setup until evidence | SDK/runtime/SQL/Node/React/Vite research pins specified; resolve transitive pins and lockfiles in S00 |
| G03 Authentication security implementation | Blocks declaring S02–S06 runtime-complete | Password/CSRF/session/rate-limit/SQL/replay acceptance specified; actual tests not run |
| G04 Last SuperAdmin and grant concurrency | Blocks declaring S08/S09 complete | SQL lock/revision/preview algorithm specified; two-request race tests not run |
| G05 Three-channel intent/delivery semantics | Blocks S10 complete | Capture/test transports and unavailable states defined; real transport integration separate evidence gate |
| G06 Local recovery and clean checkout | Blocks Local Verified label | S11 rehearsal not run; scripts are required implementation artifacts |

## Decisions deliberately outside this milestone

| Remaining topic | Current decision / blocked capability | Why M01 can proceed |
| --- | --- | --- |
| TOTP enrollment/recovery | Policy resolved: one-time recovery codes; email+password alone cannot reset MFA; implementation outside M01 | Password-only local slice; enabled-factor fixtures deny, never bypass |
| Deleted account restore UI/API | Policy resolved: same-account restore only, no email reuse, no purge; self-service restore outside M01 | UQ retained, deleted account denied; no account restore endpoint in M01 |
| Sensitive share/support projections | Policy resolved at allowlist/default-hidden level; concrete field contracts still required by module | M01 has identity/access metadata only |
| Vault portability/recovery | Policy resolved hybrid/no-operator-plaintext; crypto/key/package ADR still required | Vault not in M01 |
| Advanced Finance | Initial Finance scope resolved as basic manual records; ledger/debt/FX/budget etc. remain gated | Finance not in M01; basic fields already separated |
| Task extensions | Initial Productivity scope resolved as flat Task + one reminder; recurrence/subtask/snooze/standalone reminder/attachments gated | Productivity not in M01; core flat Tasks retain approved scope |
| Read-only outbound ingestion | News/GitHub/Monitoring read-only public outbound allowed only under later contract and network guards | No product ingestion in M01; foundation notifications distinct |
| R1 final scope / production budget/SLA | Local Stable must precede production; provider/capacity/RPO/RTO/SLA not committed | M01 explicitly internal, not release-complete |
| FX30/34/35 paused modules | Price Tracking, Automation/Scheduler/Workflows and Integrations/Webhooks/n8n remain Paused, not moved to R2 | M01 does not need them and must not enable their workers |

## Required evidence by story

| Story | Design evidence now | Runtime evidence required in implementation PRs |
| --- | --- | --- |
| S00 | Environment pins/scripts contracts | doctor + complete lockfiles/advisory review |
| S01 | TX01 + AC01 | Concurrent bootstrap, no default credential, audit redaction |
| S02 | Schema/UX/TX02/AC02 | Race/duplicate/expiry/resend/email enumeration tests |
| S03 | Cookie/CSRF/rate limits/AC03 | Login/logout/idle/absolute/revocation/missing-factor tests |
| S04 | Reset contract/TX04/AC04 | Replay, no auto-login/MFA removal/undelete |
| S05 | Profile shape/ETag/AC05 | Unknown-field/locale/timezone/parallel tabs |
| S06 | Session ownership/AC06 | Foreign-session404, revoke-all current included |
| S07 | Metadata allowlist/AC07 | Admin denied/private payload absent |
| S08 | Preview/SQL lock/AC08 | Last SA race, stale preview, denied/paused grants |
| S09 | Module gate/TX09/AC09 | Dependency failure, defaults future-only, disable data retained |
| S10 | Outbox/delivery/TX10/AC10 | Restart, dedupe, three channel outcomes, no fake Sent |
| S11 | Runbook/restore AC11 | Clean checkout/start + isolated restore evidence |

## Completion vocabulary

`Specified`: contract written. `Approved to implement`: PO authorizes exact slice. `Implemented`: code merged subject to process. `Verified locally`: all runtime acceptance passes with evidence. `Production-ready`: additional security/ops/capacity/provider gates approved and verified. Không dùng số lượng docs/actions/tables làm bằng chứng cho các trạng thái sau.

For M01, `Approved to implement` is true only for the bounded package in `DEC-20260909-001`. `Implemented`, `Verified locally` and `Production-ready` remain false until the required code/evidence exists.
