using System;
using System.Globalization;

namespace EarthWallpaper
{
    public enum AppLanguage
    {
        Auto = 0,
        En = 1,
        Zh = 2
    }

    public static class L10n
    {
        public static event Action? OnLanguageChanged;

        public static AppLanguage CurrentLanguage
        {
            get
            {
                string lang = AppConfig.Instance.Language?.ToLowerInvariant() ?? "auto";
                return lang switch
                {
                    "en" => AppLanguage.En,
                    "zh" => AppLanguage.Zh,
                    _ => AppLanguage.Auto
                };
            }
        }

        public static void SetLanguage(AppLanguage lang)
        {
            string str = lang switch
            {
                AppLanguage.En => "en",
                AppLanguage.Zh => "zh",
                _ => "auto"
            };

            if (AppConfig.Instance.Language != str)
            {
                AppConfig.Instance.Language = str;
                AppConfig.Save();
                OnLanguageChanged?.Invoke();
            }
        }

        public static bool IsChinese
        {
            get
            {
                var lang = CurrentLanguage;
                if (lang == AppLanguage.Zh) return true;
                if (lang == AppLanguage.En) return false;
                return CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            }
        }

        // ==========================================
        // General Branding & System Messages
        // ==========================================
        public static string AppName => "Earth Tabliss";
        public static string AppDescription => IsChinese 
            ? "Earth Tabliss - 高性能动态桌面壁纸与桌宠" 
            : "Earth Tabliss - High-Performance Live Wallpaper & Companion";
        public static string AlreadyRunning => IsChinese 
            ? "Earth Tabliss 已经在运行中！\n请查看桌面最底层或屏幕右下角托盘图标。" 
            : "Earth Tabliss is already running!\nPlease check your desktop background or the system tray.";
        public static string Notice => IsChinese ? "提示" : "Notice";

        // ==========================================
        // Language Selector Menu Strings
        // ==========================================
        public static string MenuLanguage => "🌐 Language / 语言";
        public static string LangAuto => IsChinese ? "跟随系统 (Auto)" : "Follow System (Auto)";
        public static string LangEn => "English";
        public static string LangZh => "简体中文 (Chinese)";

        // ==========================================
        // Tray Menu & Pet Menu Common Strings
        // ==========================================
        public static string MenuBookmarks => IsChinese ? "⭐ 我的地标收藏夹" : "⭐ My Saved Bookmarks";
        public static string MenuAskGemini => IsChinese ? "✨ Ask Gemini 地貌研判" : "✨ Ask Gemini Reasoning";
        public static string MenuToggleIcons => IsChinese ? "👁️ 显隐桌面图标" : "👁️ Toggle Desktop Icons";
        public static string MenuNextWallpaper => IsChinese ? "⏭️ 下一张地表壁纸" : "⏭️ Next Wallpaper";
        public static string MenuPrevWallpaper => IsChinese ? "⏮️ 上一张壁纸" : "⏮️ Previous Wallpaper";
        public static string MenuSwitchSkin => IsChinese ? "🎨 切换伴侣外观" : "🎨 Switch Companion Skin";
        public static string MenuDevTelemetry => IsChinese ? "🛠️ 开发者模式 (队列监控)" : "🛠️ Developer Telemetry";
        public static string MenuReload => IsChinese ? "🔄 重载 / 重启应用" : "🔄 Reload Application";
        public static string MenuStartupEnabled => IsChinese ? "🚀 开机自启 (已开启)" : "🚀 Run at Startup (Enabled)";
        public static string MenuStartupDisabled => IsChinese ? "🚀 开机自启 (未开启)" : "🚀 Run at Startup (Disabled)";
        public static string MenuExit => IsChinese ? "🚪 退出 (恢复原壁纸)" : "🚪 Exit (Restore Wallpaper)";

        // Skins
        public static string SkinClassic => IsChinese ? "初版天体核心" : "Classic Celestial Orb";
        public static string SkinProbe => IsChinese ? "O.R.B. 测绘机" : "O.R.B. Orbital Probe";
        public static string SkinSwitchedTitle => IsChinese ? "✨ 外观切换完成" : "✨ Skin Updated";

