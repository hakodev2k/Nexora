# DEC-20260909-014 — Full local E2E implementation approval

Status: **Approved by Product Owner**, source: the current instruction to continue existing PR #4 (`impl/m01-s00-scaffold`) toward the full local end-to-end Nexora application. This amendment supersedes the implementation-approval limits of DEC-20260909-001/011/012 and the local-code pause of DEC-20260909-009. It does not supersede documented business/security invariants or prove any runtime result.

- Full local E2E implementation of documented Release 1 phases/slices/modules is approved. Continue slice-by-slice beyond M01 without requesting repeated technical or slice approval.
- `DESIGN_RESOLVED_NOT_APPROVED_NOW` means approved for local implementation **when the exact API/DB/UX/AC/security/evidence contract is sufficient**. Complete missing contracts before coding.
- Previously paused modules, including Price Tracking, Automation and Integrations, may be implemented as local/simulated/integration-safe code. Their real-provider execution remains disabled and unapproved.
- Real provider calls, production deployment/public exposure, real secrets, real user data, paid services and external destructive actions are not approved. Config/credentials alone do not bypass a future explicit execution approval.
- Local SQL Server, synthetic fixtures, migrations, bootstrap, local workers and simulated transports are permitted. SQL is authority; Redis is cache. Simulation must be labelled and never reported as actual delivery/provider evidence.
- Keep feature-based Minimal API + Application/Domain/Infrastructure modular monolith. Milestones are evidence labels, not runtime folders.
- Existing Product Owner business rules remain authoritative: owner isolation, same-account restore/no deleted-email reuse, no Account/Vault purge, no operator plaintext, current state machines and format boundaries. A genuinely missing business decision is not resolved by this approval; work on independent contracted capabilities while recording the affected blocker.
- Keep all commits and evidence in PR #4; do not merge it. Report implemented/unimplemented scope and actual/not-run verification explicitly.

<a id="local-automated-qa-20261025"></a>
## Execution amendment — 2026-10-25: implementation and automated QA

**Status: Approved by Product Owner.** Source: explicit request “Nexora — Update
Agent Execution Policy for Full Implementation & Automated QA” for repository
`hakodev2k/Nexora`, branch `impl/m01-s00-scaffold`, PR #4. This dated amendment
supersedes **only** the earlier code-only / human-executes-routine-QA execution
restriction. It changes execution responsibilities, not product requirements,
acceptance criteria, R1 commitments, architecture, permissions or D01–D09.

### Approved activities and accountable owners

- Agents may implement missing approved functionality, integrate backend/API/SQL/
  frontend, fix defects, add validation/errors/edge/recovery handling, refactor where
  needed for correctness/maintainability and add safeguarded migrations. Sufficient
  current API/DB/UX/AC/security/evidence contracts remain prerequisites.
- Agents may create, modify and run unit, frontend component, integration, API,
  real isolated SQL/database, E2E and regression tests; security/permission/owner
  isolation/negative paths; concurrency/idempotency/retry/recovery; responsive and
  accessibility checks; build/typecheck/lint/static analysis. Existing green suites
  or compilation do not substitute for applicable acceptance-criteria coverage.
- Agents may launch the application in an authorized isolated local environment,
  use Playwright or equivalent browser automation, exercise real UI/API/SQL flows,
  roles, navigation/dialogs/forms, desktop/mobile, loading/empty/success/error states,
  persistence/reload/failure/recovery, and capture sanitized screenshots/test evidence.
  Remediate defects and rerun failed and affected regression checks.
- Agents may generate synthetic users/identities/roles/permissions and feature
  records (including Projects, Tasks, Documents and Bookmarks), boundary/negative/
  concurrency/lifecycle fixtures and approved simulated-service responses. Bootstrap
  through legitimate application mechanisms; test identities/grants do not bypass
  actual server-side authorization. No automatic demo seeding of non-test environments.
- Ordinary approved local slices and test runs do not require repeated PO approval
  while all current contracts, security/environment boundaries and host/tool
  permissions are satisfied. Missing business decisions remain unresolved; continue
  independent work instead of inventing behavior or changing scope.
- The Technical Lead remains accountable for implementation and automated QA
  coordination. Authorized genuine parallel agents may split backend, frontend,
  SQL, tests, E2E, security and independent review with clear ownership; otherwise
  execute sequentially. Each reports scope/files, tests changed versus executed,
  exact results, defects/fixes, risks/blockers and revision/evidence. No invented work.
- The human Product Owner performs **final UI/UX and product acceptance** after
  agent implementation/automated QA; the human is not the default routine test or
  browser-QA executor. Independent engineering/security review requirements remain.

### Required workflow and evidence

Requirements → Implementation → Automated QA → Defect Remediation →
Runtime/Browser Verification → Human UI Review.

1. Read current PO decisions and bind exact Feature/Story/BR/AC/action IDs, scope,
   dependencies, security gates and open decisions to a bounded implementation and
   verification plan. Complete sufficient contracts before code.
