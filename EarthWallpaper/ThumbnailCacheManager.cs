using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EarthWallpaper
{
    public static class ThumbnailCacheManager
    {
        private static readonly string CacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper",
            "Thumbnails"
        );

        private static readonly ConcurrentDictionary<string, Image> MemoryCache = new();
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly Image DefaultPlaceholder;

        static ThumbnailCacheManager()
        {
            try
            {
                if (!Directory.Exists(CacheDir))
                {
                    Directory.CreateDirectory(CacheDir);
                }
            }
            catch { }

            DefaultPlaceholder = CreatePlaceholder(120, 76);
        }

        public static Image GetPlaceholder() => DefaultPlaceholder;

        public static Image CreatePlaceholder(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height),
                           Color.FromArgb(30, 41, 59), Color.FromArgb(15, 23, 42), 45f))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }

                using (var pen = new Pen(Color.FromArgb(51, 65, 85), 1f))
                {
                    g.DrawRectangle(pen, 0, 0, width - 1, height - 1);
                }

                using var font = new Font("Segoe UI Emoji", 20f);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("🌍", font, Brushes.White, new RectangleF(0, 0, width, height), sf);
            }
            return bmp;
        }

        private static string GetCacheFilename(string url)
        {
            using var md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(url));
            var sb = new StringBuilder();
            foreach (byte b in hash) sb.Append(b.ToString("x2"));
            return Path.Combine(CacheDir, $"{sb}.jpg");
        }

        public static Image? GetCachedOrNull(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return null;
            if (MemoryCache.TryGetValue(imageUrl, out var cached)) return cached;

            string localPath = imageUrl;
            if (!File.Exists(localPath))
            {
                localPath = GetCacheFilename(imageUrl);
            }

            if (File.Exists(localPath))
            {
                try
                {
                    using var stream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using var src = Image.FromStream(stream);
                    var thumb = CreateThumbnail(src, 120, 76);
                    MemoryCache[imageUrl] = thumb;
                    return thumb;
                }
                catch { }
            }
            return null;
        }

        public static void LoadThumbnailAsync(string imageUrl, int targetW, int targetH, Action<Image> onLoaded)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                onLoaded(DefaultPlaceholder);
                return;
            }

            var immediate = GetCachedOrNull(imageUrl);
            if (immediate != null)
            {
                onLoaded(immediate);
                return;
            }

            // Immediately supply placeholder while downloading
            onLoaded(DefaultPlaceholder);

            Task.Run(async () =>
            {
                try
                {
                    string diskPath = GetCacheFilename(imageUrl);

                    if (!File.Exists(diskPath))
                    {
                        if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            byte[] data = await HttpClient.GetByteArrayAsync(imageUrl);
                            await File.WriteAllBytesAsync(diskPath, data);
                        }
                        else if (File.Exists(imageUrl))
                        {
                            File.Copy(imageUrl, diskPath, true);
                        }
                    }

                    if (File.Exists(diskPath))
                    {
                        using var stream = new FileStream(diskPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var src = Image.FromStream(stream);
                        var thumb = CreateThumbnail(src, targetW, targetH);
                        MemoryCache[imageUrl] = thumb;
                        onLoaded(thumb);
                    }
                }
                catch
                {
                    // Fail gracefully with placeholder
                }
            });
        }

        private static Image CreateThumbnail(Image src, int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Aspect fill cropping
            float srcAspect = (float)src.Width / src.Height;
            float dstAspect = (float)width / height;

            Rectangle srcRect;
            if (srcAspect > dstAspect)
            {
                int cropW = (int)(src.Height * dstAspect);
                int cropX = (src.Width - cropW) / 2;
                srcRect = new Rectangle(cropX, 0, cropW, src.Height);
            }
            else
            {
                int cropH = (int)(src.Width / dstAspect);
                int cropY = (src.Height - cropH) / 2;
                srcRect = new Rectangle(0, cropY, src.Width, cropH);
            }

            g.DrawImage(src, new Rectangle(0, 0, width, height), srcRect, GraphicsUnit.Pixel);

            // Subtle border
            using var borderPen = new Pen(Color.FromArgb(40, 255, 255, 255), 1f);
            g.DrawRectangle(borderPen, 0, 0, width - 1, height - 1);

            return bmp;
        }
    }
}
