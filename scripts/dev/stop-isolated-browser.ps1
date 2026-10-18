param([Parameter(Mandatory=$true)][string]$MetadataPath)
$ErrorActionPreference = 'Stop'
$taskMetadata = Get-Content -LiteralPath $MetadataPath -Raw | ConvertFrom-Json
$taskSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
if ($taskMetadata.ownerSid -ne $taskSid -or $taskMetadata.runId -notmatch '^nexora-e2e-audit-[a-f0-9]{32}$' -or $taskMetadata.database -ne ('Nexora_Test_' + $taskMetadata.runId.Substring(17))) { throw 'The test runtime identity is not owned by this operator.' }
$taskExpectedRoot = [IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetTempPath()) $taskMetadata.runId)).TrimEnd('\')
$taskActualRoot = [IO.Path]::GetFullPath($taskMetadata.runRoot).TrimEnd('\')
if ($taskActualRoot -ne $taskExpectedRoot -or [IO.Path]::GetFullPath($MetadataPath) -ne (Join-Path $taskExpectedRoot 'runtime.json')) { throw 'The resolved cleanup target is not the exact named temporary run directory.' }
if ((Get-Item -LiteralPath $taskExpectedRoot).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'A test runtime directory cannot be a reparse point.' }
$taskAcl = Get-Acl -LiteralPath $taskExpectedRoot
if ($taskAcl.GetOwner([Security.Principal.SecurityIdentifier]).Value -ne $taskSid -or !$taskAcl.AreAccessRulesProtected) { throw 'Private test runtime directory ownership or ACL changed.' }
foreach ($taskRule in $taskAcl.GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier])) {
    if ($taskRule.AccessControlType -eq 'Allow' -and $taskRule.IdentityReference.Value -ne $taskSid) { throw 'A private test directory grants another identity access.' }
}
if ($taskMetadata.sqlServer -notin @('localhost\SQLEXPRESS', '.\SQLEXPRESS')) { throw 'Only the local test SQL instance can be cleaned.' }
foreach ($taskProcess in @(@{ id=$taskMetadata.apiPid; started=$taskMetadata.apiStartedAt }, @{ id=$taskMetadata.webPid; started=$taskMetadata.webStartedAt })) {
    $taskLive = Get-Process -Id $taskProcess.id -ErrorAction SilentlyContinue
    if ($null -ne $taskLive) {
        if ($taskLive.StartTime.ToUniversalTime().Ticks -ne ([DateTimeOffset]$taskProcess.started).UtcTicks) { throw 'Process identity changed; refusing to stop a reused PID.' }
        Stop-Process -Id $taskLive.Id
    }
}
$taskConnection = New-Object Data.SqlClient.SqlConnection("Server=$($taskMetadata.sqlServer);Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True")
try {
    $taskConnection.Open()
    $taskOwned = $taskConnection.CreateCommand()
    $taskOwned.CommandText = "SELECT COUNT(*) FROM [$($taskMetadata.database)].sys.extended_properties WHERE class=0 AND name=N'nexora.test.run' AND CONVERT(nvarchar(128),value)=@Run;"
    $null = $taskOwned.Parameters.AddWithValue('@Run', $taskMetadata.runId)
    if ([int]$taskOwned.ExecuteScalar() -ne 1) { throw 'Database ownership marker does not match; refusing cleanup.' }
    $taskDrop = $taskConnection.CreateCommand()
    $taskDrop.CommandText = "ALTER DATABASE [$($taskMetadata.database)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$($taskMetadata.database)];"
    $null = $taskDrop.ExecuteNonQuery()
} finally { $taskConnection.Dispose() }
# The fully resolved absolute path above was checked against the exact generated
# temporary directory. Native PowerShell handles deletion end-to-end.
Remove-Item -LiteralPath $taskExpectedRoot -Recurse -Force
Write-Output 'Owned browser processes, generated test database and private fixtures cleaned.'
