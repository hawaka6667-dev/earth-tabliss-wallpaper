using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public static class SafeGdi
    {
        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        /// <summary>
        /// Creates a rounded rectangle region, applies it to the control,
        /// and cleanly releases both the previous Region and the unmanaged HRGN handle.
        /// </summary>
        public static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control == null || control.IsDisposed || control.Width <= 0 || control.Height <= 0) return;

            IntPtr hRgn = CreateRoundRectRgn(0, 0, control.Width, control.Height, radius, radius);
            if (hRgn == IntPtr.Zero) return;

            try
            {
                var oldRegion = control.Region;
                control.Region = Region.FromHrgn(hRgn);
                oldRegion?.Dispose();
            }
            finally
            {
                DeleteObject(hRgn);
            }
        }

        /// <summary>
        /// Creates a managed Icon from a Bitmap and immediately destroys the underlying unmanaged Win32 HICON.
        /// </summary>
        public static Icon CreateManagedIcon(Bitmap bmp)
        {
            IntPtr hIcon = bmp.GetHicon();
            try
            {
                using var tempIcon = Icon.FromHandle(hIcon);
                return (Icon)tempIcon.Clone();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        /// <summary>
        /// Applies modern Explorer dark theme to scrollbars and child controls on Windows 10/11.
        /// </summary>
        public static void ApplyDarkMode(Control control)
        {
            if (control == null || control.IsDisposed) return;
            try
            {
                if (control.IsHandleCreated)
                {
                    SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
                }
                else
                {
                    control.HandleCreated += (s, e) =>
                    {
                        try { SetWindowTheme(control.Handle, "DarkMode_Explorer", null); } catch { }
                    };
                }
            }
            catch { }
        }

        /// <summary>
        /// Enables immersive dark mode title bar for standard Win32 / WinForms windows.
        /// </summary>
        public static void ApplyDarkTitleBar(IntPtr hWnd)
        {
            try
            {
                int useDarkMode = 1;
                if (DwmSetWindowAttribute(hWnd, 20, ref useDarkMode, sizeof(int)) != 0)
                {
                    DwmSetWindowAttribute(hWnd, 19, ref useDarkMode, sizeof(int));
                }
            }
            catch { }
        }
    }
}
