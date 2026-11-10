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
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_POPUP = unchecked((int)0x80000000);

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

        public static bool AttachToDesktop(IntPtr formHandle)
        {
            ProgmanHandle = FindWindow("Progman", null);
            Program.Log($"DesktopWorker: ProgmanHandle = {ProgmanHandle}");
            if (ProgmanHandle == IntPtr.Zero)
            {
                Program.Log("[DesktopWorker] Could not find Progman window.");
                return false;
            }

            // Send message 0x052C to Progman to spawn WorkerW behind icons
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(ProgmanHandle, 0x052C, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out result);
            Program.Log($"DesktopWorker: Sent 0x052C to Progman, result = {result}");

            IntPtr workerw = IntPtr.Zero;

            // Enumerate top-level windows to locate the WorkerW that sits behind SHELLDLL_DefView
            EnumWindows((hwnd, lParam) =>
            {
                IntPtr shellView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    ShellViewHandle = shellView;
                    Program.Log($"DesktopWorker: Found SHELLDLL_DefView ({shellView}) inside hwnd {hwnd}");
                    workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    Program.Log($"DesktopWorker: Sibling WorkerW behind it = {workerw}");
                }
                return true;
            }, IntPtr.Zero);

            if (workerw == IntPtr.Zero)
            {
                workerw = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
                Program.Log($"DesktopWorker: Fallback workerw = {workerw}");
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
                Program.Log($"[DesktopWorker] Attached to WorkerW ({workerw}) successfully. Bounds: {vx},{vy} {vcx}x{vcy}");
                return true;
            }

            Program.Log("[DesktopWorker] Failed to locate WorkerW.");
            return false;
        }

        public static bool AttachPetToDesktop(IntPtr petHandle)
        {
            try
            {
                IntPtr shellView = GetShellViewHandle();
                Program.Log($"DesktopWorker: AttachPetToDesktop, shellView = {shellView}, petHandle = {petHandle}");
                if (shellView == IntPtr.Zero)
                {
                    Program.Log("DesktopWorker: ShellView not found, falling back to floating toolwindow.");
                    return false;
                }

                int style = GetWindowLong(petHandle, GWL_STYLE);
                SetWindowLong(petHandle, GWL_STYLE, (style | WS_CHILD | WS_VISIBLE) & ~WS_POPUP);
                SetParent(petHandle, shellView);

                // Ensure it sits at the top among siblings inside SHELLDLL_DefView (above desktop icons)
                SetWindowPos(petHandle, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                ShowWindow(petHandle, 1);
                Program.Log("DesktopWorker: Attached pet to SHELLDLL_DefView successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Program.Log($"DesktopWorker.AttachPetToDesktop error: {ex.Message}");
                return false;
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

        public static void RestoreOriginalWallpaper()
        {
            try
            {
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

                // 2. High-priority fallback: Windows active transcoded wallpaper cache
                string transcoded = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Themes\TranscodedWallpaper");
                string? wallpaper = Registry.GetValue(@"HKEY_CURRENT_USER\Control Panel\Desktop", "Wallpaper", "") as string;

                string target = "";
                if (File.Exists(transcoded))
                {
                    target = transcoded;
                }
                else if (!string.IsNullOrEmpty(wallpaper) && File.Exists(wallpaper))
                {
                    target = wallpaper;
                }

                Program.Log($"DesktopWorker: Restoring original wallpaper target: {target}");
                if (!string.IsNullOrEmpty(target))
                {
                    SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, target, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                }
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
