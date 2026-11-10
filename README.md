# Earth Tabliss Wallpaper 🌍✨

极低开销的 Windows 极简动态地球壁纸与**卡通小地球桌宠 (Desktop Pet)** 伴侣。融合 **Tabliss** 极简排版美学、**Google Earth View** 卫星地貌、**地标收藏夹**与 **Ask Gemini** 交互研判智能。

## ✨ 核心特性

- 🐾 **Q 版小地球卡通桌宠 (Chibi Earth Mascot)**：采用专属卡通造型（晶莹大眼、微红脸颊、金色行星环），告别几何图元。
- 🖥️ **桌面层自洽悬浮 (Desktop-Level Pinning)**：**绝不霸道浮在其他应用顶层**。当您使用 Chrome、VS Code 或工作软件时，桌宠静卧于软件之下；切回桌面即刻浮现。
- ⭐ **一键地标收藏夹 (Bookmarks & Favorites)**：一键永久留存心仪地球秘境（国家、地区、精准经纬度与原图地址）。支持随时在收藏夹中回看、**一键重设为当前壁纸**或跳转 3D Google Earth。
- 💬 **Ask Gemini 交互式地貌问答**：支持直接向 Gemini 提出任何地质/遥感/气候问题（内附一键地质断层、水系冲刷、色彩反演快捷 Prompt）。支持绑定个人 Gemini API Key，同时提供免 Key 高保真地质推理。
- ⚡ **零闲置开销 (0 FPS Idle)**：专为轻量硬件（如 Intel Celeron 平台）优化。静态无循环动画，常驻 CPU 仅约 0.0% ~ 0.9%。
- 🖼️ **秒开零黑屏机制**：内置本地高分卫星底图，冷启动 0ms 瞬间点亮，杜绝网络黑屏。
- 👁️ **无损显隐桌面图标**：通过底层 `SysListView32` 独立调度，一键隐藏/恢复桌面图标且绝不遮挡壁纸视口。
- 🛡️ **退出无损还原**：读取 `TranscodedWallpaper` 缓存，退出时 100% 恢复原有桌面壁纸与图标可见状态。

## 🛠️ 架构与技术栈

- **宿主进程**：C# .NET 10 (`net10.0-windows`) + WinForms + WebView2 Runtime
- **交互载体**：桌面层灵动桌宠 (`EarthPetForm`) + 控制中枢 (`PetMenuForm`) + 地球研判与收藏中心 (`GeminiHubForm`)
- **数据管理**：持久化地标收藏服务 (`BookmarkManager`) + Gemini 2.5 Flash 服务 (`GeminiService`)
- **渲染前端**：HTML5 / Modern Glassmorphism CSS / Vanilla JavaScript (双缓冲无黑边预加载)

## 🚀 快速启动

### 依赖项
- Windows 10 / 11 (x64)
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/)

### 构建与运行
```bash
# 构建项目
dotnet build ./EarthWallpaper/EarthWallpaper.csproj

# 运行桌面壁纸与桌宠
./start.bat
```

## 🐾 桌宠与中枢功能列表

| 控件 / 交互 | 说明 |
| :--- | :--- |
| **鼠标拖动小地球** | 桌面任意拖动，边缘严格避让 Windows 任务栏 |
| **单击小地球** | 唤起控制中枢菜单 |
| `[ ⭐ 收藏当前地标 ]` | 一键将当前卫星影像与经纬度永久保存至收藏夹 |
| `[ 📂 查看我的收藏夹 ]` | 浏览已收藏地标，支持“设为壁纸”、“向AI提问”与“删除” |
| `[ 💬 Ask Gemini 智能研判 ]` | 呼出 AI 地理问答面板，自由提问地质构造与遥感机理 |
| `[ ⏭ 换一张星球壁纸 ]` | 平滑淡入下一张卫星高分壁纸 |
| `[ 👁 显隐桌面图标 ]` | 一键切换隐藏/显示桌面图标 |
| `[ 🌐 在 Google Earth 查看 ]` | 在默认浏览器中调起 Google Earth 3D 卫星实景 |
| `[ 🔑 API 设置 ]` | 配置个人的 Google Gemini API Key（支持免费获取） |
| `[ 🚪 退出壁纸与桌宠 ]` | 平滑退出并恢复系统原壁纸 |

## 📄 License
MIT License
