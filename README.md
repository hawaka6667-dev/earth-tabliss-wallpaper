# Earth Tabliss Wallpaper 🌍✨

极低开销的 Windows 极简动态地球壁纸。融合 **Tabliss** 极简排版美学、**Google Earth View** 卫星地貌与 **Gemini** 地质研判智能。

## ✨ 核心特性

- ⚡ **零闲置开销 (0 FPS Idle)**：专为轻量硬件（如 Intel Celeron 平台）优化。剔除持续 CSS 动效，空载常驻 CPU/DWM 占用趋近于 0.0%。
- 🖥️ **Windows Shell 底层融合**：自动穿透并挂载于 Windows `WorkerW` 底层，自然沉浸在桌面图标之下。
- 👁️ **一键显隐桌面图标**：药丸栏内置 `[ 👁 图标 ]` 切换键，毫秒级隐藏/恢复桌面图标 (`SHELLDLL_DefView`)，一键进入纯净壁纸态。
- 🚀 **任务栏安全避让与自由拖拽**：原生顶层悬浮控制胶囊，支持全局任意拖拽；坐标物理钳位至 `WorkingArea`，绝对不与任务栏 (Taskbar) 发生重叠冲突。
- 🧬 **Gemini 地理地貌深度研判**：内置独立顶层交互弹窗，一键解析当前地表的构造断层、冲蚀成因与 Google Earth 3D 观测坐标。
- 🛡️ **退出无损还原**：智能读取 `TranscodedWallpaper` 高分镜像，退出时 100% 恢复原有桌面壁纸与图标可见状态，杜绝黑屏残留。

## 🛠️ 架构与技术栈

- **宿主进程**：C# .NET 10 (`net10.0-windows`) + WinForms + WebView2 Runtime
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

# 启动壁纸
./EarthWallpaper/bin/Debug/net10.0-windows/EarthWallpaper.exe
```

## ⌨️ 控制栏操作说明

| 控件 | 功能说明 |
| :--- | :--- |
| `⋮⋮` | **拖拽手柄**：按住左键自由拖动控制栏至屏幕任意舒适位置 |
| `👁 图标` | **显隐桌面图标**：一键切换隐藏/显示桌面图标 |
| `⏭ 换张` | **即刻切图**：无缝淡入下一张卫星高分壁纸 |
| `✨ 研判` | **Gemini 研判**：呼出当前地标地质构造与地貌演化解析窗口 |
| `🌐 地图` | **浏览器定位**：在默认浏览器中打开 Google Earth / Maps 3D 视图 |
| `✕` | **安全退出**：平滑释放内存并恢复系统原始壁纸 |

## 📄 License
MIT License
