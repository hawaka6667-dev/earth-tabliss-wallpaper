@echo off
setlocal
cd /d "%~dp0"

:: 1. Gracefully notify running instance to exit cleanly (no orphan WorkerW)
if exist "EarthWallpaper.exe" (
    EarthWallpaper.exe --exit >nul 2>&1
) else if exist "publish_framework\EarthWallpaper.exe" (
    "publish_framework\EarthWallpaper.exe" --exit >nul 2>&1
) else if exist "EarthWallpaper\bin\Release\net10.0-windows\EarthWallpaper.exe" (
    "EarthWallpaper\bin\Release\net10.0-windows\EarthWallpaper.exe" --exit >nul 2>&1
)

:: Brief pause for teardown
ping 127.0.0.1 -n 2 >nul

:: 2. Launch target executable
if exist "EarthWallpaper.exe" (
    start "" "%~dp0EarthWallpaper.exe"
) else if exist "publish_framework\EarthWallpaper.exe" (
    start "" "%~dp0publish_framework\EarthWallpaper.exe"
) else if exist "EarthWallpaper\bin\Release\net10.0-windows\EarthWallpaper.exe" (
    start "" "%~dp0EarthWallpaper\bin\Release\net10.0-windows\EarthWallpaper.exe"
) else if exist "EarthWallpaper\bin\Debug\net10.0-windows\EarthWallpaper.exe" (
    start "" "%~dp0EarthWallpaper\bin\Debug\net10.0-windows\EarthWallpaper.exe"
) else (
    echo [Error] EarthWallpaper.exe not found!
    pause
)
exit
