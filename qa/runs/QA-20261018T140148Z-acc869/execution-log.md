# Actual execution ledger

Run QA-20261018T140148Z-acc869; commands/logs retained, no claim of remote CI.

- baseline Git remote/branch/HEAD/status and612 hashes; environment-runtime.json.
- dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj -c Release --no-restore --logger trx: compile fail (backend-unit.log).
- npm run test:unit -- --reporter=default --reporter=junit --outputFile=<QA path>: exit1,125/5/1; configured reporter ignored output location, generated existing JUnit overwritten (known deviation).
- dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj -c Release --no-restore with explicit integrated loopback generated test connection: exit1,193/5,SQL real and fixture-owned cleanup; results/sql-integration.trx.
- Operator and Nexora.Local Release builds --no-restore exit0/0warnings/0errors; frontend production npm run build exit0,3.73s/existing chunk warning. API compiled through integration build; no separate API standalone build claimed for this QA run.
- scripts/dev/start-isolated-browser.ps1: generated ownership-marked DB,86 migrations,8 synthetic accounts; readiness200. Production preview restarted through owned PID/start-time guard.
- node browser-qa.mjs: attempt1 retained (harness/default-gate failures); corrected focused attempt10 PASS after genuine policy setup;195 total route observations,78 axe scans; production5-sample timing; offline boundary.
- node recovery-security.mjs:3PASS, real-commit lost response, dirty revocation and anonymous401.
- node document-journey.mjs:PASS current UI/version/archive/escaped HTML+SQL.
- node mobile-worklog.mjs: initial parser failure retained; retestPASS all native lifecycle/history at390x844.
- node finance-journey.mjs:PASS explicit VND/decimal/date/SQL/usage guard.
- node visual-probe.mjs: keyboard dirty guardPASS; four final overflow/duplicate repros. Internal screenshots inspected, not report attachments.
- qa-unit.csproj offline restore: sandbox config-read failures; host-principal cached restore exit0. Initial build failed due live API DLL locks; -p:BuildProjectReferences=false continuation46PASS/1FAIL/2excluded cursor cases, not original suite pass.
- dotnet run qa-unit.csproj -c Release --no-build -- --require-os windows:30PASS/0FAIL/4SKIP, exit1 because required native-principal case blocked.
- run-functional.ps1 with QA output config and genuine dev/API/SQL: all56 attempted,45PASS/11FAIL,13.0minutes,0skip/retry. No original test source/assertion changed.
- Read-only SQL ID/owner inventory1067 rows and schema/DBCC proof, before cleanup.
- Final source fingerprint verification and exact owned runtime cleanup recorded separately.

Finalization: reporter CSV schema normalized;20 findings (News legacy-heading failure included). First log collection failed due active file handle; retried read-sharing successfully, no secret values printed. Official ownership-guarded cleanup succeeded; DB_ID is NULL, private runtime absent. Source hash612/612 unchanged. Internal screenshot files removed. Planned obligations remain separate from actual executions.
