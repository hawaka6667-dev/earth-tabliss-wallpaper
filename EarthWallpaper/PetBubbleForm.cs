using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class PetBubbleForm : Form
    {
        private readonly EarthPetForm _petForm;
        private readonly WallpaperForm _wallpaperForm;
        private readonly System.Windows.Forms.Timer _dismissTimer;
        
        private string _titleIcon = "🌍";
        private string _titleText = "漫游至新地标";
        private string _locationText = "";
        private bool _isBookmarked = false;
        private bool _tailOnBottom = true;
        private int _tailTipX = 0;
        private GraphicsPath? _bubblePath;

        // Interactive quick action buttons inside bubble
        private readonly Button _btnBookmark;
        private readonly Button _btnPrev;
        private readonly Button _btnNext;
        private readonly Button _btnExplore;

        public PetBubbleForm(EarthPetForm petForm, WallpaperForm wallpaperForm)
        {
            _petForm = petForm;
            _wallpaperForm = wallpaperForm;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            Cursor = Cursors.Default;
            Size = new Size(310, 108);

            _dismissTimer = new System.Windows.Forms.Timer { Interval = 7000 };
            _dismissTimer.Tick += (s, e) =>
            {
                _dismissTimer.Stop();
                Hide();
            };

            MouseEnter += (s, e) => _dismissTimer.Stop();
            MouseLeave += (s, e) =>
            {
                if (Visible && !ClientRectangle.Contains(PointToClient(Cursor.Position)))
                {
                    _dismissTimer.Start();
                }
            };

            // Quick action buttons
            _btnPrev = CreateBubbleButton(L10n.BubbleBtnPrev, Color.FromArgb(203, 213, 225), (s, e) =>
            {
                _dismissTimer.Stop();
                _dismissTimer.Start();
                _wallpaperForm.SendWebMessage("previous_wallpaper");
            });

            _btnNext = CreateBubbleButton(L10n.BubbleBtnNext, Color.FromArgb(203, 213, 225), (s, e) =>
            {
                _dismissTimer.Stop();
                _dismissTimer.Start();
                _wallpaperForm.SendWebMessage("next_wallpaper");
            });

            _btnBookmark = CreateBubbleButton(L10n.BubbleBtnBookmark, Color.FromArgb(250, 204, 21), (s, e) =>
            {
                _dismissTimer.Stop();
                _dismissTimer.Start();
                var loc = _wallpaperForm.CurrentLocation;
                if (loc != null)
                {
                    BookmarkManager.ToggleBookmark(loc, out bool nowBm);
                    UpdateBookmarkVisual(nowBm);
                }
            });

            _btnExplore = CreateBubbleButton(L10n.BubbleBtnExplore, Color.FromArgb(56, 189, 248), (s, e) =>
            {
                _dismissTimer.Stop();
                Hide();
                _petForm.OpenHub();
            });

            Controls.Add(_btnPrev);
            Controls.Add(_btnNext);
            Controls.Add(_btnBookmark);
            Controls.Add(_btnExplore);

            LayoutButtons();
            L10n.OnLanguageChanged += RefreshLocalizedUi;
        }

        private void RefreshLocalizedUi()
        {
            if (IsDisposed) return;
            _btnPrev.Text = L10n.BubbleBtnPrev;
            _btnNext.Text = L10n.BubbleBtnNext;
            _btnBookmark.Text = _isBookmarked ? L10n.BubbleBtnBookmarkSaved : L10n.BubbleBtnBookmark;
            _btnExplore.Text = L10n.BubbleBtnExplore;
            if (_titleText == "漫游至新地标" || _titleText == "Arrived at New Landmark")
            {
                _titleText = L10n.BubbleNewLandmark;
            }
            Invalidate();
        }

        private Button CreateBubbleButton(string text, Color foreColor, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                ForeColor = foreColor,
                BackColor = Color.FromArgb(30, 41, 59), // Slate 800
                FlatStyle = FlatStyle.Flat,
                Height = 26,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(51, 65, 85);
            btn.Click += onClick;
            btn.MouseEnter += (s, e) => _dismissTimer.Stop();
            btn.MouseLeave += (s, e) =>
            {
                if (Visible && !ClientRectangle.Contains(PointToClient(Cursor.Position)))
                {
                    _dismissTimer.Start();
                }
            };
            return btn;
        }

        private void LayoutButtons()
        {
            int btnY = _tailOnBottom ? 56 : 64;
            int totalW = Width - 24;
            int gap = 5;
            int btnW = (totalW - gap * 3) / 4;

            _btnPrev.SetBounds(12, btnY, btnW, 26);
            _btnNext.SetBounds(12 + (btnW + gap), btnY, btnW, 26);
            _btnBookmark.SetBounds(12 + (btnW + gap) * 2, btnY, btnW, 26);
            _btnExplore.SetBounds(12 + (btnW + gap) * 3, btnY, totalW - (btnW + gap) * 3, 26);
        }

        private void UpdateBookmarkVisual(bool isBm)
        {
            _isBookmarked = isBm;
            _btnBookmark.Text = isBm ? "★ 已藏" : "⭐ 收藏";
            _btnBookmark.ForeColor = isBm ? Color.FromArgb(250, 204, 21) : Color.FromArgb(226, 232, 240);
            Invalidate();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                return cp;
            }
        }

        public void ShowLandmarkBubble(string locationName, bool isBookmarked)
        {
            _titleIcon = "🌍";
            _titleText = "漫游至新地标";
            _locationText = locationName;
            UpdateBookmarkVisual(isBookmarked);
            PositionAndShow();
        }

        public void ShowNotificationBubble(string title, string message)
        {
            _titleIcon = "✨";
            _titleText = title;
            _locationText = message;
            _isBookmarked = false;
            PositionAndShow();
        }

        private void PositionAndShow()
        {
            if (_petForm.IsDisposed) return;

            Rectangle petBounds = _petForm.Bounds;
            int petCenterX = petBounds.Left + petBounds.Width / 2;

            var screen = Screen.FromControl(_petForm)?.WorkingArea ?? Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);

            int bubbleTop = petBounds.Top - Height - 6;
            if (bubbleTop < screen.Top + 10)
            {
                _tailOnBottom = false;
                bubbleTop = petBounds.Bottom + 6;
            }
            else
            {
                _tailOnBottom = true;
            }

            int bubbleLeft = petCenterX - Width + 42;
            bubbleLeft = Math.Clamp(bubbleLeft, screen.Left + 10, screen.Right - Width - 10);

            _tailTipX = petCenterX - bubbleLeft;
            _tailTipX = Math.Clamp(_tailTipX, 28, Width - 28);

            Location = new Point(bubbleLeft, bubbleTop);
            LayoutButtons();
            RebuildPath();

            _dismissTimer.Stop();
            _dismissTimer.Start();

            if (!Visible)
            {
                DesktopWorker.ShowWindow(Handle, 4 /* SW_SHOWNOACTIVATE */);
            }
            DesktopWorker.SetWindowPos(Handle, (IntPtr)(-1) /* HWND_TOPMOST */, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010 /* SWP_NOSIZE | SWP_NOMOVE | SWP_NOACTIVATE */);
            Invalidate();
        }

        private void RebuildPath()
        {
            _bubblePath?.Dispose();
            _bubblePath = new GraphicsPath();

            int r = 10;
            int d = r * 2;
            int tailW = 10;
            int tailH = 10;

            if (_tailOnBottom)
            {
                int bodyBottom = Height - tailH - 1;
                _bubblePath.AddArc(0, 0, d, d, 180, 90);
                _bubblePath.AddArc(Width - 1 - d, 0, d, d, 270, 90);
                _bubblePath.AddArc(Width - 1 - d, bodyBottom - d, d, d, 0, 90);

                _bubblePath.AddLine(Width - 1 - r, bodyBottom, _tailTipX + tailW, bodyBottom);
                _bubblePath.AddLine(_tailTipX + tailW, bodyBottom, _tailTipX, Height - 1);
                _bubblePath.AddLine(_tailTipX, Height - 1, _tailTipX - tailW, bodyBottom);
                _bubblePath.AddLine(_tailTipX - tailW, bodyBottom, r, bodyBottom);

                _bubblePath.AddArc(0, bodyBottom - d, d, d, 90, 90);
                _bubblePath.CloseFigure();
            }
            else
            {
                int bodyTop = tailH;
                _bubblePath.AddArc(0, bodyTop, d, d, 180, 90);
                _bubblePath.AddLine(r, bodyTop, _tailTipX - tailW, bodyTop);
                _bubblePath.AddLine(_tailTipX - tailW, bodyTop, _tailTipX, 0);
                _bubblePath.AddLine(_tailTipX, 0, _tailTipX + tailW, bodyTop);
                _bubblePath.AddLine(_tailTipX + tailW, bodyTop, Width - 1 - r, bodyTop);

                _bubblePath.AddArc(Width - 1 - d, bodyTop, d, d, 270, 90);
                _bubblePath.AddArc(Width - 1 - d, Height - 1 - d, d, d, 0, 90);
                _bubblePath.AddArc(0, Height - 1 - d, d, d, 90, 90);
                _bubblePath.CloseFigure();
            }

            var oldRgn = Region;
            Region = new Region(_bubblePath);
            oldRgn?.Dispose();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_bubblePath == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillPath(brush, _bubblePath);
            }

            using (var pen = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
            {
                g.DrawPath(pen, _bubblePath);
            }

            int contentTop = _tailOnBottom ? 10 : 18;
            int contentLeft = 14;
            int contentWidth = Width - 28;

            // Row 1: Title & Icon
            using (var fontEmoji = new Font("Segoe UI Emoji", 9f, FontStyle.Regular))
            using (var fontTitle = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var brushTitle = new SolidBrush(Color.FromArgb(56, 189, 248)))
            {
                g.DrawString(_titleIcon, fontEmoji, brushTitle, new PointF(contentLeft, contentTop));
                g.DrawString(_titleText, fontTitle, brushTitle, new PointF(contentLeft + 20, contentTop));
            }

            // Row 2: Location Name
            using (var fontLoc = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var brushLoc = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_locationText, fontLoc, brushLoc, new RectangleF(contentLeft, contentTop + 19, contentWidth, 20), sf);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                L10n.OnLanguageChanged -= RefreshLocalizedUi;
                _dismissTimer.Dispose();
                _bubblePath?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
