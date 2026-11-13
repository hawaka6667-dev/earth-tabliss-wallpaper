using System;
using System.Globalization;

namespace EarthWallpaper
{
    public static class L10n
    {
        public static bool IsChinese => CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

        // General
        public static string AppName => "Earth Tabliss";
        public static string AppDescription => IsChinese ? "Earth Tabliss - 高性能动态桌面壁纸与桌宠" : "Earth Tabliss - High-Performance Live Wallpaper & Companion";
        public static string AlreadyRunning => IsChinese ? "Earth Tabliss 已经在运行中！\n请查看桌面最底层或屏幕右下角托盘图标。" : "Earth Tabliss is already running!\nPlease check your desktop background or the system tray.";
        public static string Notice => IsChinese ? "提示" : "Notice";

        // Tray Menu & Pet Menu
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

        // Bookmarks & Actions
        public static string BookmarkActionAdd => IsChinese ? "⭐ 收藏此地标" : "⭐ Bookmark Landmark";
        public static string BookmarkActionSaved => IsChinese ? "★ 已收藏" : "★ Bookmarked";
        public static string BookmarkBtnBubble => IsChinese ? "⭐ 收藏" : "⭐ Save";
        public static string BookmarkBtnBubbleSaved => IsChinese ? "★ 已藏" : "★ Saved";
        public static string FlyEarth3D => IsChinese ? "🌐 3D 巡礼" : "🌐 Fly in 3D";
        public static string AskGeminiAction => IsChinese ? "💬 研判" : "💬 Analyze";
        public static string NextAction => IsChinese ? "⏭️ 换图" : "⏭️ Next";
        public static string PrevAction => IsChinese ? "⏮️ 上张" : "⏮️ Prev";

        // Hub Navigation Tabs
        public static string TabAskGemini => "💬 Ask Gemini";
        public static string TabBookmarks => IsChinese ? "⭐ 地标收藏夹" : "⭐ Bookmarks Hub";
        public static string TabDevTelemetry => IsChinese ? "🛠️ 开发者模式" : "🛠️ Dev Telemetry";
        public static string TabApiSettings => IsChinese ? "🔑 API 设置" : "🔑 API Settings";

        // Category Management
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
        public static string DialogOk => IsChinese ? "确定" : "OK";
        public static string DialogCancel => IsChinese ? "取消" : "Cancel";

        // Gemini Q&A
        public static string ApiKeyMissingNotice => IsChinese 
            ? "⚠️ 您尚未配置 Gemini API 密钥。\n请点击右上角【🔑 API 设置】配置免费的 Google AI API 密钥即可开启研判！" 
            : "⚠️ No Gemini API key detected.\nPlease click [🔑 API Settings] at top right to configure your free Google AI Studio key!";
        public static string AnalyzingStatus => IsChinese 
            ? "🛰️ 正在调取轨道遥感物性特征与断层数据库，Gemini 实时推演中..." 
            : "🛰️ Retrieving orbital remote sensing metrics, Gemini is reasoning...";
    }
}
