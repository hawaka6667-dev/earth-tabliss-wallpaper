using System;
using System.IO;
using System.Text.Json;

namespace EarthWallpaper
{
    public enum PetSkin
    {
        MinimalOrb,   // 初版：极简赛博行星球 (Minimalist Cyber Orb)
        HoloGlobe,    // 方案二：全息经纬线光球 (Holographic Wireframe)
        PixelAstro,   // 方案三：复古像素宇航员 (Retro Pixel Explorer)
        ChibiEarth,   // 方案四：治愈系Q版小地球 (Kawaii Mascot)
        MiniPill      // 方案五：极简悬浮徽标 (Dynamic Micro Capsule)
    }

    public static class PetSkinManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper",
            "skin_config.json"
        );

        public static PetSkin CurrentSkin { get; private set; } = PetSkin.MinimalOrb; // Default to initial minimal clean skin
        public static event Action? OnSkinChanged;

        static PetSkinManager()
        {
            LoadSkin();
        }

        public static void SetSkin(PetSkin skin)
        {
            CurrentSkin = skin;
            SaveSkin();
            OnSkinChanged?.Invoke();
        }

        public static void CycleNextSkin()
        {
            int next = ((int)CurrentSkin + 1) % 5;
            SetSkin((PetSkin)next);
        }

        public static string GetSkinDisplayName(PetSkin skin)
        {
            return skin switch
            {
                PetSkin.MinimalOrb => "初版 · 极简天体光球 (推荐)",
                PetSkin.HoloGlobe => "全息 · 经纬线雷达球",
                PetSkin.PixelAstro => "像素 · 复古宇航漫游者",
                PetSkin.ChibiEarth => "萌系 · Q版治愈小地球",
                PetSkin.MiniPill => "微型 · 极简悬浮胶囊",
                _ => "未知皮肤"
            };
        }

        private static void LoadSkin()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("skin", out var skinEl))
                    {
                        if (Enum.TryParse<PetSkin>(skinEl.GetString(), out var loaded))
                        {
                            CurrentSkin = loaded;
                        }
                    }
                }
            }
            catch { }
        }

        private static void SaveSkin()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(new { skin = CurrentSkin.ToString() }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Program.Log($"PetSkinManager.SaveSkin failed: {ex.Message}");
            }
        }
    }
}
