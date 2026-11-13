# Earth Tabliss 🌍

> A lightweight, minimalist live wallpaper for Windows featuring 1,500+ curated high-resolution satellite landscapes from Google Earth View, integrated with Tabliss-inspired typography and geological AI analysis.

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-blue?style=flat-square&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet" alt=".NET" />
  <img src="https://img.shields.io/badge/CPU%20Idle-0%20FPS%20%7C%20~0.0%25-22c55e?style=flat-square" alt="Efficiency" />
  <img src="https://img.shields.io/badge/AI-Gemini%204%20Hydrogen-4285F4?style=flat-square&logo=google" alt="AI" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=flat-square" alt="License" />
</p>

<p align="center">
  <img src="docs/images/desktop_preview.jpg" alt="Desktop Preview" width="100%" />
</p>

## ✨ Highlights

* **1,500+ Satellite Views**: Curated landscapes from Google Earth View across deserts, glaciers, reefs, and salt flats.
* **0 FPS Zero-Overhead Engine**: Halts redraw loops once the 0.6s wallpaper transition finishes. Idles at **~0.0% CPU**.
* **Clean System Teardown**: Hooks non-destructively to Windows `WorkerW`. Automatically backs up and restores your original wallpaper on exit.
* **Ask Gemini**: Instant geological and satellite reasoning powered by **Gemini 4 Hydrogen** (`gemini-4-hydrogen`).
* **Visual Bookmarks Hub**: Save and organize your favorite coordinates with instant cached satellite thumbnails.

<p align="center">
  <img src="docs/images/wallpaper_gallery_grid.jpg" alt="Wallpaper Gallery" width="100%" />
</p>

## 🚀 Quick Start

### Requirements
* Windows 10 (1903+) or Windows 11 (64-bit)
* [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download)

### Run
```bash
git clone https://github.com/hawaka6667-dev/earth-tabliss-wallpaper.git
cd earth-tabliss-wallpaper
dotnet run --project EarthWallpaper/EarthWallpaper.csproj
```
*Or double-click `start.bat`.*

## 📄 License
[MIT](LICENSE) · Imagery © Google Earth View.
