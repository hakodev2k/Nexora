param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('windows', 'linux')]
    [string]$RequireOs
)

$ErrorActionPreference = 'Stop'
dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release -- --require-os $RequireOs
exit $LASTEXITCODE
