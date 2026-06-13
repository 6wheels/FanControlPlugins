param(
    [string]$DllSrc,
    [string]$DestDir,
    [switch]$Elevated
)

$resultFile = Join-Path $env:TEMP 'fc_deploy_result.txt'

if (-not $Elevated) {
    $tempDll    = Join-Path $env:TEMP (Split-Path $DllSrc -Leaf)
    $tempScript = Join-Path $env:TEMP 'fc_deploy.ps1'

    Remove-Item $resultFile -Force -ErrorAction SilentlyContinue
    Copy-Item $DllSrc        $tempDll    -Force
    Copy-Item $PSCommandPath $tempScript -Force

    # Single-string args with explicit quoting — array join breaks paths with spaces
    Start-Process powershell -Verb RunAs -ArgumentList `
        "-NonInteractive -NoProfile -ExecutionPolicy Bypass -File `"$tempScript`" -DllSrc `"$tempDll`" -DestDir `"$DestDir`" -Elevated"

    $timeout = 30
    $elapsed = 0
    while ($elapsed -lt $timeout -and -not (Test-Path $resultFile)) {
        Start-Sleep -Seconds 1
        $elapsed++
    }

    if (-not (Test-Path $resultFile)) {
        Write-Error "Deploy timed out after $timeout seconds."
        exit 1
    }

    $result = (Get-Content $resultFile -Raw).Trim()
    Remove-Item $resultFile -Force -ErrorAction SilentlyContinue
    if ($result -ne 'OK') { Write-Error $result; exit 1 }
    exit 0
}

# --- Elevated section ---

function Set-Result([string]$msg) {
    $msg | Set-Content $resultFile
    Remove-Item $PSCommandPath -Force -ErrorAction SilentlyContinue
}

$proc = Get-Process FanControl -ErrorAction SilentlyContinue
if ($proc) { $proc.Kill(); $proc.WaitForExit(5000) }

if (-not (Test-Path $DestDir)) { New-Item -ItemType Directory -Path $DestDir -Force | Out-Null }

Copy-Item $DllSrc $DestDir -Force

$dest = Join-Path $DestDir (Split-Path $DllSrc -Leaf)
$ok   = (Get-FileHash $DllSrc -Algorithm SHA256).Hash -eq (Get-FileHash $dest -Algorithm SHA256).Hash
Remove-Item $DllSrc -Force -ErrorAction SilentlyContinue

if (-not $ok) { Set-Result 'FAIL: checksum mismatch after copy.'; exit 1 }

$exe = Join-Path (Split-Path (Split-Path $DestDir)) 'FanControl.exe'
if (-not (Test-Path $exe)) { Set-Result "FAIL: FanControl.exe not found at $exe"; exit 1 }

$psi = New-Object System.Diagnostics.ProcessStartInfo($exe)
$psi.UseShellExecute = $true
[System.Diagnostics.Process]::Start($psi) | Out-Null

$timeout = 15
$elapsed = 0
while ($elapsed -lt $timeout -and -not (Get-Process FanControl -ErrorAction SilentlyContinue)) {
    Start-Sleep -Seconds 1
    $elapsed++
}

if (-not (Get-Process FanControl -ErrorAction SilentlyContinue)) {
    Set-Result 'FAIL: FanControl did not start within 15 seconds.'
    exit 1
}

Set-Result 'OK'
