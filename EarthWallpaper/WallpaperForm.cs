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

        private ToolStripMenuItem? _itemIcons;
        private ToolStripMenuItem? _itemNext;
        private ToolStripMenuItem? _itemPrev;
        private ToolStripMenuItem? _itemBookmarks;
        private ToolStripMenuItem? _itemGemini;
        private ToolStripMenuItem? _itemSkin;
        private ToolStripMenuItem? _itemDev;
        private ToolStripMenuItem? _itemReload;
        private ToolStripMenuItem? _itemStartup;
        private ToolStripMenuItem? _itemExit;
        private ToolStripMenuItem? _itemLangMenu;
        private ToolStripMenuItem? _itemLangAuto;
        private ToolStripMenuItem? _itemLangEn;
        private ToolStripMenuItem? _itemLangZh;

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
            StartGracefulExitWatcher();
        }

        private EventWaitHandle? _exitWatcherEvent;
        private Thread? _exitWatcherThread;

        private void StartGracefulExitWatcher()
        {
            try
            {
                _exitWatcherEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "EarthTabliss_GracefulExitEvent", out _);
                _exitWatcherThread = new Thread(() =>
                {
                    try
                    {
                        while (!_isExiting)
                        {
                            if (_exitWatcherEvent.WaitOne(400))
                            {
                                Program.Log("WallpaperForm: Graceful exit signaled from external process.");
                                if (IsHandleCreated && !IsDisposed)
                                {
                                    BeginInvoke(new Action(ExitApplication));
                                }
                                break;
                            }
                        }
                    }
                    catch { }
                })
                {
                    IsBackground = true,
                    Name = "GracefulExitWatcher"
                };
                _exitWatcherThread.Start();
            }
            catch (Exception ex)
            {
                Program.Log($"StartGracefulExitWatcher error: {ex.Message}");
            }
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
            
            _itemIcons = new ToolStripMenuItem(L10n.MenuToggleIcons, null, (s, e) => DesktopWorker.ToggleDesktopIcons());
            _itemNext = new ToolStripMenuItem(L10n.MenuNextWallpaper, null, (s, e) => SendWebMessage("next_wallpaper"));
            _itemPrev = new ToolStripMenuItem(L10n.MenuPrevWallpaper, null, (s, e) => SendWebMessage("previous_wallpaper"));
            _itemBookmarks = new ToolStripMenuItem(L10n.MenuBookmarks, null, (s, e) => _petForm?.OpenGeminiHub(true));
            _itemGemini = new ToolStripMenuItem(L10n.MenuAskGemini, null, (s, e) => _petForm?.OpenGeminiHub(false));
            
            _itemSkin = new ToolStripMenuItem($"{L10n.MenuSwitchSkin} ({_petForm?.GetSkinDisplayName() ?? "Classic"})", null, (s, e) =>
            {
                _petForm?.ToggleSkin();
                UpdateSkinMenuItemText();
            });

            _itemLangMenu = new ToolStripMenuItem(L10n.MenuLanguage);
            _itemLangAuto = new ToolStripMenuItem(L10n.LangAuto, null, (s, e) => L10n.SetLanguage(AppLanguage.Auto));
            _itemLangEn = new ToolStripMenuItem(L10n.LangEn, null, (s, e) => L10n.SetLanguage(AppLanguage.En));
            _itemLangZh = new ToolStripMenuItem(L10n.LangZh, null, (s, e) => L10n.SetLanguage(AppLanguage.Zh));

            _itemLangMenu.DropDownItems.Add(_itemLangAuto);
            _itemLangMenu.DropDownItems.Add(_itemLangEn);
            _itemLangMenu.DropDownItems.Add(_itemLangZh);

            _itemDev = new ToolStripMenuItem(L10n.MenuDevTelemetry, null, (s, e) => _petForm?.OpenGeminiHub(false, true));
            _itemReload = new ToolStripMenuItem(L10n.MenuReload, null, (s, e) => RestartApplication());
            
            _itemStartup = new ToolStripMenuItem();
            _itemStartup.Checked = IsStartupEnabled();
            _itemStartup.Text = _itemStartup.Checked ? L10n.MenuStartupEnabled : L10n.MenuStartupDisabled;
            _itemStartup.Click += (s, e) =>
            {
                bool newState = !_itemStartup.Checked;
                SetStartupEnabled(newState);
                _itemStartup.Checked = newState;
                _itemStartup.Text = newState ? L10n.MenuStartupEnabled : L10n.MenuStartupDisabled;
            };

            _itemExit = new ToolStripMenuItem(L10n.MenuExit, null, (s, e) => ExitApplication());

            menu.Items.AddRange(new ToolStripItem[] {
                _itemIcons,
                _itemNext,
                _itemPrev,
                new ToolStripSeparator(),
                _itemBookmarks,
                _itemGemini,
                _itemSkin,
                _itemLangMenu,
                new ToolStripSeparator(),
                _itemDev,
                _itemStartup,
                _itemReload,
                _itemExit
            });

            _trayIcon = new NotifyIcon
            {
                Text = L10n.AppDescription,
                Icon = CreateEarthTrayIcon(),
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (s, e) => _petForm?.OpenGeminiHub(false);

            UpdateLanguageCheckmarks();
            L10n.OnLanguageChanged += OnLanguageChangedHandler;
        }

        private void UpdateSkinMenuItemText()
        {
            if (_itemSkin != null && _petForm != null)
            {
                _itemSkin.Text = $"{L10n.MenuSwitchSkin} ({_petForm.GetSkinDisplayName()})";
            }
        }

        private void UpdateLanguageCheckmarks()
        {
            var cur = L10n.CurrentLanguage;
            if (_itemLangAuto != null) _itemLangAuto.Checked = (cur == AppLanguage.Auto);
            if (_itemLangEn != null) _itemLangEn.Checked = (cur == AppLanguage.En);
            if (_itemLangZh != null) _itemLangZh.Checked = (cur == AppLanguage.Zh);
        }

        private void OnLanguageChangedHandler()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(OnLanguageChangedHandler));
                return;
            }

            if (_trayIcon != null) _trayIcon.Text = L10n.AppDescription;
            if (_itemIcons != null) _itemIcons.Text = L10n.MenuToggleIcons;
            if (_itemNext != null) _itemNext.Text = L10n.MenuNextWallpaper;
            if (_itemPrev != null) _itemPrev.Text = L10n.MenuPrevWallpaper;
            if (_itemBookmarks != null) _itemBookmarks.Text = L10n.MenuBookmarks;
            if (_itemGemini != null) _itemGemini.Text = L10n.MenuAskGemini;
            UpdateSkinMenuItemText();
            if (_itemDev != null) _itemDev.Text = L10n.MenuDevTelemetry;
            if (_itemReload != null) _itemReload.Text = L10n.MenuReload;
            if (_itemStartup != null) _itemStartup.Text = _itemStartup.Checked ? L10n.MenuStartupEnabled : L10n.MenuStartupDisabled;
            if (_itemExit != null) _itemExit.Text = L10n.MenuExit;

            if (_itemLangMenu != null) _itemLangMenu.Text = L10n.MenuLanguage;
            if (_itemLangAuto != null) _itemLangAuto.Text = L10n.LangAuto;
            if (_itemLangEn != null) _itemLangEn.Text = L10n.LangEn;
            if (_itemLangZh != null) _itemLangZh.Text = L10n.LangZh;

            UpdateLanguageCheckmarks();
            SendWebMessage("set_language", L10n.IsChinese ? "zh" : "en");
        }

        private Icon CreateEarthTrayIcon()
        {
            try
            {
                using var bmp = new Bitmap(32, 32);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;

                    using (var b = new SolidBrush(Color.FromArgb(15, 23, 42)))
                        g.FillEllipse(b, 1, 1, 29, 29);
                    using (var p = new Pen(Color.FromArgb(56, 189, 248), 2f))
                        g.DrawEllipse(p, 2, 2, 27, 27);
                    using var font = new Font("Segoe UI Emoji", 13f);
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("🌍", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
                }

                return SafeGdi.CreateManagedIcon(bmp);
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

                    _webView.CoreWebView2.NavigationCompleted += (s, e) =>
                    {
                        SendWebMessage("set_language", L10n.IsChinese ? "zh" : "en");
                    };
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
                        ImageUrl = dataEl.TryGetProperty("image", out var imgEl) ? imgEl.GetString() ?? "" : "",
                        Terrain = dataEl.TryGetProperty("terrain", out var tEl) ? tEl.GetString() ?? "" : ""
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

        public void SendWebMessage(string actionType, string lang)
        {
            if (_webView != null && _webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.PostWebMessageAsJson($"{{\"type\":\"{actionType}\",\"lang\":\"{lang}\"}}");
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
                L10n.OnLanguageChanged -= OnLanguageChangedHandler;
                _powerGovernor?.Dispose();
                _trayIcon?.Dispose();
                _petForm?.Dispose();
                _webView?.Dispose();
                _exitWatcherEvent?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