2. Complete the applicable vertical slice, owner authorization and recovery. Review
   code and perform required independent engineering/security/migration review.
3. Execute applicable verification layers, focused checks during work and affected
   tests/release-critical regression after final relevant code edits. Real SQL
   semantics require an approved isolated real SQL Server, not in-memory substitutes.
4. Preserve every actual failure/reproduction context. Diagnose implementation,
   oracle, fixture, environment or ambiguous requirement; fix authorized root causes
   while preserving meaningful assertions. Never skip/delete/disable failures or
   weaken production checks to obtain green. Rerun failed scenarios and affected
   regression; document exact blockers and continue independent executable work.
5. Verify actual permitted/denied user journeys and database persistence, failure/
   recovery, desktop/mobile and affected regressions. Record exact commit SHA,
   environment, test case, command/result and sanitized evidence. Worktree/tree runs
   must identify that source and be reconciled to the delivered commit; relevant
   later changes invalidate affected evidence. Source/build/earlier screenshots do
   not prove runtime success. Existing CI is not full AC coverage.
6. Before human review, finish all authorized automated verification possible,
   resolve actionable failures and provide final revision/evidence, exact remaining
   environment/decision blockers, startup/review instructions and concise relevant
   UI journey checklist. Never mark unexecuted checks PASS.

Track separately: `IMPLEMENTATION COMPLETE`, `AUTOMATED QA VERIFIED`,
`HUMAN UI REVIEW PENDING`, `HUMAN UI REVIEW ACCEPTED`, `BLOCKED`, `NOT VERIFIED`.
These dimensions do not replace existing module/action lifecycle or D09 gates.
Automated QA may be verified before human review; only the human's explicit
acceptance for the relevant scope/revision sets HUMAN UI REVIEW ACCEPTED. Full
RELEASE READY still requires all mandatory human, independent, security and release
gates. Trace Requirement → AC → Code → Test Case → Execution Evidence → Release Status.

Results mean `PASS` executed successfully with evidence; `FAIL` executed and failed;
`PARTIAL` defined subset verified; `NOT TESTED` not executed; `BLOCKED` exact documented
blocker; `NOT APPLICABLE` justified non-applicability. Record coverage, execution,
security, runtime, human acceptance and release status independently. No retroactive
test execution, earlier-result promotion or automatic acceptance from this amendment.

### Isolation and unchanged security boundaries

Use a dedicated, uniquely identified test database such as `Nexora_Test_<runid>`,
synthetic records, least-privilege test credentials, isolated browser sessions and
test-only configuration/transports. Prove run ownership through markers and exact
target checks before reset/cleanup. Never destructively reuse unknown/unowned DBs.
SQL Server remains authority; Redis is rebuildable cache, not proof of SQL constraints,
concurrency, owner isolation, migrations or transactions. Label simulations; mock/
simulated success cannot prove actual provider delivery/integration.

No production database, real user data, production secrets, paid provisioning,
public app publishing/deployment, live destructive action, authentication/authorization
bypass, cross-owner leak or unapproved network/provider activation is authorized.
Never commit synthetic passwords/credentials/private browser storage or expose keys
in logs/artifacts. Keep D01–D09 exactly as defined in DEC-20261019-PR4: no Vault
secret-bearing crypto activation, advanced Finance/Task auto-expansion, unapproved
sensitive projections or Learning Plan gate promotion; audit/encryption/backup/restore
requirements and independent current-head acceptance/CI remain. Paused capabilities
may use approved local simulations only; real effects stay disabled. Do not force
push or merge PR #4. Separate valid PO decisions are required for excluded effects.

### Superseded execution restriction — historical record only

The following earlier run-specific policy is retained solely for traceability.
It is **not** an active instruction and cannot block the local automated QA above:

> The current Product Owner instruction further narrows this run to **code-only implementation**. The human owner handles functional testing, manual QA, test-data/mock-data design, fixture preparation, user acceptance testing and runtime verification. Agents may run compile/build/typecheck/lint commands only to catch implementation errors; they must not add new unit, integration, E2E/browser test suites, mock/demo records or artificial fixtures, and must report those checks as not performed unless an existing command was actually run.
>
> This execution amendment does not reduce the approved product scope or relax any security, privacy, owner-isolation, provider or production gate. A real local operator bootstrap is allowed; a demo seed is not.

## Effective status overlay

This amendment takes precedence over the earlier action overlay, delivery gate register, goals snapshots and readiness statements that require future slice approval or prohibit local paused-module code. They remain evidence of the previous boundary. `SUPERSEDED` action keys remain retired; this amendment does not revive them. Production/network gates continue to prohibit real external execution. Policy/security contracts remain mandatory before the corresponding local feature is enabled.

`Approved locally` is not `implemented`, `verified locally`, `R1 complete` or `production approved`. A simulated provider may satisfy explicitly simulated acceptance only.
