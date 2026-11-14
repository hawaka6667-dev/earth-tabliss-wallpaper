param(
    [switch]$Repair,
    [switch]$ExitOnly,
    [switch]$Run
)

$exePath = Join-Path $PSScriptRoot "EarthWallpaper\bin\Debug\net10.0-windows\EarthWallpaper.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "publish_framework\EarthWallpaper.exe"
}

if ($Repair) {
    Write-Host "[DevHub] Restoring desktop and clearing orphan WorkerW layers..." -ForegroundColor Cyan
    if (Test-Path $exePath) {
        & $exePath --restore-desktop
    }
    return
}

Write-Host "[DevHub] Gracefully signaling EarthWallpaper to exit..." -ForegroundColor Yellow
if (Test-Path $exePath) {
    & $exePath --exit
    Start-Sleep -Milliseconds 400
}

# If still running, close cleanly
Get-Process -Name "EarthWallpaper" -ErrorAction SilentlyContinue | ForEach-Object {
    $_.CloseMainWindow() | Out-Null
    Start-Sleep -Milliseconds 300
}

# Clear any orphan WorkerW layers
if (Test-Path $exePath) {
    & $exePath --restore-desktop
}

if ($ExitOnly) {
    Write-Host "[DevHub] Application exited cleanly." -ForegroundColor Green
    return
}

if ($Run) {
    Write-Host "[DevHub] Launching clean debug instance..." -ForegroundColor Green
    Start-Process $exePath
}
