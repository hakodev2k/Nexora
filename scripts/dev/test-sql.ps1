$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($env:NEXORA_TEST_SQL_CONNECTION)) {
    [Console]::Error.WriteLine('BLOCKED: NEXORA_TEST_SQL_CONNECTION is required and must point to an isolated loopback Nexora_Test_<GUID> target.')
    exit 2
}

dotnet test tests/Nexora.IntegrationTests/Nexora.IntegrationTests.csproj --configuration Release --logger "trx;LogFileName=sql-api-integration.trx" --results-directory artifacts/test-results --collect:"XPlat Code Coverage"
exit $LASTEXITCODE
