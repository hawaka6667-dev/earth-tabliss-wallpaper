using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class PetMenuForm : Form
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private readonly WallpaperForm _wallpaperForm;
        private readonly EarthPetForm _petForm;
        private Button? _btnBookmark;
        private Button? _btnIcons;
        private Label? _lblLoc;
        private bool _iconsVisible = true;

        public PetMenuForm(WallpaperForm wallpaperForm, EarthPetForm petForm)
        {
            _wallpaperForm = wallpaperForm;
            _petForm = petForm;
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
            Size = new Size(290, 395);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            ForeColor = Color.FromArgb(241, 245, 249);

            // 1. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            var lblTitle = new Label
            {
                Text = "🌍 小地球桌宠 · 控制中枢",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(12, 10),
                AutoSize = true
            };

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(28, 26),
                Location = new Point(Width - 36, 6),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Hide();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);
            Controls.Add(pnlHeader);

            // 2. Location Badge
            _lblLoc = new Label
            {
                Text = "📍 当前漫游: 澳大利亚 · 托雷斯海峡",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(12, 46),
                Size = new Size(266, 22),
                AutoEllipsis = true
            };
            Controls.Add(_lblLoc);

            // 3. Action Buttons (Full width, NO truncation)
            int btnY = 72;
            const int btnHeight = 30;
            const int btnSpacing = 5;

            // Bookmark Button
            _btnBookmark = CreateMenuButton("⭐ 收藏当前地标", 12, btnY, Color.FromArgb(250, 204, 21), OnToggleBookmark);
            btnY += btnHeight + btnSpacing;

            // View Bookmarks
            var btnViewBookmarks = CreateMenuButton("📂 查看我的收藏夹", 12, btnY, Color.FromArgb(241, 245, 249), (s, e) =>
            {
                Hide();
                _petForm.OpenGeminiHub(true);
            });
            btnY += btnHeight + btnSpacing;

            // Ask Gemini Hub
            var btnGemini = CreateMenuButton("💬 Ask Gemini 智能研判", 12, btnY, Color.FromArgb(165, 180, 252), (s, e) =>
            {
                Hide();
                _petForm.OpenGeminiHub(false);
            });
            btnY += btnHeight + btnSpacing;

            // Skin Switcher
            Button? btnSkin = null;
            btnSkin = CreateMenuButton($"🎨 皮肤: {PetSkinManager.GetSkinDisplayName(PetSkinManager.CurrentSkin)}", 12, btnY, Color.FromArgb(244, 114, 182), (s, e) =>
            {
                PetSkinManager.CycleNextSkin();
                if (btnSkin != null) btnSkin.Text = $"🎨 皮肤: {PetSkinManager.GetSkinDisplayName(PetSkinManager.CurrentSkin)}";
            });
            btnY += btnHeight + btnSpacing;

            // Skin Showcase HTML
            var btnShowcase = CreateMenuButton("📱 挑选桌宠方案 (HTML 交互预览)", 12, btnY, Color.FromArgb(192, 132, 252), (s, e) =>
            {
                _wallpaperForm.OpenSkinShowcase();
            });
            btnY += btnHeight + btnSpacing;

            // Next Wallpaper
            var btnNext = CreateMenuButton("⏭ 换一张星球壁纸", 12, btnY, Color.FromArgb(241, 245, 249), (s, e) =>
            {
                _wallpaperForm.SendWebMessage("next_wallpaper");
                Hide();
            });
            btnY += btnHeight + btnSpacing;

            // Desktop Icons
            _btnIcons = CreateMenuButton("👁 显隐桌面图标 (当前: 显示)", 12, btnY, Color.FromArgb(56, 189, 248), OnToggleIcons);
            btnY += btnHeight + btnSpacing;

            // 3D Map
            var btnMap = CreateMenuButton("🌐 在 Google Earth 3D 查看", 12, btnY, Color.FromArgb(52, 211, 153), (s, e) =>
            {
                Hide();
                _petForm.OpenGoogleEarthMap();
            });
            btnY += btnHeight + btnSpacing;

            // Exit
            var btnExit = CreateMenuButton("🚪 退出壁纸与桌宠 (恢复原壁纸)", 12, btnY, Color.FromArgb(248, 113, 113), (s, e) =>
            {
                DesktopWorker.RestoreOriginalWallpaper();
                Application.Exit();
            });

            Controls.Add(_btnBookmark);
            Controls.Add(btnViewBookmarks);
            Controls.Add(btnGemini);
            Controls.Add(btnSkin);
            Controls.Add(btnShowcase);
            Controls.Add(btnNext);
            Controls.Add(_btnIcons);
            Controls.Add(btnMap);
            Controls.Add(btnExit);

            ResumeLayout(false);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Hide();
            };

            Deactivate += (s, e) => Hide();
        }

        public void UpdateLocationDisplay(LocationInfo? loc)
        {
            if (_lblLoc != null && loc != null)
            {
                _lblLoc.Text = $"📍 当前漫游: {loc.GetDisplayName()}";
            }

            if (_btnBookmark != null && loc != null)
            {
                bool isBm = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng);
                _btnBookmark.Text = isBm ? "★ 已在收藏夹 (点击取消)" : "⭐ 收藏当前地标";
                _btnBookmark.ForeColor = isBm ? Color.FromArgb(250, 204, 21) : Color.FromArgb(241, 245, 249);
            }
        }

        private Button CreateMenuButton(string text, int x, int y, Color textColor, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(266, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = textColor,
                BackColor = Color.FromArgb(30, 41, 59),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            btn.Click += onClick;
            return btn;
        }

        private void OnToggleBookmark(object? sender, EventArgs e)
        {
            var loc = _wallpaperForm.CurrentLocation;
            if (loc == null) return;

            BookmarkManager.ToggleBookmark(loc, out bool nowBookmarked);
            if (_btnBookmark != null)
            {
                _btnBookmark.Text = nowBookmarked ? "★ 已在收藏夹 (点击取消)" : "⭐ 收藏当前地标";
                _btnBookmark.ForeColor = nowBookmarked ? Color.FromArgb(250, 204, 21) : Color.FromArgb(241, 245, 249);
            }
        }

        private void OnToggleIcons(object? sender, EventArgs e)
        {
            bool nowVisible = DesktopWorker.ToggleDesktopIcons();
            _iconsVisible = nowVisible;
            if (_btnIcons != null)
            {
                _btnIcons.Text = _iconsVisible ? "👁 显隐桌面图标 (当前: 显示)" : "👁‍🗨 显隐桌面图标 (当前: 已隐藏)";
                _btnIcons.ForeColor = _iconsVisible ? Color.FromArgb(56, 189, 248) : Color.FromArgb(250, 204, 21);
            }
        }

        public void PositionNearPet(Point petLocation, Size petSize)
        {
            var workArea = Screen.FromPoint(petLocation).WorkingArea;

            int targetX = petLocation.X + (petSize.Width - Width) / 2;
            int targetY = petLocation.Y - Height - 10;

            if (targetY < workArea.Top)
            {
                targetY = petLocation.Y + petSize.Height + 10;
            }

            int safeX = Math.Clamp(targetX, workArea.Left + 10, workArea.Right - Width - 10);
            int safeY = Math.Clamp(targetY, workArea.Top + 10, workArea.Bottom - Height - 10);

            Location = new Point(safeX, safeY);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 16, 16));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(51, 65, 85), 1.5f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
