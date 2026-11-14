using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EarthWallpaper
{
    public class GeminiHubForm : Form
    {
        private readonly WallpaperForm _wallpaperForm;
        private LocationInfo _currentLoc;

        private Label? _lblLogo;
        private Button? _btnTabAsk;
        private Button? _btnTabBookmarks;
        private Button? _btnTabDev;
        private Button? _btnBookmarkAction;
        private Button? _btnKey;
        private Button? _btnMin;
        private Button? _btnClose;
        private Button? _btnOpenMap;
        private Button? _btnAddCat;

        private Label? _lblDevTitle;
        private Button? _btnDevReshuffle;
        private Button? _btnDevPrev;
        private Button? _btnDevNext;
        private Button? _btnDevRefresh;
        private Label? _lblDevQueueHeader;
        private Label? _lblDevHistoryHeader;

        private Panel? _pnlAskContent;
        private Panel? _pnlBookmarksContent;
        private Panel? _pnlDevContent;

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
        private TextBox? _txtBookmarkSearch;
        private string _selectedCategory = "全部";
        private string _searchKeyword = "";

        // Developer Mode controls
        private Label? _lblStatQueue;
        private Label? _lblStatHistory;
        private Label? _lblStatDeck;
        private Label? _lblStatWorkerW;
        private FlowLayoutPanel? _pnlDevQueueCards;
        private FlowLayoutPanel? _pnlDevHistoryCards;

        private List<(string role, string text)> _chatHistory = new();

        // Drag window state
        private bool _isDraggingForm = false;
        private Point _dragStartPoint;

        public GeminiHubForm(WallpaperForm wallpaperForm, LocationInfo location, bool openToBookmarks = false, bool openToDev = false)
        {
            _wallpaperForm = wallpaperForm;
            _currentLoc = location;
            InitializeComponent();

            _wallpaperForm.OnQueueStatusUpdated += OnQueueStatusUpdatedHandler;
            L10n.OnLanguageChanged += RefreshLocalizedUi;

            if (openToDev) SwitchTab(2);
            else if (openToBookmarks) SwitchTab(1);
            else SwitchTab(0);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = true;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = false;
            DoubleBuffered = true;
            Size = new Size(950, 640);
            BackColor = Color.FromArgb(15, 23, 42); // Slate 900
            ForeColor = Color.FromArgb(241, 245, 249);

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

            _lblLogo = new Label
            {
                Text = L10n.HubTitle,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(16, 14),
                AutoSize = true
            };

            _btnTabAsk = CreateNavTab(L10n.TabAskGemini, 150, 9, 145, true, (s, e) => SwitchTab(0));
            _btnTabBookmarks = CreateNavTab(L10n.TabBookmarks, 303, 9, 145, false, (s, e) => SwitchTab(1));
            _btnTabDev = CreateNavTab(L10n.TabDevTelemetry, 456, 9, 160, false, (s, e) => SwitchTab(2));

            _btnKey = new Button
            {
                Text = L10n.TabApiSettings,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(116, 32),
                Location = new Point(Width - 212, 10),
                Cursor = Cursors.Hand
            };
            _btnKey.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            _btnKey.Click += (s, e) => PromptApiKeySetting();

            _btnMin = new Button
            {
                Text = "—",
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(38, 32),
                Location = new Point(Width - 88, 10),
                Cursor = Cursors.Hand
            };
            _btnMin.FlatAppearance.BorderSize = 0;
            _btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;

            _btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(38, 32),
                Location = new Point(Width - 44, 10),
                Cursor = Cursors.Hand
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Click += (s, e) => Close();

            pnlTopNav.Controls.Add(_lblLogo);
            pnlTopNav.Controls.Add(_btnTabAsk);
            pnlTopNav.Controls.Add(_btnTabBookmarks);
            pnlTopNav.Controls.Add(_btnTabDev);
            pnlTopNav.Controls.Add(_btnKey);
            pnlTopNav.Controls.Add(_btnMin);
            pnlTopNav.Controls.Add(_btnClose);
            Controls.Add(pnlTopNav);

            // ==========================================
            // 2. Tab Panels
            // ==========================================
            _pnlAskContent = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(Width, Height - 52),
                Visible = true
            };
            InitAskTab(_pnlAskContent);
            Controls.Add(_pnlAskContent);

            _pnlBookmarksContent = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(Width, Height - 52),
                Visible = false
            };
            InitBookmarksTab(_pnlBookmarksContent);
            Controls.Add(_pnlBookmarksContent);

            _pnlDevContent = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(Width, Height - 52),
                Visible = false
            };
            InitDevTab(_pnlDevContent);
            Controls.Add(_pnlDevContent);

            ResumeLayout(false);

            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) Close();
            };
        }

        private Button CreateNavTab(string text, int x, int y, int width, bool isSelected, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isSelected ? Color.White : Color.FromArgb(148, 163, 184),
                BackColor = isSelected ? Color.FromArgb(15, 23, 42) : Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(width, 34),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(56, 189, 248) : Color.FromArgb(51, 65, 85);
            btn.FlatAppearance.BorderSize = isSelected ? 2 : 1;
            btn.Click += onClick;
            return btn;
        }

        public void SwitchTab(int tabIndex)
        {
            if (_pnlAskContent == null || _pnlBookmarksContent == null || _pnlDevContent == null) return;
            if (_btnTabAsk == null || _btnTabBookmarks == null || _btnTabDev == null) return;

            _pnlAskContent.Visible = (tabIndex == 0);
            _pnlBookmarksContent.Visible = (tabIndex == 1);
            _pnlDevContent.Visible = (tabIndex == 2);

            UpdateTabButtonVisual(_btnTabAsk, tabIndex == 0);
            UpdateTabButtonVisual(_btnTabBookmarks, tabIndex == 1);
            UpdateTabButtonVisual(_btnTabDev, tabIndex == 2);

            if (tabIndex == 1)
            {
                RefreshBookmarksList();
            }
            else if (tabIndex == 2)
            {
                if (_wallpaperForm.LatestQueueStatus != null)
                {
                    RenderQueueStatus(_wallpaperForm.LatestQueueStatus);
                }
                _wallpaperForm.RequestQueueStatus();
            }
        }

        public void SwitchToAsk() => SwitchTab(0);
        public void SwitchToBookmarks() => SwitchTab(1);
        public void SwitchToDev() => SwitchTab(2);

        private void UpdateTabButtonVisual(Button btn, bool isSelected)
        {
            btn.Font = new Font("Segoe UI", 9.5f, isSelected ? FontStyle.Bold : FontStyle.Regular);
            btn.ForeColor = isSelected ? Color.White : Color.FromArgb(148, 163, 184);
            btn.BackColor = isSelected ? Color.FromArgb(15, 23, 42) : Color.Transparent;
            btn.FlatAppearance.BorderColor = isSelected ? Color.FromArgb(56, 189, 248) : Color.FromArgb(51, 65, 85);
            btn.FlatAppearance.BorderSize = isSelected ? 2 : 1;
        }

        // ==========================================
        // 1. Ask Gemini Tab
        // ==========================================
        private void InitAskTab(Panel container)
        {
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
                Text = isBookmarked ? L10n.PetBtnBookmarkSaved : L10n.PetBtnBookmarkAdd,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = isBookmarked ? Color.FromArgb(250, 204, 21) : Color.FromArgb(56, 189, 248),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 28),
                Location = new Point(container.Width - 260, 8),
                Cursor = Cursors.Hand
            };
            _btnBookmarkAction.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            _btnBookmarkAction.Click += (s, e) => ToggleCurrentBookmark();

            _btnOpenMap = new Button
            {
                Text = L10n.BtnFly3D,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(52, 211, 153),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(110, 28),
                Location = new Point(container.Width - 120, 8),
                Cursor = Cursors.Hand
            };
            _btnOpenMap.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            _btnOpenMap.Click += (s, e) =>
            {
                string target = !string.IsNullOrEmpty(_currentLoc.MapUrl) ? _currentLoc.MapUrl : "https://earth.google.com/web/";
                try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); } catch { }
            };

            pnlLocBar.Controls.Add(_lblTargetLoc);
            pnlLocBar.Controls.Add(_btnBookmarkAction);
            pnlLocBar.Controls.Add(_btnOpenMap);
            container.Controls.Add(pnlLocBar);

            _pnlChatHistory = new Panel
            {
                Location = new Point(16, 48),
                Size = new Size(container.Width - 32, container.Height - 152),
                AutoScroll = true,
                BackColor = Color.FromArgb(10, 15, 30),
                Padding = new Padding(12)
            };
            SafeGdi.ApplyDarkMode(_pnlChatHistory);
            container.Controls.Add(_pnlChatHistory);

            string welcomeMsg = L10n.IsChinese 
                ? $"欢迎进入行星地学研判室！\n视界锁定在 **{_currentLoc.GetDisplayName()}**。\n我可以为您解析此地的**地质断层演化、水系冲刷沉积机理、遥感物性特征**或推荐观测视角。您可以点击下方快捷问题或直接输入提问！"
                : $"Welcome to the Planetary Reasoning Hub!\nTarget view locked on **{_currentLoc.GetDisplayName()}**.\nAsk Gemini anything about this terrain's geological formation, hydrology, or satellite features!";
            AddChatBubble(false, welcomeMsg);

            _pnlQuickChips = new FlowLayoutPanel
            {
                Location = new Point(16, container.Height - 96),
                Size = new Size(container.Width - 32, 32),
                WrapContents = false,
                AutoScroll = false
            };
            AddQuickChip(L10n.Chip1);
            AddQuickChip(L10n.Chip2);
            AddQuickChip(L10n.Chip3);
            AddQuickChip(L10n.Chip4);
            container.Controls.Add(_pnlQuickChips);

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
                PlaceholderText = L10n.AskPlaceholder
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
                Text = L10n.BtnSend,
                Location = new Point(pnlInputBar.Width - 100, 7),
                Size = new Size(88, 30),
                BackColor = Color.FromArgb(56, 189, 248),
                ForeColor = Color.FromArgb(15, 23, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
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
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Height = 28,
                Margin = new Padding(0, 0, 8, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.Click += (s, e) =>
            {
                if (_txtQuestion != null)
                {
                    _txtQuestion.Text = text.Substring(2).Trim();
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
            _chatHistory.Add(("user", q));

            if (!GeminiService.HasApiKey())
            {
                AddChatBubble(false, "⚠️ 您尚未配置 Gemini API 密钥。\n请点击右上角【🔑 API 设置】配置免费的 Google AI API 密钥即可开启研判！");
                return;
            }

            var pnlLoading = AddChatBubble(false, "🛰️ 正在调取轨道遥感物性特征与断层数据库，Gemini 实时推演中...");

            try
            {
                string answer = await GeminiService.AskQuestionAsync(_currentLoc, q, _chatHistory);
                pnlLoading.Parent?.Controls.Remove(pnlLoading);
                AddChatBubble(false, answer);
                _chatHistory.Add(("assistant", answer));
            }
            catch (Exception ex)
            {
                pnlLoading.Parent?.Controls.Remove(pnlLoading);
                AddChatBubble(false, $"❌ 研判请求受阻: {ex.Message}\n请检查网络连接或 API 配额。");
            }
        }

        private Panel AddChatBubble(bool isUser, string text)
        {
            if (_pnlChatHistory == null) return new Panel();

            int maxWidth = (int)(_pnlChatHistory.Width * 0.78);
            var pnl = new Panel
            {
                AutoSize = true,
                MaximumSize = new Size(maxWidth, 0),
                Padding = new Padding(14, 10, 14, 10),
                BackColor = isUser ? Color.FromArgb(37, 99, 235) : Color.FromArgb(30, 41, 59),
                Margin = new Padding(isUser ? _pnlChatHistory.Width - maxWidth - 40 : 4, 6, 4, 6)
            };

            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = Color.White,
                AutoSize = true,
                MaximumSize = new Size(maxWidth - 28, 0)
            };
            pnl.Controls.Add(lbl);

            int y = 8;
            foreach (Control c in _pnlChatHistory.Controls)
            {
                y = Math.Max(y, c.Bottom + 8);
            }
            pnl.Location = new Point(isUser ? _pnlChatHistory.Width - pnl.PreferredSize.Width - 28 : 12, y);

            _pnlChatHistory.Controls.Add(pnl);
            _pnlChatHistory.ScrollControlIntoView(pnl);
            return pnl;
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
        // 2. Refactored Bookmarks Tab (With Thumbnails)
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

            // Search Box
            var pnlSearch = new Panel
            {
                Location = new Point(container.Width - 380, 6),
                Size = new Size(230, 26),
                BackColor = Color.FromArgb(30, 41, 59)
            };
            pnlSearch.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(71, 85, 105), 1f);
                e.Graphics.DrawRectangle(pen, 0, 0, pnlSearch.Width - 1, pnlSearch.Height - 1);
            };

            _txtBookmarkSearch = new TextBox
            {
                Location = new Point(8, 4),
                Size = new Size(214, 20),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = L10n.SearchPlaceholder
            };
            _txtBookmarkSearch.TextChanged += (s, e) =>
            {
                _searchKeyword = _txtBookmarkSearch.Text.Trim();
                RefreshBookmarksList();
            };
            pnlSearch.Controls.Add(_txtBookmarkSearch);

            _btnAddCat = new Button
            {
                Text = L10n.BtnAddCategory,
                Font = new Font("Segoe UI", 8.8f),
                ForeColor = Color.FromArgb(56, 189, 248),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(116, 26),
                Location = new Point(container.Width - 136, 6),
                Cursor = Cursors.Hand
            };
            _btnAddCat.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            _btnAddCat.Click += (s, e) => PromptCreateCategory();

            _pnlCategoryTabs = new FlowLayoutPanel
            {
                Location = new Point(14, 38),
                Size = new Size(container.Width - 28, 40),
                AutoScroll = true,
                WrapContents = false,
                Height = 40
            };

            pnlHeader.Controls.Add(_lblBookmarksCount);
            pnlHeader.Controls.Add(pnlSearch);
            pnlHeader.Controls.Add(_btnAddCat);
            pnlHeader.Controls.Add(_pnlCategoryTabs);
            container.Controls.Add(pnlHeader);

            _pnlBookmarksList = new FlowLayoutPanel
            {
                Location = new Point(16, 92),
                Size = new Size(container.Width - 32, container.Height - 100),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            SafeGdi.ApplyDarkMode(_pnlBookmarksList);
            container.Controls.Add(_pnlBookmarksList);
        }

        private void PromptCreateCategory()
        {
            string cat = PromptInput(L10n.PromptNewCategoryTitle, L10n.PromptNewCategoryMessage, "");
            if (!string.IsNullOrWhiteSpace(cat))
            {
                string trimmed = cat.Trim();
                BookmarkManager.AddCustomCategory(trimmed);
                _selectedCategory = trimmed;
                RefreshBookmarksList();
            }
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
            bool isAll = string.Equals(_selectedCategory, "全部", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(_selectedCategory, "All", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(_selectedCategory, L10n.CatAll, StringComparison.OrdinalIgnoreCase);

            var filteredItems = allItems.FindAll(b =>
            {
                bool matchesCat = isAll || string.Equals(b.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase);
                if (!matchesCat) return false;

                if (!string.IsNullOrEmpty(_searchKeyword))
                {
                    string k = _searchKeyword.ToLower();
                    return b.Country.ToLower().Contains(k) ||
                           b.Region.ToLower().Contains(k) ||
                           b.Category.ToLower().Contains(k) ||
                           b.Lat.Contains(k) || b.Lng.Contains(k);
                }
                return true;
            });

            if (_lblBookmarksCount != null)
            {
                string catDesc = isAll ? L10n.CatAll : $"{_selectedCategory}";
                _lblBookmarksCount.Text = $"⭐ {string.Format(L10n.BookmarksCountFormat, allItems.Count)} ({catDesc}: {filteredItems.Count})";
            }

            if (filteredItems.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Text = isAll
                        ? (L10n.IsChinese 
                            ? "\n\n暂无收藏地标。\n\n在桌宠气泡或上方点击【⭐ 收藏此地标】，即可将当前美景永久保存！"
                            : "\n\nNo bookmarks saved yet.\nClick [⭐ Save] on the pet bubble or above to save this view!")
                        : (L10n.IsChinese 
                            ? $"\n\n当前分类【{_selectedCategory}】下暂无地标。\n可在卡片上点击分类按钮移入！"
                            : $"\n\nNo bookmarks under category [{_selectedCategory}].\nAssign categories using the card pill button!"),
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
                    Height = 88,
                    BackColor = Color.FromArgb(30, 41, 59),
                    Margin = new Padding(0, 0, 0, 8),
                    Padding = new Padding(10, 8, 10, 8)
                };

                // Card Border Highlight
                pnlCard.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(51, 65, 85), 1f);
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlCard.Width - 1, pnlCard.Height - 1);
                };

                // 1. Satellite Thumbnail Preview (Rounded picture box with fallback placeholder)
                var picThumb = new PictureBox
                {
                    Location = new Point(10, 7),
                    Size = new Size(110, 74),
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    BackColor = Color.FromArgb(15, 23, 42),
                    Cursor = Cursors.Hand
                };
                picThumb.Image = ThumbnailCacheManager.GetPlaceholder();

                ThumbnailCacheManager.LoadThumbnailAsync(item.ImageUrl, 110, 74, thumb =>
                {
                    if (picThumb.IsHandleCreated && !picThumb.IsDisposed)
                    {
                        picThumb.BeginInvoke(new Action(() => picThumb.Image = thumb));
                    }
                });

                picThumb.Click += (s, e) =>
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
                };

                // 2. Location Info & Metadata
                var lblName = new Label
                {
                    Text = $"📍 {item.GetDisplayName()}",
                    Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                    ForeColor = Color.White,
                    Location = new Point(132, 10),
                    Size = new Size(pnlCard.Width - 490, 22),
                    AutoEllipsis = true
                };

                string catText = string.IsNullOrWhiteSpace(item.Category) ? L10n.CatDefault : (item.Category == "默认" ? L10n.CatDefault : item.Category);
                string coordLabel = L10n.IsChinese ? "坐标:" : "Coords:";
                var lblCoords = new Label
                {
                    Text = $"{coordLabel} {item.Lat}, {item.Lng}   •   {item.SavedAt:yyyy-MM-dd HH:mm}",
                    Font = new Font("Consolas", 8.8f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(148, 163, 184),
                    Location = new Point(134, 36),
                    Size = new Size(pnlCard.Width - 490, 18),
                    AutoEllipsis = true
                };

                // Tag Pill button
                var btnMoveCat = new Button
                {
                    Text = $"📁 {catText}",
                    Font = new Font("Segoe UI", 8.2f),
                    ForeColor = Color.FromArgb(248, 250, 252),
                    BackColor = Color.FromArgb(51, 65, 85),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(96, 22),
                    Location = new Point(134, 58),
                    Cursor = Cursors.Hand
                };
                btnMoveCat.FlatAppearance.BorderSize = 0;
                btnMoveCat.Click += (s, e) =>
                {
                    string newCat = PromptInput(L10n.PromptEditCategoryTitle, L10n.PromptEditCategoryMessage, item.Category);
                    if (!string.IsNullOrWhiteSpace(newCat))
                    {
                        BookmarkManager.UpdateCategory(item.Id, newCat);
                        RefreshBookmarksList();
                    }
                };

                // 3. Action Buttons on Right
                int btnTop = 28;

                var btnApply = new Button
                {
                    Text = L10n.BtnSetWallpaper,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(56, 189, 248),
                    BackColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(118, 32),
                    Location = new Point(pnlCard.Width - 336, btnTop),
                    Cursor = Cursors.Hand
                };
                btnApply.FlatAppearance.BorderColor = Color.FromArgb(56, 189, 248);
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
                };

                var btnOpenEarth = new Button
                {
                    Text = L10n.BtnFly3D,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(52, 211, 153),
                    BackColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(72, 32),
                    Location = new Point(pnlCard.Width - 212, btnTop),
                    Cursor = Cursors.Hand
                };
                btnOpenEarth.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                btnOpenEarth.Click += (s, e) =>
                {
                    var url = $"https://earth.google.com/web/@{item.Lat},{item.Lng},1000a,35y,0h,0t,0r";
                    try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
                };

                var btnAskThis = new Button
                {
                    Text = L10n.BtnAnalyze,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(165, 180, 252),
                    BackColor = Color.FromArgb(15, 23, 42),
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(84, 32),
                    Location = new Point(pnlCard.Width - 134, btnTop),
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
                    SwitchTab(0);
                };

                var btnDelete = new Button
                {
                    Text = L10n.BtnDelete,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(248, 113, 113),
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Size = new Size(32, 32),
                    Location = new Point(pnlCard.Width - 44, btnTop),
                    Cursor = Cursors.Hand
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                btnDelete.Click += (s, e) =>
                {
                    BookmarkManager.Remove(item.Id);
                    RefreshBookmarksList();
                };

                pnlCard.Controls.Add(picThumb);
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

        // ==========================================
        // 3. Developer Mode / Queue Inspector Tab
        // ==========================================
        private void InitDevTab(Panel container)
        {
            // Top Toolbar
            var pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.FromArgb(20, 30, 48),
                Padding = new Padding(16, 6, 16, 6)
            };

            _lblDevTitle = new Label
            {
                Text = L10n.DevTitle,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(14, 12),
                AutoSize = true
            };

            var pnlActions = new FlowLayoutPanel
            {
                Location = new Point(container.Width - 500, 6),
                Size = new Size(484, 32),
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            _btnDevReshuffle = CreateDevButton(L10n.DevReshuffle, (s, e) => _wallpaperForm.ReshuffleDeck());
            _btnDevPrev = CreateDevButton(L10n.BubbleBtnPrev, (s, e) => _wallpaperForm.SendWebMessage("previous_wallpaper"));
            _btnDevNext = CreateDevButton(L10n.BubbleBtnNext, (s, e) => _wallpaperForm.SendWebMessage("next_wallpaper"));
            _btnDevRefresh = CreateDevButton("🔄 " + (L10n.IsChinese ? "刷新遥测" : "Refresh"), (s, e) => _wallpaperForm.RequestQueueStatus());

            pnlActions.Controls.Add(_btnDevReshuffle);
            pnlActions.Controls.Add(_btnDevPrev);
            pnlActions.Controls.Add(_btnDevNext);
            pnlActions.Controls.Add(_btnDevRefresh);

            pnlToolbar.Controls.Add(_lblDevTitle);
            pnlToolbar.Controls.Add(pnlActions);
            container.Controls.Add(pnlToolbar);

            // Metrics Bar
            var pnlMetrics = new Panel
            {
                Location = new Point(16, 52),
                Size = new Size(container.Width - 32, 60),
                BackColor = Color.FromArgb(10, 15, 30)
            };

            int statW = (pnlMetrics.Width - 24) / 4;
            _lblStatQueue = CreateStatCard(pnlMetrics, L10n.DevQueueBuffer, "0 / 5", 4, statW);
            _lblStatHistory = CreateStatCard(pnlMetrics, L10n.DevHistory, "0 / 25", 8 + statW, statW);
            _lblStatDeck = CreateStatCard(pnlMetrics, L10n.DevShuffle, "0 / 1548", 12 + statW * 2, statW);
            _lblStatWorkerW = CreateStatCard(pnlMetrics, "Topology", "WorkerW (Hooked)", 16 + statW * 3, statW);
            container.Controls.Add(pnlMetrics);

            // Live Queue Cards Panel
            _lblDevQueueHeader = new Label
            {
                Text = $"📦 {L10n.DevQueueBuffer} (0ms):",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(16, 120),
                AutoSize = true
            };
            container.Controls.Add(_lblDevQueueHeader);

            _pnlDevQueueCards = new FlowLayoutPanel
            {
                Location = new Point(16, 144),
                Size = new Size(container.Width - 32, 210),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            SafeGdi.ApplyDarkMode(_pnlDevQueueCards);
            container.Controls.Add(_pnlDevQueueCards);

            // History Stack Panel
            _lblDevHistoryHeader = new Label
            {
                Text = $"⏮ {L10n.DevHistory}:",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(226, 232, 240),
                Location = new Point(16, 362),
                AutoSize = true
            };
            container.Controls.Add(_lblDevHistoryHeader);

            _pnlDevHistoryCards = new FlowLayoutPanel
            {
                Location = new Point(16, 386),
                Size = new Size(container.Width - 32, container.Height - 396),
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.FromArgb(15, 23, 42)
            };
            SafeGdi.ApplyDarkMode(_pnlDevHistoryCards);
            container.Controls.Add(_pnlDevHistoryCards);

            if (_wallpaperForm.LatestQueueStatus != null)
            {
                RenderQueueStatus(_wallpaperForm.LatestQueueStatus);
            }
        }

        private Button CreateDevButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(226, 232, 240),
                BackColor = Color.FromArgb(30, 41, 59),
                FlatStyle = FlatStyle.Flat,
                AutoSize = true,
                Height = 30,
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(6, 2, 6, 2),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
            btn.Click += onClick;
            return btn;
        }

        private Label CreateStatCard(Panel parent, string title, string val, int x, int width)
        {
            var pnl = new Panel
            {
                Location = new Point(x, 6),
                Size = new Size(width, 48),
                BackColor = Color.FromArgb(30, 41, 59)
            };

            var lblT = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(8, 5),
                AutoSize = true
            };

            var lblV = new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(56, 189, 248),
                Location = new Point(8, 22),
                AutoSize = true
            };

            pnl.Controls.Add(lblT);
            pnl.Controls.Add(lblV);
            parent.Controls.Add(pnl);
            return lblV;
        }

        private void OnQueueStatusUpdatedHandler(QueueStatusInfo info)
        {
            if (IsHandleCreated && !IsDisposed)
            {
                BeginInvoke(new Action(() => RenderQueueStatus(info)));
            }
        }

        private void RenderQueueStatus(QueueStatusInfo info)
        {
            if (_lblStatQueue != null)
                _lblStatQueue.Text = $"{info.QueueLength} / {info.MaxQueueSize} ({(L10n.IsChinese ? "就绪" : "Ready")})";
            if (_lblStatHistory != null)
                _lblStatHistory.Text = $"{info.HistoryLength} / 25";
            if (_lblStatDeck != null)
                _lblStatDeck.Text = $"{info.ShufflePtr} / {info.TotalLocations}";

            // Render Queue Cards
            if (_pnlDevQueueCards != null)
            {
                _pnlDevQueueCards.Controls.Clear();
                if (info.QueuedItems.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = L10n.IsChinese ? "队列正在异步缓冲填充中..." : "Preload buffer refilling asynchronously...",
                        ForeColor = Color.FromArgb(148, 163, 184),
                        Font = new Font("Segoe UI", 9.5f),
                        AutoSize = true,
                        Padding = new Padding(20)
                    };
                    _pnlDevQueueCards.Controls.Add(lblEmpty);
                }
                else
                {
                    for (int i = 0; i < info.QueuedItems.Count; i++)
                    {
                        var item = info.QueuedItems[i];
                        int slotIdx = i;

                        var card = new Panel
                        {
                            Size = new Size(160, 190),
                            BackColor = Color.FromArgb(30, 41, 59),
                            Margin = new Padding(0, 0, 10, 0),
                            Padding = new Padding(8)
                        };

                        var pb = new PictureBox
                        {
                            Location = new Point(8, 8),
                            Size = new Size(144, 88),
                            SizeMode = PictureBoxSizeMode.CenterImage,
                            BackColor = Color.FromArgb(15, 23, 42)
                        };
                        pb.Image = ThumbnailCacheManager.GetPlaceholder();
                        ThumbnailCacheManager.LoadThumbnailAsync(item.Image, 144, 88, thumb =>
                        {
                            if (pb.IsHandleCreated && !pb.IsDisposed)
                                pb.BeginInvoke(new Action(() => pb.Image = thumb));
                        });

                        var lblBadge = new Label
                        {
                            Text = slotIdx == 0 ? (L10n.IsChinese ? "🔥 下一张 (Next)" : "🔥 Next Up") : $"{(L10n.IsChinese ? "槽位 #" : "Slot #")}{slotIdx + 1}",
                            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                            ForeColor = slotIdx == 0 ? Color.FromArgb(250, 204, 21) : Color.FromArgb(56, 189, 248),
                            Location = new Point(8, 100),
                            AutoSize = true
                        };

                        var lblName = new Label
                        {
                            Text = item.GetDisplayName(),
                            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                            ForeColor = Color.White,
                            Location = new Point(8, 118),
                            Size = new Size(144, 32),
                            AutoEllipsis = true
                        };

                        var btnApplyNow = new Button
                        {
                            Text = L10n.IsChinese ? "立即应用" : "Apply",
                            Font = new Font("Segoe UI", 8f),
                            ForeColor = Color.FromArgb(56, 189, 248),
                            BackColor = Color.FromArgb(15, 23, 42),
                            FlatStyle = FlatStyle.Flat,
                            Size = new Size(144, 24),
                            Location = new Point(8, 156),
                            Cursor = Cursors.Hand
                        };
                        btnApplyNow.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                        btnApplyNow.Click += (s, e) => _wallpaperForm.ApplyQueuedItem(slotIdx);

                        card.Controls.Add(pb);
                        card.Controls.Add(lblBadge);
                        card.Controls.Add(lblName);
                        card.Controls.Add(btnApplyNow);
                        _pnlDevQueueCards.Controls.Add(card);
                    }
                }
            }

            // Render History Stack Cards
            if (_pnlDevHistoryCards != null)
            {
                _pnlDevHistoryCards.Controls.Clear();
                if (info.HistoryItems.Count == 0)
                {
                    var lblEmpty = new Label
                    {
                        Text = "暂无轮播回退记录",
                        ForeColor = Color.FromArgb(148, 163, 184),
                        Font = new Font("Segoe UI", 9f),
                        AutoSize = true,
                        Padding = new Padding(20)
                    };
                    _pnlDevHistoryCards.Controls.Add(lblEmpty);
                }
                else
                {
                    for (int i = info.HistoryItems.Count - 1; i >= 0; i--)
                    {
                        var hItem = info.HistoryItems[i];
                        var card = new Panel
                        {
                            Size = new Size(150, 160),
                            BackColor = Color.FromArgb(24, 32, 47),
                            Margin = new Padding(0, 0, 10, 0),
                            Padding = new Padding(6)
                        };

                        var pb = new PictureBox
                        {
                            Location = new Point(6, 6),
                            Size = new Size(138, 80),
                            SizeMode = PictureBoxSizeMode.CenterImage,
                            BackColor = Color.FromArgb(15, 23, 42)
                        };
                        pb.Image = ThumbnailCacheManager.GetPlaceholder();
                        ThumbnailCacheManager.LoadThumbnailAsync(hItem.Image, 138, 80, thumb =>
                        {
                            if (pb.IsHandleCreated && !pb.IsDisposed)
                                pb.BeginInvoke(new Action(() => pb.Image = thumb));
                        });

                        var lblName = new Label
                        {
                            Text = hItem.GetDisplayName(),
                            Font = new Font("Segoe UI", 8f),
                            ForeColor = Color.FromArgb(203, 213, 225),
                            Location = new Point(6, 90),
                            Size = new Size(138, 28),
                            AutoEllipsis = true
                        };

                        var btnRollback = new Button
                        {
                            Text = L10n.IsChinese ? "回退至此" : "Rollback",
                            Font = new Font("Segoe UI", 7.8f),
                            ForeColor = Color.FromArgb(52, 211, 153),
                            BackColor = Color.FromArgb(15, 23, 42),
                            FlatStyle = FlatStyle.Flat,
                            Size = new Size(138, 24),
                            Location = new Point(6, 124),
                            Cursor = Cursors.Hand
                        };
                        btnRollback.FlatAppearance.BorderColor = Color.FromArgb(51, 65, 85);
                        btnRollback.Click += (s, e) =>
                        {
                            _wallpaperForm.ApplyCustomWallpaper(new LocationInfo
                            {
                                Country = hItem.Country,
                                Region = hItem.Region,
                                Lat = hItem.Lat,
                                Lng = hItem.Lng,
                                ImageUrl = hItem.Image
                            });
                        };

                        card.Controls.Add(pb);
                        card.Controls.Add(lblName);
                        card.Controls.Add(btnRollback);
                        _pnlDevHistoryCards.Controls.Add(card);
                    }
                }
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
            var btnOk = new Button { Text = L10n.DialogOk, Left = 200, Top = 96, Width = 84, Height = 28, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(56, 189, 248), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat };
            var btnCancel = new Button { Text = L10n.DialogCancel, Left = 296, Top = 96, Width = 84, Height = 28, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(51, 65, 85), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

            form.Controls.Add(lbl);
            form.Controls.Add(txt);
            form.Controls.Add(btnOk);
            form.Controls.Add(btnCancel);
            form.AcceptButton = btnOk;
            form.CancelButton = btnCancel;

            return form.ShowDialog() == DialogResult.OK ? txt.Text.Trim() : string.Empty;
        }

        private void PromptApiKeySetting()
        {
            using var dlg = new ApiKeySettingsForm();
            dlg.ShowDialog(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SafeGdi.ApplyRoundedRegion(this, 16);
        }

        private void RefreshLocalizedUi()
        {
            if (IsDisposed) return;
            if (_lblLogo != null) _lblLogo.Text = L10n.HubTitle;
            if (_btnTabAsk != null) _btnTabAsk.Text = L10n.TabAskGemini;
            if (_btnTabBookmarks != null) _btnTabBookmarks.Text = L10n.TabBookmarks;
            if (_btnTabDev != null) _btnTabDev.Text = L10n.TabDevTelemetry;
            if (_btnKey != null) _btnKey.Text = L10n.TabApiSettings;

            if (_btnBookmarkAction != null)
            {
                bool isBm = BookmarkManager.IsBookmarked(_currentLoc.Lat, _currentLoc.Lng);
                _btnBookmarkAction.Text = isBm ? L10n.PetBtnBookmarkSaved : L10n.PetBtnBookmarkAdd;
            }
            if (_btnOpenMap != null) _btnOpenMap.Text = L10n.BtnFly3D;
            if (_txtQuestion != null) _txtQuestion.PlaceholderText = L10n.AskPlaceholder;
            if (_btnSend != null) _btnSend.Text = L10n.BtnSend;

            if (_txtBookmarkSearch != null) _txtBookmarkSearch.PlaceholderText = L10n.SearchPlaceholder;
            if (_btnAddCat != null) _btnAddCat.Text = L10n.BtnAddCategory;

            if (_lblDevTitle != null) _lblDevTitle.Text = L10n.DevTitle;
            if (_btnDevReshuffle != null) _btnDevReshuffle.Text = L10n.DevReshuffle;
            if (_btnDevPrev != null) _btnDevPrev.Text = L10n.BubbleBtnPrev;
            if (_btnDevNext != null) _btnDevNext.Text = L10n.BubbleBtnNext;
            if (_btnDevRefresh != null) _btnDevRefresh.Text = "🔄 " + (L10n.IsChinese ? "刷新遥测" : "Refresh");
            if (_lblDevQueueHeader != null) _lblDevQueueHeader.Text = $"📦 {L10n.DevQueueBuffer} (0ms):";
            if (_lblDevHistoryHeader != null) _lblDevHistoryHeader.Text = $"⏮ {L10n.DevHistory}:";

            RefreshBookmarksList();
            if (_wallpaperForm.LatestQueueStatus != null)
            {
                RenderQueueStatus(_wallpaperForm.LatestQueueStatus);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(56, 189, 248), 1.5f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                L10n.OnLanguageChanged -= RefreshLocalizedUi;
                _wallpaperForm.OnQueueStatusUpdated -= OnQueueStatusUpdatedHandler;
            }
            base.Dispose(disposing);
        }
    }
}
