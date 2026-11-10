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
        private bool _isDesktopAttached = false;
        private string _appPath = string.Empty;

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

            // Match full primary screen initially
            var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1920, 1080);
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
            
            var itemNext = new ToolStripMenuItem("⏭ 下一张地球壁纸 (Space)", null, (s, e) => SendWebMessage("next_wallpaper"));
            var itemGemini = new ToolStripMenuItem("✨ Gemini 地貌深度研判 (G)", null, (s, e) => SendWebMessage("open_gemini"));
            var itemBookmarks = new ToolStripMenuItem("📌 打开研究收藏库 (B)", null, (s, e) => SendWebMessage("open_bookmarks"));
            var itemSettings = new ToolStripMenuItem("⚙ 壁纸设置 (S)", null, (s, e) => SendWebMessage("open_settings"));
            var itemToggleMode = new ToolStripMenuItem("🖥 切换窗口/桌面模式", null, (s, e) => ToggleWindowMode());
            var itemExit = new ToolStripMenuItem("🚪 退出 Earth Tabliss", null, (s, e) => ExitApplication());

            menu.Items.AddRange(new ToolStripItem[] {
                itemNext,
                itemGemini,
                itemBookmarks,
                new ToolStripSeparator(),
                itemSettings,
                itemToggleMode,
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
                Program.Log($"InitWebViewAsync: userData = {userData}");
                var env = await CoreWebView2Environment.CreateAsync(null, userData);
                Program.Log("InitWebViewAsync: CoreWebView2Environment created");

                if (_webView != null)
                {
                    await _webView.EnsureCoreWebView2Async(env);
                    Program.Log("InitWebViewAsync: EnsureCoreWebView2Async completed");

                    _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    _webView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;

                    _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "earthwallpaper.local",
                        _appPath,
                        CoreWebView2HostResourceAccessKind.Allow
                    );

                    _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
                    _webView.CoreWebView2.NavigationCompleted += (s, e) =>
                    {
                        Program.Log($"WebView2 NavigationCompleted: isSuccess={e.IsSuccess}, status={e.WebErrorStatus}");
                    };

                    Program.Log("InitWebViewAsync: Navigating to https://earthwallpaper.local/index.html");
                    _webView.CoreWebView2.Navigate("https://earthwallpaper.local/index.html");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"InitWebViewAsync EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"WebView2 初始化异常: {ex.Message}", "Earth Tabliss 错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AttachToDesktopLayer()
        {
            _isDesktopAttached = DesktopWorker.AttachToDesktop(Handle);
            if (!_isDesktopAttached)
            {
                Program.Log("[WallpaperForm] Failed to attach to WorkerW, falling back to visible windowed mode so user can see it.");
                FormBorderStyle = FormBorderStyle.Sizable;
                Bounds = new Rectangle(120, 120, 1366, 768);
                StartPosition = FormStartPosition.CenterScreen;
                ShowInTaskbar = true;
                Visible = true;
            }
        }

        private void ToggleWindowMode()
        {
            if (_isDesktopAttached)
            {
                // Detach to floating window
                DesktopWorker.SetParent(Handle, IntPtr.Zero);
                FormBorderStyle = FormBorderStyle.Sizable;
                Bounds = new Rectangle(100, 100, 1280, 720);
                ShowInTaskbar = true;
                _isDesktopAttached = false;
            }
            else
            {
                // Re-attach to desktop
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                AttachToDesktopLayer();
            }
        }

        private void OnPowerStateChanged(bool isPaused)
        {
            if (_webView != null && _webView.CoreWebView2 != null)
            {
                string json = $"{{\"type\":\"power_state\",\"isPaused\":{isPaused.ToString().ToLower()}}}";
                _webView.CoreWebView2.PostWebMessageAsJson(json);

                // Set memory target to low when suspended to reclaim RAM for Celeron system
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

                if (type == "open_url" && root.TryGetProperty("url", out var urlProp))
                {
                    string url = urlProp.GetString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(url))
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                }
                else if (type == "save_bookmark" && root.TryGetProperty("bookmark", out var bmProp))
                {
                    SaveBookmarkToDisk(bmProp.GetRawText());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WallpaperForm] WebMessage parse error: {ex.Message}");
            }
        }

        private void SaveBookmarkToDisk(string bookmarkJson)
        {
            try
            {
                string dataDir = Path.Combine(_appPath, "data");
                Directory.CreateDirectory(dataDir);
                string file = Path.Combine(dataDir, "bookmarks.json");

                System.IO.File.AppendAllText(file, bookmarkJson + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WallpaperForm] Failed to write bookmark: {ex.Message}");
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
            Application.Exit();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _powerGovernor?.Dispose();
                _trayIcon?.Dispose();
                _webView?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
