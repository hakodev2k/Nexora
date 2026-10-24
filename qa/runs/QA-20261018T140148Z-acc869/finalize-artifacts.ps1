$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskCases = @(Import-Csv (Join-Path $taskRoot 'test-cases.csv'))
$taskCases | Where-Object { $_.CaseId -like 'ENDPOINT:*' } | Export-Csv (Join-Path $taskRoot 'planned-case-obligations.csv') -NoTypeInformation -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskRoot 'executed-cases.csv') -Destination (Join-Path $taskRoot 'test-cases.csv') -Force
$taskFindings = @(Get-Content (Join-Path $taskRoot 'findings.json') -Raw | ConvertFrom-Json)
if ('NQA-020' -notin $taskFindings.ID) {
 $taskNews = $taskFindings[16].PSObject.Copy()
 $taskNews.ID = 'NQA-020'
 $taskNews.Title = 'News journey waits for a legacy category-page heading'
 $taskNews.RouteOrEndpoint = 'web/Nexora.Web/functional/platform.spec.ts:31; FX29 News route'
 $taskNews.StepsToReproduce = 'Run the existing News category journey after its genuine module enable/grant prerequisites; wait for h1 News categories.'
 $taskNews.ExpectedContract = 'Locate the current News reader/category management using current feature and UX contracts; navigation alone cannot prove category CRUD.'
 $taskNews.ActualImpact = 'Legacy heading wait times out against the current reader layout before category assertions complete. Remaining operations are NOT_RUN in this journey.'
 $taskNews.ReproRate = '1/1'
 $taskNews.EvidenceRefs = 'functional-results.json;functional-execution.log;functional-output/'
 $taskNews.LikelyCause = 'Hypothesis: obsolete heading locator; requires contract and current DOM review before changing test. No product defect confirmed.'
 $taskNews.RecommendedFix = 'Review current category entry point and accessible heading, update navigation prerequisites/selectors without weakening category CRUD assertions, then rerun against real API/SQL.'
 $taskNews.RegressionAndClosure = 'Category create/edit/order/denial and persistence assertions execute on the current UI, with no bypass or forced click.'
 $taskFindings += $taskNews
}
$taskFindings | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $taskRoot 'findings.json') -Encoding utf8
$taskFindings | Export-Csv (Join-Path $taskRoot 'findings.csv') -NoTypeInformation -Encoding utf8
$taskText = Get-Content (Join-Path $taskRoot 'findings.md') -Raw
$taskText = $taskText.Replace('19 tickets:2','20 tickets:2').Replace('15 test/verification','16 test/verification')
if ($taskText -notmatch '## NQA-020') {
 $taskText += "`n## NQA-020 — $($taskNews.Title)`n`n" + (($taskNews.PSObject.Properties | ForEach-Object { '**' + $_.Name + '**: ' + $_.Value }) -join "`n`n") + "`n"
}
$taskText | Set-Content (Join-Path $taskRoot 'findings.md') -Encoding utf8
$taskSummary = Get-Content (Join-Path $taskRoot 'summary.md') -Raw
$taskSummary = $taskSummary.Replace('and15 test/verification','and16 test/verification')
$taskSummary += "`nReport correction: planned-case-obligations.csv holds unexpanded NOT_RUN templates; test-cases.csv now uses the same consistent schema as executed-cases.csv. These templates are not designed/executed acceptance cases. NQA-020 tracks the News heading failure. Goal numeric definition update was exercised; definition removal was not tested.`n"
$taskSummary | Set-Content (Join-Path $taskRoot 'summary.md') -Encoding utf8
$taskResults = Get-Content (Join-Path $taskRoot 'qa-results.json') -Raw | ConvertFrom-Json
$taskResults.findings = $taskFindings.Count
$taskResults | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $taskRoot 'qa-results.json') -Encoding utf8
Import-Csv (Join-Path $taskRoot 'feature-test-matrix.csv') | Where-Object ImplementationState -eq 'NOT_IMPLEMENTED' | Export-Csv (Join-Path $taskRoot 'implementation-gaps.csv') -NoTypeInformation -Encoding utf8
Write-Output ('Reports normalized; findings=' + $taskFindings.Count + '; executed rows=' + @(Import-Csv (Join-Path $taskRoot 'test-cases.csv')).Count)
