using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class PetMenuForm : Form
    {
        private readonly WallpaperForm _wallpaperForm;
        private readonly EarthPetForm _petForm;

        private Label? _lblTitle;
        private Label? _lblLoc;
        private Button? _btnBookmark;
        private Button? _btnBookmarks;
        private Button? _btnGemini;
        private Button? _btnSkin;
        private Button? _btnLang;
        private Button? _btnNext;
        private Button? _btnIcons;
        private Button? _btnMap;
        private Button? _btnDev;
        private Button? _btnExit;

        private bool _iconsVisible = true;

        public PetMenuForm(WallpaperForm wallpaperForm, EarthPetForm petForm)
        {
            _wallpaperForm = wallpaperForm;
            _petForm = petForm;
            InitializeComponent();
            L10n.OnLanguageChanged += RefreshLocalizedUi;
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            Size = new Size(296, 440);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            ForeColor = Color.FromArgb(241, 245, 249);

            // 1. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            _lblTitle = new Label
            {
                Text = L10n.PetMenuTitle,
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

            pnlHeader.Controls.Add(_lblTitle);
            pnlHeader.Controls.Add(btnClose);
            Controls.Add(pnlHeader);

            // 2. Location Badge
            _lblLoc = new Label
            {
                Text = $"{L10n.PetLocPrefix}...",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(12, 46),
                Size = new Size(272, 22),
                AutoEllipsis = true
            };
            Controls.Add(_lblLoc);

            // 3. Action Buttons (Full width, NO truncation)
            int btnY = 72;
            const int btnHeight = 30;
            const int btnSpacing = 5;

            // Bookmark Button
            _btnBookmark = CreateMenuButton(L10n.PetBtnBookmarkAdd, 12, btnY, Color.FromArgb(250, 204, 21), OnToggleBookmark);
            btnY += btnHeight + btnSpacing;

            // View Bookmarks
            _btnBookmarks = CreateMenuButton(L10n.PetBtnBookmarks, 12, btnY, Color.FromArgb(241, 245, 249), (s, e) =>
            {
                Hide();
                _petForm.OpenGeminiHub(true);
            });
            btnY += btnHeight + btnSpacing;

            // Ask Gemini Hub
            _btnGemini = CreateMenuButton(L10n.PetBtnAskGemini, 12, btnY, Color.FromArgb(165, 180, 252), (s, e) =>
            {
                Hide();
                _petForm.OpenGeminiHub(false);
            });
            btnY += btnHeight + btnSpacing;

            // Dual Skin Toggle (Classic Orb / O.R.B. Probe)
            _btnSkin = CreateMenuButton($"{L10n.MenuSwitchSkin}: {_petForm.GetSkinDisplayName()}", 12, btnY, Color.FromArgb(192, 132, 252), (s, e) =>
            {
                _petForm.ToggleSkin();
                UpdateSkinButtonText();
            });
            btnY += btnHeight + btnSpacing;

            // Language Switcher Toggle
            _btnLang = CreateMenuButton(GetLanguageButtonText(), 12, btnY, Color.FromArgb(52, 211, 153), (s, e) =>
            {
                CycleLanguage();
            });
            btnY += btnHeight + btnSpacing;

            // Next Wallpaper
            _btnNext = CreateMenuButton(L10n.PetBtnNext, 12, btnY, Color.FromArgb(241, 245, 249), (s, e) =>
            {
                _wallpaperForm.SendWebMessage("next_wallpaper");
                Hide();
            });
            btnY += btnHeight + btnSpacing;

            // Desktop Icons
            _btnIcons = CreateMenuButton(L10n.PetBtnIconsShow, 12, btnY, Color.FromArgb(56, 189, 248), OnToggleIcons);
            btnY += btnHeight + btnSpacing;

            // 3D Map
            _btnMap = CreateMenuButton(L10n.PetBtnMap, 12, btnY, Color.FromArgb(52, 211, 153), (s, e) =>
            {
                Hide();
                _petForm.OpenGoogleEarthMap();
            });
            btnY += btnHeight + btnSpacing;

            // Dev Queue Monitor
            _btnDev = CreateMenuButton(L10n.PetBtnDev, 12, btnY, Color.FromArgb(148, 163, 184), (s, e) =>
            {
                Hide();
                _petForm.OpenGeminiHub(false, true);
            });
            btnY += btnHeight + btnSpacing;

            // Exit
            _btnExit = CreateMenuButton(L10n.PetBtnExit, 12, btnY, Color.FromArgb(248, 113, 113), (s, e) =>
            {
                DesktopWorker.RestoreOriginalWallpaper();
                Application.Exit();
            });

            Controls.Add(_btnBookmark);
            Controls.Add(_btnBookmarks);
            Controls.Add(_btnGemini);
            Controls.Add(_btnSkin);
            Controls.Add(_btnLang);
            Controls.Add(_btnNext);
            Controls.Add(_btnIcons);
            Controls.Add(_btnMap);
            Controls.Add(_btnDev);
            Controls.Add(_btnExit);

            ResumeLayout(false);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Hide();
            };

            Deactivate += (s, e) => Hide();
        }

        private string GetLanguageButtonText()
        {
            var cur = L10n.CurrentLanguage;
            string display = cur switch
            {
                AppLanguage.En => "English",
                AppLanguage.Zh => "简体中文",
                _ => L10n.IsChinese ? "跟随系统" : "System"
            };
            return $"🌐 语言 / Lang: {display}";
        }

        private void CycleLanguage()
        {
            var cur = L10n.CurrentLanguage;
            var next = cur switch
            {
                AppLanguage.Auto => AppLanguage.En,
                AppLanguage.En => AppLanguage.Zh,
                _ => AppLanguage.Auto
            };
            L10n.SetLanguage(next);
        }

        public void RefreshLocalizedUi()
        {
            if (IsDisposed) return;
            if (_lblTitle != null) _lblTitle.Text = L10n.PetMenuTitle;
            if (_btnBookmarks != null) _btnBookmarks.Text = L10n.PetBtnBookmarks;
            if (_btnGemini != null) _btnGemini.Text = L10n.PetBtnAskGemini;
            if (_btnSkin != null) _btnSkin.Text = $"{L10n.MenuSwitchSkin}: {_petForm.GetSkinDisplayName()}";
            if (_btnLang != null) _btnLang.Text = GetLanguageButtonText();
            if (_btnNext != null) _btnNext.Text = L10n.PetBtnNext;
            if (_btnIcons != null) _btnIcons.Text = _iconsVisible ? L10n.PetBtnIconsShow : L10n.PetBtnIconsHidden;
            if (_btnMap != null) _btnMap.Text = L10n.PetBtnMap;
            if (_btnDev != null) _btnDev.Text = L10n.PetBtnDev;
            if (_btnExit != null) _btnExit.Text = L10n.PetBtnExit;

            var loc = _wallpaperForm.CurrentLocation;
            UpdateLocationDisplay(loc);
        }

        public void UpdateLocationDisplay(LocationInfo? loc)
        {
            if (_lblLoc != null && loc != null)
            {
                _lblLoc.Text = $"{L10n.PetLocPrefix}{loc.GetDisplayName()}";
            }

            if (_btnBookmark != null && loc != null)
            {
                bool isBm = BookmarkManager.IsBookmarked(loc.Lat, loc.Lng);
                _btnBookmark.Text = isBm ? L10n.PetBtnBookmarkSaved : L10n.PetBtnBookmarkAdd;
                _btnBookmark.ForeColor = isBm ? Color.FromArgb(250, 204, 21) : Color.FromArgb(241, 245, 249);
            }
        }

        public void UpdateSkinButtonText()
        {
            if (_btnSkin != null)
            {
                _btnSkin.Text = $"{L10n.MenuSwitchSkin}: {_petForm.GetSkinDisplayName()}";
            }
        }

        private Button CreateMenuButton(string text, int x, int y, Color textColor, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(272, 30),
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
                _btnBookmark.Text = nowBookmarked ? L10n.PetBtnBookmarkSaved : L10n.PetBtnBookmarkAdd;
                _btnBookmark.ForeColor = nowBookmarked ? Color.FromArgb(250, 204, 21) : Color.FromArgb(241, 245, 249);
            }
        }

        private void OnToggleIcons(object? sender, EventArgs e)
        {
            bool nowVisible = DesktopWorker.ToggleDesktopIcons();
            _iconsVisible = nowVisible;
            if (_btnIcons != null)
            {
                _btnIcons.Text = _iconsVisible ? L10n.PetBtnIconsShow : L10n.PetBtnIconsHidden;
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
            SafeGdi.ApplyRoundedRegion(this, 16);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(51, 65, 85), 1.5f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                L10n.OnLanguageChanged -= RefreshLocalizedUi;
            }
            base.Dispose(disposing);
        }
    }
}
