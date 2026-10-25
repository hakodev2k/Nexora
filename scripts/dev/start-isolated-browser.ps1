param([string]$SqlServer = 'localhost\SQLEXPRESS')
$ErrorActionPreference = 'Stop'
$taskRepo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($SqlServer -notin @('localhost\SQLEXPRESS', '.\SQLEXPRESS')) { throw 'Only the explicitly local SQL Express test instance is supported.' }
foreach ($taskPort in @(5001, 5173)) {
    if (Get-NetTCPConnection -LocalPort $taskPort -State Listen -ErrorAction SilentlyContinue) { throw "Loopback port $taskPort is already owned by another process; it will not be stopped." }
}
$taskSuffix = [guid]::NewGuid().ToString('N')
$taskRunId = 'nexora-e2e-audit-' + $taskSuffix
$taskDatabase = 'Nexora_Test_' + $taskSuffix
$taskRunRoot = Join-Path ([IO.Path]::GetTempPath()) $taskRunId
$taskSid = [Security.Principal.WindowsIdentity]::GetCurrent().User
New-Item -ItemType Directory -Path $taskRunRoot | Out-Null
$taskAcl = New-Object Security.AccessControl.DirectorySecurity
$taskAcl.SetOwner($taskSid)
$taskAcl.SetAccessRuleProtection($true, $false)
$taskAcl.AddAccessRule((New-Object Security.AccessControl.FileSystemAccessRule($taskSid, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
Set-Acl -LiteralPath $taskRunRoot -AclObject $taskAcl
$taskMetadataPath = Join-Path $taskRunRoot 'runtime.json'
$taskMaster = New-Object Data.SqlClient.SqlConnection("Server=$SqlServer;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True")
$taskCreated = $false
$taskApi = $null
$taskWeb = $null
$taskEnvironmentKeys = @('DOTNET_ENVIRONMENT','ASPNETCORE_ENVIRONMENT','ASPNETCORE_URLS','NEXORA_SQL_CONNECTION_STRING','ConnectionStrings__NexoraSql','NEXORA_MIGRATIONS_DIR','NEXORA_E2E_ACCOUNTS','NEXORA_LOCAL_MESSAGE_CAPTURE_PATH','NEXORA_LOCAL_FILE_STORAGE_PATH','Nexora__LocalAccountMessageCapturePath','Nexora__LocalFileStoragePath','NEXORA_IDEMPOTENCY_SECRET','NEXORA_CSRF_SECRET','NEXORA_LOCAL_MESSAGE_KEY','NEXORA_MODULE_PREVIEW_SECRET','Automation__LocalTaskExecutionEnabled','Automation__LocalSchedulingEnabled','NEXORA_LOCAL_MESSAGE_OPERATOR_SID','NEXORA_LOCAL_MESSAGE_RUNTIME_SID','Nexora__LocalAccountMessageOperatorSid','Nexora__LocalAccountMessageRuntimeSid','NEXORA_ALLOWED_ORIGINS')
$taskPreviousEnvironment = @{}
foreach ($taskKey in $taskEnvironmentKeys) { $taskPreviousEnvironment[$taskKey] = [Environment]::GetEnvironmentVariable($taskKey, 'Process') }
function New-RunSecret {
    $taskBytes = New-Object byte[] 32
    $taskRandom = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $taskRandom.GetBytes($taskBytes); return [Convert]::ToBase64String($taskBytes) } finally { $taskRandom.Dispose() }
}
try {
    $taskMaster.Open()
    $taskAbsent = $taskMaster.CreateCommand()
    $taskAbsent.CommandText = 'SELECT COUNT(*) FROM sys.databases WHERE name=@Name;'
    $null = $taskAbsent.Parameters.AddWithValue('@Name', $taskDatabase)
    if ([int]$taskAbsent.ExecuteScalar() -ne 0) { throw 'The generated test database unexpectedly exists; refusing to seed it.' }
    $taskCreate = $taskMaster.CreateCommand()
    $taskCreate.CommandText = "CREATE DATABASE [$taskDatabase];"
    $null = $taskCreate.ExecuteNonQuery(); $taskCreated = $true
    $taskMark = $taskMaster.CreateCommand()
    $taskMark.CommandText = "USE [$taskDatabase]; EXEC sys.sp_addextendedproperty @name=N'nexora.test.run',@value=@Run;"
    $null = $taskMark.Parameters.AddWithValue('@Run', $taskRunId)
    $null = $taskMark.ExecuteNonQuery()

    $env:DOTNET_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'https://localhost:5001'
    $env:NEXORA_SQL_CONNECTION_STRING = "Server=$SqlServer;Initial Catalog=$taskDatabase;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
    $env:ConnectionStrings__NexoraSql = $env:NEXORA_SQL_CONNECTION_STRING
    $env:NEXORA_MIGRATIONS_DIR = Join-Path $taskRepo 'database/migrations'
    $env:NEXORA_E2E_ACCOUNTS = Join-Path $taskRunRoot 'accounts.json'
    $env:NEXORA_LOCAL_MESSAGE_CAPTURE_PATH = Join-Path $taskRunRoot 'capture'
    $env:NEXORA_LOCAL_FILE_STORAGE_PATH = Join-Path $taskRunRoot 'files'
    $env:Nexora__LocalAccountMessageCapturePath = $env:NEXORA_LOCAL_MESSAGE_CAPTURE_PATH
    $env:Nexora__LocalFileStoragePath = $env:NEXORA_LOCAL_FILE_STORAGE_PATH
    $env:NEXORA_IDEMPOTENCY_SECRET = New-RunSecret
    $env:NEXORA_CSRF_SECRET = New-RunSecret
    $env:NEXORA_LOCAL_MESSAGE_KEY = New-RunSecret
    $env:NEXORA_MODULE_PREVIEW_SECRET = New-RunSecret
    $env:Automation__LocalTaskExecutionEnabled = 'false'
    $env:Automation__LocalSchedulingEnabled = 'false'
    $env:NEXORA_LOCAL_MESSAGE_OPERATOR_SID = $taskSid.Value
    $env:NEXORA_LOCAL_MESSAGE_RUNTIME_SID = $taskSid.Value
    $env:Nexora__LocalAccountMessageOperatorSid = $taskSid.Value
    $env:Nexora__LocalAccountMessageRuntimeSid = $taskSid.Value
    $env:NEXORA_ALLOWED_ORIGINS = 'http://localhost:5173,http://127.0.0.1:5173'

    $taskOperator = Join-Path $taskRepo 'tests/Nexora.FunctionalTestOperator/bin/Release/net10.0/Nexora.FunctionalTestOperator.dll'
    & dotnet $taskOperator seed
    if ($LASTEXITCODE -ne 0) { throw 'Generated-database browser fixture provisioning failed.' }
    $taskSqlOperator = [IO.Path]::ChangeExtension($taskOperator, '.exe')
    $taskCaptureOperator = Join-Path $taskRepo 'src/Nexora.Local/bin/Release/net10.0/Nexora.Local.exe'
    if (!(Test-Path -LiteralPath $taskSqlOperator) -or !(Test-Path -LiteralPath $taskCaptureOperator)) { throw 'Compile native local operator apphosts before startup.' }
    $taskRunEnvironment = @{}
    foreach ($taskKey in $taskEnvironmentKeys) { $taskRunEnvironment[$taskKey] = [Environment]::GetEnvironmentVariable($taskKey, 'Process') }
    $taskEnvironmentPath = Join-Path $taskRunRoot 'runtime-environment.json'
    # Contains generated synthetic signing keys only; inherits the current-SID-only ACL.
    $taskRunEnvironment | ConvertTo-Json | Set-Content -LiteralPath $taskEnvironmentPath -Encoding UTF8
    $taskApiDll = Join-Path $taskRepo 'src/Nexora.Api/bin/Release/net10.0/Nexora.Api.dll'
    $taskWebEntry = Join-Path $taskRepo 'web/Nexora.Web/node_modules/vite/bin/vite.js'
    $taskApi = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('"' + $taskApiDll + '"') -WorkingDirectory (Join-Path $taskRepo 'src/Nexora.Api') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskRunRoot 'api.log') -RedirectStandardError (Join-Path $taskRunRoot 'api-error.log')
    $taskWeb = Start-Process -FilePath (Get-Command node).Source -ArgumentList @('"' + $taskWebEntry + '"', '--host', '127.0.0.1', '--port', '5173', '--strictPort') -WorkingDirectory (Join-Path $taskRepo 'web/Nexora.Web') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $taskRunRoot 'web.log') -RedirectStandardError (Join-Path $taskRunRoot 'web-error.log')
    # Match the CI browser origin. Playwright's APIRequestContext does not send
    # Secure session cookies to HTTP 127.0.0.1, although browser fetch on loopback
    # can succeed; localhost keeps independent API assertions on the same origin.
    $taskMetadata = [ordered]@{ runId=$taskRunId; runRoot=$taskRunRoot; repository=$taskRepo; database=$taskDatabase; sqlServer=$SqlServer; ownerSid=$taskSid.Value; environment=$taskEnvironmentPath; accounts=$env:NEXORA_E2E_ACCOUNTS; sqlOperator=$taskSqlOperator; captureOperator=$taskCaptureOperator; capture=$env:NEXORA_LOCAL_MESSAGE_CAPTURE_PATH; apiPid=$taskApi.Id; apiStartedAt=$taskApi.StartTime.ToUniversalTime().ToString('O'); webPid=$taskWeb.Id; webStartedAt=$taskWeb.StartTime.ToUniversalTime().ToString('O'); baseUrl='http://localhost:5173' }
    $taskMetadata | ConvertTo-Json | Set-Content -LiteralPath $taskMetadataPath -Encoding UTF8
    # Readiness is polled by the caller using these exact owned process identities;
    # this metadata is private, contains no passwords or runtime signing keys.
    Write-Output "Isolated browser runtime metadata: $taskMetadataPath"
} catch {
    if ($null -ne $taskApi -and !$taskApi.HasExited) { Stop-Process -InputObject $taskApi -ErrorAction SilentlyContinue }
    if ($null -ne $taskWeb -and !$taskWeb.HasExited) { Stop-Process -InputObject $taskWeb -ErrorAction SilentlyContinue }
    if ($taskCreated -and $taskMaster.State -eq 'Open') {
        $taskDrop = $taskMaster.CreateCommand()
        $taskDrop.CommandText = "USE [master]; ALTER DATABASE [$taskDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$taskDatabase];"
        $null = $taskDrop.ExecuteNonQuery()
    }
    $taskResolvedCleanup = [IO.Path]::GetFullPath($taskRunRoot)
    $taskExpectedCleanup = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) $taskRunId))
    if ($taskResolvedCleanup -ne $taskExpectedCleanup -or (Get-Item -LiteralPath $taskResolvedCleanup).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Refusing an unexpected failure cleanup directory.' }
    Remove-Item -LiteralPath $taskResolvedCleanup -Recurse -Force
    throw
} finally {
    $taskMaster.Dispose()
    foreach ($taskKey in $taskEnvironmentKeys) { [Environment]::SetEnvironmentVariable($taskKey, $taskPreviousEnvironment[$taskKey], 'Process') }
}
