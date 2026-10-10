param([switch]$Strict)
$ErrorActionPreference = 'Stop'
$missing = $false

function Test-Required($Name, $Command) {
  $cmd = Get-Command $Command -ErrorAction SilentlyContinue
  if ($null -eq $cmd) {
    Write-Host "[missing] $Name ($Command)"
    $script:missing = $true
    return
  }
  $version = & $Command --version 2>$null | Select-Object -First 1
  Write-Host "[ok] $Name: $version"
}

function Test-Optional($Name, $Command) {
  $cmd = Get-Command $Command -ErrorAction SilentlyContinue
  if ($null -eq $cmd) {
    Write-Host "[optional-missing] $Name ($Command)"
    return
  }
  $version = & $Command --version 2>$null | Select-Object -First 1
  Write-Host "[ok] $Name: $version"
}

Test-Required ".NET SDK 10" "dotnet"
Test-Required "Node.js" "node"
Test-Required "npm" "npm"
Test-Required "Python" "python3"
Test-Optional "Docker" "docker"

if ($Strict -and $missing) {
  throw "S00 doctor failed: required local tooling is missing."
}

if ($missing) {
  Write-Host "S00 doctor completed with missing required tools; rerun with -Strict to fail."
} else {
  Write-Host "S00 doctor completed successfully."
}
