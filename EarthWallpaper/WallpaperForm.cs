using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace EarthWallpaper
{
    public class WallpaperForm : Form
    {
        private WebView2? _webView;
        private PowerGovernor? _powerGovernor;
        private NotifyIcon? _trayIcon;
        private EarthPetForm? _petForm;
        private bool _isDesktopAttached = false;
        private string _appPath = string.Empty;

        public LocationInfo? CurrentLocation { get; private set; }
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public QueueStatusInfo? LatestQueueStatus { get; set; }
        public event Action<QueueStatusInfo>? OnQueueStatusUpdated;

        public void RequestQueueStatus() => SendWebMessage("request_queue_status");
        public void ReshuffleDeck() => SendWebMessage("reshuffle_deck");
        public void FlushQueue() => SendWebMessage("flush_queue");
        public void ApplyQueuedItem(int index)
        {
            if (_webView != null && _webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.PostWebMessageAsJson($"{{\"type\":\"apply_queued_item\",\"index\":{index}}}");
            }
        }

        public WallpaperForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = Color.Black;

            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1536, 864);
            Bounds = bounds;

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                DefaultBackgroundColor = Color.Black
            };
            Controls.Add(_webView);

            ResumeLayout(false);

            InitTrayIcon();
        }

        private void InitTrayIcon()
        {
            var menu = new ContextMenuStrip();
            
            var itemIcons = new ToolStripMenuItem("👁 显隐桌面图标", null, (s, e) => DesktopWorker.ToggleDesktopIcons());
            var itemNext = new ToolStripMenuItem("⏭ 下一张地球壁纸", null, (s, e) => SendWebMessage("next_wallpaper"));
            var itemPrev = new ToolStripMenuItem("⏮ 上一张地球壁纸", null, (s, e) => SendWebMessage("previous_wallpaper"));
            var itemBookmarks = new ToolStripMenuItem("⭐ 我的地标收藏夹", null, (s, e) => _petForm?.OpenGeminiHub(true));
            var itemGemini = new ToolStripMenuItem("✨ Ask Gemini 地貌研判", null, (s, e) => _petForm?.OpenGeminiHub(false));
            
            ToolStripMenuItem? itemSkin = null;
            itemSkin = new ToolStripMenuItem($"🎨 切换桌宠外观 ({_petForm?.GetSkinDisplayName() ?? "初版天体"})", null, (s, e) =>
            {
                _petForm?.ToggleSkin();
                if (itemSkin != null && _petForm != null)
                {
                    itemSkin.Text = $"🎨 切换桌宠外观 ({_petForm.GetSkinDisplayName()})";
                }
            });

            var itemDev = new ToolStripMenuItem("🛠️ 开发者模式 (队列监控)", null, (s, e) => _petForm?.OpenGeminiHub(false, true));
            var itemReload = new ToolStripMenuItem("🔄 重载 / 重启应用 (Reload)", null, (s, e) => RestartApplication());
            
            var itemStartup = new ToolStripMenuItem { Text = "🚀 开机自启" };
            itemStartup.Checked = IsStartupEnabled();
            itemStartup.Text = itemStartup.Checked ? "🚀 开机自启 (已开启)" : "🚀 开机自启 (未开启)";
            itemStartup.Click += (s, e) =>
            {
                bool newState = !itemStartup.Checked;
                SetStartupEnabled(newState);
                itemStartup.Checked = newState;
                itemStartup.Text = newState ? "🚀 开机自启 (已开启)" : "🚀 开机自启 (未开启)";
            };

            var itemExit = new ToolStripMenuItem("🚪 退出 Earth Tabliss (恢复原壁纸)", null, (s, e) => ExitApplication());

            menu.Items.AddRange(new ToolStripItem[] {
                itemIcons,
                itemNext,
                itemPrev,
                new ToolStripSeparator(),
                itemBookmarks,
                itemGemini,
                itemSkin,
                new ToolStripSeparator(),
                itemDev,
                itemStartup,
                itemReload,
                itemExit
            });

            _trayIcon = new NotifyIcon
            {
                Text = "Earth Tabliss - 高性能动态桌面壁纸与桌宠",
                Icon = CreateEarthTrayIcon(),
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => _petForm?.OpenGeminiHub(false);
        }

        private Icon CreateEarthTrayIcon()
        {
            try
            {
                using var bmp = new Bitmap(32, 32);
                using var g = Graphics.FromImage(bmp);
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var b = new SolidBrush(Color.FromArgb(15, 23, 42)))
                    g.FillEllipse(b, 1, 1, 29, 29);
                using (var p = new Pen(Color.FromArgb(56, 189, 248), 2f))
                    g.DrawEllipse(p, 2, 2, 27, 27);
                using var font = new Font("Segoe UI Emoji", 13f);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("🌍", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
            catch
            {
                return SystemIcons.Application;
            }
        }

        private void RestartApplication()
        {
            try
            {
                Program.Log("WallpaperForm: Restarting application...");
                DesktopWorker.IsRestarting = true;

                string startBat = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "start.bat"));
                if (!File.Exists(startBat)) startBat = @"F:\antigravity\earth屏保\start.bat";

                _isExiting = true;
                _powerGovernor?.Dispose();
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                }
                _petForm?.Dispose();

                // Cleanly exit current process. start.bat will wait and spawn a clean instance
                if (File.Exists(startBat))
                {
                    Process.Start(new ProcessStartInfo(startBat) { UseShellExecute = true });
                }
                else
                {
                    Process.Start(new ProcessStartInfo(Application.ExecutablePath) { UseShellExecute = true });
                }

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Program.Log($"RestartApplication error: {ex.Message}");
            }
        }

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppStartupName = "EarthTabliss";

        private static bool IsStartupEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, false);
                return key?.GetValue(AppStartupName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartupEnabled(bool enable)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunRegistryKey, true);
                if (key == null) return;

                if (enable)
                {
                    string startBat = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "start.bat"));
                    if (!File.Exists(startBat)) startBat = @"F:\antigravity\earth屏保\start.bat";

                    if (File.Exists(startBat))
                    {
                        key.SetValue(AppStartupName, $"\"{startBat}\"");
                    }
                    else
                    {
                        key.SetValue(AppStartupName, $"\"{Application.ExecutablePath}\"");
                    }
                }
                else
                {
                    key.DeleteValue(AppStartupName, false);
                }
            }
            catch (Exception ex)
            {
                Program.Log($"SetStartupEnabled error: {ex.Message}");
            }
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Program.Log("WallpaperForm.OnLoad started");
            await InitWebViewAsync();

            Program.Log("InitWebViewAsync completed, attaching to desktop...");
            AttachToDesktopLayer();

            // Launch interactive Desktop Pet Form (小地球桌宠)
            try
            {
                _petForm = new EarthPetForm(this);
                _petForm.Show();
                Program.Log("EarthPetForm launched successfully.");
            }
            catch (Exception ex)
            {
                Program.Log($"EarthPetForm launch exception: {ex.Message}");
            }

            _powerGovernor = new PowerGovernor(OnPowerStateChanged);
            _powerGovernor.Start();
            Program.Log("PowerGovernor started");
        }

        private async Task InitWebViewAsync()
        {
            try
            {
                Program.Log("InitWebViewAsync: resolving app directory...");
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string candidateA = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "app"));
                string candidateB = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "app"));
                string candidateC = Path.GetFullPath(Path.Combine(baseDir, "app"));

                if (Directory.Exists(candidateA)) _appPath = candidateA;
                else if (Directory.Exists(candidateB)) _appPath = candidateB;
                else if (Directory.Exists(candidateC)) _appPath = candidateC;
                else _appPath = Path.GetFullPath(@"F:\antigravity\earth屏保\app");

                Program.Log($"InitWebViewAsync: _appPath = {_appPath}");

                // Sync backed up original wallpaper to app assets so WebView2 can always use it as zero-black fallback
                string backupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EarthWallpaper", "original_wallpaper_backup.png");
                string targetAsset = Path.Combine(_appPath, "assets", "original_wallpaper.png");
                if (File.Exists(backupPath))
                {
                    try
                    {
                        string assetDir = Path.GetDirectoryName(targetAsset)!;
                        if (!Directory.Exists(assetDir)) Directory.CreateDirectory(assetDir);
                        File.Copy(backupPath, targetAsset, true);
                        Program.Log($"InitWebViewAsync: Synced user wallpaper to {targetAsset}");
                    }
                    catch { }
                }

                string userData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EarthWallpaper", "WebView2Data");
                var env = await CoreWebView2Environment.CreateAsync(null, userData);

                if (_webView != null)
                {
                    await _webView.EnsureCoreWebView2Async(env);

                    _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;

                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "earthwallpaper.local",
                        _appPath,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                    _webView.CoreWebView2.Navigate("https://earthwallpaper.local/index.html");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"InitWebViewAsync EXCEPTION: {ex.Message}");
            }
        }

        private void AttachToDesktopLayer()
        {
            _isDesktopAttached = DesktopWorker.AttachToDesktop(Handle);
            if (!_isDesktopAttached)
            {
                Program.Log("[WallpaperForm] Failed to attach to WorkerW, fallback windowed.");
                FormBorderStyle = FormBorderStyle.Sizable;
                Bounds = new Rectangle(120, 120, 1366, 768);
                StartPosition = FormStartPosition.CenterScreen;
                ShowInTaskbar = true;
                Visible = true;
            }
        }

        private void OnPowerStateChanged(bool isPaused)
        {
            if (_webView != null && _webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.MemoryUsageTargetLevel = isPaused 
                    ? CoreWebView2MemoryUsageTargetLevel.Low 
                    : CoreWebView2MemoryUsageTargetLevel.Normal;
            }
        }

        private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.WebMessageAsJson);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;
                string type = typeProp.GetString() ?? string.Empty;

                if (type == "location_changed" && root.TryGetProperty("data", out var dataEl))
                {
                    CurrentLocation = new LocationInfo
                    {
                        Id = dataEl.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "",
                        Country = dataEl.TryGetProperty("country", out var cEl) ? cEl.GetString() ?? "" : "",
                        Region = dataEl.TryGetProperty("region", out var rEl) ? rEl.GetString() ?? "" : "",
                        MapUrl = dataEl.TryGetProperty("map", out var mEl) ? mEl.GetString() ?? "" : "",
                        ImageUrl = dataEl.TryGetProperty("image", out var imgEl) ? imgEl.GetString() ?? "" : ""
                    };
                    if (dataEl.TryGetProperty("coords", out var coordsEl))
                    {
                        CurrentLocation.Lat = coordsEl.TryGetProperty("lat", out var latEl) ? latEl.GetString() ?? "" : "";
                        CurrentLocation.Lng = coordsEl.TryGetProperty("lng", out var lngEl) ? lngEl.GetString() ?? "" : "";
                    }
                    Program.Log($"WallpaperForm: CurrentLocation updated to {CurrentLocation.GetDisplayName()} ({CurrentLocation.Lat}, {CurrentLocation.Lng})");
                    _petForm?.NotifyLocationChanged(CurrentLocation);
                }
                else if (type == "queue_status" && root.TryGetProperty("data", out var qDataEl))
                {
                    try
                    {
                        var info = JsonSerializer.Deserialize<QueueStatusInfo>(qDataEl.GetRawText(), new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (info != null)
                        {
                            LatestQueueStatus = info;
                            OnQueueStatusUpdated?.Invoke(info);
                        }
                    }
                    catch (Exception qEx)
                    {
                        Program.Log($"Queue status parse error: {qEx.Message}");
                    }
                }
                else if (type == "open_url" && root.TryGetProperty("url", out var urlProp))
                {
                    string url = urlProp.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(url))
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"OnWebMessageReceived error: {ex.Message}");
            }
        }

        public void SendWebMessage(string actionType)
        {
            if (_webView != null && _webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.PostWebMessageAsJson($"{{\"type\":\"{actionType}\"}}");
            }
        }

        public void ApplyCustomWallpaper(LocationInfo loc)
        {
            CurrentLocation = loc;
            if (_webView != null && _webView.CoreWebView2 != null)
            {
                string payload = JsonSerializer.Serialize(new
                {
                    type = "set_custom_wallpaper",
                    data = new
                    {
                        country = loc.Country,
                        region = loc.Region,
                        lat = loc.Lat,
                        lng = loc.Lng,
                        map = loc.MapUrl,
                        image = loc.ImageUrl
                    }
                });
                _webView.CoreWebView2.PostWebMessageAsJson(payload);
            }
            _petForm?.NotifyLocationChanged(loc);
        }

        private bool _isExiting = false;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            ExitApplication();
        }

        private void ExitApplication()
        {
            if (_isExiting) return;
            _isExiting = true;

            _powerGovernor?.Dispose();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            _petForm?.Dispose();

            // Detach window from WorkerW before destroying
            if (_isDesktopAttached)
            {
                DesktopWorker.DetachFromDesktop(Handle);
            }

            // Always restore original Windows wallpaper on exit
            DesktopWorker.RestoreOriginalWallpaper();

            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _powerGovernor?.Dispose();
                _trayIcon?.Dispose();
                _petForm?.Dispose();
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
