using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class PetBubbleForm : Form
    {
        private readonly EarthPetForm _petForm;
        private readonly System.Windows.Forms.Timer _dismissTimer;
        private string _titleIcon = "🌍";
        private string _titleText = "漫游至新地标";
        private string _locationText = "";
        private string _statusText = "";
        private bool _isBookmarked = false;
        private bool _tailOnBottom = true;
        private int _tailTipX = 0;
        private GraphicsPath? _bubblePath;

        public PetBubbleForm(EarthPetForm petForm)
        {
            _petForm = petForm;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            Cursor = Cursors.Hand;
            Size = new Size(270, 88);

            _dismissTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _dismissTimer.Tick += (s, e) =>
            {
                _dismissTimer.Stop();
                Hide();
            };

            Click += (s, e) =>
            {
                _dismissTimer.Stop();
                Hide();
                _petForm.OpenHub();
            };

            MouseEnter += (s, e) => _dismissTimer.Stop();
            MouseLeave += (s, e) =>
            {
                if (Visible) _dismissTimer.Start();
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE (Never steal focus)
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Pin bubble strictly to desktop icon layer (SHELLDLL_DefView) so it NEVER floats above user apps
            DesktopWorker.AttachPetToDesktop(Handle);
        }

        public void ShowLandmarkBubble(string locationName, bool isBookmarked)
        {
            _titleIcon = "🌍";
            _titleText = "漫游至新地标";
            _locationText = locationName;
            _isBookmarked = isBookmarked;
            _statusText = isBookmarked ? "已收藏" : "未收藏";

            PositionAndShow();
        }

        public void ShowNotificationBubble(string title, string message)
        {
            _titleIcon = "✨";
            _titleText = title;
            _locationText = message;
            _isBookmarked = false;
            _statusText = "已同步";

            PositionAndShow();
        }

        private void PositionAndShow()
        {
            if (_petForm.IsDisposed) return;

            Rectangle petBounds = _petForm.Bounds;
            int petCenterX = petBounds.Left + petBounds.Width / 2;

            var screen = Screen.FromControl(_petForm)?.WorkingArea ?? Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);

            // Determine if bubble should sit above or below pet
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

            // Horizontally align bubble near pet's right edge
            int bubbleLeft = petCenterX - Width + 42;
            bubbleLeft = Math.Clamp(bubbleLeft, screen.Left + 10, screen.Right - Width - 10);

            // Tail tip should point directly at pet's center horizontally
            _tailTipX = petCenterX - bubbleLeft;
            _tailTipX = Math.Clamp(_tailTipX, 28, Width - 28);

            Location = new Point(bubbleLeft, bubbleTop);
            RebuildPath();

            _dismissTimer.Stop();
            _dismissTimer.Start();

            if (!Visible)
            {
                DesktopWorker.ShowWindow(Handle, 4 /* SW_SHOWNOACTIVATE */);
            }
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
                // Top-left
                _bubblePath.AddArc(0, 0, d, d, 180, 90);
                // Top-right
                _bubblePath.AddArc(Width - 1 - d, 0, d, d, 270, 90);
                // Bottom-right
                _bubblePath.AddArc(Width - 1 - d, bodyBottom - d, d, d, 0, 90);

                // Bottom line to right side of tail
                _bubblePath.AddLine(Width - 1 - r, bodyBottom, _tailTipX + tailW, bodyBottom);
                // Tail point down to pet
                _bubblePath.AddLine(_tailTipX + tailW, bodyBottom, _tailTipX, Height - 1);
                _bubblePath.AddLine(_tailTipX, Height - 1, _tailTipX - tailW, bodyBottom);
                // Bottom line from left side of tail to bottom-left corner
                _bubblePath.AddLine(_tailTipX - tailW, bodyBottom, r, bodyBottom);

                // Bottom-left
                _bubblePath.AddArc(0, bodyBottom - d, d, d, 90, 90);
                _bubblePath.CloseFigure();
            }
            else
            {
                int bodyTop = tailH;
                // Top-left after corner to left side of tail
                _bubblePath.AddArc(0, bodyTop, d, d, 180, 90);
                _bubblePath.AddLine(r, bodyTop, _tailTipX - tailW, bodyTop);
                // Tail point up to pet
                _bubblePath.AddLine(_tailTipX - tailW, bodyTop, _tailTipX, 0);
                _bubblePath.AddLine(_tailTipX, 0, _tailTipX + tailW, bodyTop);
                _bubblePath.AddLine(_tailTipX + tailW, bodyTop, Width - 1 - r, bodyTop);

                // Top-right
                _bubblePath.AddArc(Width - 1 - d, bodyTop, d, d, 270, 90);
                // Bottom-right
                _bubblePath.AddArc(Width - 1 - d, Height - 1 - d, d, d, 0, 90);
                // Bottom-left
                _bubblePath.AddArc(0, Height - 1 - d, d, d, 90, 90);
                _bubblePath.CloseFigure();
            }

            Region = new Region(_bubblePath);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_bubblePath == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // 1. Slate 900 Background Fill
            using (var brush = new SolidBrush(Color.FromArgb(15, 23, 42)))
            {
                g.FillPath(brush, _bubblePath);
            }

            // 2. Cyan Glow Border
            using (var pen = new Pen(Color.FromArgb(56, 189, 248), 1.5f))
            {
                g.DrawPath(pen, _bubblePath);
            }

            // 3. Typography
            int contentTop = _tailOnBottom ? 10 : 18;
            int contentLeft = 14;
            int contentWidth = Width - 28;

            // Row 1: Icon + Title
            using (var fontEmoji = new Font("Segoe UI Emoji", 9f, FontStyle.Regular))
            using (var fontTitle = new Font("Segoe UI", 9f, FontStyle.Bold))
            using (var brushTitle = new SolidBrush(Color.FromArgb(56, 189, 248)))
            {
                g.DrawString(_titleIcon, fontEmoji, brushTitle, new PointF(contentLeft, contentTop));
                g.DrawString(_titleText, fontTitle, brushTitle, new PointF(contentLeft + 20, contentTop));
            }

            // Row 2: Location Display Name (Auto-Ellipsis)
            using (var fontLoc = new Font("Segoe UI", 9.5f, FontStyle.Bold))
            using (var brushLoc = new SolidBrush(Color.FromArgb(248, 250, 252)))
            {
                var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                g.DrawString(_locationText, fontLoc, brushLoc, new RectangleF(contentLeft, contentTop + 20, contentWidth, 20), sf);
            }

            // Row 3: Bookmark Status & Action Prompt
            using (var fontEmoji = new Font("Segoe UI Emoji", 8f, FontStyle.Regular))
            using (var fontStatus = new Font("Segoe UI", 8.5f, FontStyle.Bold))
            using (var brushStatus = new SolidBrush(_isBookmarked ? Color.FromArgb(250, 204, 21) : Color.FromArgb(148, 163, 184)))
            using (var fontHint = new Font("Segoe UI", 8f, FontStyle.Regular))
            using (var brushHint = new SolidBrush(Color.FromArgb(100, 116, 139)))
            {
                string star = _isBookmarked ? "★ " : "☆ ";
                g.DrawString(star + _statusText, fontStatus, brushStatus, new PointF(contentLeft, contentTop + 42));
                var sz = g.MeasureString(star + _statusText, fontStatus);
                g.DrawString(" (点击桌宠研判/收藏)", fontHint, brushHint, new PointF(contentLeft + sz.Width + 4, contentTop + 43));
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _dismissTimer.Dispose();
                _bubblePath?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