        // ==========================================
        // Pet Menu & Pet Bubble
        // ==========================================
        public static string PetMenuTitle => IsChinese ? "🌍 小地球桌宠 · 控制中枢" : "🌍 Earth Companion · Hub";
        public static string PetLocPrefix => IsChinese ? "📍 当前漫游: " : "📍 Current: ";
        public static string PetBtnBookmarkAdd => IsChinese ? "⭐ 收藏当前地标" : "⭐ Bookmark Current Landmark";
        public static string PetBtnBookmarkSaved => IsChinese ? "★ 已在收藏夹 (点击取消)" : "★ Bookmarked (Click to remove)";
        public static string PetBtnBookmarks => IsChinese ? "📂 查看我的收藏夹" : "📂 View My Bookmarks";
        public static string PetBtnAskGemini => IsChinese ? "💬 Ask Gemini 智能研判" : "💬 Ask Gemini Reasoning";
        public static string PetBtnNext => IsChinese ? "⏭ 换一张星球壁纸" : "⏭ Next Wallpaper";
        public static string PetBtnIconsShow => IsChinese ? "👁 显隐桌面图标 (当前: 显示)" : "👁 Toggle Desktop Icons (Visible)";
        public static string PetBtnIconsHidden => IsChinese ? "👁‍🗨 显隐桌面图标 (当前: 已隐藏)" : "👁‍🗨 Toggle Desktop Icons (Hidden)";
        public static string PetBtnMap => IsChinese ? "🌐 在 Google Earth 3D 查看" : "🌐 View in Google Earth 3D";
        public static string PetBtnDev => IsChinese ? "🛠️ 开发者 / 队列监控" : "🛠️ Dev / Queue Telemetry";
        public static string PetBtnExit => IsChinese ? "🚪 退出壁纸与桌宠 (恢复原壁纸)" : "🚪 Exit (Restore Wallpaper)";

        public static string BubbleNewLandmark => IsChinese ? "漫游至新地标" : "Arrived at New Landmark";
        public static string BubbleBtnPrev => IsChinese ? "⏮ 上张" : "⏮ Prev";
        public static string BubbleBtnNext => IsChinese ? "⏭ 换图" : "⏭ Next";
        public static string BubbleBtnBookmark => IsChinese ? "⭐ 收藏" : "⭐ Save";
        public static string BubbleBtnBookmarkSaved => IsChinese ? "★ 已藏" : "★ Saved";
        public static string BubbleBtnExplore => IsChinese ? "研判/详情" : "Explore";

        // ==========================================
        // Hub Navigation Tabs
        // ==========================================
        public static string HubTitle => IsChinese ? "🌍 地球中枢" : "🌍 Earth Hub";
        public static string TabAskGemini => "💬 Ask Gemini";
        public static string TabBookmarks => IsChinese ? "⭐ 地标收藏夹" : "⭐ Bookmarks Hub";
        public static string TabDevTelemetry => IsChinese ? "🛠️ 开发者 / 队列" : "🛠️ Dev Telemetry";
        public static string TabApiSettings => IsChinese ? "🔑 API 设置" : "🔑 API Settings";

        // Ask Gemini
        public static string AskTargetPrefix => IsChinese ? "当前漫游地貌" : "Target Landmark";
        public static string AskPlaceholder => IsChinese 
            ? "向 Gemini 提问关于这片地表的任何地理/地质问题..." 
            : "Ask Gemini anything about this terrain's geology, climate, or history...";
        public static string BtnSend => IsChinese ? "发送 ↵" : "Send ↵";
        public static string Chip1 => IsChinese ? "这片地貌是如何形成的？" : "How was this terrain formed?";
        public static string Chip2 => IsChinese ? "主要由什么矿物或岩石构成？" : "What minerals and rocks compose this?";
        public static string Chip3 => IsChinese ? "附近有人类文明或工程奇迹吗？" : "Are there human settlements or marvels nearby?";
        public static string Chip4 => IsChinese ? "当地生态与气候特征是什么？" : "What is the local climate and biome?";
        public static string ApiKeyMissingNotice => IsChinese 
            ? "⚠️ 您尚未配置 Gemini API 密钥。\n请点击右上角【🔑 API 设置】配置免费的 Google AI API 密钥即可开启研判！" 
            : "⚠️ No Gemini API key detected.\nPlease click [🔑 API Settings] at top right to configure your free Google AI Studio key!";
        public static string AnalyzingStatus => IsChinese 
            ? "🛰️ 正在调取轨道遥感物性特征与断层数据库，Gemini 实时推演中..." 
            : "🛰️ Retrieving orbital remote sensing metrics, Gemini is reasoning...";

