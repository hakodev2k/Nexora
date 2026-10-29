# R1 checkpoint8eb5aaff — actual CI evidence

Implementation HEAD `8eb5aaff54c493d27ae1a0baa7209bdadecda948`, branch
`impl/m01-s00-scaffold`, PR4 open Draft/unmerged. GitHub-hosted automatic CI
checkout `a0980039f41d351b4579483734fb8bd94e542abc`; commit metadata confirms its
tree equals HEAD exactly: `ddee8459389812d06539604844ba979b04b8517a`.
This report does not certify subsequent Toolbox/navigation edits.

Observed [implementation run40087636499](https://github.com/hakodev2k/Nexora/actions/runs/40087636499):
6started/6completed SUCCESS/0failed/0canceled. Independent
[Agent baseline40087636545](https://github.com/hakodev2k/Nexora/actions/runs/40087636545)
also SUCCESS. These jobs were automatically triggered by authorized push, not new
locally launched functional suites or newly created tests/fixtures.

| Layer / job | Actual result | Evidence |
|---|---|---|
| backend-unit120853705372 |131pass/0fail/0skip | Job log final xUnit summary; artifact12557617662 |
| frontend-unit120853705347 |201pass/0fail/1existing skip,25files | Vitest summary; artifact12558081276 |
| sql-api-integration120853705319 |290pass/0fail/0skip,2m17s | Real SQL/API job summary; artifact12557987020 |
| filesystem-windows-acl120853705326 |31pass/0fail/3Linux-only skips,34discovered | Actual Windows job summary; genuine A/B/C gate job SUCCESS |
| filesystem-linux120853705361 |32pass/0fail/2Windows-only skips,34discovered | Actual Linux job summary |
| browser-e2e120853705223 |12pass/0fail/0errors/0skip | Artifact12557408780 downloaded through connector reference; JUnit testcase/failure/error/skipped elements parsed |

Backend job repeats Linux filesystem checks; overlapping counts are not added
again. Browser artifact was read only; no image from it was republished. Raw ZIP
stays ignored under `.remediation-work`; temporary signed-reference file removed.
Initial sandbox download was socket-denied, then the authorized read succeeded
with command-local escalation. XML reader prohibited DTD/external resolution.
No secrets, raw request logs or synthetic screenshots are attached to this report.

Six actual AI agents and four separate-author review assignments in this increment
are documented in06 and per-worker reports. Source fixes include CR-01/COL-R01/
R24-PT-01 and R24-AUTH-01 (Toolbox subset also identified as R24-PT-02); their bounded
source closures do not mean every new operation has runtime acceptance coverage.
Existing tests passing is not a complete requirement/action/journey matrix.

Local checks remain compile only; no local functional cases, fixtures, API runtime,
migrations, browser, performance or ACL test launched. Plan readiness remains false.
No whole FX01–40 or R1 accepted. Full UI/popup/viewport/a11y/business concurrency,
upgrade/rollback and owner release gates remain open. Release recommendation:
**HOLD Draft; continue authorized implementation and owner functional QA.**
