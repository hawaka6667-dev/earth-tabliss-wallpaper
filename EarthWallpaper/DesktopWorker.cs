using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public const int GWL_STYLE = -16;
        public const int WS_CHILD = 0x40000000;
        public const int WS_VISIBLE = 0x10000000;
        public const int WS_POPUP = unchecked((int)0x80000000);

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;

        public static IntPtr WorkerWHandle { get; private set; } = IntPtr.Zero;
        public static IntPtr ProgmanHandle { get; private set; } = IntPtr.Zero;

        public static bool AttachToDesktop(IntPtr formHandle)
        {
            ProgmanHandle = FindWindow("Progman", null);
            Program.Log($"DesktopWorker: ProgmanHandle = {ProgmanHandle}");
            if (ProgmanHandle == IntPtr.Zero)
            {
                Program.Log("[DesktopWorker] Could not find Progman window.");
                return false;
            }

            // Send message 0x052C to Progman to spawn the WorkerW behind icons
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(ProgmanHandle, 0x052C, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out result);
            Program.Log($"DesktopWorker: Sent 0x052C to Progman, result = {result}");

            IntPtr workerw = IntPtr.Zero;

            // Enumerate top-level windows to locate WorkerW
            EnumWindows((hwnd, lParam) =>
            {
                IntPtr shellView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    Program.Log($"DesktopWorker: Found SHELLDLL_DefView inside hwnd {hwnd}");
                    // Gets the WorkerW window immediately behind SHELLDLL_DefView
                    workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                    Program.Log($"DesktopWorker: Sibling WorkerW behind it = {workerw}");
                }
                return true;
            }, IntPtr.Zero);

            if (workerw == IntPtr.Zero)
            {
                // Fallback: in some Windows versions WorkerW is directly Progman's sibling
                workerw = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
                Program.Log($"DesktopWorker: Fallback workerw = {workerw}");
            }

            if (workerw != IntPtr.Zero)
            {
                WorkerWHandle = workerw;

                // Adjust window style to WS_CHILD before SetParent
                int style = GetWindowLong(formHandle, GWL_STYLE);
                SetWindowLong(formHandle, GWL_STYLE, (style | WS_CHILD | WS_VISIBLE) & ~WS_POPUP);

                IntPtr prevParent = SetParent(formHandle, workerw);
                Program.Log($"DesktopWorker: SetParent(form={formHandle}, workerw={workerw}), prevParent={prevParent}");

                // Resize to virtual screen covering all monitors
                int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
                int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
                int vcx = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                int vcy = GetSystemMetrics(SM_CYVIRTUALSCREEN);

                SetWindowPos(formHandle, IntPtr.Zero, vx, vy, vcx, vcy, SWP_NOZORDER | SWP_SHOWWINDOW);
                ShowWindow(formHandle, 1); // SW_SHOWNORMAL
                Program.Log($"[DesktopWorker] Attached to WorkerW ({workerw}) successfully. Bounds: {vx},{vy} {vcx}x{vcy}");
                return true;
            }

            Program.Log("[DesktopWorker] Failed to locate WorkerW.");
            return false;
        }
    }
}
