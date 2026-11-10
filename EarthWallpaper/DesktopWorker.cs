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
            if (ProgmanHandle == IntPtr.Zero)
            {
                Debug.WriteLine("[DesktopWorker] Could not find Progman window.");
                return false;
            }

            // Send message 0x052C to Progman to spawn the WorkerW behind icons
            SendMessageTimeout(ProgmanHandle, 0x052C, IntPtr.Zero, IntPtr.Zero, 0x0002, 1000, out _);

            IntPtr workerw = IntPtr.Zero;

            // Enumerate top-level windows to locate WorkerW
            EnumWindows((hwnd, lParam) =>
            {
                IntPtr shellView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    // Gets the WorkerW window immediately behind SHELLDLL_DefView
                    workerw = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                }
                return true;
            }, IntPtr.Zero);

            if (workerw == IntPtr.Zero)
            {
                // Fallback: in some Windows versions WorkerW is directly Progman's sibling
                workerw = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "WorkerW", null);
            }

            if (workerw != IntPtr.Zero)
            {
                WorkerWHandle = workerw;
                SetParent(formHandle, workerw);

                // Resize to virtual screen covering all monitors
                int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
                int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
                int vcx = GetSystemMetrics(SM_CXVIRTUALSCREEN);
                int vcy = GetSystemMetrics(SM_CYVIRTUALSCREEN);

                SetWindowPos(formHandle, IntPtr.Zero, vx, vy, vcx, vcy, SWP_NOZORDER | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                Debug.WriteLine($"[DesktopWorker] Attached to WorkerW ({workerw}) successfully. Bounds: {vx},{vy} {vcx}x{vcy}");
                return true;
            }

            Debug.WriteLine("[DesktopWorker] Failed to locate WorkerW.");
            return false;
        }
    }
}
