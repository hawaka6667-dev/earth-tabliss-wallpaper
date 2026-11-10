using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EarthWallpaper
{
    public class BookmarkItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Country { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Lat { get; set; } = "0.000000";
        public string Lng { get; set; } = "0.000000";
        public string MapUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(Region))
            {
                return $"{Country} - {Region}";
            }
            return !string.IsNullOrEmpty(Country) ? Country : "未知地标";
        }
    }

    public static class BookmarkManager
    {
        private static readonly string StoragePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper",
            "bookmarks.json"
        );

        private static List<BookmarkItem> _items = new();
        private static bool _loaded = false;

        public static event Action? OnBookmarksChanged;

        public static List<BookmarkItem> GetAll()
        {
            EnsureLoaded();
            return new List<BookmarkItem>(_items);
        }

        public static bool IsBookmarked(string? lat, string? lng)
        {
            if (string.IsNullOrEmpty(lat) || string.IsNullOrEmpty(lng)) return false;
            EnsureLoaded();
            return _items.Exists(b => Math.Abs(ParseDouble(b.Lat) - ParseDouble(lat)) < 0.0001 &&
                                      Math.Abs(ParseDouble(b.Lng) - ParseDouble(lng)) < 0.0001);
        }

        public static bool ToggleBookmark(LocationInfo loc, out bool nowBookmarked)
        {
            EnsureLoaded();
            if (IsBookmarked(loc.Lat, loc.Lng))
            {
                _items.RemoveAll(b => Math.Abs(ParseDouble(b.Lat) - ParseDouble(loc.Lat)) < 0.0001 &&
                                      Math.Abs(ParseDouble(b.Lng) - ParseDouble(loc.Lng)) < 0.0001);
                Save();
                nowBookmarked = false;
                OnBookmarksChanged?.Invoke();
                return true;
            }
            else
            {
                _items.Insert(0, new BookmarkItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Country = loc.Country,
                    Region = loc.Region,
                    Lat = loc.Lat,
                    Lng = loc.Lng,
                    MapUrl = loc.MapUrl,
                    ImageUrl = loc.ImageUrl,
                    SavedAt = DateTime.Now
                });
                Save();
                nowBookmarked = true;
                OnBookmarksChanged?.Invoke();
                return true;
            }
        }

        public static bool Remove(string id)
        {
            EnsureLoaded();
            int count = _items.RemoveAll(b => b.Id == id);
            if (count > 0)
            {
                Save();
                OnBookmarksChanged?.Invoke();
                return true;
            }
            return false;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            try
            {
                if (File.Exists(StoragePath))
                {
                    string json = File.ReadAllText(StoragePath);
                    _items = JsonSerializer.Deserialize<List<BookmarkItem>>(json) ?? new();
                }
            }
            catch { _items = new(); }
            _loaded = true;
        }

        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(StoragePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(_items, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StoragePath, json);
            }
            catch (Exception ex)
            {
                Program.Log($"BookmarkManager.Save failed: {ex.Message}");
            }
        }

        private static double ParseDouble(string val)
        {
            if (double.TryParse(val, out double d)) return d;
            return 0;
        }
    }
}
