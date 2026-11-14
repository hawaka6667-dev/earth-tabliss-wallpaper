using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class ApiKeySettingsForm : Form
    {
        private TextBox _txtKey = null!;
        private Label _lblTitle = null!;
        private Label _lblInfo = null!;
        private Button _btnGetLink = null!;
        private Label _lblInputTitle = null!;
        private Label _lblStatus = null!;
        private Button _btnTest = null!;
        private Button _btnClear = null!;
        private Button _btnCancel = null!;
        private Button _btnSave = null!;
        private Button _btnToggleMask = null!;
        private Point _dragStart;

        public ApiKeySettingsForm()
        {
            InitializeComponent();
            L10n.OnLanguageChanged += RefreshLocalizedUi;
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            TopMost = false;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            ClientSize = new Size(680, 350);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            ForeColor = Color.FromArgb(241, 245, 249); // Slate 100

            // ==========================================
            // 1. Top Title Bar (Draggable)
            // ==========================================
            var pnlTitle = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(30, 41, 59) // Slate 800
            };

            _lblTitle = new Label
            {
                Text = L10n.ApiTitle,
                Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248), // Cyan 400
                Location = new Point(20, 12),
                AutoSize = true
            };

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 34),
                Location = new Point(ClientSize.Width - 48, 6),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            // Enable window dragging on title bar
            pnlTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) _dragStart = e.Location; };
            pnlTitle.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Location = new Point(Location.X + (e.X - _dragStart.X), Location.Y + (e.Y - _dragStart.Y));
                }
            };
            _lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) _dragStart = e.Location; };
            _lblTitle.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Location = new Point(Location.X + (e.X - _dragStart.X), Location.Y + (e.Y - _dragStart.Y));
                }
            };

            pnlTitle.Controls.Add(_lblTitle);
            pnlTitle.Controls.Add(btnClose);
            Controls.Add(pnlTitle);

            // ==========================================
            // 2. Info Line & AI Studio Portal Link
            // ==========================================
            _lblInfo = new Label
            {
                Text = L10n.ApiInfo,
                Font = new Font("Microsoft YaHei UI", 9f),
                ForeColor = Color.FromArgb(148, 163, 184), // Slate 400
                Location = new Point(24, 60),
                Size = new Size(630, 24)
            };
            Controls.Add(_lblInfo);

            _btnGetLink = new Button
            {
                Text = L10n.ApiGetLink,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Underline),
                ForeColor = Color.FromArgb(56, 189, 248),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(24, 86),
                Size = new Size(520, 26),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleLeft
            };
            _btnGetLink.FlatAppearance.BorderSize = 0;
            _btnGetLink.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("https://aistudio.google.com/app/apikey") { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Program.Log($"Failed to open AI Studio link: {ex.Message}");
                }
            };
            Controls.Add(_btnGetLink);

            // ==========================================
            // 3. API Key Input Wrapper
            // ==========================================
            _lblInputTitle = new Label
            {
                Text = L10n.ApiKeyLabel,
                Font = new Font("Microsoft YaHei UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(24, 122),
                AutoSize = true
            };
            Controls.Add(_lblInputTitle);

            var pnlInputBox = new Panel
            {
                Location = new Point(24, 148),
                Size = new Size(632, 40),
                BackColor = Color.FromArgb(30, 41, 59) // Slate 800
            };

            string currentKey = GeminiService.GetApiKey();
            _txtKey = new TextBox
            {
                Text = currentKey,
                Location = new Point(12, 10),
                Size = new Size(510, 22),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Font = new Font("Consolas", 10.5f),
                BorderStyle = BorderStyle.None,
                UseSystemPasswordChar = true
            };

            _btnToggleMask = new Button
            {
                Text = "👁️",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(532, 5),
                Size = new Size(42, 30),
                Cursor = Cursors.Hand
            };
            _btnToggleMask.FlatAppearance.BorderSize = 0;
            _btnToggleMask.Click += (s, e) =>
            {
                _txtKey.UseSystemPasswordChar = !_txtKey.UseSystemPasswordChar;
            };

            var btnPaste = new Button
            {
                Text = "📋",
                Font = new Font("Segoe UI Emoji", 9.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(578, 5),
                Size = new Size(46, 30),
                Cursor = Cursors.Hand
            };
            btnPaste.FlatAppearance.BorderSize = 0;
            btnPaste.Click += (s, e) =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        _txtKey.Text = Clipboard.GetText().Trim();
                    }
                }
                catch { }
            };

            pnlInputBox.Controls.Add(_txtKey);
            pnlInputBox.Controls.Add(_btnToggleMask);
            pnlInputBox.Controls.Add(btnPaste);
            Controls.Add(pnlInputBox);

            // ==========================================
            // 4. Status Indicator Label
            // ==========================================
            _lblStatus = new Label
            {
                Font = new Font("Microsoft YaHei UI", 9f),
                Location = new Point(24, 198),
                Size = new Size(632, 54)
            };
            UpdateStatusInitial(currentKey);
            Controls.Add(_lblStatus);

            // ==========================================
            // 5. Action Buttons Bar
            // ==========================================
            _btnTest = new Button
            {
                Text = L10n.ApiBtnTest,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(56, 189, 248),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(24, 278),
                Size = new Size(148, 38),
                Cursor = Cursors.Hand
            };
            _btnTest.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            _btnTest.Click += async (s, e) => await RunValidationAsync();
            Controls.Add(_btnTest);

            _btnClear = new Button
            {
                Text = L10n.ApiBtnClear,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(248, 113, 113),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(184, 278),
                Size = new Size(130, 38),
                Cursor = Cursors.Hand
            };
            _btnClear.FlatAppearance.BorderSize = 0;
            _btnClear.Click += (s, e) =>
            {
                _txtKey.Text = string.Empty;
                _lblStatus.Text = L10n.ApiStatusCleared;
                _lblStatus.ForeColor = Color.FromArgb(250, 204, 21);
            };
            Controls.Add(_btnClear);

            _btnCancel = new Button
            {
                Text = L10n.ApiBtnCancel,
                DialogResult = DialogResult.Cancel,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(410, 278),
                Size = new Size(95, 38),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            _btnCancel.Click += (s, e) => Close();
            Controls.Add(_btnCancel);

            _btnSave = new Button
            {
                Text = L10n.ApiBtnSave,
                DialogResult = DialogResult.OK,
                Font = new Font("Microsoft YaHei UI", 9.2f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(2, 132, 199), // Sky 600
                FlatStyle = FlatStyle.Flat,
                Location = new Point(518, 278),
                Size = new Size(138, 38),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (s, e) =>
            {
                GeminiService.SetApiKey(_txtKey.Text.Trim());
                Close();
            };
            Controls.Add(_btnSave);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };

            ResumeLayout(false);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SafeGdi.ApplyRoundedRegion(this, 16);
        }

        private void RefreshLocalizedUi()
        {
            if (IsDisposed) return;
            _lblTitle.Text = L10n.ApiTitle;
            _lblInfo.Text = L10n.ApiInfo;
            _btnGetLink.Text = L10n.ApiGetLink;
            _lblInputTitle.Text = L10n.ApiKeyLabel;
            _btnTest.Text = L10n.ApiBtnTest;
            _btnClear.Text = L10n.ApiBtnClear;
            _btnCancel.Text = L10n.ApiBtnCancel;
            _btnSave.Text = L10n.ApiBtnSave;
            UpdateStatusInitial(_txtKey.Text.Trim());
        }

        private void UpdateStatusInitial(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                _lblStatus.Text = L10n.ApiStatusConfigured;
                _lblStatus.ForeColor = Color.FromArgb(52, 211, 153); // Emerald 400
            }
            else
            {
                _lblStatus.Text = L10n.ApiStatusEmpty;
                _lblStatus.ForeColor = Color.FromArgb(148, 163, 184); // Slate 400
            }
        }

        private async System.Threading.Tasks.Task RunValidationAsync()
        {
            string key = _txtKey.Text.Trim();
            if (string.IsNullOrEmpty(key))
            {
                _lblStatus.Text = L10n.IsChinese 
                    ? "⚠️ 请先在输入框中填入待验证的 API Key。" 
                    : "⚠️ Please enter an API key to verify.";
                _lblStatus.ForeColor = Color.FromArgb(250, 204, 21);
                return;
            }

            _btnTest.Enabled = false;
            _btnSave.Enabled = false;
            _lblStatus.Text = L10n.ApiStatusTesting;
            _lblStatus.ForeColor = Color.FromArgb(56, 189, 248);

            var (success, message) = await GeminiService.VerifyApiKeyAsync(key);

            _lblStatus.Text = (success ? "🟢 " : "🔴 ") + message;
            _lblStatus.ForeColor = success ? Color.FromArgb(52, 211, 153) : Color.FromArgb(248, 113, 113);

            _btnTest.Enabled = true;
            _btnSave.Enabled = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(56, 189, 248), 1.5f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
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
