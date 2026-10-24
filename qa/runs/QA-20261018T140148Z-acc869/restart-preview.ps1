param([Parameter(Mandatory=$true)][string]$MetadataPath,[ValidateSet('preview','dev')][string]$Mode='preview')
$ErrorActionPreference='Stop'
$m=Get-Content -LiteralPath $MetadataPath -Raw|ConvertFrom-Json
if($m.runId -ne 'nexora-e2e-audit-85b9ce58db3445e7897dfaa7385d32d4' -or $m.ownerSid -ne [Security.Principal.WindowsIdentity]::GetCurrent().User.Value){throw 'Foreign QA runtime'}
$web=Get-Process -Id $m.webPid
if($web.StartTime.ToUniversalTime().Ticks -ne ([DateTimeOffset]$m.webStartedAt).UtcTicks){throw 'Reused PID'}
Stop-Process -Id $web.Id
$entry=Join-Path $m.repository 'web/Nexora.Web/node_modules/vite/bin/vite.js'
$argsList=@('"'+$entry+'"');if($Mode -eq 'preview'){$argsList+='preview'};$argsList+=@('--host','127.0.0.1','--port','5173','--strictPort')
$p=Start-Process -FilePath (Get-Command node).Source -ArgumentList $argsList -WorkingDirectory (Join-Path $m.repository 'web/Nexora.Web') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $m.runRoot ($Mode+'.log')) -RedirectStandardError (Join-Path $m.runRoot ($Mode+'-error.log'))
$m.webPid=$p.Id;$m.webStartedAt=$p.StartTime.ToUniversalTime().ToString('O')
$m|ConvertTo-Json|Set-Content -LiteralPath $MetadataPath -Encoding utf8
Write-Output "Owned frontend restarted as Vite $Mode; product configuration untouched."
