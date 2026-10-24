# Windows-only real-token ACL gate. Use an elevated disposable Windows CI runner.
# No existing account, policy, production directory or application config is modified.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProbeDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$started = [DateTime]::UtcNow
Write-Output "Windows A/B/C gate started UTC: $($started.ToString('o'))"
if ($env:OS -ne 'Windows_NT' -or -not [Environment]::Is64BitProcess) {
    throw 'Real A/B/C gate requires 64-bit Windows PowerShell.'
}
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$isAdmin = ([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole(
    [Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Output "Actual controller SID: $($identity.User.Value); elevated administrator: $isAdmin"
if (-not $isAdmin) {
    Write-Output "Windows A/B/C gate ended UTC: $([DateTime]::UtcNow.ToString('o')); BLOCKED before mutation"
    throw 'BLOCKED: elevated local administrator token required to create/remove three ephemeral standard users. No principals or files were created.'
}
Import-Module Microsoft.PowerShell.LocalAccounts -ErrorAction Stop
$source = (Resolve-Path -LiteralPath $ProbeDirectory).Path
if (-not (Test-Path -LiteralPath (Join-Path $source 'Nexora.UnitTests.exe') -PathType Leaf)) {
    throw 'Build the Release custom runner before invoking this helper.'
}

# Reject links before descending; cleanup may only remove this unique run directory.
function Assert-NoReparseTree([string]$Path) {
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Refusing to copy/delete a reparse point in the synthetic test tree.'
    }
    if ($item.PSIsContainer) {
        foreach ($child in Get-ChildItem -LiteralPath $Path -Force) {
            Assert-NoReparseTree $child.FullName
        }
    }
}
Assert-NoReparseTree $source
$runId = [Guid]::NewGuid().ToString('N')
$base = [IO.Path]::GetFullPath($env:ProgramData).TrimEnd('\')
$root = Join-Path $base "NexoraAcl-$runId"
$root = [IO.Path]::GetFullPath($root)
if ((Split-Path -Parent $root) -ne $base -or (Test-Path -LiteralPath $root)) {
    throw 'Refusing an existing or out-of-bound test root.'
}
$accounts = @()
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()
$cleanupErrors = [Collections.Generic.List[string]]::new()
$failed = $false
$messageId = [Guid]::NewGuid().ToString('N')
$workspace = Join-Path $root 'workspace'
$stage = Join-Path $root 'bin'
$logs = Join-Path $root 'logs'

function Invoke-Probe([int]$AccountIndex, [string]$Mode) {
    $account = $accounts[$AccountIndex]
    $stdout = Join-Path $logs "$Mode-$AccountIndex-$([Guid]::NewGuid().ToString('N')).out"
    $stderr = "$stdout.err"
    # All arguments are generated here. Passwords only travel in PSCredential, never argv/env/logs.
    $arguments = @('--windows-capture-principal-probe', $Mode, ('"' + $workspace + '"'),
        $accounts[0].Sid.Value, $accounts[1].Sid.Value, $account.Sid.Value, $messageId)
    $process = Start-Process -FilePath (Join-Path $stage 'Nexora.UnitTests.exe') `
        -ArgumentList $arguments -Credential $account.Credential -WorkingDirectory $stage `
        -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr -PassThru
    $processes.Add($process)
    if (-not $process.WaitForExit(45000)) {
        # Kill only this recorded child, never an image-name/global process set.
        $process.Kill()
        $process.WaitForExit()
        throw "Principal operation timed out: $Mode"
    }
    $process.WaitForExit()
    $process.Refresh()
    if ($process.ExitCode -ne 0) {
        throw "Principal operation failed: $Mode; exit=$($process.ExitCode). Raw payload output suppressed."
    }
    $attestation = @(Get-Content -LiteralPath $stdout)
    $expected = "[principal] ${Mode}: actualSid=$($account.Sid.Value); pid=$($process.Id); passed"
    if ($attestation.Count -ne 1 -or $attestation[0] -cne $expected) {
        throw "Missing actual-token/process attestation for $Mode."
    }
    Write-Output $expected
}

try {
    foreach ($role in @('a', 'b', 'c')) {
        $name = 'nxacl' + $runId.Substring(0, 10) + $role
        if (Get-LocalUser -Name $name -ErrorAction SilentlyContinue) { throw 'Ephemeral principal collision.' }
        $bytes = New-Object byte[] 32
        $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
        try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
        $password = ConvertTo-SecureString ('Nx9!' + [Convert]::ToBase64String($bytes)) -AsPlainText -Force
        [Array]::Clear($bytes, 0, $bytes.Length)
        $user = New-LocalUser -Name $name -Password $password -AccountExpires ([DateTime]::Now.AddHours(2)) `
            -Description "Nexora disposable ACL verification $runId" -UserMayNotChangePassword
        # Record immediately so later setup failures still remove this exact newly created SID.
        $accounts += [pscustomobject]@{
            Name = $name; Sid = $user.SID
            Credential = [Management.Automation.PSCredential]::new("$env:COMPUTERNAME\$name", $password)
        }
        Add-LocalGroupMember -SID ([Security.Principal.SecurityIdentifier]::new('S-1-5-32-545')) -Member $user
    }
    if (@($accounts | ForEach-Object { $_.Sid.Value } | Select-Object -Unique).Count -ne 3) {
        throw 'A/B/C must resolve to three distinct real local account SIDs.'
    }
    [void][IO.Directory]::CreateDirectory($root)
    $acl = [Security.AccessControl.DirectorySecurity]::new()
    $acl.SetAccessRuleProtection($true, $false)
    $inherit = [Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit'
    $none = [Security.AccessControl.PropagationFlags]::None
    $allow = [Security.AccessControl.AccessControlType]::Allow
    foreach ($sid in @($identity.User, [Security.Principal.SecurityIdentifier]::new('S-1-5-18'))) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $sid, [Security.AccessControl.FileSystemRights]::FullControl, $inherit, $none, $allow))
    }
    foreach ($account in $accounts) {
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            $account.Sid, [Security.AccessControl.FileSystemRights]::ReadAndExecute, $inherit, $none, $allow))
    }
    Set-Acl -LiteralPath $root -AclObject $acl
    [void][IO.Directory]::CreateDirectory($stage)
    [void][IO.Directory]::CreateDirectory($logs)
    [void][IO.Directory]::CreateDirectory($workspace)
    $workspaceAcl = Get-Acl -LiteralPath $workspace
    $workspaceAcl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $accounts[0].Sid, [Security.AccessControl.FileSystemRights]::FullControl, $inherit, $none, $allow))
    Set-Acl -LiteralPath $workspace -AclObject $workspaceAcl
    Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $stage -Recurse -Force
    Invoke-Probe 0 'publish'
    Invoke-Probe 1 'read'
    Invoke-Probe 2 'deny'
    Invoke-Probe 0 'read' # New A process proves stable configured runtime after restart.
    Invoke-Probe 1 'read' # New B process proves stable operator policy after restart.
}
catch {
    $failed = $true
    Write-Error -Message $_.Exception.Message -ErrorAction Continue
}
finally {
    foreach ($process in $processes) {
        try {
            if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        } catch { $cleanupErrors.Add('Could not stop a recorded principal child.') }
    }
    if ((Test-Path -LiteralPath $workspace) -and $accounts.Count -eq 3) {
        try { Invoke-Probe 0 'cleanup' }
        catch { $cleanupErrors.Add("Capture cleanup failed. Retained synthetic root: $root") }
    }
    if (Test-Path -LiteralPath $root) {
        try {
            # Do not broaden the capture DACL to let the controller inspect/delete it.
            if ($cleanupErrors.Count -ne 0) { throw 'Preserving root for explicit operator recovery.' }
            $resolved = (Resolve-Path -LiteralPath $root).Path
            if ($resolved -ne $root -or (Split-Path -Parent $resolved) -ne $base) { throw 'Cleanup root mismatch.' }
            Assert-NoReparseTree $resolved
            Remove-Item -LiteralPath $resolved -Recurse -Force
            if (Test-Path -LiteralPath $resolved) { throw 'Synthetic root still exists.' }
        } catch { $cleanupErrors.Add("Root cleanup failed: $root") }
    }
    foreach ($account in $accounts) {
        try {
            $actual = Get-LocalUser -SID $account.Sid -ErrorAction Stop
            if ($actual.Name -cne $account.Name) { throw 'Principal changed during run.' }
            Remove-LocalUser -SID $account.Sid
            if (Get-LocalUser -SID $account.Sid -ErrorAction SilentlyContinue) { throw 'Principal still exists.' }
        } catch { $cleanupErrors.Add("Account cleanup failed: $($account.Name); SID=$($account.Sid.Value)") }
        $account.Credential.Password.Dispose()
    }
    foreach ($process in $processes) { $process.Dispose() }
    $identity.Dispose()
    Write-Output "Windows A/B/C gate ended UTC: $([DateTime]::UtcNow.ToString('o')); elapsed seconds=$([Math]::Round(([DateTime]::UtcNow - $started).TotalSeconds, 2))"
}
if ($cleanupErrors.Count -ne 0) {
    foreach ($cleanupError in $cleanupErrors) { Write-Error $cleanupError -ErrorAction Continue }
    exit 1
}
if ($failed) { exit 1 }
Write-Output 'Real Windows A/B/C gate passed; ephemeral users and synthetic files removed.'
exit 0
