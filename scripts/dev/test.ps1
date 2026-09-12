$ErrorActionPreference = 'Stop'
python3 scripts/dev/verify-s00.py
dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release
