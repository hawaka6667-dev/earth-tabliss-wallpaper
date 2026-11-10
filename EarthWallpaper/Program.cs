using System;
using System.Threading;
using System.Windows.Forms;

namespace EarthWallpaper
{
    static class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        static void Main()
        {
            const string appName = "EarthTablissLiveWallpaper_SingleInstanceMutex";
            _mutex = new Mutex(true, appName, out bool createdNew);

            if (!createdNew)
            {
                // App is already running in background
                MessageBox.Show("Earth Tabliss 壁纸已经在运行中！\n请查看桌面最底层或屏幕右下角托盘图标。", "Earth Tabliss 提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new WallpaperForm());

            GC.KeepAlive(_mutex);
        }
    }
}
