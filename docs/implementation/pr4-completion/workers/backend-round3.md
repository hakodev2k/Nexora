# Backend continuation — 2026-10-22

Owner: root. Baseline94fac585ee4f24edf0743f92e9b8dd396c2abbd0.
Current source rehydrated and Git blob hashes verified,1698files.
Authorization: user MASTER PROMPT and continuation, DEC014/D01–D09 retained.
Repository nexora-engineering, profile/routing, technical-lead and security/testing
rules loaded. Historical code-only text preserved; later explicit isolated QA
authorization applies, as recorded in current PO decision and PR.

## Changed behavior and contracts

ActionGrantPolicy now admits exactly five shipped Goals SELF actions to Admin
grant eligibility: goals.goal.history/archive/unarchive, goals.target.update/remove.
Sources: action catalog16, migrations0077–0079 and matching remediation contracts.
No actual permission Allow, module flag, runtime session check or source rights
changed. Unshipped goals.target.link_task remains denied in regression assertions.

Unit migration oracle independently lists86ordered files through0085, matching
the existing production manifest. Restore-version/source-open eligibility test
is reconciled to shipped0059/0064 contracts; SYSTEM/organization assignment deny
assertions remain. Windows required actual-principal gate is unchanged.

LocalAccountMessageSinkTests now prepends current assembly only under the dotnet
host; apphost behavior is unchanged. Removed premature Dispose that masked child
startup error during catch cleanup. Actual child-process verification is separate.

D07 SQL assertion expects exactly owner revoked metadata inactive/token-null;
viewer/detail404 and active filtering assertions remain. Markdown SQL assertion
accepts literal stored script text under safe renderer contract, with no HTML
execution permission. Reviewer caught an incorrect tuple helper use before build;
author corrected it to AssertStringPropertyAsync. Renderer security proof remains
separate from persistence proof.

## Actual verification

- Locked .NET10.0.100 unit restore PASS (-m:1 -nr:false).
- dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore
  -m:1 -nr:false --logger trx:54passed/0failed/0skipped,150ms test duration.
- Integration locked restore/build PASS,0warnings/0errors. Fresh CI738f528 merge6e2c08c SQL262pass/0fail/0skip; final later head requires separate checks.
- Independent reviewer: review_round3, source contracts checked; see review report.
- No new grants, provider calls, SQL migration changes or merge performed.

Prior lost-session test results are not used as current evidence. Logs/TRX are
copied into this report's evidence directory in the final evidence checkpoint.
Whole FX01–FX10/16/24 and full R1 remain incomplete, not accepted.
