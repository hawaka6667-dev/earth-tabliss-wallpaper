# Earth Tabliss Launcher
$exe = Join-Path $PSScriptRoot "EarthWallpaper\bin\Debug\net10.0-windows\EarthWallpaper.exe"
if (Test-Path $exe) {
    Start-Process -FilePath $exe
    Write-Host "[OK] Earth Tabliss 动态壁纸已启动并在托盘驻留。" -ForegroundColor Green
} else {
    Write-Host "[Error] 未找到可执行文件，请先运行 dotnet build。" -ForegroundColor Red
}
