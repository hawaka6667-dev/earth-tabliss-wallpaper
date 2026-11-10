using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class EarthPetForm : Form
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateEllipticRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        private readonly WallpaperForm _wallpaperForm;
        private PetMenuForm? _menuForm;
        private GeminiHubForm? _hubForm;
        private ToolTip? _petTooltip;
        private Image? _mascotImage;

        private bool _isMouseDown = false;
        private bool _isDragging = false;
        private Point _mouseDownLocation;
        private Point _dragStartCursor;
        private bool _isHovered = false;

        public EarthPetForm(WallpaperForm wallpaperForm)
        {
            _wallpaperForm = wallpaperForm;
            InitializeComponent();
            LoadMascotImage();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            // CRITICAL: TopMost = false so the pet ONLY floats on the desktop, never obstructing other apps
            TopMost = false;
            DoubleBuffered = true;
            Size = new Size(82, 82);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            Cursor = Cursors.Hand;

            // Position at bottom-right of work area, strictly above taskbar
            var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1536, 816);
            Location = new Point(workArea.Right - 110, workArea.Bottom - 140);

            _petTooltip = new ToolTip
            {
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(56, 189, 248),
                InitialDelay = 400,
                AutoPopDelay = 6000,
                IsBalloon = true
            };
            _petTooltip.SetToolTip(this, "🌍 小地球桌宠 (点击展开中枢，拖动自由漫游)\n⭐ 单击可收藏地标或与 Gemini 对话");

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
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW (prevents cluttering Alt+Tab)
                return cp;
            }
        }

        private void LoadMascotImage()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string candidateA = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "app", "assets", "earth_pet.png"));
                string candidateB = Path.GetFullPath(Path.Combine(baseDir, "app", "assets", "earth_pet.png"));
                string candidateC = @"F:\antigravity\earth屏保\app\assets\earth_pet.png";

                string target = "";
                if (File.Exists(candidateA)) target = candidateA;
                else if (File.Exists(candidateB)) target = candidateB;
                else if (File.Exists(candidateC)) target = candidateC;

                if (!string.IsNullOrEmpty(target))
                {
                    using var stream = new MemoryStream(File.ReadAllBytes(target));
                    _mascotImage = Image.FromStream(stream);
                    Program.Log($"EarthPetForm: Loaded cartoon mascot image from {target}");
                }
            }
            catch (Exception ex)
            {
                Program.Log($"EarthPetForm.LoadMascotImage error: {ex.Message}");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, Width, Height);
            Region = new Region(path);

            PetSkinManager.OnSkinChanged += () =>
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(Invalidate));
                }
            };
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
                        string status = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng) ? "★ 已在收藏夹" : "⭐ 未收藏";
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
                    var workArea = Screen.FromControl(this).WorkingArea;
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
                _menuForm.PositionNearPet(Location, Size);
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var skin = PetSkinManager.CurrentSkin;

            // 1. Cosmic Gradient Background
            using (var brush = new LinearGradientBrush(new Rectangle(0, 0, Width, Height),
                       Color.FromArgb(15, 23, 42), Color.FromArgb(2, 6, 23), 45f))
            {
                g.FillEllipse(brush, 1, 1, Width - 3, Height - 3);
            }

            // 2. Render Based on Skin System
            if (skin == PetSkin.ChibiEarth && _mascotImage != null)
            {
                int pad = 5;
                g.DrawImage(_mascotImage, pad, pad, Width - pad * 2, Height - pad * 2);
            }
            else if (skin == PetSkin.HoloGlobe)
            {
                DrawHoloWireframeGlobe(g);
            }
            else if (skin == PetSkin.PixelAstro)
            {
                DrawPixelAstro(g);
            }
            else if (skin == PetSkin.MiniPill)
            {
                DrawMiniPill(g);
            }
            else // Default: MinimalOrb (初版：极简天体光球)
            {
                DrawMinimalOrb(g);
            }

            // 3. Glowing Atmospheric Halo Ring
            Color ringColor = _isHovered ? Color.FromArgb(56, 189, 248) : Color.FromArgb(14, 116, 144);
            using (var pen = new Pen(ringColor, _isHovered ? 2.5f : 1.8f))
            {
                g.DrawEllipse(pen, 2, 2, Width - 5, Height - 5);
            }

            // 4. Online Indicator Dot (Green pulse)
            using (var dotBrush = new SolidBrush(Color.FromArgb(34, 197, 94)))
            {
                g.FillEllipse(dotBrush, Width - 18, Height - 18, 8, 8);
            }
            using (var dotRingPen = new Pen(Color.FromArgb(15, 23, 42), 1.5f))
            {
                g.DrawEllipse(dotRingPen, Width - 18, Height - 18, 8, 8);
            }
        }

        private void DrawMinimalOrb(Graphics g)
        {
            int cx = Width / 2;
            int cy = Height / 2;
            int r = Width / 2 - 10;

            // Deep ocean base
            using (var coreBrush = new SolidBrush(Color.FromArgb(8, 47, 73)))
            {
                g.FillEllipse(coreBrush, cx - r, cy - r, r * 2, r * 2);
            }

            // Stylized continent vectors
            using (var landBrush = new SolidBrush(Color.FromArgb(16, 185, 129)))
            {
                g.FillEllipse(landBrush, cx - 12, cy - 14, 16, 14);
                g.FillEllipse(landBrush, cx - 4, cy + 2, 14, 14);
                g.FillEllipse(landBrush, cx + 6, cy - 8, 11, 11);
            }

            // Tilted equator line
            using (var linePen = new Pen(Color.FromArgb(90, 56, 189, 248), 1.2f))
            {
                g.DrawArc(linePen, cx - r, cy - 8, r * 2, 16, 0, 180);
            }

            // Dotted orbital ring
            using (var orbitPen = new Pen(Color.FromArgb(140, 241, 245, 249), 1.2f))
            {
                orbitPen.DashStyle = DashStyle.Dot;
                g.DrawEllipse(orbitPen, cx - r - 4, cy - 7, (r + 4) * 2, 14);
            }
        }

        private void DrawHoloWireframeGlobe(Graphics g)
        {
            int cx = Width / 2;
            int cy = Height / 2;
            int r = Width / 2 - 12;

            using var pen = new Pen(Color.FromArgb(56, 189, 248), 1.2f);
            g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);
            g.DrawEllipse(pen, cx - r / 2, cy - r, r, r * 2);
            g.DrawLine(pen, cx, cy - r, cx, cy + r);
            g.DrawLine(pen, cx - r, cy, cx + r, cy);
            g.DrawArc(pen, cx - r + 3, cy - r / 2, (r - 3) * 2, r, 0, 180);
            g.DrawArc(pen, cx - r + 3, cy - r / 2 - 4, (r - 3) * 2, r, 180, 180);
        }

        private void DrawPixelAstro(Graphics g)
        {
            using var font = new Font("Segoe UI Emoji", 24f, FontStyle.Regular);
            string icon = "👨‍🚀";
            var size = g.MeasureString(icon, font);
            g.DrawString(icon, font, Brushes.White, (Width - size.Width) / 2f + 1, (Height - size.Height) / 2f - 2);
        }

        private void DrawMiniPill(Graphics g)
        {
            int cx = Width / 2;
            int cy = Height / 2;
            using var b = new SolidBrush(Color.FromArgb(2, 132, 199));
            g.FillEllipse(b, cx - 14, cy - 14, 28, 28);
            using var p = new Pen(Color.White, 2f);
            g.DrawEllipse(p, cx - 14, cy - 14, 28, 28);
            using var font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            g.DrawString("ET", font, Brushes.White, cx - 8, cy - 8);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _menuForm?.Dispose();
                _hubForm?.Dispose();
                _petTooltip?.Dispose();
                _mascotImage?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
