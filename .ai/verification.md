# Verification of the Nexora agent baseline

## Authorized local application QA and human acceptance

The 2026-10-25 amendment to DEC-20260909-014 in
`docs/requirements/12-owner-decisions-local-e2e-implementation.md#local-automated-qa-20261025`
supersedes the old code-only/human-executes-QA restriction. Agents may create/update
and run applicable unit, component, API/integration, real isolated SQL, E2E,
regression, negative/permission/isolation/security, concurrency/idempotency/recovery,
responsive/accessibility and static checks. Agents own ordinary automated QA,
synthetic fixture preparation and local runtime/browser verification, without
repeated PO approval inside the approved contract-complete local scope.

Standard workflow: Requirements → Implementation → Automated QA → Defect
Remediation → Runtime/Browser Verification → Human UI Review. Use focused checks
during work; after final relevant edits rerun affected scenarios and release-critical
regression on the final revision. Record failures and diagnose implementation,
oracle, fixture, environment or requirement ambiguity; fix authorized root causes
and retain meaningful assertions. Never delete/disable/skip failures or weaken
production validation/security merely for green. Record exact blockers and continue
independently executable work. Source review/build/old CI cannot prove full AC coverage.

Run actual allowed/denied UI/API/SQL journeys with persistence/reload, error/recovery
and desktop/mobile states. Use unique run-owned disposable test DBs, synthetic
identities/grants through legitimate application flows, isolated browser/configuration,
least privilege and labelled local simulations. Prove ownership before cleanup;
never use real data/secrets, unknown DBs, authorization bypass or live providers.
SQL Server remains authority; mocks/in-memory/Redis cannot prove SQL semantics or
real provider integration. Never commit passwords/credentials/private browser storage.

Every result records exact commit SHA, environment, test case, command/result and
sanitized evidence. Worktree runs record tree/source identity and are reconciled to
the delivered commit; later relevant changes invalidate affected evidence. Use
PASS (executed successfully), FAIL (executed and failed), PARTIAL (defined subset),
NOT TESTED (not executed), BLOCKED (documented blocker), NOT APPLICABLE (justified).
Track Requirement → AC → Code → Test Case → Execution Evidence → Release Status.

Before handoff, complete authorized automated verification possible, resolve
actionable failures and provide source/evidence, exact blockers, startup/review
instructions and a concise UI acceptance checklist. Record separately
IMPLEMENTATION COMPLETE, AUTOMATED QA VERIFIED, HUMAN UI REVIEW PENDING,
HUMAN UI REVIEW ACCEPTED, BLOCKED and NOT VERIFIED. Automated QA does not imply
human acceptance; only explicit human confirmation for the relevant scope/revision
sets ACCEPTED. Mandatory human UI/product acceptance, independent review and D09
current-head CI/traceability still prevent RELEASE READY while pending. This policy
does not retroactively execute tests or change D01–D09/product/deployment authority.

## Agent-kit validation

Run from the repository root with Python 3.9+ (standard library only):

```sh
python3 .ai/scripts/verify-baseline.py
```

This checks required entry points, exact routing paths, package structure and gate
regressions. CI runs the same command. It does not test Nexora application code,
prove that an agent read instructions, or enforce GitHub branch protection.
Configure `agent-baseline` as a required PR check through the repository owner if
merge enforcement is desired. Do not grant a PR workflow secrets or write permissions.

## Task evidence

Every handoff must identify authorization and scope, rule/skill paths actually
loaded, requirement/story/acceptance IDs, changed files, executed commands and
results, source revision, evidence references, risks and pending review. Never
claim a missing tool, on-demand package, app test, independent review or CI run
passed. A test command must have actually executed successfully before its
evidence is entered. Later relevant changes invalidate earlier evidence.

The completion script validates a self-reported ledger, not the truth of an
artifact. Supply a nonempty `revision` (commit or source-tree identifier); each
evidence item requires the same `revision`, a nonempty `reference`, and explicit
JSON booleans `fresh: true` and `passed: true`. Example shape:

```json
{"revision":"<tested-source-tree>","claims":["implemented"],"evidence":[{"type":"change_recorded","revision":"<tested-source-tree>","reference":"<actual-diff-or-artifact>","fresh":true,"passed":true}]}
```

```sh
python3 .ai/guards/agent-ground-truth-completion-gate/scripts/completion_gate.py ledger.json --policy .ai/guards/agent-ground-truth-completion-gate/config/completion-policy.json
python3 .ai/controls/agent-multi-tenant-data-boundary-gate/scripts/tenant_boundary_gate.py operation.json
```

The completion policy's independent-verifier requirement is a workflow obligation;
the script cannot authenticate reviewer independence. Root AGENTS defines when
independent review is required. Record reviewer identity and evidence or leave the
gate pending. The static checker is not that independent review.

## Owner-boundary gate limitations

Generic `tenant` means Nexora PersonalSpace `OwnerId`, not authentication `UserId`.
Only server-validated claims or resolved service context may supply authority;
route/body/header values alone are not trusted. `config/policy.yaml` is descriptive
upstream policy, not runtime-loaded configuration; script validation is explicit.
Approval flags cannot prove access-context validity. Cross-owner manifests block
for dedicated contract tests and independent review; this does not prohibit
product-approved read-only sharing, Support/Emergency or scoped recovery flows.
Test those flows against current authorization, projection and recovery contracts.

Application verification must follow the approved slice's runbook and readiness
matrix. For M01, read `docs/delivery/milestone-01/05-environment-and-runbook.md`
and `06-readiness-and-evidence.md`. Use disposable SQL Server with synthetic
owners/users, grants, denied contexts, revoked/expired access and concurrency cases.
Mocks are useful for isolated unit tests, but cannot substitute for actual SQL
constraints, migrations, transactions or end-to-end authorization. Redis/provider
failures require the specified integration scenarios. Do not provision these
environments or install application dependencies under agent-kit review approval.
