$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskMatrix = @(Import-Csv (Join-Path $taskRoot 'feature-test-matrix.csv'))
foreach ($taskRow in $taskMatrix) {
 $taskLink = $null
 if ($taskRow.FeatureId -match '^ENDPOINT:WorkLogEndpoints:') {
  $taskLink = @{cases='QA-WRK-UI-CREATE;QA-WRK-INVALID;QA-WRK-OWNER;QA-WRK-SEED-PAGING;QA-WRK-REPLAY-ETAG;QA-WRK-LIFECYCLE;QA-RESPONSE-LOST;QA-REVOKE-DIRTY;QA-ANONYMOUS-HISTORY;QA-WRK-MOBILE-LIFECYCLE';evidence='browser-qa.mjs;browser-cases.json;recovery-security.mjs;recovery-security.json;mobile-worklog.mjs;mobile-worklog.json'}
 } elseif ($taskRow.FeatureId -match '^ENDPOINT:GoalsEndpoints:(16|22|28|53|63|81|107|117|125)$') {
  $taskLink = @{cases='QA-GOAL-HISTORY';evidence='browser-qa.mjs;browser-cases.json'}
 } elseif ($taskRow.FeatureId -match '^ENDPOINT:FinanceEndpoints:(15|25|29|52|62|69)$') {
  $taskLink = @{cases='QA-FINANCE-CURRENT-UI';evidence='finance-journey.mjs;finance-journey.json'}
 }
 if ($taskLink) {
  $taskRow.CaseIds += ';' + $taskLink.cases
  $taskRow.EvidenceRefs += ';' + $taskLink.evidence
  $taskRow.Limitation += ' Targeted PASS evidence linked as a case group; exact endpoint assertions are in executable harness. ExecutionStatus remains NOT_RUN for unexpanded per-operation obligations, not a denial of those targeted executions. Group link does not imply every case exercised every endpoint.'
 }
}
$taskMatrix | Export-Csv (Join-Path $taskRoot 'feature-test-matrix.csv') -NoTypeInformation -Encoding utf8
Add-Content (Join-Path $taskRoot 'summary.md') "`nFinal verification: final-source-verification.json confirms same HEAD/branch and 0 changes across612 source/config/test paths. Exact owned SQL database and private runtime were removed and verified absent (cleanup.log); synthetic IDs/redacted reports retained. Two internally inspected screenshots were deleted, not attached. 449 normalized executed-case records:426 PASS/22 FAIL/1 NOT_RUN across overlapping layers;30 additional passing custom checks and4 skips remain separately reported, never business requirement coverage. Detailed obligations remain unexecuted. Independent review Pending.`n"
Add-Content (Join-Path $taskRoot 'test-data-plan.md') "`nExecution qualification: original functional suites also created their own UUID-scoped fixtures in this exact isolated database. seed-manifest.json records targeted harness IDs; database-id-inventory.json captures1067 current ID/OwnerId rows including reference/catalog rows, not1067 newly created records. Previously deleted IDs from original suites are not individually exported. Full DB marker isolation and exact cleanup cover those rows; a complete per-entity provenance manifest for every old suite is unavailable. Goal definition removal and unexecuted data-plan variants are NOT_RUN.`n"
Add-Content (Join-Path $taskRoot 'execution-log.md') "`nFinalization: reporter CSV schema normalized;20 findings (News legacy-heading failure included). First log collection failed due active file handle; retried read-sharing successfully, no secret values printed. Official ownership-guarded cleanup succeeded; DB_ID is NULL, private runtime absent. Source hash612/612 unchanged. Internal screenshot files removed. Planned obligations remain separate from actual executions.`n"
$taskRequired = @('summary.md','environment.md','feature-test-matrix.csv','test-cases.csv','test-data-plan.md','seed-manifest.json','findings.md','findings.csv','execution-log.md','performance-results.csv','retest-plan.md')
foreach ($taskName in $taskRequired) { if ((Get-Item -LiteralPath (Join-Path $taskRoot $taskName)).Length -eq 0) { throw ('Empty required artifact: ' + $taskName) } }
Get-ChildItem -LiteralPath $taskRoot -File | Where-Object Name -ne 'evidence-manifest.csv' | ForEach-Object { [pscustomobject]@{Path=$_.Name;Bytes=$_.Length;SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash} } | Export-Csv (Join-Path $taskRoot 'evidence-manifest.csv') -NoTypeInformation -Encoding utf8
Write-Output 'Required artifacts verified; evidence hashes recorded; no product fix applied.'
