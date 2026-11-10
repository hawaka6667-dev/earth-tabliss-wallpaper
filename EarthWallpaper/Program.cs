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

            Log("=== EarthWallpaper Starting (Main Thread) ===");

            // 1. Immediately back up user's active wallpaper before touching desktop
            DesktopWorker.BackupOriginalWallpaper();

            const string appName = "EarthTablissLiveWallpaper_SingleInstanceMutex";
            _mutex = new Mutex(true, appName, out bool createdNew);

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
