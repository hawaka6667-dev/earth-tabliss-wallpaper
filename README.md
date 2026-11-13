# Earth Tabliss Wallpaper 🌍✨

<p align="center">
  <img src="docs/images/mascot.png" alt="Earth Tabliss Mascot" width="140" height="140" />
</p>

<p align="center">
  <strong>Silent stars, breathtaking landscapes — High-resolution curated satellite wallpapers & intelligent desktop companion for Windows.</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue?style=flat-square&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-10.0%20Windows-512BD4?style=flat-square&logo=dotnet" alt=".NET Version" />
  <img src="https://img.shields.io/badge/Engine-Direct%20WorkerW%20Layer-0078D7?style=flat-square" alt="Engine" />
  <img src="https://img.shields.io/badge/CPU%20Overhead-0%20FPS%20%7C%20~0.0%25%20Idle-22c55e?style=flat-square" alt="Efficiency" />
  <img src="https://img.shields.io/badge/AI%20Model-Gemini%202.0%20Flash-4285F4?style=flat-square&logo=google" alt="AI Model" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License" />
</p>

<p align="center">
  <a href="README.md">English</a> • <a href="README.zh-CN.md">简体中文</a>
</p>

<p align="center">
  <img src="docs/images/desktop_preview.jpg" alt="Earth Tabliss Desktop Wallpaper Preview" width="100%" />
</p>

---

## 🌌 Overview

In a fast-paced digital workflow, your desktop shouldn't just be an icon dump — it should be an expansive window to the planet.

**Earth Tabliss** transforms your Windows desktop into an ever-refreshing visual expedition across 1,500+ curated high-resolution satellite landscapes from Google Earth View, integrated with Tabliss-inspired minimalist typography, an interactive desktop companion, and deep planetary geological reasoning powered by Google Gemini.

<p align="center">
  <img src="docs/images/wallpaper_gallery_grid.jpg" alt="Curated High-Resolution Satellite Wallpapers" width="100%" />
  <br />
  <em>Over 1,500+ ultra-high-resolution satellite captures across deserts, glaciers, coral reefs, and volcanic plains.</em>
</p>

---

## ✨ Key Features

### 🖼️ 1. Ultra-High-Resolution Curated Earth View
* **1,500+ Masterpieces**: Hand-picked satellite captures spanning all seven continents — tectonic faultlines, salt flats, meandering rivers, and glacial valleys.
* **Minimalist Information Overlay**: Displays clean local time, geographic coordinates, and regional names without cluttering your workspace.
* **One-Click 3D Flyover**: Instantly transition from your 2D desktop wallpaper into a full 3D interactive flight on Google Earth Web.

### 🍃 2. True Zero-FPS Idle Efficiency (0.0% CPU)
* **Static Zero-Refresh Architecture**: Unlike video loops or heavy rendering engines that constantly burn GPU cycles, Earth Tabliss halts all redraw loops once a wallpaper fade-in completes.
* **Extreme Low Resource Footprint**: Idles at **~0.0% CPU overhead** and low memory usage. Completely silent, fan-free, and battery-friendly — runs effortlessly on both ultra-thin laptops and entry-level quad-core machines.

### 🐾 3. Non-Intrusive Desktop Companion
* **Zero Window Dominance**: Resides naturally above the desktop layer without stealing keyboard focus or popping over your IDE, terminal, or browser.
* **Intuitive Mouse Matrix**:
  * **Left Click**: Toggles the interactive landmark bubble (quick switch, bookmark, or analyze).
  * **Left Double-Click**: Opens the comprehensive **Earth Hub**.
  * **Left Drag**: Freely relocates the pet anywhere across multi-monitor setups with automatic taskbar boundary snapping.
  * **Right Click**: Opens the system utility context menu (skin switching, toggle desktop icons, bookmarks, telemetry, exit).

### ⭐ 4. Visual Bookmarks Hub with Satellite Thumbnails
* **Persistent Two-Tier Image Cache**: Stores and renders high-definition satellite preview thumbnails with instant responsiveness.
* **Tag-Based Filtering & Search**: Organize your collection by tags (Cities, Deserts, Rivers, Favorites) or search by country, region, and coordinates.
* **Instant Wallpaper Application**: Reapply any saved landmark as your live wallpaper with a single click.

<p align="center">
  <img src="docs/images/bookmarks_hub.png" alt="Bookmarks Hub" width="90%" />
</p>

### 💬 5. "Ask Gemini" Geological & Satellite Intelligence
* **Planetary Geomorphology Q&A**: Ask Gemini anything about structural geology, sedimentology, erosion dynamics, and orbital imaging spectra.
* **Powered by Gemini 2.0 Flash**: Fast, responsive inference directly from Google AI Studio.
* **Seamless API Onboarding**: Comes with offline reasoning models out of the box; easily attach your personal free Gemini API Key for comprehensive analysis.

<p align="center">
  <img src="docs/images/ask_gemini.png" alt="Ask Gemini Analysis" width="90%" />
</p>

### 🛡️ 6. Zero Host Pollution & Clean Teardown
* **Non-Destructive Execution**: Does not tamper with Windows input hooks or Explorer thread message loops.
* **Original Wallpaper Auto-Restoration**: Backs up your active system wallpaper upon launch and 100% cleanly restores it on exit.
* **Desktop Icon Toggle**: One-click visibility toggle for desktop icons whenever you want a completely clean, unobstructed planetary vista.

---

## 🎮 Control Matrix

| Input | Target | Action & Description |
| :--- | :--- | :--- |
| **Left Click** | Desktop Pet | Toggle landmark speech bubble (quick navigation & bookmark) |
| **Left Double-Click** | Desktop Pet | Open the Earth Hub (Bookmarks, Gemini Q&A, Settings) |
| **Left Drag** | Desktop Pet | Reposition pet with auto boundary collision prevention |
| **Right Click** | Desktop Pet | Open pet management menu (Skins, Icons, Settings, Exit) |
| **Right Click** | Tray Icon | Open system tray menu (Next/Prev Wallpaper, Auto-start, Exit) |
| **ESC Key** | Any Modal | Quickly close any active Earth Tabliss dialog |

---

## 🚀 Getting Started

### System Requirements
* **OS**: Windows 10 (1903+) or Windows 11 (64-bit)
* **Runtime**: [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (or [.NET 8.0/9.0](https://dotnet.microsoft.com/download))
* **Dependencies**: Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 10/11)

### Installation & Launch

1. **Clone the Repository**:
   ```bash
   git clone https://github.com/hawaka6667-dev/earth-tabliss-wallpaper.git
   cd earth-tabliss-wallpaper
   ```

2. **Build and Run**:
   ```bash
   dotnet build EarthWallpaper/EarthWallpaper.csproj -c Release
   dotnet run --project EarthWallpaper/EarthWallpaper.csproj
   ```
   *Alternatively, double-click `start.bat` in the project root.*

---

## 🛠️ Architecture & Tech Stack

```
EarthWallpaper.exe (.NET 10 WinForms UI Host)
 ├── DesktopWorker.cs (Win32 WorkerW Layer Integration)
 ├── PowerGovernor.cs (Fullscreen App Detector & 0% CPU Governor)
 ├── GeminiService.cs (Gemini 2.0 Flash Client & Planetary Prompts)
 ├── EarthPetForm.cs (WS_EX_NOACTIVATE Isolated ToolWindow Companion)
 └── WebView2 Subsystem (Static HTML5/Canvas Minimalist Renderer)
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE). Curated satellite imagery belongs to **Google Earth View** and respective satellite data providers.
