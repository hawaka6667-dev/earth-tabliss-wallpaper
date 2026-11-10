using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class EarthPetForm : Form
    {
        private readonly WallpaperForm _wallpaperForm;
        private PetMenuForm? _menuForm;
        private GeminiHubForm? _hubForm;
        private ToolTip? _petTooltip;

        private bool _isMouseDown = false;
        private bool _isDragging = false;
        private Point _mouseDownLocation;
        private Point _dragStartCursor;
        private bool _isHovered = false;

        public EarthPetForm(WallpaperForm wallpaperForm)
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
            DoubleBuffered = true;
            Size = new Size(76, 76);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            Cursor = Cursors.Hand;

            // Position at bottom-right corner of work area, strictly above taskbar
            var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1536, 816);
            Location = new Point(workArea.Right - 100, workArea.Bottom - 140);

            _petTooltip = new ToolTip
            {
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(56, 189, 248),
                InitialDelay = 400,
                AutoPopDelay = 6000,
                IsBalloon = true
            };
            _petTooltip.SetToolTip(this, "🌍 小地球桌宠 (初版)\n点击唤起中枢，拖拽可自由漫游\n⭐ 可收藏地标或与 Gemini 对话");

            MouseDown += OnPetMouseDown;
            MouseMove += OnPetMouseMove;
            MouseUp += OnPetMouseUp;
            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };

            ResumeLayout(false);

            _menuForm = new PetMenuForm(_wallpaperForm, this);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Perfect circular region using GDI+ GraphicsPath (DPI-scaled, zero distortion, zero black borders)
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, Width, Height);
            Region = new Region(path);

            // Pin to desktop layer (SHELLDLL_DefView):
            // 1. Never minimizes on "Show Desktop" (Win+D)
            // 2. Never floats above user application windows (Chrome, VS Code, etc.)
            DesktopWorker.AttachPetToDesktop(Handle);
        }

        public void NotifyLocationChanged(LocationInfo? loc)
        {
            if (loc == null) return;
            _menuForm?.UpdateLocationDisplay(loc);

            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        string status = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng) ? "★ 已收藏" : "⭐ 未收藏";
                        _petTooltip?.Show($"🌍 漫游至新地标:\n{loc.GetDisplayName()}\n[{status}] (点击桌宠可收藏/提问)", this, Width / 2, -60, 4500);
                    }));
                }
            }
            catch { }
        }

        private void OnPetMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isMouseDown = true;
                _isDragging = false;
                _mouseDownLocation = Location;
                _dragStartCursor = Cursor.Position;
            }
        }

        private void OnPetMouseMove(object? sender, MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                var curPos = Cursor.Position;
                int deltaX = curPos.X - _dragStartCursor.X;
                int deltaY = curPos.Y - _dragStartCursor.Y;

                if (!_isDragging && (Math.Abs(deltaX) > 4 || Math.Abs(deltaY) > 4))
                {
                    _isDragging = true;
                    Cursor = Cursors.SizeAll;
                    if (_menuForm != null && _menuForm.Visible)
                    {
                        _menuForm.Hide();
                    }
                }

                if (_isDragging)
                {
                    var workArea = Screen.FromControl(this)?.WorkingArea ?? Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
                    int rawX = _mouseDownLocation.X + deltaX;
                    int rawY = _mouseDownLocation.Y + deltaY;

                    // Strictly clamp to WorkingArea so pet NEVER collides with taskbar
                    int safeX = Math.Clamp(rawX, workArea.Left, workArea.Right - Width);
                    int safeY = Math.Clamp(rawY, workArea.Top, workArea.Bottom - Height);

                    Location = new Point(safeX, safeY);
                }
            }
        }

        private void OnPetMouseUp(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool wasDragging = _isDragging;
                _isMouseDown = false;
                _isDragging = false;
                Cursor = Cursors.Hand;

                if (!wasDragging)
                {
                    TogglePetMenu();
                }
            }
        }

        public void TogglePetMenu()
        {
            if (_menuForm == null || _menuForm.IsDisposed)
            {
                _menuForm = new PetMenuForm(_wallpaperForm, this);
            }

            if (_menuForm.Visible)
            {
                _menuForm.Hide();
            }
            else
            {
                _menuForm.UpdateLocationDisplay(_wallpaperForm.CurrentLocation);
                Point screenPos = PointToScreen(Point.Empty);
                _menuForm.PositionNearPet(screenPos, Size);
                _menuForm.Show();
                _menuForm.BringToFront();
            }
        }

        public void OpenGeminiHub(bool openToBookmarks = false)
        {
            if (_hubForm != null && !_hubForm.IsDisposed)
            {
                _hubForm.BringToFront();
                return;
            }

            var loc = _wallpaperForm.CurrentLocation ?? new LocationInfo
            {
                Country = "Australia",
                Region = "Torres Strait",
                Lat = "-10.040181",
                Lng = "143.560709"
            };

            _hubForm = new GeminiHubForm(_wallpaperForm, loc, openToBookmarks);
            _hubForm.Show();
        }

        public void OpenGoogleEarthMap()
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

        // ==========================================
        // Exact Initial Version Painting (Commit 021c831)
        // ==========================================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 1. Cosmic Deep Gradient Background
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                       Color.FromArgb(10, 15, 30), Color.FromArgb(2, 6, 23), 45f))
            {
                g.FillEllipse(brush, 1, 1, Width - 3, Height - 3);
            }

            // 2. Glowing Atmosphere Ring
            Color ringColor = _isHovered ? Color.FromArgb(56, 189, 248) : Color.FromArgb(14, 116, 144);
            using (var pen = new Pen(ringColor, _isHovered ? 2.5f : 1.8f))
            {
                g.DrawEllipse(pen, 3, 3, Width - 7, Height - 7);
            }

            // 3. Central Earth Mascot Icon (🌍)
            using var font = new Font("Segoe UI Emoji", 24f, FontStyle.Regular);
            string icon = "🌍";
            var size = g.MeasureString(icon, font);
            float ix = (Width - size.Width) / 2f + 1;
            float iy = (Height - size.Height) / 2f - 1;
            g.DrawString(icon, font, Brushes.White, ix, iy);

            // 4. Online Indicator Dot (Green pulse)
            using (var dotBrush = new SolidBrush(Color.FromArgb(34, 197, 94)))
            {
                g.FillEllipse(dotBrush, Width - 20, Height - 20, 8, 8);
            }
            using (var dotRingPen = new Pen(Color.FromArgb(15, 23, 42), 1.5f))
            {
                g.DrawEllipse(dotRingPen, Width - 20, Height - 20, 8, 8);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _menuForm?.Dispose();
                _hubForm?.Dispose();
                _petTooltip?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