        // Bookmarks Hub
        public static string SearchPlaceholder => IsChinese ? "🔍 搜索地标或国家..." : "🔍 Search landmarks or countries...";
        public static string BookmarksCountFormat => IsChinese ? "已收藏 {0} 处星球地貌" : "Saved {0} planetary landmarks";
        public static string BtnAddCategory => IsChinese ? "+ 新建分类" : "+ New Category";
        public static string PromptNewCategoryTitle => IsChinese ? "新建收藏分类" : "New Category";
        public static string PromptNewCategoryMessage => IsChinese ? "请输入新分类/文件夹名称 (如: 城市、荒漠、水系、震撼):" : "Enter a new category or folder name:";
        public static string PromptEditCategoryTitle => IsChinese ? "修改所属分类" : "Edit Category";
        public static string PromptEditCategoryMessage => IsChinese ? "请输入新的所属分类/文件夹:" : "Enter category name:";
        public static string CatAll => IsChinese ? "全部" : "All";
        public static string CatDefault => IsChinese ? "默认" : "Default";
        public static string CatCity => IsChinese ? "城市" : "Cities";
        public static string CatDesert => IsChinese ? "荒漠" : "Deserts";
        public static string CatRiver => IsChinese ? "水系" : "Rivers";
        public static string CatFav => IsChinese ? "精选" : "Favorites";
        public static string BtnSetWallpaper => IsChinese ? "设为壁纸" : "Set Wallpaper";
        public static string BtnFly3D => IsChinese ? "🌐 3D 巡礼" : "🌐 Fly in 3D";
        public static string BtnAnalyze => IsChinese ? "💬 研判" : "💬 Analyze";
        public static string BtnDelete => IsChinese ? "🗑️ 移除" : "🗑️ Delete";
        public static string DialogOk => IsChinese ? "确定" : "OK";
        public static string DialogCancel => IsChinese ? "取消" : "Cancel";

        // Dev Telemetry
        public static string DevTitle => IsChinese ? "🛠️ 队列与引擎实时监控" : "🛠️ Queue & Engine Telemetry";
        public static string DevQueueBuffer => IsChinese ? "双缓冲预加载队列" : "Preload Queue Buffer";
        public static string DevHistory => IsChinese ? "壁纸历史栈" : "History Stack";
        public static string DevShuffle => IsChinese ? "洗牌图库" : "Shuffle Deck";
        public static string DevReshuffle => IsChinese ? "🔀 重新洗牌" : "🔀 Reshuffle";
        public static string DevFlushQueue => IsChinese ? "🧹 清空队列" : "🧹 Flush Queue";
        public static string DevWorkerWActive => IsChinese ? "WorkerW 句柄: 正常注入" : "WorkerW: Hooked";
        public static string DevWorkerWNone => IsChinese ? "WorkerW 句柄: 离线/桌面未捕获" : "WorkerW: Not attached";

        // API Settings Form
        public static string ApiTitle => IsChinese ? "🔑 配置 Google Gemini API 密钥" : "🔑 Configure Google Gemini API Key";
        public static string ApiInfo => IsChinese 
            ? "配置个人 API Key 可解锁实时地学智能问答（留空使用内置离线地质引擎）。" 
            : "Configure your personal API key for live AI reasoning (leave blank for offline engine).";
        public static string ApiGetLink => IsChinese 
            ? "🌐 免费获取 Gemini API 密钥 (aistudio.google.com) ↗" 
            : "🌐 Get Free Gemini API Key (aistudio.google.com) ↗";
        public static string ApiKeyLabel => IsChinese ? "API 密钥 (API Key)：" : "API Key:";
        public static string ApiBtnTest => IsChinese ? "⚡ 验证有效性" : "⚡ Verify Key";
        public static string ApiBtnClear => IsChinese ? "🗑️ 清除密钥" : "🗑️ Clear Key";
        public static string ApiBtnSave => IsChinese ? "💾 保存设置" : "💾 Save Settings";
        public static string ApiBtnCancel => IsChinese ? "取消" : "Cancel";
        public static string ApiStatusConfigured => IsChinese 
            ? "🟢 已配置 API Key（可点击下方【⚡ 验证有效性】测试连通状态）" 
            : "🟢 API Key configured (click [⚡ Verify Key] to test connection)";
        public static string ApiStatusEmpty => IsChinese 
            ? "⚪ 当前未配置 API Key（自动使用内置高质量离线地质引擎）" 
            : "⚪ No API Key configured (using built-in offline geological engine)";
        public static string ApiStatusTesting => IsChinese 
            ? "⏳ 正在连接 Google AI Studio 验证 API 密钥有效性，请稍候..." 
            : "⏳ Connecting to Google AI Studio to verify key, please wait...";
        public static string ApiStatusCleared => IsChinese 
            ? "⚪ 密钥已清空。点击【保存设置】即可还原为内置离线引擎。" 
            : "⚪ Key cleared. Click [Save Settings] to use built-in offline engine.";
    }
}
