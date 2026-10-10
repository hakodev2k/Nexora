$ErrorActionPreference = 'Stop'
dotnet build src/Nexora.Api/Nexora.Api.csproj --configuration Release
dotnet build src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release
npm install --prefix web/Nexora.Web
npm run build --prefix web/Nexora.Web
