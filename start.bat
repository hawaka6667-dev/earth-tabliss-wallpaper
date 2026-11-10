@echo off
cd /d "%~dp0"
taskkill /F /IM EarthWallpaper.exe 2>nul
start "" "%~dp0EarthWallpaper\bin\Debug\net10.0-windows\EarthWallpaper.exe"
exit
