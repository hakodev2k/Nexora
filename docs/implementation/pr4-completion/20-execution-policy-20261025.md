# Local implementation and automated-QA execution policy — 2026-10-25

Status: policy reconciliation, not application QA or release acceptance. Effective
authority is [DEC-014 execution amendment](../../requirements/12-owner-decisions-local-e2e-implementation.md#local-automated-qa-20261025).

## Task brief and authority

| Field | Recorded scope |
|---|---|
| Accountable owner | Root Technical Lead; independent read-only policy review by templates_completion |
| Requested outcome / authorization | Explicit Product Owner instruction “Update Agent Execution Policy for Full Implementation & Automated QA”: replace code-only/routine-human-QA restriction, authorize approved isolated implementation/tests/synthetic data/runtime; human final UI/product acceptance retained; GitHub commit to existing PR4 |
| Source state | Clean `impl/m01-s00-scaffold`, local and connector direct ref `cd34b9c9534e108082e0cff2682aa074e3d1d978`; no unrelated work at intake |
| Applicable source IDs | DEC-20260909-014 execution amendment dated2026-10-25; DEC-20261019-PR4 D01–D09 unchanged. No application Feature/Story/AC behavior change; per-slice IDs remain mandatory for future implementation. |
| Goals | NXG-SYS-02/03/05/08/12/13/14/16; execution/evidence ownership only, no goal/runtime acceptance promoted |
| Loaded instructions | AGENTS, nexora-engineering skill, implementation profile, Technical Lead README/core/workflows, `.ai/routing.json`, `.ai/verification.md`, QA strategy/contract-testing skills, completion gate README/rules/evidence skill, docs/delivery/goals/task template, DEC014/D01–D09/release contract and current PR4 ledger/report19 |
| Scope / plan | Discover restrictions; classify active/historical/false-positive hits; edit only active execution guidance and current entry points; preserve old policy as explicitly superseded; validate invariant/catalog/link/diff consistency; independently review; deliver through GitHub connector with expected parent/no force |
| Boundaries | Documentation/policy only. No application/SQL/test/workflow/dependency change, new fixture, runtime launch, provider, production, real data/secret, merge or R1 rescope. No new runtime claim from policy activation. |
| Rollback | Revert this bounded policy-doc commit; retain historical evidence. No DB/data/runtime side effect or destructive rollback. |
| Open gates | Application blockers and PR4 HOLD remain; existing D09 independent/current-head CI/traceability and explicit final human UI acceptance are not waived |

## A — Files updated and before/after comparison

Each row records a policy edit, not a feature status or test-result change.

| File | Previous instruction / discrepancy | New effective instruction / reason |
|---|---|---|
| `AGENTS.md` | Explicit code-only; human does functional QA/data/runtime; tests forbidden; generic workflow | Agent implementation+applicable automated QA+isolated data/runtime, six-phase workflow and human final UI review; exact safeguards and honest state/evidence |
| `.ai/profiles/nexora-implementation-agent.md` | No explicit current execution/human acceptance ownership | Route to dated DEC014 amendment; Technical Lead owns implementation/QA, human explicit acceptance remains separate |
| `.ai/verification.md` | General evidence rules without explicit new local QA authority | Applicable layers, isolated fixtures/runtime, final affected reruns, failure triage and human handoff/status requirements |
| `.ai/guards/agent-ground-truth-completion-gate/rules/completion-claim-policy.md` | Implemented/Measured/Verified distinction without explicit human UI dimension | Add independent human acceptance dimension; pending human/D09 gates prevent RELEASE READY; no retrospective evidence |
| `docs/requirements/12-owner-decisions-local-e2e-implementation.md` | Current code-only amendment overrides local E2E QA permission | Dated PO-authorized implementation/automated-QA amendment; old restriction quoted under Superseded historical section; original product/security approval preserved |
| `docs/requirements/11-owner-decisions-20260909-implementation-readiness.md` | Current overlay delegates tests/runtime to owner | Current overlay points to agent QA/human final acceptance; original historical M01/decision text retained |
| `docs/requirements/13-owner-decisions-20261019-pr4-blockers.md` | Authority paragraph describes code-only amendment as current | Update execution reference only; all D01–D09 dispositions/effects/integrity text unchanged |
| `docs/README.md` | Three active owner-QA/no-test statements | Unified agent isolated-QA/human final review ownership |
| `docs/delivery/README.md` | Two active code-only/no-fixture statements | Unified dated execution amendment and retained product boundaries |
| `docs/delivery/01-current-scope.md` | Current scope intro delegates QA to owner | Agent isolated QA, final human acceptance; R1 scope table untouched |
| `docs/delivery/04-paused-blocked-gate-register.md` | Active no-test language and owner-only evidence requirement | Agents execute local QA; independent/human release evidence remains; real-provider no-go unchanged |
| `docs/delivery/milestone-01/README.md` | Current authority banner forbids tests/fixtures; DEC014 link typo | Current QA authority and correct DEC014 target; original M01 contract stays historical |
| `docs/delivery/milestone-01/06-readiness-and-evidence.md` | Current overlay assigns runtime evidence to owner | Agents produce isolated QA evidence; final human acceptance remains |
| `docs/delivery/milestone-01/07-implementation-handoff.md` | Current banner forbids tests/fixtures | Agent QA authority; preserve no production/provider and bounded M01 handoff |
| `docs/action-catalog/09-effective-implementation-status-20260909.md` | Current no-test restriction; old snapshot “code-only environment” | Current agent QA authority; snapshot not-run evidence remains explicitly historical |
| `docs/action-catalog/catalog.csv` | Sixteen Gate rationale cells assign runtime verification to owner | Only QA ownership wording changed; all733IDs, statuses, contexts, grantability, prerequisites, guards and scope flags retained |
| `docs/action-catalog/modules/27-finance.md` | Intro and eight Gate rationale cells assign runtime QA to owner | Agent QA + final human review; Finance scope/action/security columns unchanged |
| `docs/features/27-finance.md` | Runtime verification remains owner work | Agent isolated QA + final human UI/product acceptance; business body unchanged |
| `docs/features/90-open-decisions.md` | Active no-test/no-fixture restriction; DEC014 link typo | Current execution amendment and correct link; decisions/gates unchanged |
| `docs/goals/README.md` | No tests/mock/demo data in current run | Agent isolated QA/synthetic data; no goal runtime result promoted |
| `docs/goals/06-source-review-and-gates.md` | Current addendum delegates QA/runtime to owner | Current dated execution authority; original docs-only task history retained |
| `docs/goals/modules/32-developer-toolbox.md` | Runtime/functional verification remains owner-owned | Agent QA/human final acceptance, explicitly no recorded-result change |
| `docs/ux-ui/modules/21-bookmarks.md` | Header assigns runtime work to owner | Agent isolated QA/human acceptance; UI/business specification unchanged |
| `docs/ux-ui/modules/22-snippets.md` | Same owner-only runtime header | Same reconciled execution ownership; specification unchanged |
| `docs/ux-ui/modules/27-finance.md` | Same owner-only runtime header | Same reconciled execution ownership; specification unchanged |
| `docs/implementation/pr4-completion/README.md` | Latest entry point reinstates code-only; older windows contradict | Add authoritative current policy then classify all earlier checkpoints as historical execution windows |
| `docs/implementation/pr4-completion/04-feature-implementation-ledger.md` | “Latest” policy reinstates code-only | Current policy link, previous restriction past-tense at cd34b9c; feature/QA statuses retained |
| `docs/implementation/pr4-completion/16-full-feature-inventory-20261025.md` | Current delta says owner must runtime-verify late UI | Current QA authorization; all733 inventory/722quality/201screen rows and actual results retained |
| `docs/implementation/pr4-completion/19-completion-uiux-20261025.md` | Run-specific code-only/human-QA facts could be mistaken for ongoing authority | Add historical checkpoint banner and superseding authority only; body/failures/not-run/HOLD preserved |
| `docs/implementation/pr4-completion/20-execution-policy-20261025.md` | No policy reconciliation record | This task brief, per-file comparison, permissions, safeguards, search classification and actual validation/handoff |

## B — Effective execution authorization matrix

| Activity | Permission / accountable owner |
|---|---|
| Code implementation | ALLOWED for documented approved contract-complete local scope; agents |
| Unit testing | ALLOWED create/modify/run; agents |
| Integration testing | ALLOWED create/modify/run in authorized isolated environment; agents |
| SQL/API testing | ALLOWED with real isolated SQL for SQL semantics; agents |
| Browser E2E | ALLOWED Playwright/equivalent actual local UI/API/SQL; agents |
| Automated UI/UX | ALLOWED responsive/accessibility/loading/error/form checks and sanitized captures; agents; not human acceptance |
| Synthetic test data | ALLOWED synthetic users/roles/grants/domain/boundary fixtures only in isolated owned test resources; legitimate authorization/bootstrap |
| Local test bootstrap | ALLOWED run-owned DB/config/browser/simulated transport; least privilege and ownership-proven cleanup |
| Regression testing | ALLOWED/REQUIRED where applicable after final affected changes; agents |
| Human final UI/product acceptance | REQUIRED explicit human confirmation for scope/revision; agents cannot self-accept |
| Production deployment/public publishing | NOT AUTHORIZED |
| Real-provider execution | NOT AUTHORIZED; approved local simulations do not enable effects or prove live integration |
| Real-data/production-secret access | NOT AUTHORIZED; test credentials private/ephemeral, never committed |

Normal approved slices/runs do not need repeated PO approval. Host/tool permissions,
contract sufficiency and all existing product/security/environment gates still apply.

## C — Security and scope invariants

- D01–D09 selected table, effects, contract/handoff map and record-integrity section
  are preserved; only the obsolete execution sentence in the authority paragraph
  changes. No Vault secret functionality, advanced Finance/Task scope, sensitive
  allowlist, Learning Plan readiness or live-provider activation is approved.
- AGENTS Nexora invariants and Approval boundaries sections are unchanged; owner
  isolation, server authorization, SQL authority/Redis cache, audit, encryption,
  backup/restore and no real data/secrets remain mandatory.
- Catalog changes only16 Gate explanation cells; no action ID/status/grantability/
  context/prerequisite/guard/scope promotion. The Finance module changes only its
  QA ownership prose. No application, migration, runtime configuration or CI workflow
  is edited. D09 current-head CI/independent review/traceability/Draft/no-merge remain.
- Human UI acceptance is not implied by AUTOMATED QA VERIFIED; required human
  and other gates must pass before full RELEASE READY. No feature completion,
  previous execution or product scope is reclassified by this policy change.

## D — Consistency discovery and verification

Discovery used GitHub direct branch ref plus local git state, required source reads
and `git grep -n -I -i -E` across tracked Markdown/CSV/configuration. `rg` is not
available. Review classified matches, rather than doing a repository-wide replace:

| Match category | Disposition |
|---|---|
| Active code-only/no-test/no-fixture/human-routine-QA instructions | Reconciled in startup, decision, delivery/goals/catalog/UX and current completion entry points above |
| Prior code-only policy in DEC014 | Preserved verbatim as quoted SUPERSEDED historical context, not executable authority |
| `docs/history/**`, dated implementation/testing/worker reports, `qa/runs/**`, LearningPlan worker brief and Windows helper run history | Preserved as actual historical authorization/execution evidence; current completion index explicitly labels older windows |
| Existing no-runtime/compile-only result snapshots (for example Documents goal and Learning action header) | Preserved as evidence limitations, not policy prohibitions or new test results |
| “provider code only”, “JWT decode only”, “local-safe disabled code only”, no demo data in production | False positives or valid security/functional restrictions; unchanged |
| Engineering skill, QA skills, execution controls, generic Technical Lead workflows | Read and compatible; no unrelated rule/control/script/configuration rewrite |

Local checks and exact actual results are recorded below after execution. Remaining
active contradictions must be zero before delivery; historical restriction text is
deliberately not deleted. No new application tests or runtime verification are
claimed by this policy-document task.

### Validation execution

- `python3 .ai/scripts/verify-baseline.py`: local attempt FAILED to start (exit1),
  command not found. No Python/py/uv interpreter was discovered through PATH,
  standard local Programs/Python locations or Python registry install paths. No
  installation or dependency change was made; baseline gate tests are NOT RUN locally.
- Existing `.github/workflows/agent-baseline.yml` runs the canonical baseline on
  pull requests with read-only contents permission. Final-commit workflow outcome
  and link are to be reported through the PR completion summary; no pending run is
  counted PASS here.
- Policy/invariant/catalog/changed-link/diff checks and independent source review:
  actual execution recorded below. These are policy checks, not app test results.

### Final local validation and source review

Executed `.\.tmp\verify-execution-policy.ps1` (read-only scratch consistency check,
PowerShell, source baseline cd34b9c plus this policy worktree), exit0:

| Assertion / actual check | Result |
|---|---|
| DEC-PR4 text from Selected dispositions through record integrity vs baseline | PASS, identical; D01–D09 table/effects/contracts retained |
| AGENTS Nexora invariants and Approval boundaries sections vs baseline | PASS, identical |
| Catalog parsed old/new rows | PASS,733→733; exactly16 Gate rationale changes; zero changes to every other field |
| Active .ai/AGENTS/requirements/delivery/features/goals/UX/catalog instruction scan | PASS,332tracked Markdown files scanned with targeted forbidden-policy phrases; zero remaining active contradictions. Superseded DEC014 quotation explicitly excluded as historical. Broader discovery matches independently classified above. |
| Newly added relative links in tracked diff | PASS,28file targets exist across final staged/unstaged diff vs HEAD; corrected two DEC014 authority links and included report20. |
| Changed-file scope | PASS, no application/test/migration/workflow/dependency changes; no historical QA/run files or D01–D09 contract files changed |
| `git -c safe.directory=... diff --check` | PASS; LF/CRLF normalization advisories only, no whitespace error |
| Independent source review / templates_completion | COMPLETE, no blocking finding across29policy-file diff; reviewed startup/DEC014, .ai guidance, all active cross-reference edits, catalogs and current report entry points. Independently confirmed733rows/16Gate-only changes and preserved D01–D09/human review. Report20 was not part of that review; no product tests/data/runtime or edits by reviewer. |

The local helper is a scratch validation command, not a new product test suite or
enforcement mechanism. Search/manual review cannot prove agent runtime compliance.
Canonical agent-baseline Python regression remains pending GitHub execution at this
recording point. No instruction could not be reconciled; no product/host permission
was bypassed. Prior application failures and NOT VERIFIED states remain unchanged.

## E — GitHub delivery and evidence boundary

Deliver this documentation-only change on existing `impl/m01-s00-scaffold`, PR4,
parent cd34b9c. Connector must recheck current remote ref and use expected parent,
force:false; do not overwrite concurrent work. Commit containing this report is
the policy revision; PR completion summary supplies exact SHA/commit URL/current
Agent baseline result. Do not confuse the policy check with product QA acceptance.

## F — Effective policy statement

Once committed with reconciled active instructions, Nexora supports **full approved
local implementation + automated QA + isolated synthetic-data testing + final human
UI/product review**. This supersedes only the old execution restriction. Product
contracts, D01–D09, current independent/security/CI gates, PR4 Draft/HOLD and all
production/real-data/real-provider exclusions remain. The implementation backlog,
failed Documents regression and unverified late UI/FX18 code still require actual
agent QA; nothing in this policy record marks them tested or accepted.
