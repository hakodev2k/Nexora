# Actual worker execution and interruption accountability

Actual AI subagents started across this continuous task:16 (root excluded).
They ran in3attempts:6initial,6first recovery,4current recovery. At most6children
ran concurrently. Two scratch resets erased unpushed local artifacts; old test
counts in chat are not substituted for recreated source/current test artifacts.

Current4workers all returned completed bounded handoffs. Earlier recorded final
notifications establish2completed workers and2usage-limit failures;8other earlier
worker final states are unavailable after runtime reset. Therefore confirmed
aggregate:16started,6completed,2failed due usage limit,8reset/unrecoverable final
states. No explicit cancellation was recorded; zero recorded is not proof that
all internal work completed. Do not call all16successfully completed.

| Attempt / worker | Scope and actual actions | Outcome / retained evidence |
|---|---|---|
| Initial security_backend | Goals eligibility, D07/D08 contract/oracle review, Toolbox/Document source review | Confirmed finished; unpushed files lost, later reconstructed |
| Initial frontend_ui | Dashboard/Finance/shell/component fixtures | Reset; final worker state/artifacts unavailable |
| Initial sql_qa | CI logs, migration oracle, Linux child harness | Reset; old counts not current evidence |
| Initial productivity_learning | Focus/Time races and dirty forms | Reset; later reconstructed |
| Initial knowledge_features | Document diff/import and safe renderer | Reset; later reconstructed |
| Initial independent_review | Scoped review/privacy findings | Reset; final state unavailable |
| Recovery restore_frontend | Restored UI, attempted components/browser | Reset; final status unavailable |
| Recovery restore_knowledge | Restored diff/import, controlled browser setup | Reset; final status unavailable |
| Recovery restore_productivity | Revocation/native forms; reported10tests | Explicit usage-limit failure; no old pass reused |
| Recovery restore_backend | Eligibility/oracles/restore/build | Explicit usage-limit failure; later actual checks rerun |
| Recovery inventory | Reported40FX/token/source inventory | Reset; reconstructed inventory explicitly lexical |
| Recovery review | Found validation/history-permission issues; read fixes | Confirmed finished; lost report later replaced |
| Current frontend | Changed6owned files; ran81component cases, build,4browser cases and3width probes, captured9PNGs |80pass/0fail/1existing skip; browser4pass; widths3pass; workers/frontend.md |
| Current knowledge | Changed4production+4testfiles; bounded diff, selector/mode invalidation, malicious source tests |18pass/0fail/0skip; workers/knowledge.md |
| Current productivity | ChangedFocus/Time+10regressions, stalled reads and independent action loss, native dirty forms |10pass/0fail/0skip; tscPASS; workers/productivity.md |
| Current review_round3 | Separate author/reviewer; exactGoal contract, diff/privacy/modal review; caught compile/CSS/revocation defects |15independent diff tests pass (overlap subset, not additivecoverage); frozen hashes; workers/review-round3.md |

Each current report lists exact files/functions, actions, state observations,
errors, command results, source fingerprints and unexecuted gates. Root owns
integration, backend changes, full regression, GitHub commits and retained logs.
Commits:94fac58 Toolbox;ccbc64d backend;053b015 Knowledge;4eed0af Productivity;
738f528 frontend. Final CSS/docs/evidence are a separate follow-up commit.

## Deterministic workers and CI

Current controlled Playwright smoke used1worker across2viewport projects and4
cases. A separate sequential3width script is a deterministic browser probe, not
three AI reviewers. Component suites are test processes, not AI subagents.

At run39791037107 associated with head738f528, exactly6CIjobs were observed:
backend-unit,frontend-unit,filesystem-linux,filesystem-windows-acl,sql-api-
integration,browser-e2e. First3passed,Windowsfailed,SQLcompleted262pass/0fail/0skip; browser was
still running at the latest observation. Separate AgentBaseline39791037583passed. This is one observed
run, not an invented count for all automatically triggered/canceled intermediate
runs. Final source/run status and tested PR merge SHA are tracked in07verification.
