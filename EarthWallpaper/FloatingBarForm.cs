using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class FloatingBarForm : Form
    {
        private readonly WallpaperForm _wallpaperForm;
        private Label? _lblDragGrip;
        private Button? _btnIcons;
        private Button? _btnNext;
        private Button? _btnGemini;
        private Button? _btnMap;
        private Button? _btnClose;
        private GeminiAnalysisForm? _geminiForm;

        private bool _isDragging = false;
        private Point _dragStartPoint;
        private bool _iconsVisible = true;

        public FloatingBarForm(WallpaperForm wallpaperForm)
        {
            _wallpaperForm = wallpaperForm;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            Size = new Size(390, 42);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            Cursor = Cursors.Default;

            // Position at top-right corner of primary screen work area, strictly above taskbar
            var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1536, 816);
            Location = new Point(workArea.Right - 410, 16);

            // Drag Grip (⋮⋮)
            _lblDragGrip = new Label
            {
                Text = "⋮⋮",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(4, 8),
                Size = new Size(18, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.SizeAll
            };
            _lblDragGrip.MouseDown += OnStartDrag;
            _lblDragGrip.MouseMove += OnDoDrag;
            _lblDragGrip.MouseUp += OnStopDrag;

            // Buttons:
            // 1. Eye Icon: Toggle Desktop Icons (SHELLDLL_DefView)
            _btnIcons = CreateButton("👁 图标", 24, 5, 76, Color.FromArgb(56, 189, 248), OnToggleIconsClicked);
            // 2. Next: Switch Wallpaper
            _btnNext = CreateButton("⏭ 换张", 104, 5, 68, Color.FromArgb(241, 245, 249), (s, e) => _wallpaperForm.SendWebMessage("next_wallpaper"));
            // 3. Gemini: Open Geographical Deep Analysis Window
            _btnGemini = CreateButton("✨ 研判", 176, 5, 72, Color.FromArgb(165, 180, 252), OnOpenGeminiClicked);
            // 4. Map: Open Google Earth in browser
            _btnMap = CreateButton("🌐 地图", 252, 5, 68, Color.FromArgb(52, 211, 153), OnOpenMapClicked);
            // 5. Exit: Clean Exit and Restore Original Wallpaper
            _btnClose = CreateButton("✕", 324, 5, 42, Color.FromArgb(248, 113, 113), (s, e) => ExitApplication());

            Controls.Add(_lblDragGrip);
            Controls.Add(_btnIcons);
            Controls.Add(_btnNext);
            Controls.Add(_btnGemini);
            Controls.Add(_btnMap);
            Controls.Add(_btnClose);

            // Allow dragging from form background as well
            MouseDown += OnStartDrag;
            MouseMove += OnDoDrag;
            MouseUp += OnStopDrag;

            ResumeLayout(false);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SafeGdi.ApplyRoundedRegion(this, 16);
        }

        private Button CreateButton(string text, int x, int y, int width, Color textColor, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = textColor,
                BackColor = Color.FromArgb(30, 41, 59),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            btn.Click += onClick;
            return btn;
        }

        // ==========================================
        // Free Dragging - Strictly Clamped to WorkArea
        // ==========================================
        private void OnStartDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStartPoint = e.Location;
            }
        }

        private void OnDoDrag(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var workArea = Screen.FromControl(this).WorkingArea;
                int rawX = Location.X + (e.X - _dragStartPoint.X);
                int rawY = Location.Y + (e.Y - _dragStartPoint.Y);

                // Math.Clamp strictly prevents colliding or overlapping with the Taskbar
                int safeX = Math.Clamp(rawX, workArea.Left, workArea.Right - Width);
                int safeY = Math.Clamp(rawY, workArea.Top, workArea.Bottom - Height);

                Location = new Point(safeX, safeY);
            }
        }

        private void OnStopDrag(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = false;
            }
        }

        // ==========================================
        // Eye Icon: Toggle Desktop Icons Visibility
        // ==========================================
        private void OnToggleIconsClicked(object? sender, EventArgs e)
        {
            bool nowVisible = DesktopWorker.ToggleDesktopIcons();
            _iconsVisible = nowVisible;
            if (_btnIcons != null)
            {
                _btnIcons.Text = _iconsVisible ? "👁 图标" : "👁‍🗨 已隐";
                _btnIcons.ForeColor = _iconsVisible ? Color.FromArgb(56, 189, 248) : Color.FromArgb(250, 204, 21);
            }
        }

        // ==========================================
        // Gemini Analysis Dialog
        // ==========================================
        private void OnOpenGeminiClicked(object? sender, EventArgs e)
        {
            if (_geminiForm != null && !_geminiForm.IsDisposed)
            {
                _geminiForm.BringToFront();
                return;
            }

            var loc = _wallpaperForm.CurrentLocation ?? new LocationInfo
            {
                Country = "地球秘境",
                Region = "卫星高分观测",
                Lat = "-10.040181",
                Lng = "143.560709"
            };

            _geminiForm = new GeminiAnalysisForm(loc);
            _geminiForm.Show();
        }

        private void OnOpenMapClicked(object? sender, EventArgs e)
        {
            var loc = _wallpaperForm.CurrentLocation;
            string targetUrl = (loc != null && !string.IsNullOrEmpty(loc.MapUrl))
                ? loc.MapUrl
                : "https://earth.google.com/web/";

            try
            {
                Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Program.Log($"Failed to open map: {ex.Message}");
            }
        }

        private void ExitApplication()
        {
            DesktopWorker.RestoreOriginalWallpaper();
            Application.Exit();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(71, 85, 105), 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
