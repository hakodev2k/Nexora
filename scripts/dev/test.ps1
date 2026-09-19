$ErrorActionPreference = 'Stop'
python scripts/dev/verify-s00.py
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release --logger "trx;LogFileName=unit-xunit.trx" --results-directory artifacts/test-results --collect:"XPlat Code Coverage"
exit $LASTEXITCODE
