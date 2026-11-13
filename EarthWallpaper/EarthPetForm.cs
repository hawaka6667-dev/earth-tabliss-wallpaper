using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public enum PetSkin
    {
        ClassicOrb = 0, // 方案四：初版经典天体核心
        OrbProbe = 1    // 方案三：O.R.B. 轨道测绘机
    }

    public class EarthPetForm : Form
    {
        private readonly WallpaperForm _wallpaperForm;
        private PetMenuForm? _menuForm;
        private GeminiHubForm? _hubForm;
        private PetBubbleForm? _bubbleForm;

        private bool _isMouseDown = false;
        private bool _isDragging = false;
        private Point _mouseDownLocation;
        private Point _dragStartCursor;
        private bool _isHovered = false;

        public PetSkin CurrentSkin { get; private set; } = PetSkin.ClassicOrb;
        private static readonly string SkinConfigFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper", "pet_skin.txt");

        public EarthPetForm(WallpaperForm wallpaperForm)
        {
            _wallpaperForm = wallpaperForm;
            LoadSavedSkin();
            InitializeComponent();
        }

        private void LoadSavedSkin()
        {
            try
            {
                if (File.Exists(SkinConfigFile))
                {
                    string text = File.ReadAllText(SkinConfigFile).Trim();
                    if (Enum.TryParse<PetSkin>(text, out var skin))
                    {
                        CurrentSkin = skin;
                        Program.Log($"EarthPetForm: Loaded skin {CurrentSkin}");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"EarthPetForm.LoadSavedSkin error: {ex.Message}");
            }
        }

        public void ToggleSkin()
        {
            CurrentSkin = CurrentSkin == PetSkin.ClassicOrb ? PetSkin.OrbProbe : PetSkin.ClassicOrb;
            try
            {
                string dir = Path.GetDirectoryName(SkinConfigFile)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(SkinConfigFile, CurrentSkin.ToString());
            }
            catch { }

            Invalidate();
            string name = GetSkinDisplayName();
            _bubbleForm?.ShowNotificationBubble("✨ 外观切换完成", name);
        }

        public string GetSkinDisplayName()
        {
            return CurrentSkin == PetSkin.ClassicOrb ? "初版天体核心" : "O.R.B. 测绘机";
        }

        public void OpenHub() => OpenGeminiHub(false);

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

            MouseDown += OnPetMouseDown;
            MouseMove += OnPetMouseMove;
            MouseUp += OnPetMouseUp;
            MouseDoubleClick += OnPetMouseDoubleClick;
            MouseEnter += (s, e) =>
            {
                _isHovered = true;
                Invalidate();
            };
            MouseLeave += (s, e) =>
            {
                _isHovered = false;
                Invalidate();
            };

            ResumeLayout(false);

            _menuForm = new PetMenuForm(_wallpaperForm, this);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: Hidden from Alt+Tab and taskbar
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: Never steals keyboard focus from user applications
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

            // Safe floating toolwindow configuration: zero cross-process SetParent, zero message-loop hijacking
            DesktopWorker.ConfigurePetFloatingWindow(Handle);

            _bubbleForm = new PetBubbleForm(this, _wallpaperForm);
            _bubbleForm.CreateControl();
        }

        public bool IsAnyModalOrMenuOpen()
        {
            if (_menuForm != null && !_menuForm.IsDisposed && _menuForm.Visible) return true;
            if (_hubForm != null && !_hubForm.IsDisposed && _hubForm.Visible) return true;
            return false;
        }

        public void NotifyLocationChanged(LocationInfo? loc)
        {
            if (loc == null) return;
            _menuForm?.UpdateLocationDisplay(loc);

            // 1. If Hub or PetMenu is open, do not pop up bubble
            if (IsAnyModalOrMenuOpen()) return;

            // 2. Do not pop up over active user applications (Obsidian, VS Code, Chrome, etc.)
            IntPtr fg = DesktopWorker.GetForegroundWindow();
            if (fg != IntPtr.Zero && !DesktopWorker.IsDesktopWindow(fg))
            {
                Program.Log("DesktopWorker: User is in foreground app, suppressing automatic bubble popup.");
                return;
            }

            try
            {
                if (IsHandleCreated && !IsDisposed)
                {
                    BeginInvoke(new Action(() =>
                    {
                        bool isBookmarked = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng);
                        _bubbleForm?.ShowLandmarkBubble(loc.GetDisplayName(), isBookmarked);
                    }));
                }
                else
                {
                    // Fallback when handle is not created yet
                    bool isBookmarked = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng);
                    _bubbleForm?.ShowLandmarkBubble(loc.GetDisplayName(), isBookmarked);
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
            else if (e.Button == MouseButtons.Right)
            {
                // Right click: Toggle Pet Management Menu
                TogglePetMenu();
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
                    _bubbleForm?.Hide();
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
                    // Left click (single click): Toggle Interactive Pet Speech Bubble
                    ToggleBubble();
                }
            }
        }

        private void OnPetMouseDoubleClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Left double click: Directly open Earth Hub
                _bubbleForm?.Hide();
                _menuForm?.Hide();
                OpenGeminiHub();
            }
        }

        public void ToggleBubble()
        {
            if (_bubbleForm == null || _bubbleForm.IsDisposed)
            {
                _bubbleForm = new PetBubbleForm(this, _wallpaperForm);
            }

            if (_bubbleForm.Visible)
            {
                _bubbleForm.Hide();
            }
            else
            {
                _menuForm?.Hide();
                var loc = _wallpaperForm.CurrentLocation;
                if (loc != null)
                {
                    bool isBookmarked = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng);
                    _bubbleForm.ShowLandmarkBubble(loc.GetDisplayName(), isBookmarked);
                }
                else
                {
                    _bubbleForm.ShowNotificationBubble("🌍 漫游蔚蓝星球", "点击快速漫游行星地表");
                }
            }
        }

        public void TogglePetMenu()
        {
            _bubbleForm?.Hide();

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
                _menuForm.UpdateSkinButtonText();
                Point screenPos = PointToScreen(Point.Empty);
                _menuForm.PositionNearPet(screenPos, Size);
                _menuForm.Show();
                _menuForm.BringToFront();
            }
        }

        public void OpenGeminiHub(bool openToBookmarks = false, bool openToDev = false)
        {
            _bubbleForm?.Hide();
            _menuForm?.Hide();

            if (_hubForm != null && !_hubForm.IsDisposed)
            {
                if (openToDev) _hubForm.SwitchTab(2);
                else if (openToBookmarks) _hubForm.SwitchTab(1);
                else _hubForm.SwitchTab(0);
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

            _hubForm = new GeminiHubForm(_wallpaperForm, loc, openToBookmarks, openToDev);
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
        // Visual Rendering (Selected Skins 3 & 4)
        // ==========================================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            if (CurrentSkin == PetSkin.OrbProbe)
            {
                DrawOrbProbe(g);
            }
            else
            {
                DrawClassicOrb(g);
            }
        }

        // 方案四：初版经典天体核心 (Classic Orb)
        private void DrawClassicOrb(Graphics g)
        {
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

        // 方案三：O.R.B. 轨道测绘机 (Orbital Recon Bot)
        private void DrawOrbProbe(Graphics g)
        {
            int cx = Width / 2;
            int cy = Height / 2;

            // 1. Cyber Dark Base
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillEllipse(brush, 2, 2, Width - 5, Height - 5);
            }

            // 2. Levitation Wings (Left & Right)
            int wingOffset = _isHovered ? 3 : 0;
            using (var wingBrush = new SolidBrush(Color.FromArgb(30, 41, 59)))
            using (var wingPen = new Pen(Color.FromArgb(6, 182, 212), 1.5f))
            {
                // Left Wing
                Point[] leftWing = new Point[] {
                    new Point(10 - wingOffset, cy - 14),
                    new Point(20 - wingOffset, cy - 18),
                    new Point(22 - wingOffset, cy + 18),
                    new Point(8 - wingOffset, cy + 14)
                };
                g.FillPolygon(wingBrush, leftWing);
                g.DrawPolygon(wingPen, leftWing);

                // Right Wing
                Point[] rightWing = new Point[] {
                    new Point(Width - 10 + wingOffset, cy - 14),
                    new Point(Width - 20 + wingOffset, cy - 18),
                    new Point(Width - 22 + wingOffset, cy + 18),
                    new Point(Width - 8 + wingOffset, cy + 14)
                };
                g.FillPolygon(wingBrush, rightWing);
                g.DrawPolygon(wingPen, rightWing);
            }

            // 3. Central Diamond Cybernetic Armor (Rotated 45 degrees)
            int coreSize = 20;
            Point[] diamond = new Point[] {
                new Point(cx, cy - coreSize),
                new Point(cx + coreSize, cy),
                new Point(cx, cy + coreSize),
                new Point(cx - coreSize, cy)
            };
            using (var coreBrush = new SolidBrush(Color.FromArgb(2, 6, 23)))
            using (var corePen = new Pen(_isHovered ? Color.FromArgb(56, 189, 248) : Color.FromArgb(6, 182, 212), 2f))
            {
                g.FillPolygon(coreBrush, diamond);
                g.DrawPolygon(corePen, diamond);
            }

            // 4. Central Aperture Optical Lens (Iris)
            int irisR = 10;
            using (var irisBrush = new SolidBrush(Color.FromArgb(8, 47, 73)))
            using (var irisPen = new Pen(Color.FromArgb(34, 211, 238), 1.5f))
            {
                g.FillEllipse(irisBrush, cx - irisR, cy - irisR, irisR * 2, irisR * 2);
                g.DrawEllipse(irisPen, cx - irisR, cy - irisR, irisR * 2, irisR * 2);
            }

            // 5. Optical Pupil Focus (Glowing Center)
            int pupilR = _isHovered ? 5 : 3;
            using (var pupilBrush = new SolidBrush(_isHovered ? Color.FromArgb(125, 211, 252) : Color.FromArgb(56, 189, 248)))
            {
                g.FillEllipse(pupilBrush, cx - pupilR, cy - pupilR, pupilR * 2, pupilR * 2);
            }

            // 6. Online Status Beacon
            using (var dotBrush = new SolidBrush(Color.FromArgb(34, 197, 94)))
            using (var dotRingPen = new Pen(Color.FromArgb(15, 23, 42), 1.5f))
            {
                g.FillEllipse(dotBrush, Width - 18, Height - 18, 8, 8);
                g.DrawEllipse(dotRingPen, Width - 18, Height - 18, 8, 8);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _menuForm?.Dispose();
                _hubForm?.Dispose();
                _bubbleForm?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
