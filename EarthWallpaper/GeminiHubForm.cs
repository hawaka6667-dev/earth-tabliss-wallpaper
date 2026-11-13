using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class GeminiHubForm : Form
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        private readonly WallpaperForm _wallpaperForm;
        private LocationInfo _currentLoc;

        private Button? _btnTabAsk;
        private Button? _btnTabBookmarks;
        private Button? _btnBookmarkAction;

        private Panel? _pnlAskContent;
        private Panel? _pnlBookmarksContent;

        // Ask Gemini controls
        private Label? _lblTargetLoc;
        private Panel? _pnlChatHistory;
        private TextBox? _txtQuestion;
        private Button? _btnSend;
        private FlowLayoutPanel? _pnlQuickChips;

        // Bookmarks controls
        private FlowLayoutPanel? _pnlBookmarksList;
        private Label? _lblBookmarksCount;
        private FlowLayoutPanel? _pnlCategoryTabs;
        private string _selectedCategory = "全部";

        private List<(string role, string text)> _chatHistory = new();

        // Drag window state
        private bool _isDraggingForm = false;
        private Point _dragStartPoint;

        public GeminiHubForm(WallpaperForm wallpaperForm, LocationInfo location, bool openToBookmarks = false)
        {
            _wallpaperForm = wallpaperForm;
            _currentLoc = location;
            InitializeComponent();
            if (openToBookmarks) SwitchTab(false);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            Size = new Size(840, 600);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            ForeColor = Color.FromArgb(241, 245, 249);

            // Position safely within work area
            var workArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1536, 816);
            int posX = Math.Max(workArea.Left + 20, (workArea.Width - Width) / 2);
            int posY = Math.Max(workArea.Top + 20, (workArea.Height - Height) / 2);
            Location = new Point(posX, posY);

            // ==========================================
            // 1. Top Navigation Bar
            // ==========================================
            var pnlTopNav = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            pnlTopNav.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingForm = true;
                    _dragStartPoint = new Point(e.X, e.Y);
                }
            };
            pnlTopNav.MouseMove += (s, e) =>
            {
                if (_isDraggingForm)
                {
                    Point p = PointToScreen(e.Location);
                    Location = new Point(p.X - _dragStartPoint.X, p.Y - _dragStartPoint.Y);
                }
            };
            pnlTopNav.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDraggingForm = false;
                }
            };

            var lblLogo = new Label
            {
                Text = "🌍 地球中枢",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(16, 14),
                AutoSize = true
            };

            _btnTabAsk = CreateNavTab("💬 Ask Gemini 研判", 138, 10, true, (s, e) => SwitchTab(true));
            _btnTabBookmarks = CreateNavTab("⭐ 地标收藏夹", 304, 10, false, (s, e) => SwitchTab(false));

            var btnKey = new Button
            {
                Text = "🔑 API 设置",
                Font = new Font("Segoe UI", 8.8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(104, 30),
                Location = new Point(Width - 160, 11),
                Cursor = Cursors.Hand
            };
            btnKey.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            btnKey.Click += (s, e) => PromptApiKeySetting();

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(38, 30),
                Location = new Point(Width - 48, 11),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => Close();

            pnlTopNav.Controls.Add(lblLogo);
            pnlTopNav.Controls.Add(_btnTabAsk);
            pnlTopNav.Controls.Add(_btnTabBookmarks);
            pnlTopNav.Controls.Add(btnKey);
            pnlTopNav.Controls.Add(btnClose);
            Controls.Add(pnlTopNav);

            // ==========================================
            // 2. Tab 1: Ask Gemini Panel
            // ==========================================
            _pnlAskContent = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(Width, Height - 52),
                Visible = true
            };
            InitAskTab(_pnlAskContent);
            Controls.Add(_pnlAskContent);

            // ==========================================
            // 3. Tab 2: Bookmarks Panel
            // ==========================================
            _pnlBookmarksContent = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(Width, Height - 52),
                Visible = false
            };
            InitBookmarksTab(_pnlBookmarksContent);
            Controls.Add(_pnlBookmarksContent);

            ResumeLayout(false);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        private Button CreateNavTab(string text, int x, int y, bool isActive, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(156, 32),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, isActive ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isActive ? Color.White : Color.FromArgb(148, 163, 184),
                BackColor = isActive ? Color.FromArgb(2, 132, 199) : Color.FromArgb(15, 23, 42),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        private void SwitchTab(bool toAsk)
        {
            if (_pnlAskContent != null) _pnlAskContent.Visible = toAsk;
            if (_pnlBookmarksContent != null) _pnlBookmarksContent.Visible = !toAsk;

            if (_btnTabAsk != null)
            {
                _btnTabAsk.BackColor = toAsk ? Color.FromArgb(2, 132, 199) : Color.FromArgb(15, 23, 42);
                _btnTabAsk.ForeColor = toAsk ? Color.White : Color.FromArgb(148, 163, 184);
                _btnTabAsk.Font = new Font("Segoe UI", 9.5f, toAsk ? FontStyle.Bold : FontStyle.Regular);
            }
            if (_btnTabBookmarks != null)
            {
                _btnTabBookmarks.BackColor = !toAsk ? Color.FromArgb(2, 132, 199) : Color.FromArgb(15, 23, 42);
                _btnTabBookmarks.ForeColor = !toAsk ? Color.White : Color.FromArgb(148, 163, 184);
                _btnTabBookmarks.Font = new Font("Segoe UI", 9.5f, !toAsk ? FontStyle.Bold : FontStyle.Regular);
            }

            if (!toAsk) RefreshBookmarksList();
        }

        // ==========================================
        // Ask Gemini Tab Layout
        // ==========================================
        private void InitAskTab(Panel container)
        {
            // Current Location Bar
            var pnlLocBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(20, 30, 48),
                Padding = new Padding(16, 6, 16, 6)
            };

            _lblTargetLoc = new Label
            {
                Text = $"📍 {_currentLoc.GetDisplayName()} ({_currentLoc.Lat}° N, {_currentLoc.Lng}° E)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(14, 12),
                AutoSize = true
            };

            bool isBookmarked = BookmarkManager.IsBookmarked(_currentLoc.Lat, _currentLoc.Lng);
            _btnBookmarkAction = new Button
            {
                Text = isBookmarked ? "★ 已收藏" : "⭐ 收藏此地标",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = isBookmarked ? Color.FromArgb(250, 204, 21) : Color.FromArgb(56, 189, 248),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(116, 28),
                Location = new Point(container.Width - 250, 8),
                Cursor = Cursors.Hand
            };
            _btnBookmarkAction.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            _btnBookmarkAction.Click += (s, e) => ToggleCurrentBookmark();

            var btnOpenMap = new Button
            {
                Text = "🌐 3D 查看",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 211, 153),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(104, 28),
                Location = new Point(container.Width - 120, 8),
                Cursor = Cursors.Hand
            };
            btnOpenMap.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            btnOpenMap.Click += (s, e) =>
            {
                string target = !string.IsNullOrEmpty(_currentLoc.MapUrl) ? _currentLoc.MapUrl : "https://earth.google.com/web/";
                try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch { }
            };

            pnlLocBar.Controls.Add(_lblTargetLoc);
            pnlLocBar.Controls.Add(_btnBookmarkAction);
            pnlLocBar.Controls.Add(btnOpenMap);
            container.Controls.Add(pnlLocBar);

            // Chat History Panel
            _pnlChatHistory = new Panel
            {
                Location = new Point(16, 48),
                Size = new Size(container.Width - 32, container.Height - 152),
                AutoScroll = true,
                BackColor = Color.FromArgb(10, 15, 30),
                Padding = new Padding(12)
            };
            container.Controls.Add(_pnlChatHistory);

            // Initial Welcome Bubble
            AddChatBubble(false, $"欢迎进入行星地学研判室！\n" +
                                 $"当前视界锁定在 **{_currentLoc.GetDisplayName()}**。\n" +
                                 $"我可以为您解析此地的**地质断层演化、水系冲刷沉积机理、遥感物性特征**或推荐观测视角。您可以点击下方快捷问题或直接输入提问！");

            // Quick Chips Panel (AutoScroll false to eliminate native ugly scrollbar)
            _pnlQuickChips = new FlowLayoutPanel
            {
                Location = new Point(16, container.Height - 96),
                Size = new Size(container.Width - 32, 32),
                WrapContents = false,
                AutoScroll = false
            };
            AddQuickChip("🌋 地质构造成因");
            AddQuickChip("🌊 水文沉积演化");
            AddQuickChip("🛰️ 遥感波段特征");
            AddQuickChip("🔍 周边地貌奇观");
            container.Controls.Add(_pnlQuickChips);

            // Input Bar
            var pnlInputBar = new Panel
            {
                Location = new Point(16, container.Height - 56),
                Size = new Size(container.Width - 32, 44),
                BackColor = Color.FromArgb(30, 41, 59)
            };
            pnlInputBar.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(71, 85, 105), 1f);
                e.Graphics.DrawRectangle(p, 0, 0, pnlInputBar.Width - 1, pnlInputBar.Height - 1);
            };

            _txtQuestion = new TextBox
            {
                Location = new Point(14, 11),
                Size = new Size(pnlInputBar.Width - 120, 24),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                PlaceholderText = "输入您想研判的地貌机理或地理问题..."
            };
            _txtQuestion.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SendUserQuestion();
                }
            };

            _btnSend = new Button
            {
                Text = "发送 ✨",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(2, 132, 199),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(90, 32),
                Location = new Point(pnlInputBar.Width - 98, 6),
                Cursor = Cursors.Hand
            };
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Click += (s, e) => SendUserQuestion();

            pnlInputBar.Controls.Add(_txtQuestion);
            pnlInputBar.Controls.Add(_btnSend);
            container.Controls.Add(pnlInputBar);
        }

        private void AddQuickChip(string text)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 28,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            btn.Click += (s, e) =>
            {
                if (_txtQuestion != null)
                {
                    _txtQuestion.Text = text;
                    SendUserQuestion();
                }
            };
            _pnlQuickChips?.Controls.Add(btn);
        }

        private async void SendUserQuestion()
        {
            if (_txtQuestion == null || string.IsNullOrWhiteSpace(_txtQuestion.Text)) return;
            string q = _txtQuestion.Text.Trim();
            _txtQuestion.Text = "";

            AddChatBubble(true, q);
            _btnSend!.Enabled = false;

            var loadingLabel = AddChatBubble(false, "Gemini 正在反演地质机理...");

            try
            {
                string answer = await GeminiService.AskQuestionAsync(_currentLoc, q, _chatHistory);
                _chatHistory.Add(("user", q));
                _chatHistory.Add(("model", answer));
                loadingLabel.Text = answer;
                AdjustBubbleSize(loadingLabel);
            }
            catch (Exception ex)
            {
                loadingLabel.Text = $"研判请求出错: {ex.Message}";
                AdjustBubbleSize(loadingLabel);
            }
            finally
            {
                _btnSend.Enabled = true;
            }
        }

        private Label AddChatBubble(bool isUser, string text)
        {
            if (_pnlChatHistory == null) return new Label();

            var pnlBubble = new Panel
            {
                Width = _pnlChatHistory.Width - 28,
                BackColor = isUser ? Color.FromArgb(3, 105, 161) : Color.FromArgb(30, 41, 59),
                Margin = new Padding(0, 6, 0, 8),
                Padding = new Padding(12)
            };

            var lblHeader = new Label
            {
                Text = isUser ? "🙋 您：" : "🛰️ Gemini 遥感研判：",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = isUser ? Color.FromArgb(224, 242, 254) : Color.FromArgb(56, 189, 248),
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblBody = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(241, 245, 249),
                Dock = DockStyle.Top,
                AutoSize = true,
                MaximumSize = new Size(pnlBubble.Width - 24, 0)
            };

            pnlBubble.Controls.Add(lblBody);
            pnlBubble.Controls.Add(lblHeader);

            int h = lblBody.PreferredHeight + 42;
            pnlBubble.Height = h;

            int y = 10;
            foreach (Control c in _pnlChatHistory.Controls)
            {
                y += c.Height + 10;
            }
            pnlBubble.Location = new Point(4, y);

            _pnlChatHistory.Controls.Add(pnlBubble);
            _pnlChatHistory.ScrollControlIntoView(pnlBubble);
            return lblBody;
        }

        private void AdjustBubbleSize(Label lbl)
        {
            var pnl = lbl.Parent as Panel;
            if (pnl != null)
            {
                int h = lbl.PreferredHeight + 42;
                pnl.Height = h;
                _pnlChatHistory?.ScrollControlIntoView(pnl);
            }
        }

        private void ToggleCurrentBookmark()
        {
            BookmarkManager.ToggleBookmark(_currentLoc, out bool nowBookmarked);
            if (_btnBookmarkAction != null)
            {
                _btnBookmarkAction.Text = nowBookmarked ? "★ 已收藏" : "⭐ 收藏此地标";
                _btnBookmarkAction.ForeColor = nowBookmarked ? Color.FromArgb(250, 204, 21) : Color.FromArgb(56, 189, 248);
            }
        }

        // ==========================================
        // Bookmarks Tab Layout
        // ==========================================
        private void InitBookmarksTab(Panel container)
        {
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Color.FromArgb(20, 30, 48),
                Padding = new Padding(16, 8, 16, 8)
            };

            _lblBookmarksCount = new Label
            {
                Text = "⭐ 已收藏 0 处地球壮丽地貌",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(14, 8),
                AutoSize = true
            };

            var btnAddCategory = new Button
            {
                Text = "+ 新建分类",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(56, 189, 248),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(106, 26),
                Location = new Point(container.Width - 128, 6),
                Cursor = Cursors.Hand
            };
            btnAddCategory.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btnAddCategory.Click += (s, e) => PromptCreateCategory();

            _pnlCategoryTabs = new FlowLayoutPanel
            {
                Location = new Point(14, 38),
                Size = new Size(container.Width - 28, 40),
                AutoScroll = true,
                WrapContents = false,
                Height = 40
            };

            pnlHeader.Controls.Add(_lblBookmarksCount);
            pnlHeader.Controls.Add(btnAddCategory);
            pnlHeader.Controls.Add(_pnlCategoryTabs);
            container.Controls.Add(pnlHeader);

            _pnlBookmarksList = new FlowLayoutPanel
            {
                Location = new Point(16, 92),
                Size = new Size(container.Width - 32, container.Height - 102),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            container.Controls.Add(_pnlBookmarksList);
        }

        private void PromptCreateCategory()
        {
            string cat = PromptInput("新建收藏分类", "请输入新分类/文件夹名称 (如: 城市、荒地、河流、好看):", "");
            if (!string.IsNullOrWhiteSpace(cat))
            {
                _selectedCategory = cat.Trim();
                RefreshBookmarksList();
            }
        }

        private static string PromptInput(string title, string prompt, string defaultVal)
        {
            using var form = new Form
            {
                Width = 420,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label { Left = 20, Top = 16, Width = 360, Height = 36, Text = prompt, ForeColor = Color.FromArgb(226, 232, 240) };
            var txt = new TextBox { Left = 20, Top = 58, Width = 360, Text = defaultVal, BackColor = Color.FromArgb(30, 41, 59), ForeColor = Color.White };
            var btnOk = new Button { Text = "确定", Left = 200, Top = 96, Width = 84, Height = 28, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(56, 189, 248), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = "取消", Left = 296, Top = 96, Width = 84, Height = 28, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(51, 65, 85), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : string.Empty;
        }

        public void RefreshBookmarksList()
        {
            if (_pnlBookmarksList == null) return;
            _pnlBookmarksList.Controls.Clear();

            // Render category filter chips
            if (_pnlCategoryTabs != null)
            {
                _pnlCategoryTabs.Controls.Clear();
                var categories = BookmarkManager.GetAllCategories();
                foreach (var cat in categories)
                {
                    bool isSelected = string.Equals(_selectedCategory, cat, StringComparison.OrdinalIgnoreCase);
                    var btnChip = new Button
                    {
                        Text = cat,
                        Font = new Font("Segoe UI", 8.5f, isSelected ? FontStyle.Bold : FontStyle.Regular),
                        ForeColor = isSelected ? Color.White : Color.FromArgb(148, 163, 184),
                        BackColor = isSelected ? Color.FromArgb(56, 189, 248) : Color.FromArgb(30, 41, 59),
                        FlatStyle = FlatStyle.Flat,
                        AutoSize = true,
                        Height = 26,
                        Margin = new Padding(0, 0, 6, 0),
                        Cursor = Cursors.Hand
                    };
                    btnChip.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(56, 189, 248) : Color.FromArgb(51, 65, 85);
                    btnChip.Click += (s, e) =>
                    {
                        _selectedCategory = cat;
                        RefreshBookmarksList();
                    };
                    _pnlCategoryTabs.Controls.Add(btnChip);
                }
            }

            var allItems = BookmarkManager.GetAll();
            var filteredItems = _selectedCategory == "全部"
                ? allItems
                : allItems.FindAll(b => string.Equals(b.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase));

            if (_lblBookmarksCount != null)
            {
                string catFilterDesc = _selectedCategory == "全部" ? "全部" : $"分类: {_selectedCategory}";
                _lblBookmarksCount.Text = $"⭐ 已收藏 {allItems.Count} 处地貌 ({catFilterDesc} 显示 {filteredItems.Count} 项)";
            }

            if (filteredItems.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = _selectedCategory == "全部"
                        ? "\n\n暂无收藏地标。\n\n在桌宠气泡或上方点击【⭐ 收藏此地标】，即可将当前美景永久保存！"
                        : $"\n\n当前分类【{_selectedCategory}】下暂无收藏地标。\n\n可在已有地标卡片上点击分类按钮，将地标移入该分类！",
                    Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Size = new Size(_pnlBookmarksList.Width - 20, 200)
                };
                _pnlBookmarksList.Controls.Add(lblEmpty);
                return;
            }

            foreach (var item in filteredItems)
            {
                var pnlCard = new Panel
                {
                    Width = _pnlBookmarksList.Width - 24,
                    Height = 68,
                    BackColor = Color.FromArgb(30, 41, 59),
                    Margin = new Padding(0, 0, 0, 8),
                    Padding = new Padding(14, 10, 14, 10)
                };

                var lblName = new Label
                {
                    Text = $"📍 {item.GetDisplayName()}",
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(10, 12),
                    Size = new Size(pnlCard.Width - 440, 22),
                    AutoEllipsis = true
                };

                string catText = string.IsNullOrWhiteSpace(item.Category) ? "默认" : item.Category;
                var lblCoords = new Label
                {
                    Text = $"分类: [{catText}] • 坐标: {item.Lat}, {item.Lng} • {item.SavedAt:MM-dd HH:mm}",
                    Font = new Font("Consolas", 8.5f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(12, 38),
                    Size = new Size(pnlCard.Width - 440, 20),
                    AutoEllipsis = true
                };

                // Move Category button
                var btnMoveCat = new Button
                {
                    Text = $"📁 {catText}",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = Color.FromArgb(248, 250, 252),
                    BackColor = Color.FromArgb(51, 65, 85),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(82, 32),
                    Location = new Point(pnlCard.Width - 424, 18),
                    Cursor = Cursors.Hand
                };
                btnMoveCat.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
                btnMoveCat.Click += (s, e) =>
                {
                    string newCat = PromptInput("修改分类", "请输入新的所属分类/文件夹:", item.Category);
                    if (!string.IsNullOrWhiteSpace(newCat))
                    {
                        BookmarkManager.UpdateCategory(item.Id, newCat);
                        RefreshBookmarksList();
                    }
                };

                // Action buttons on card
                var btnApply = new Button
                {
                    Text = "设为壁纸",
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(56, 189, 248),
                    BackColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(84, 32),
                    Location = new Point(pnlCard.Width - 328, 18),
                    Cursor = Cursors.Hand
                };
                btnApply.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                btnApply.Click += (s, e) =>
                {
                    _wallpaperForm.ApplyCustomWallpaper(new LocationInfo
                    {
                        Country = item.Country,
                        Region = item.Region,
                        Lat = item.Lat,
                        Lng = item.Lng,
                        MapUrl = item.MapUrl,
                        ImageUrl = item.ImageUrl
                    });
                    MessageBox.Show($"已将壁纸切换至: {item.GetDisplayName()}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };

                var btnOpenEarth = new Button
                {
                    Text = "🌍 3D",
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(52, 211, 153),
                    BackColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(64, 32),
                    Location = new Point(pnlCard.Width - 236, 18),
                    Cursor = Cursors.Hand
                };
                btnOpenEarth.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                btnOpenEarth.Click += (s, e) =>
                {
                    var url = $"https://earth.google.com/web/@{item.Lat},{item.Lng},1000a,35y,0h,0t,0r";
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
                };

                var btnAskThis = new Button
                {
                    Text = "向AI提问",
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(165, 180, 252),
                    BackColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(82, 32),
                    Location = new Point(pnlCard.Width - 164, 18),
                    Cursor = Cursors.Hand
                };
                btnAskThis.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                btnAskThis.Click += (s, e) =>
                {
                    _currentLoc = new LocationInfo
                    {
                        Country = item.Country,
                        Region = item.Region,
                        Lat = item.Lat,
                        Lng = item.Lng,
                        MapUrl = item.MapUrl,
                        ImageUrl = item.ImageUrl
                    };
                    if (_lblTargetLoc != null)
                        _lblTargetLoc.Text = $"📍 {_currentLoc.GetDisplayName()} ({_currentLoc.Lat}° N, {_currentLoc.Lng}° E)";
                    SwitchTab(true);
                };

                var btnDelete = new Button
                {
                    Text = "✕",
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(248, 113, 113),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(34, 32),
                    Location = new Point(pnlCard.Width - 46, 18),
                    Cursor = Cursors.Hand
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                btnDelete.Click += (s, e) =>
                {
                    BookmarkManager.Remove(item.Id);
                    RefreshBookmarksList();
                };

                pnlCard.Controls.Add(lblName);
                pnlCard.Controls.Add(lblCoords);
                pnlCard.Controls.Add(btnMoveCat);
                pnlCard.Controls.Add(btnApply);
                pnlCard.Controls.Add(btnOpenEarth);
                pnlCard.Controls.Add(btnAskThis);
                pnlCard.Controls.Add(btnDelete);

                _pnlBookmarksList.Controls.Add(pnlCard);
            }
        }

        private void PromptApiKeySetting()
        {
            using var dlg = new ApiKeySettingsForm();
            dlg.ShowDialog(this);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(56, 189, 248), 1.5f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }
    }
}
