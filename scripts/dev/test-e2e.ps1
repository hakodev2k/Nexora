$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($env:NEXORA_E2E_BASE_URL) -or
    [string]::IsNullOrWhiteSpace($env:NEXORA_E2E_OPERATOR_CLI) -or
    [string]::IsNullOrWhiteSpace($env:NEXORA_E2E_RUN_ID)) {
    [Console]::Error.WriteLine('BLOCKED: E2E needs a loopback synthetic target, local operator CLI, and isolated run id.')
    exit 2
}
npm ci --prefix web/Nexora.Web
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
npm run test:e2e --prefix web/Nexora.Web
exit $LASTEXITCODE
