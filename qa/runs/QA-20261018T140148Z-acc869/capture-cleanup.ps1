$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskMetadataPath = 'C:/Users/hakodev/AppData/Local/Temp/nexora-e2e-audit-85b9ce58db3445e7897dfaa7385d32d4/runtime.json'
$taskMeta = Get-Content -LiteralPath $taskMetadataPath -Raw | ConvertFrom-Json
$taskSecrets = [Collections.Generic.List[string]]::new()
function Find-SecretValues($taskValue) {
 if ($null -eq $taskValue) { return }
 if ($taskValue -is [string]) { return }
 if ($taskValue -is [System.Collections.IEnumerable] -and $taskValue -isnot [pscustomobject]) { foreach ($taskItem in $taskValue) { Find-SecretValues $taskItem }; return }
 foreach ($taskProperty in $taskValue.PSObject.Properties) {
  if ($taskProperty.Name -match '(?i)password|secret|token|key' -and $taskProperty.Value -is [string] -and $taskProperty.Value.Length -gt 5) { $taskSecrets.Add($taskProperty.Value) }
  else { Find-SecretValues $taskProperty.Value }
 }
}
Find-SecretValues (Get-Content -LiteralPath $taskMeta.accounts -Raw | ConvertFrom-Json)
Find-SecretValues (Get-Content -LiteralPath $taskMeta.environment -Raw | ConvertFrom-Json)
function Redact-Text([string]$taskText) {
 foreach ($taskSecret in $taskSecrets) { $taskText = $taskText.Replace($taskSecret, '[REDACTED]') }
 $taskText = $taskText -replace '(?i)(/share/)[A-Za-z0-9_-]{24,}', '$1[REDACTED]'
 $taskText = $taskText -replace '(?i)(Bearer\s+)[A-Za-z0-9._~-]{20,}', '$1[REDACTED]'
 return $taskText
}
$taskServer = ''
foreach ($taskName in @('api.log','api-error.log')) {
 $taskLog = Join-Path $taskMeta.runRoot $taskName
 if (Test-Path -LiteralPath $taskLog) {
  $taskStream = [IO.FileStream]::new($taskLog,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::ReadWrite)
  $taskReader = [IO.StreamReader]::new($taskStream)
  try { $taskServer += "`n--- $taskName ---`n" + (Redact-Text ($taskReader.ReadToEnd())) } finally { $taskReader.Dispose() }
 }
}
[IO.File]::WriteAllText((Join-Path $taskRoot 'server-redacted.log'),$taskServer)
$taskRedactedFiles = 0
Get-ChildItem -LiteralPath $taskRoot -Recurse -File | Where-Object { $_.Extension -in @('.log','.json','.xml','.trx','.md','.csv') -and $_.FullName -notmatch '\\(bin|obj|lock-backups)\\' } | ForEach-Object {
 $taskOld = [IO.File]::ReadAllText($_.FullName)
 $taskNew = Redact-Text $taskOld
 if ($taskOld -cne $taskNew) { [IO.File]::WriteAllText($_.FullName,$taskNew); $taskRedactedFiles++ }
}
$taskCleanup = & (Join-Path (Get-Location) 'scripts/dev/stop-isolated-browser.ps1') -MetadataPath $taskMetadataPath
$taskConnection = [Data.SqlClient.SqlConnection]::new('Server=localhost\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;Encrypt=True;TrustServerCertificate=True')
try {
 $taskConnection.Open()
 $taskCommand = $taskConnection.CreateCommand()
 $taskCommand.CommandText = 'SELECT DB_ID(@Name);'
 $null = $taskCommand.Parameters.AddWithValue('@Name',$taskMeta.database)
 $taskAbsent = $taskCommand.ExecuteScalar() -is [DBNull]
} finally { $taskConnection.Dispose() }
if (!$taskAbsent -or (Test-Path -LiteralPath $taskMeta.runRoot)) { throw 'Cleanup verification failed' }
$taskManifestPath = Join-Path $taskRoot 'seed-manifest.json'
$taskManifest = Get-Content -LiteralPath $taskManifestPath -Raw | ConvertFrom-Json
$taskManifest | Add-Member -Force NoteProperty cleanup ([ordered]@{status='COMPLETED';utc=[DateTime]::UtcNow.ToString('O');databaseAbsent=$taskAbsent;privateRuntimeAbsent=$true;method='Official exact ownership-marker/SID/PID-start-time guarded stop-isolated-browser.ps1';redactedFiles=$taskRedactedFiles;retained='Synthetic IDs and redacted QA artifacts only; no active test accounts/grants'})
$taskManifest | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $taskManifestPath -Encoding utf8
@($taskCleanup,'Database absence verified; private credential/runtime directory absent.','Known synthetic secret values and share-token/Bearer patterns redacted before runtime deletion.','Redacted files: ' + $taskRedactedFiles) | Set-Content (Join-Path $taskRoot 'cleanup.log') -Encoding utf8
Write-Output 'Owned runtime cleanup verified. No secret values emitted.'
