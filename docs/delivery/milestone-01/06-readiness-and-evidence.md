# M01 readiness and evidence gates

## Current status

Specification is concrete enough to review a **bounded first implementation approval**, starting S00. No application compiled, DB created, migration executed, browser journey tested, capacity measured or production service approved. Documentation validation checks references/contracts only.

| Gate | M01 effect | Current state / next owner |
| --- | --- | --- |
| G01 Product Owner implementation approval | Blocks code execution for all stories | Await explicit PO approval; this docs task is not that approval |
| G02 Exact package/toolchain compatibility | S00 technical work; blocks merging scaffold/runtime setup until evidence | SDK/runtime/SQL/Node/React/Vite research pins specified; resolve transitive pins and lockfiles in S00 |
| G03 Authentication security implementation | Blocks declaring S02–S06 runtime-complete | Password/CSRF/session/rate-limit/SQL/replay acceptance specified; actual tests not run |
| G04 Last SuperAdmin and grant concurrency | Blocks declaring S08/S09 complete | SQL lock/revision/preview algorithm specified; two-request race tests not run |
| G05 Three-channel intent/delivery semantics | Blocks S10 complete | Capture/test transports and unavailable states defined; real transport integration separate evidence gate |
| G06 Local recovery and clean checkout | Blocks Local Verified label | S11 rehearsal not run; scripts are required future artifacts |

## Decisions deliberately outside this milestone

| Remaining proposal | Blocked capability | Why M01 can proceed after approval |
| --- | --- | --- |
| P-H01 MFA lost-device recovery | MFA enrollment/recovery release | Password-only local slice; enabled-factor fixtures deny, never bypass |
| P-H02 deleted account recovery/email reuse | Account restore/reuse workflow | UQ retained, deleted account denied; no account restore endpoint |
| P-H03 sensitive share/support | Finance/Vault/Assets/Career/Learning external/support projections | M01 has identity/access metadata only |
| P-H04 Vault portability | Encrypted owner export/import policy | Vault not in M01 |
| P-H05 advanced Finance | Ledger/debt/FX/deletion semantics | Finance not in M01; basic fields already separated |
| P-H06 Task extensions | Recurrence/subtask/snooze | Productivity not in M01; core flat Tasks retain approved scope |
| P-H07 outbound ingestion | News/GitHub/monitoring network behavior | No product ingestion in M01; foundation notifications distinct |
| P-H08 R1 final scope / production budget/SLA | Release1 completion/public production commitment | M01 explicitly internal, not release-complete |

## Required evidence by story

| Story | Design evidence now | Runtime evidence required later |
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
