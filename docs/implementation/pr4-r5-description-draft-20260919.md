# Draft PR description — R5 remediation and test engineering

**Draft only — do not post automatically. Do not merge from this draft.**

## Scope

- Branch: `impl/m01-s00-scaffold`; target remains `main`.
- Normative main: `8782f46be51f4b0f3cb54f0f0f7a06eae3b0d3e3`.
- Reviewed implementation parent: `aada36f089ca0ea85424dfa249a2228d70ca740b`.
- Approval used: main `DEC-20260909-001`, M01 S00-S11, scaffold and local synthetic verification.
- No merge, deployment, provider execution, production data/secret, migration rewrite or data reset is included.

## What changed

- Hardened local account-message capture creation and bounded orphan recovery. Temporary files use private creation semantics where supported; unsafe per-file state is isolated rather than blocking unrelated delivery.
- Enforced final capture filename/payload identity ownership before read, retention or fence cleanup; compatible matching legacy capture files remain readable.
- Kept configured runtime/operator ACL policy and caller validation fail-closed.
- Added xUnit/TestServer/Vitest/Playwright orchestration, fail-closed SQL/E2E/OS scripts and separate CI job definitions. Existing custom assertions remain retained.
- Preserved receipt dual-read compatibility, signed anonymous binding, IdentityV3 rehash, readiness 0025/0026, AdminGrantable gate and profile-timezone behavior.

## Local evidence and gates

- Final local source tree: API, backend unit and SQL/API integration projects build successfully with zero warnings/errors.
- Historical pre-final-hardening source evidence: custom runner 30 passed / 4 explicit skips; xUnit 11 passed; Vitest 5 passed / 1 explicit DST-policy skip; frontend build passed.
- SQL/API, Linux filesystem, actual Windows A/B/C ACL and full browser E2E are not verified. Missing environments fail closed; no skipped environment is reported as Passed.
- CI was not triggered because this worktree is uncommitted/unpushed. Independent security/runtime review is pending.

## Reviewer focus

1. Confirm ownership, ACL, recovery and fence boundaries in `LocalAccountMessageSink`.
2. Require real SQL Server RCSI/concurrency/receipt evidence, actual separate Windows A/B/C principals, Linux lock/mode proof and full-stack browser journeys before a runtime-verified claim.
3. Review the local test/runbook artifacts and fail-closed CI jobs; no branch-protection change is requested.

## Out of scope / carried backlog

No post-M01 repair was opened. Documents dirty transition, Favorites route, Task Project picker, Calendar DST/navigation/status, nullable Sharing Priority, Reminder/Files queues, mutation-read/replay, audit and module-boundary findings retain their prior disposition.

See `Nexora-PR4-r5-remediation-test-engineering-main-aada36f-vi.md` and `Nexora-PR4-r5-test-runbook-vi.md` for the exact matrix, commands, artifacts and limitations.
