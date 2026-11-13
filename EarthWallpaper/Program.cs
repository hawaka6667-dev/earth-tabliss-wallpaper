using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EarthWallpaper
{
    static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();

        private const uint DESKTOP_ALL_ACCESS = 0x01FF;

        private static Mutex? _mutex;
        public static string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

        public static void Log(string msg)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] {msg}";
                File.AppendAllText(LogFile, line + Environment.NewLine);
                Console.WriteLine(line);
            }
            catch { }
        }

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--render-api-dialog")
            {
                try
                {
                    ApplicationConfiguration.Initialize();
                    var form = new ApiKeySettingsForm();
                    var timer = new System.Windows.Forms.Timer { Interval = 600 };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        using var bmp = new System.Drawing.Bitmap(form.ClientSize.Width, form.ClientSize.Height);
                        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                        string outPath = args.Length > 1 ? args[1] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "api_settings_verified.png");
                        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                        Log($"Rendered API settings dialog to {outPath}");
                        form.Close();
                    };
                    timer.Start();
                    Application.Run(form);
                }
                catch (Exception ex)
                {
                    Log($"Render API dialog exception: {ex}");
                }
                return;
            }

            if (args.Length > 0 && args[0] == "--render-hub-bookmarks")
            {
                try
                {
                    ApplicationConfiguration.Initialize();
                    var loc = new LocationInfo { Country = "Spain", Region = "Barcelona", Lat = "41.406985", Lng = "2.175912" };
                    var form = new GeminiHubForm(new WallpaperForm(), loc, true, false);
                    var timer = new System.Windows.Forms.Timer { Interval = 1200 };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        using var bmp = new System.Drawing.Bitmap(form.ClientSize.Width, form.ClientSize.Height);
                        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                        string outPath = args.Length > 1 ? args[1] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hub_bookmarks_verified.png");
                        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                        Log($"Rendered Hub bookmarks to {outPath}");
                        form.Close();
                    };
                    timer.Start();
                    Application.Run(form);
                }
                catch (Exception ex)
                {
                    Log($"Render Hub bookmarks exception: {ex}");
                }
                return;
            }

            if (args.Length > 0 && args[0] == "--render-hub-dev")
            {
                try
                {
                    ApplicationConfiguration.Initialize();
                    var loc = new LocationInfo { Country = "Spain", Region = "Barcelona", Lat = "41.406985", Lng = "2.175912" };
                    var wpForm = new WallpaperForm();
                    // Supply realistic mock queue data for developer inspection rendering
                    wpForm.LatestQueueStatus = new QueueStatusInfo
                    {
                        QueueLength = 5,
                        LoadingCount = 0,
                        MaxQueueSize = 5,
                        HistoryLength = 4,
                        ShufflePtr = 42,
                        TotalLocations = 1450,
                        CurrentLocation = new QueueItemInfo
                        {
                            Country = "Spain",
                            Region = "Barcelona",
                            Lat = "41.406985",
                            Lng = "2.175912",
                            Image = "https://www.gstatic.com/prettyearth/assets/full/1329.jpg"
                        },
                        QueuedItems = new List<QueueItemInfo>
                        {
                            new QueueItemInfo { Country = "United States", Region = "Grand Canyon", Lat = "36.0544", Lng = "-112.1401", Image = "https://www.gstatic.com/prettyearth/assets/full/1003.jpg" },
                            new QueueItemInfo { Country = "Iceland", Region = "Vatnajökull", Lat = "64.4216", Lng = "-16.8093", Image = "https://www.gstatic.com/prettyearth/assets/full/1024.jpg" },
                            new QueueItemInfo { Country = "Japan", Region = "Mount Fuji", Lat = "35.3606", Lng = "138.7274", Image = "https://www.gstatic.com/prettyearth/assets/full/1150.jpg" },
                            new QueueItemInfo { Country = "Egypt", Region = "Nile Delta", Lat = "30.8025", Lng = "31.2500", Image = "https://www.gstatic.com/prettyearth/assets/full/1200.jpg" },
                            new QueueItemInfo { Country = "Australia", Region = "Great Barrier Reef", Lat = "-18.2871", Lng = "147.6992", Image = "https://www.gstatic.com/prettyearth/assets/full/1300.jpg" }
                        },
                        HistoryItems = new List<QueueItemInfo>
                        {
                            new QueueItemInfo { Country = "Norway", Region = "Lofoten Islands", Lat = "68.2036", Lng = "13.6052" },
                            new QueueItemInfo { Country = "Chile", Region = "Atacama Desert", Lat = "-23.8634", Lng = "-69.1328" }
                        }
                    };

                    var form = new GeminiHubForm(wpForm, loc, false, true);
                    var timer = new System.Windows.Forms.Timer { Interval = 1000 };
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        using var bmp = new System.Drawing.Bitmap(form.ClientSize.Width, form.ClientSize.Height);
                        form.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, form.ClientSize.Width, form.ClientSize.Height));
                        string outPath = args.Length > 1 ? args[1] : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hub_dev_verified.png");
                        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
                        Log($"Rendered Hub dev queue inspector to {outPath}");
                        form.Close();
                    };
                    timer.Start();
                    Application.Run(form);
                }
                catch (Exception ex)
                {
                    Log($"Render Hub dev exception: {ex}");
                }
                return;
            }

            Log("=== EarthWallpaper Starting (Main Thread) ===");

            // 1. Immediately back up user's active wallpaper before touching desktop
            DesktopWorker.BackupOriginalWallpaper();

            const string appName = "EarthTablissLiveWallpaper_SingleInstanceMutex";
            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                // Give up to 1500ms for a restarting/terminating previous instance to cleanly exit
                try
                {
                    if (_mutex.WaitOne(1500, false))
                    {
                        createdNew = true;
                    }
                }
                catch (AbandonedMutexException)
                {
                    createdNew = true;
                }
            }

            if (!createdNew)
            {
                Log("Another instance is already running.");
                MessageBox.Show("Earth Tabliss 已经在运行中！\n请查看桌面最底层或屏幕右下角托盘图标。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += (s, e) =>
            {
                DesktopWorker.RestoreOriginalWallpaper();
            };

            // Spawn clean STA UI thread to avoid CLR [STAThread] pre-creating OLE window on sandbox desktop
            var uiThread = new Thread(RunUIThread);
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            uiThread.Join();

            DesktopWorker.RestoreOriginalWallpaper();

            if (_mutex != null)
            {
                try { _mutex.ReleaseMutex(); } catch { }
                _mutex.Dispose();
            }
            Log("=== EarthWallpaper Terminated ===");
        }

        private static void RunUIThread()
        {
            try
            {
                Log("UI Thread started, attempting to bind to user Default desktop...");
                IntPtr hDefault = OpenDesktop("Default", 0, false, DESKTOP_ALL_ACCESS);
                if (hDefault != IntPtr.Zero)
                {
                    bool switched = SetThreadDesktop(hDefault);
                    Log($"Switched UI ThreadDesktop to Default: {switched} (hDefault={hDefault}, lastError={GetLastError()})");
                }
                else
                {
                    Log($"OpenDesktop(Default) failed, lastError={GetLastError()}");
                }

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    Log($"UnhandledException: {e.ExceptionObject}");
                };
                Application.ThreadException += (s, e) =>
                {
                    Log($"ThreadException: {e.Exception.Message}\n{e.Exception.StackTrace}");
                };

                ApplicationConfiguration.Initialize();
                Log("ApplicationConfiguration Initialized");

                var form = new WallpaperForm();
                Log("WallpaperForm Instantiated, running Application.Run...");
                Application.Run(form);
                Log("Application.Run exited.");
            }
            catch (Exception ex)
            {
                Log($"UI Thread Exception: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
