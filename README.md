# Earth Tabliss Wallpaper 🌍✨

极低开销的 Windows 极简动态地球壁纸与**桌宠 (Desktop Pet)** 伴侣。融合 **Tabliss** 极简排版美学、**Google Earth View** 卫星地貌与 **Gemini** 地质研判智能。

## ✨ 核心特性

- ⚡ **零闲置开销 (0 FPS Idle)**：专为轻量硬件（如 Intel Celeron 平台）优化。剔除持续 CSS 动效，空载常驻 CPU/DWM 占用趋近于 0.0%。
- 🖼️ **秒开零黑屏 (Instant Local Asset)**：内置本地离线高分卫星地貌底图，冷启动 0ms 直出视觉，杜绝网络拉取造成的黑屏空窗。
- 🖥️ **Windows Shell 底层融合**：自动穿透并挂载于 Windows `WorkerW` 底层，自然沉浸在桌面图标之下。
- 🐾 **灵动桌宠形态 (Desktop Pet Mascot)**：告别生硬矩形工具栏，化身为优雅灵动的 76px 圆形“小地球”桌宠。支持全桌面自由拖拽、悬停呼吸光效与漫游气泡提醒。
- 👁️ **无损显隐桌面图标**：精准控制桌面 `SysListView32` 图标层，毫秒级隐藏/恢复桌面图标，绝不破坏底层壁纸透明度与渲染层级。
- 🚀 **任务栏物理避让 (Taskbar Clearance)**：桌宠与弹出中枢坐标实时通过 `WorkingArea` 动态钳位，物理级杜绝侵入或遮挡任务栏 (Taskbar)。
- 🧬 **Gemini 地理地貌深度研判**：内置独立顶层交互弹窗，一键解析当前地表的构造断层、冲蚀成因与 Google Earth 3D 观测坐标。
- 🛡️ **退出无损还原**：智能读取 `TranscodedWallpaper` 高分镜像，退出时 100% 恢复原有桌面壁纸与图标可见状态，杜绝黑屏残留。

## 🛠️ 架构与技术栈

- **宿主进程**：C# .NET 10 (`net10.0-windows`) + WinForms + WebView2 Runtime
- **交互载体**：圆形双缓冲透明桌宠 (`EarthPetForm`) + 展开式控制中枢 (`PetMenuForm`)
- **渲染前端**：HTML5 / Modern Glassmorphism CSS / Vanilla JavaScript (双缓冲无黑边预加载)
- **地标数据**：Google Earth View (1,520+ 全球精选地标与高精经纬度)

## 🚀 快速启动

### 依赖项
- Windows 10 / 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

### 构建与运行
```bash
# 克隆仓库
git clone https://github.com/hawaka6667-dev/earth-tabliss-wallpaper.git
cd earth-tabliss-wallpaper

# 构建项目
dotnet build ./EarthWallpaper/EarthWallpaper.csproj

# 启动壁纸与桌宠
./EarthWallpaper/bin/Debug/net10.0-windows/EarthWallpaper.exe
```

## 🐾 桌宠互动说明

| 交互行为 | 说明 |
| :--- | :--- |
| **鼠标拖拽桌宠** | 按住左键可在桌面上自由拖动小地球，边缘自动避让任务栏 |
| **单机桌宠** | 唤起/收起展开式控制中枢卡片 |
| **漫游气泡提醒** | 切换新壁纸时，桌宠会自动弹出当前地标气泡提示 |
| `[ 👁 显隐桌面图标 ]` | 一键隐藏或恢复所有桌面图标，享受纯净地球全景 |
| `[ ⏭ 换一张星球壁纸 ]` | 0.5s 平滑淡入下一张卫星高分壁纸 |
| `[ ✨ Gemini 地貌深度研判 ]` | 呼出当前地标地质构造与地貌演化解析窗口 |
| `[ 🌐 在 Google Earth 查看 ]` | 在默认浏览器中打开 Google Earth 3D 卫星实景 |
| `[ 🚪 退出壁纸与桌宠 ]` | 平滑释放内存并恢复系统原始壁纸与桌面图标 |

## 📄 License
MIT License
