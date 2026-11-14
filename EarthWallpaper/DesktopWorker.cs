using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace EarthWallpaper
{
    public static class DesktopWorker
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? className, string? windowTitle);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        public const int SPI_SETDESKWALLPAPER = 20;
        public const int SPIF_UPDATEINIFILE = 0x01;
        public const int SPIF_SENDCHANGE = 0x02;

        public const int GWL_STYLE = -16;
        public const int GWL_EXSTYLE = -20;
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public static IntPtr WorkerWHandle { get; private set; } = IntPtr.Zero;
        public static IntPtr ProgmanHandle { get; private set; } = IntPtr.Zero;
        public static IntPtr ShellViewHandle { get; private set; } = IntPtr.Zero;
        public static bool IsRestarting { get; set; } = false;

        public static bool IsDesktopWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return true;
            if (hWnd == ProgmanHandle || hWnd == ShellViewHandle || hWnd == WorkerWHandle) return true;

            var sb = new System.Text.StringBuilder(256);
            GetClassName(hWnd, sb, 256);
            string cls = sb.ToString();
            if (cls == "Progman" || cls == "WorkerW" || cls == "Shell_TrayWnd") return true;

            return false;
        }

        public static bool AttachToDesktop(IntPtr formHandle)
        {
            ProgmanHandle = FindWindow("Progman", null);
            Program.Log($"DesktopWorker: ProgmanHandle = {ProgmanHandle}");
            if (ProgmanHandle == IntPtr.Zero)
            {
                Program.Log("[DesktopWorker] Could not find Progman window.");
                return false;
            }

            // Send message 0x052C to Progman to spawn/ensure WorkerW behind icons
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(ProgmanHandle, 0x052C, new IntPtr(0xD), new IntPtr(0x1), 0x0002, 1000, out result);
            Program.Log($"DesktopWorker: Sent 0x052C to Progman, result = {result}");

            IntPtr workerw = IntPtr.Zero;
            IntPtr shellViewParent = IntPtr.Zero;

            // Enumerate top-level windows to locate SHELLDLL_DefView
            EnumWindows((hwnd, lParam) =>
            {
                IntPtr shellView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    ShellViewHandle = shellView;
                    shellViewParent = hwnd;
                    Program.Log($"DesktopWorker: Found SHELLDLL_DefView ({shellView}) inside hwnd {hwnd}");
                }
                return true;
            }, IntPtr.Zero);

            if (shellViewParent != IntPtr.Zero)
            {
                // The WorkerW behind the icons is the sibling WorkerW directly after shellViewParent
                workerw = FindWindowEx(IntPtr.Zero, shellViewParent, "WorkerW", null);
                Program.Log($"DesktopWorker: Sibling WorkerW behind it = {workerw}");
            }

            // Fallback: If no sibling WorkerW was found after shellViewParent, find the WorkerW without SHELLDLL_DefView
            if (workerw == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    var sb = new System.Text.StringBuilder(256);
                    GetClassName(hwnd, sb, 256);
                    if (sb.ToString() == "WorkerW")
                    {
                        IntPtr sv = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (sv == IntPtr.Zero)
                        {
                            workerw = hwnd;
                            return false; // Found background WorkerW
                        }
                    }
                    return true;
                }, IntPtr.Zero);
                Program.Log($"DesktopWorker: Fallback empty WorkerW = {workerw}");
            }

            // Final fallback: Progman itself
            if (workerw == IntPtr.Zero)
            {
                workerw = ProgmanHandle;
                Program.Log($"DesktopWorker: Fallback to Progman = {workerw}");
            }

            if (workerw != IntPtr.Zero)
            {
                WorkerWHandle = workerw;

                int style = GetWindowLong(formHandle, GWL_STYLE);
                SetWindowLong(formHandle, GWL_STYLE, (style | WS_CHILD | WS_VISIBLE) & ~WS_POPUP);

                SetParent(formHandle, workerw);

                int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
                int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
                int vcx = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                int vcy = GetSystemMetrics(SM_CYVIRTUALSCREEN);

                SetWindowPos(formHandle, IntPtr.Zero, vx, vy, vcx, vcy, SWP_NOZORDER | SWP_SHOWWINDOW);
                ShowWindow(formHandle, 1);
                Program.Log($"[DesktopWorker] Attached to window ({workerw}) successfully. Bounds: {vx},{vy} {vcx}x{vcy}");
                return true;
            }

            Program.Log("[DesktopWorker] Failed to locate WorkerW.");
            return false;
        }

        public static bool ConfigurePetFloatingWindow(IntPtr petHandle)
        {
            try
            {
                // Safely configure pet as an independent top-level toolwindow:
                // 1. NEVER call SetParent into Explorer's SHELLDLL_DefView (prevents cross-process thread input desync & Alt latching)
                // 2. Apply WS_EX_TOOLWINDOW (hides from Alt+Tab / taskbar) and WS_EX_NOACTIVATE (never steals keyboard focus)
                int exStyle = GetWindowLong(petHandle, GWL_EXSTYLE);
                SetWindowLong(petHandle, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);

                // Position cleanly without activating or stealing focus from user applications
                SetWindowPos(petHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                ShowWindow(petHandle, 1);
                Program.Log("DesktopWorker: Configured pet as safe floating toolwindow (isolated from Explorer message loop).");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"DesktopWorker.ConfigurePetFloatingWindow error: {ex.Message}");
                return false;
            }
        }

        public static void DetachFromDesktop(IntPtr formHandle)
        {
            try
            {
                if (formHandle != IntPtr.Zero)
                {
                    SetParent(formHandle, IntPtr.Zero);
                    ShowWindow(formHandle, 0 /* SW_HIDE */);
                    Program.Log($"DesktopWorker: Detached window {formHandle} from parent cleanly.");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"DesktopWorker.DetachFromDesktop error: {ex.Message}");
            }
        }

        public static IntPtr GetShellViewHandle()
        {
            if (ShellViewHandle != IntPtr.Zero)
            {
                return ShellViewHandle;
            }

            IntPtr progman = FindWindow("Progman", null);
            IntPtr shellView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView == IntPtr.Zero)
            {
                EnumWindows((hwnd, lParam) =>
                {
                    IntPtr sv = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                    if (sv != IntPtr.Zero)
                    {
                        shellView = sv;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            }

            ShellViewHandle = shellView;
            return shellView;
        }

        public static bool ToggleDesktopIcons()
        {
            IntPtr shellView = GetShellViewHandle();
            Program.Log($"DesktopWorker: ToggleDesktopIcons, shellView = {shellView}");
            if (shellView == IntPtr.Zero) return true;

            // Target the actual icons ListView inside SHELLDLL_DefView to preserve transparency of SHELLDLL_DefView
            IntPtr listView = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
            if (listView != IntPtr.Zero)
            {
                bool isVisible = IsWindowVisible(listView);
                ShowWindow(listView, isVisible ? 0 /* SW_HIDE */ : 5 /* SW_SHOW */);
                Program.Log($"DesktopWorker: Toggled SysListView32 to {!isVisible}");
                return !isVisible;
            }

            // Fallback: Dispatch Explorer native "Show desktop icons" toggle command (0x7402 / 29698)
            SendMessage(shellView, 0x0111 /* WM_COMMAND */, (IntPtr)0x7402, IntPtr.Zero);
            Program.Log("DesktopWorker: Sent WM_COMMAND 0x7402 to SHELLDLL_DefView");
            return true;
        }

        private static readonly string BackupFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper", "original_wallpaper_backup.png");

        public static void BackupOriginalWallpaper()
        {
            try
            {
                string dir = Path.GetDirectoryName(BackupFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                // Check 1: Windows active transcoded wallpaper (fresh capture on launch)
                string transcoded = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Themes\TranscodedWallpaper");
                if (File.Exists(transcoded) && new FileInfo(transcoded).Length > 1000)
                {
                    File.Copy(transcoded, BackupFilePath, true);
                    Program.Log($"DesktopWorker: Captured active TranscodedWallpaper ({new FileInfo(BackupFilePath).Length} bytes) to {BackupFilePath}");
                    return;
                }

                // Check 2: Registry Wallpaper path
                string? regWall = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", "") as string;
                if (!string.IsNullOrEmpty(regWall) && File.Exists(regWall) && !regWall.Contains("EarthWallpaper"))
                {
                    File.Copy(regWall, BackupFilePath, true);
                    Program.Log($"DesktopWorker: Captured active Registry Wallpaper ({regWall}) to {BackupFilePath}");
                    return;
                }

                // Check 3: Registry TranscodedImageCache
                var cache = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "TranscodedImageCache", null) as byte[];
                if (cache != null && cache.Length > 24)
                {
                    string decoded = System.Text.Encoding.Unicode.GetString(cache, 24, cache.Length - 24).Trim('\0');
                    if (File.Exists(decoded) && !decoded.Contains("EarthWallpaper"))
                    {
                        File.Copy(decoded, BackupFilePath, true);
                        Program.Log($"DesktopWorker: Captured active TranscodedImageCache ({decoded}) to {BackupFilePath}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"DesktopWorker: BackupOriginalWallpaper error: {ex.Message}");
            }
        }

        public static void CleanupOrphanWorkerWs()
        {
            try
            {
                EnumWindows((hwnd, lParam) =>
                {
                    var sb = new System.Text.StringBuilder(256);
                    GetClassName(hwnd, sb, 256);
                    if (sb.ToString() == "WorkerW")
                    {
                        IntPtr defView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                        if (defView == IntPtr.Zero)
                        {
                            ShowWindow(hwnd, 0 /* SW_HIDE */);
                            Program.Log($"DesktopWorker: Cleaned up orphan WorkerW layer ({hwnd}).");
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Program.Log($"DesktopWorker.CleanupOrphanWorkerWs error: {ex.Message}");
            }
        }

        public static void RestoreOriginalWallpaper()
        {
            if (IsRestarting)
            {
                Program.Log("DesktopWorker: Application is restarting, skipping wallpaper restore.");
                return;
            }

            try
            {
                Program.Log("DesktopWorker: Restoring original desktop state...");

                // 1. Ensure desktop icons are restored visible if previously hidden
                IntPtr shellView = GetShellViewHandle();
                if (shellView != IntPtr.Zero)
                {
                    IntPtr listView = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
                    if (listView != IntPtr.Zero && !IsWindowVisible(listView))
                    {
                        ShowWindow(listView, 5 /* SW_SHOW */);
                        Program.Log("DesktopWorker: Restored SysListView32 visibility on exit.");
                    }
                }

                // 2. Hide WorkerW if alive to unveil the underlying desktop immediately
                if (WorkerWHandle != IntPtr.Zero)
                {
                    ShowWindow(WorkerWHandle, 0 /* SW_HIDE */);
                    Program.Log("DesktopWorker: Hid WorkerWHandle on exit.");
                }

                // 3. Clean up ANY orphan WorkerW layers left behind by crashed/aborted instances
                CleanupOrphanWorkerWs();

                // 4. Force Progman and ShellView full dual redraw to reveal underlying wallpaper
                IntPtr progman = FindWindow("Progman", null);
                if (progman != IntPtr.Zero)
                {
                    RedrawWindow(progman, IntPtr.Zero, IntPtr.Zero, 0x0185 /* RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_UPDATENOW */);
                }

                if (shellView != IntPtr.Zero)
                {
                    RedrawWindow(shellView, IntPtr.Zero, IntPtr.Zero, 0x0185 /* RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_UPDATENOW */);
                }

                // 5. Broadcast setting change WITHOUT modifying user registry (NO SPIF_UPDATEINIFILE)
                SendMessageTimeout((IntPtr)0xffff /* HWND_BROADCAST */, 0x001A /* WM_SETTINGCHANGE */, (IntPtr)SPI_SETDESKWALLPAPER, IntPtr.Zero, 0x0002, 1000, out _);

                // Refresh Windows desktop wallpaper engine safely (reads registry without overriding)
                SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_SENDCHANGE);
                Program.Log("DesktopWorker: Broadcasted wallpaper refresh to Windows Explorer.");
            }
            catch (Exception ex)
            {
                Program.Log($"DesktopWorker: RestoreOriginalWallpaper exception: {ex.Message}");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
