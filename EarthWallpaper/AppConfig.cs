using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EarthWallpaper
{
    public class AppConfigData
    {
        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "auto"; // "auto", "en", "zh"

        [JsonPropertyName("petSkin")]
        public string PetSkin { get; set; } = "ClassicOrb"; // "ClassicOrb", "OrbProbe"
    }

    public static class AppConfig
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper"
        );
        private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");
        private static readonly string LegacySkinPath = Path.Combine(ConfigDir, "pet_skin.txt");

        private static AppConfigData? _instance;
        private static readonly object _lock = new();

        public static AppConfigData Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= Load();
                    }
                }
                return _instance;
            }
        }

        public static AppConfigData Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var data = JsonSerializer.Deserialize<AppConfigData>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (data != null)
                    {
                        // Check legacy skin file migration if petSkin was not set
                        if (string.IsNullOrEmpty(data.PetSkin) || data.PetSkin == "ClassicOrb")
                        {
                            if (File.Exists(LegacySkinPath))
                            {
                                string legacy = File.ReadAllText(LegacySkinPath).Trim();
                                if (!string.IsNullOrEmpty(legacy)) data.PetSkin = legacy;
                            }
                        }
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log($"AppConfig.Load error: {ex.Message}");
            }

            var fallback = new AppConfigData();
            if (File.Exists(LegacySkinPath))
            {
                try
                {
                    string legacy = File.ReadAllText(LegacySkinPath).Trim();
                    if (!string.IsNullOrEmpty(legacy)) fallback.PetSkin = legacy;
                }
                catch { }
            }
            return fallback;
        }

        public static void Save()
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);
                    string json = JsonSerializer.Serialize(Instance, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    File.WriteAllText(ConfigPath, json);
                }
            }
            catch (Exception ex)
            {
                Program.Log($"AppConfig.Save error: {ex.Message}");
            }
        }
    }
}
