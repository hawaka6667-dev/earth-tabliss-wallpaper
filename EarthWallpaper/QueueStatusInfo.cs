using System;
using System.Collections.Generic;

namespace EarthWallpaper
{
    public class QueueItemInfo
    {
        public string Country { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Lat { get; set; } = "0.000000";
        public string Lng { get; set; } = "0.000000";
        public string Image { get; set; } = string.Empty;

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(Country) && !string.IsNullOrEmpty(Region))
                return $"{Country} - {Region}";
            return !string.IsNullOrEmpty(Country) ? Country : "未知地标";
        }
    }

    public class QueueStatusInfo
    {
        public int QueueLength { get; set; }
        public int LoadingCount { get; set; }
        public int MaxQueueSize { get; set; } = 5;
        public int HistoryLength { get; set; }
        public int ShufflePtr { get; set; }
        public int TotalLocations { get; set; }
        public QueueItemInfo? CurrentLocation { get; set; }
        public List<QueueItemInfo> QueuedItems { get; set; } = new();
        public List<QueueItemInfo> HistoryItems { get; set; } = new();
    }
}
