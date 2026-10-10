$email = Read-Host 'Bootstrap SuperAdmin email'
$securePassword = Read-Host 'Bootstrap SuperAdmin password (not echoed)' -AsSecureString
$password = [System.Net.NetworkCredential]::new('', $securePassword).Password
$env:NEXORA_BOOTSTRAP_EMAIL = $email
$env:NEXORA_BOOTSTRAP_PASSWORD = $password
if (-not $env:NEXORA_BOOTSTRAP_TIMEZONE) { $env:NEXORA_BOOTSTRAP_TIMEZONE = 'UTC' }

dotnet run --project src/Nexora.Bootstrap/Nexora.Bootstrap.csproj --configuration Release
