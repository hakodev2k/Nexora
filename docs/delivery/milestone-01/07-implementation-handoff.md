# M01 implementation handoff prompt

Use this prompt only after merging the 2026-09-09 implementation-readiness decisions or working on a branch that contains them.

```text
Implement Nexora M01 only.

Repository: hakodev2k/Nexora.
Current Product Owner approval: DEC-20260909-001.
Approved scope: M01 stories S00-S11 plus the backend scaffold, React frontend scaffold and local scripts needed to prove M01 locally.

Before coding:
1. Read AGENTS.md.
2. Read .agents/skills/nexora-engineering/SKILL.md.
3. Read .ai/profiles/nexora-implementation-agent.md.
4. Read docs/README.md.
5. Read docs/requirements/11-owner-decisions-20260909-implementation-readiness.md.
6. Read docs/delivery/README.md.
7. Read docs/delivery/01-current-scope.md.
8. Read all files under docs/delivery/milestone-01/.
9. Read docs/goals/README.md and bind the relevant goal IDs to M01 story/action/acceptance IDs.

Implement only the smallest coherent vertical slice for M01. Do not implement business modules outside M01. Do not implement Files, Sharing, Support/Emergency, Vault, full Notification Center UI, Finance, Projects, Tasks, Calendar, Documents, News/GitHub/Monitoring ingestion, Price Tracking, Automation or Integrations.

Mandatory technical boundaries:
- Stack: .NET 10 / ASP.NET Core, ReactJS, SQL Server, Redis as rebuildable cache only.
- Architecture: modular monolith; no microservices, brokers, Kubernetes, search cluster or cloud-specific topology without an approved ADR.
- SQL Server is authoritative for sessions, grants, idempotency, jobs, notification intents and security decisions.
- Same-origin cookie auth, CSRF, SQL authority recheck, no bearer token JSON/localStorage.
- Personal-only owner isolation; OwnerId means PersonalSpace, not UserId.
- M01 is password/email only. MFA-enabled fixtures must fail closed; do not bypass them.
- Deleted accounts must not login, reset into active state or allow email reuse for a new owner.
- Paused modules FX30/34/35 must remain unavailable and must not start workers.
- Real outbound provider work is not approved. Use local/captured/simulated transports for evidence.
- Use synthetic data only. Do not use production secrets/data.

Required implementation artifacts:
- Backend solution scaffold and M01 API implementation.
- React frontend scaffold and M01 screens/journeys.
- SQL Server migrations/schema for M01 tables only plus required structural dependencies.
- Local scripts/runbook artifacts for doctor/configure/dependencies/migrate/seed/bootstrap/run/verify.
- Tests proving M01 acceptance criteria, including denied paths and race/fault cases where specified.
- Evidence report mapping source requirement/AC/action to code and actual command/test output.

Required evidence before marking done:
- Build passes on the reported revision.
- SQL migrations apply to a clean local target.
- Bootstrap race proves exactly one SuperAdmin.
- Registration/verification denies enumeration and handles duplicate/race/expiry.
- Login/logout/session revocation honors cookie/CSRF/idle/absolute/security-stamp rules.
- Password reset does not auto-login, undelete or remove MFA.
- Profile update honors ETag/If-Match, unknown-field rejection and locale/timezone rules.
- Session list/revoke proves owner isolation.
- Admin user/access/module APIs expose metadata only and deny personal domain payloads.
- Role/permission/module policy commit proves preview staleness, last-SuperAdmin protection, dependency blockers and paused/blocked actions.
- Delivery foundation creates durable logical intents with independent channel states, retry/dedupe and no fake Delivered/Sent.
- Clean checkout/start and isolated restore rehearsal are run or explicitly reported as Not run with reason.

Open a PR to main. Do not write directly to main. In the PR body, include:
- Approved scope and DEC-20260909-001 reference.
- Story/action/AC coverage table.
- Files changed summary.
- Commands/tests actually run and results.
- Evidence links/artifacts.
- Pending gates and residual risks.
- Confirmation that production, paused modules, provider writes, production data/secrets and out-of-M01 modules were not touched.
```

## Non-goals

This handoff does not authorize implementation outside M01, production deployment, provider spend, public launch or paused modules. Future slices require their own bounded approval and contracts.
