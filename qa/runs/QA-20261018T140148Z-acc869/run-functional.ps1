param([Parameter(Mandatory=$true)][string]$MetadataPath,[switch]$List)
$ErrorActionPreference='Stop'
$m=Get-Content -LiteralPath $MetadataPath -Raw|ConvertFrom-Json
if($m.runId -ne 'nexora-e2e-audit-85b9ce58db3445e7897dfaa7385d32d4'){throw 'Foreign runtime'}
$vars=Get-Content -LiteralPath $m.environment -Raw|ConvertFrom-Json
foreach($key in $vars.PSObject.Properties){[Environment]::SetEnvironmentVariable($key.Name,[string]$key.Value,'Process')}
$env:NEXORA_E2E_BASE_URL=$m.baseUrl;$env:NEXORA_E2E_ACCOUNTS=$m.accounts
$env:NEXORA_E2E_SQL_OPERATOR=$m.sqlOperator;$env:NEXORA_E2E_OPERATOR_CLI=$m.captureOperator
$env:NEXORA_E2E_RUN_ID=$m.runId;$env:NEXORA_E2E_LOGIN_PACING_MS='7000'
$cli=Join-Path $m.repository 'web/Nexora.Web/node_modules/@playwright/test/cli.js'
$config=Join-Path $PSScriptRoot 'functional.config.mjs'
if($List){& node $cli test --config $config --list}else{& node $cli test --config $config}
exit $LASTEXITCODE
