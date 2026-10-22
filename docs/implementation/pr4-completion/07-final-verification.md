# Verification checkpoint — incomplete release

Actual application/test source snapshot digest:
`2282dac6600ac2e0ccff90c49b1302e4bbecf1c9a3a81ec58b8413d13f596b27`.
See evidence/tested-source-manifest.json (576source/test/migration files; SHA256
ordered path/content map). Documentation-only edits do not change that digest.
Code checkpoints are94fac58→ccbc64d→053b015→4eed0af→738f528 plus final scopedCSS.

| Check | Exact command / run | Result |
|---|---|---|
| Unit locked restore | dotnet restore tests/Nexora.UnitTests/Nexora.UnitTests.csproj --locked-mode -m:1 -nr:false | PASS |
| Backend xUnit | dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --no-restore -m:1 -nr:false --logger trx |54pass/0fail/0skip; evidence/backend/unit.log andunit.trx |
| Required Linux filesystem | dotnet tests/Nexora.UnitTests/bin/Debug/net10.0/Nexora.UnitTests.dll --require-os linux |34discovered;32pass/0fail/2Windows-only skips; exit0 |
| Integration locked restore/build | dotnet restore/build tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --locked-mode / --no-restore -m:1 -nr:false | PASS;build0warnings/0errors; no SQL runtime inferred |
| Frontend final fullsuite | npm run test:unit -- --maxWorkers=1 |184pass/0fail/1existing skip,185cases20files;42.65s; evidence/frontend-full.log+JUnit |
| Frontend final build | npm run build | TypeScript/VitePASS; existing>500kBchunk warning retained;2.54sVite stage |
| Controlled browser | Playwrightui-smoke.config.ts; owned Vite+browser same exec; headless-shell153 |4pass/0fail/0skip; controlled HTTP, notSQL |
| Three widths | Controlled Home/Finance320/768/1440probe |3pass/0fail after finalCSS;9PNGs+caseJSON |
| Independent review | review_round3 exact source hashes; actual15diff tests | Bounded source review completed; allR1/security/OS acceptance not claimed |
| SQL checkpoint94fac58 | CI39749855849,job119727389510 |260pass/2fail/0skip,262total; two historical Markdown/D07oracles nowcorrected; actual checkout mergef21d4a4, not literalbranchHEAD |
| New code CI738f528 | CI39791037107 | Backend/frontend/Linuxpass; Windowsfail; SQL262pass/0fail/0skip; browserpending. Actual tested merge6e2c08c for branch738f528. Final CSS/docs checkpoint requires its own current-head CI |
| Agent baseline | CI39791037583 | PASS; agent-kit check is not product verification |
| Windows A/B/C | Existing mandatory actual distinct-principal/process gate | FAIL/UNIMPLEMENTED; not exercised on Linux, protection not weakened |
| Upgrade/rollback,allSQL/browserAC,fullUIUX | Required complete matrices | NOT CERTIFIED / verification pending |

Run URLs:
https://github.com/hakodev2k/Nexora/actions/runs/39791037107
https://github.com/hakodev2k/Nexora/actions/runs/39749855849

Test counts from overlapping focused/independent suites are not added to the
fullsuite count. Initial fixture/namespace/test-helper errors and their fixes are
documented in worker reports. Screenshots alone or sampled axe are not complete
visual/accessibility certification. Historical missing/lost artifacts cannot
prove tests on current source. No wholeFX or R1 marked ACCEPTED.

## Fresh SQL evidence

CI39791037107 job119866803167 completed SUCCESS:262passed/0failed/0skipped,2m16s.
The actual checkout was merge6e2c08c of738f528 into main8782f46; this is not
a literal branch-HEAD checkout. This closes the two old SQL oracle failures
for that tested source, including the10Toolbox regression cases in the suite.
It does not certify all733actions or final later CSS/documentation commit.
