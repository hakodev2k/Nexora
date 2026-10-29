param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('windows', 'linux')]
    [string]$RequireOs
)

$ErrorActionPreference = 'Stop'
# Windows includes the real A/B/C helper and requires an elevated disposable runner.
# A missing administrator token is a failure, never a skip or a synthetic SID fallback.
Push-Location (Join-Path $PSScriptRoot '../..')
try {
    dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release -- --require-os $RequireOs
    $result = $LASTEXITCODE
} finally {
    Pop-Location
}
exit $result
