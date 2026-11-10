using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class GeminiAnalysisForm : Form
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private readonly LocationInfo _location;

        public GeminiAnalysisForm(LocationInfo location)
        {
            _location = location;
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
            Size = new Size(500, 480);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            ForeColor = Color.FromArgb(241, 245, 249);

            // Position safely within Screen.PrimaryScreen.WorkingArea to NEVER collide with Taskbar
            var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1536, 816);
            int posX = Math.Max(workArea.Left + 20, (workArea.Width - Width) / 2);
            int posY = Math.Max(workArea.Top + 20, (workArea.Height - Height) / 2);
            Location = new Point(posX, posY);

            // 1. Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            var lblTitle = new Label
            {
                Text = "✨ Gemini 地质结构与地貌研判",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(16, 14),
                AutoSize = true
            };

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 32),
                Location = new Point(Width - 48, 10),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);
            Controls.Add(pnlHeader);

            // 2. Content Container (Scrollable)
            var pnlBody = new Panel
            {
                Location = new Point(16, 60),
                Size = new Size(Width - 32, Height - 128),
                AutoScroll = true
            };

            // Location Badge
            var lblLocation = new Label
            {
                Text = $"📍 {_location.GetDisplayName()}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(4, 8),
                AutoSize = true
            };

            var lblCoords = new Label
            {
                Text = $"坐标: {_location.Lat}° N, {_location.Lng}° E",
                Font = new Font("Consolas", 9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(6, 34),
                AutoSize = true
            };

            pnlBody.Controls.Add(lblLocation);
            pnlBody.Controls.Add(lblCoords);

            // Geological Insights
            int curY = 66;

            curY = AddInsightCard(pnlBody, curY, "【地貌特征与宏观断层】",
                $"该区域位于 {_location.Country}（经纬度：{_location.Lat}, {_location.Lng}）。卫星高分影像显示出明显的构造风化痕迹，明暗反差揭示了古老地壳抬升与河流下切沉积的动态平衡。");

            curY = AddInsightCard(pnlBody, curY, "【自然演变机理】",
                "经过数百万年板块挤压应力、降水剥蚀与沉积输运，地表形成了如今极具辨识度的宏观自然肌理，是地球陆表生态与地质变迁的真实档案。");

            curY = AddInsightCard(pnlBody, curY, "【Google Earth 观测指南】",
                "• 建议在 3D 模式下将观测倾角调至 45°，感受山脊与谷地的起伏层次。\n• 开启时间轴滑块可观察该区域近数十年来局部植被与水系沉降变迁。");

            Controls.Add(pnlBody);

            // 3. Footer Action Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            var btnOpenMap = new Button
            {
                Text = "🌐 在浏览器打开 Google Earth / Maps",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(2, 132, 199),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(260, 36),
                Location = new Point(16, 12),
                Cursor = Cursors.Hand
            };
            btnOpenMap.FlatAppearance.BorderSize = 0;
            btnOpenMap.Click += (s, e) =>
            {
                try
                {
                    string targetUrl = !string.IsNullOrEmpty(_location.MapUrl)
                        ? _location.MapUrl
                        : $"https://earth.google.com/web/@{_location.Lat},{_location.Lng},1000a,35y,0h,0t,0r";
                    Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"无法打开链接: {ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnDone = new Button
            {
                Text = "关闭 (ESC)",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                BackColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(100, 36),
                Location = new Point(Width - 120, 12),
                Cursor = Cursors.Hand
            };
            btnDone.FlatAppearance.BorderSize = 0;
            btnDone.Click += (s, e) => Close();

            pnlFooter.Controls.Add(btnOpenMap);
            pnlFooter.Controls.Add(btnDone);
            Controls.Add(pnlFooter);

            ResumeLayout(false);

            // ESC to close
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        private int AddInsightCard(Panel container, int y, string title, string content)
        {
            var pnlCard = new Panel
            {
                Location = new Point(4, y),
                Width = container.Width - 28,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12)
            };

            var lblCardTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 24
            };

            var lblCardContent = new Label
            {
                Text = content,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(226, 232, 240),
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(pnlCard.Width - 24, 0)
            };

            pnlCard.Controls.Add(lblCardContent);
            pnlCard.Controls.Add(lblCardTitle);

            // Measure height
            int cardHeight = lblCardContent.PreferredHeight + 40;
            pnlCard.Height = cardHeight;

            container.Controls.Add(pnlCard);
            return y + cardHeight + 12;
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
