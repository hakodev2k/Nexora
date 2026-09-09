$ErrorActionPreference = 'Stop'
./scripts/dev/doctor.ps1 -Strict
python3 scripts/dev/verify-s00.py
dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release
dotnet run --project tests/Nexora.UnitTests/Nexora.UnitTests.csproj --configuration Release
npm install --prefix web/Nexora.Web
npm run build --prefix web/Nexora.Web
