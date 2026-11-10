using System;
using System.Diagnostics;
using System.Drawing;
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
            var itemGemini = new ToolStripMenuItem("✨ Gemini 地貌深度研判", null, (s, e) => SendWebMessage("open_gemini"));
            var itemExit = new ToolStripMenuItem("🚪 退出 Earth Tabliss (恢复原壁纸)", null, (s, e) => ExitApplication());

            menu.Items.AddRange(new ToolStripItem[] {
                itemIcons,
                itemNext,
                itemGemini,
                new ToolStripSeparator(),
                itemExit
            });

            _trayIcon = new NotifyIcon
            {
                Text = "Earth Tabliss - 高性能动态桌面壁纸",
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => SendWebMessage("open_gemini");
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

        private void ExitApplication()
        {
            _powerGovernor?.Dispose();
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }
            _petForm?.Dispose();

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
