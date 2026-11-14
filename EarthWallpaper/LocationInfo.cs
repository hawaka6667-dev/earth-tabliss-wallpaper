using System;

namespace EarthWallpaper
{
    public class LocationInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Lat { get; set; } = "0.000000";
        public string Lng { get; set; } = "0.000000";
        public string MapUrl { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Terrain { get; set; } = string.Empty;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(Region))
            {
                return $"{Country} - {Region}";
            }
            return !string.IsNullOrEmpty(Country) ? Country : "未知秘境";
        }
    }
}
