using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EarthWallpaper
{
    public static class GeminiService
    {
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "EarthWallpaper",
            "config.json"
        );

        private static string _apiKey = string.Empty;
        private static bool _configLoaded = false;

        public static string GetApiKey()
        {
            EnsureConfigLoaded();
            if (!string.IsNullOrEmpty(_apiKey)) return _apiKey;
            return Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
        }

        public static void SetApiKey(string key)
        {
            _apiKey = key.Trim();
            SaveConfig();
        }

        public static bool HasApiKey()
        {
            return !string.IsNullOrEmpty(GetApiKey());
        }

        private static void EnsureConfigLoaded()
        {
            if (_configLoaded) return;
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("apiKey", out var keyEl))
                    {
                        _apiKey = keyEl.GetString() ?? string.Empty;
                    }
                }
            }
            catch { }
            _configLoaded = true;
        }

        private static void SaveConfig()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(new { apiKey = _apiKey }, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Program.Log($"GeminiService.SaveConfig error: {ex.Message}");
            }
        }

        public static async Task<string> AskQuestionAsync(LocationInfo location, string userQuestion, List<(string role, string text)> history)
        {
            string key = GetApiKey();
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    return await CallGeminiApiAsync(key, location, userQuestion, history);
                }
                catch (Exception ex)
                {
                    Program.Log($"Gemini API Call failed: {ex.Message}, falling back to built-in reasoning.");
                    return $"[API 连接波动: {ex.Message}]\n\n" + GetHeuristicAnswer(location, userQuestion);
                }
            }

            // Fallback to high-fidelity built-in domain reasoning
            return GetHeuristicAnswer(location, userQuestion);
        }

        public static async Task<(bool success, string message)> VerifyApiKeyAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return (false, "密钥为空，请输入有效的 API Key。");
            }

            try
            {
                string cleanKey = apiKey.Trim();
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-4-hydrogen:generateContent?key={cleanKey}";
                var payload = new
                {
                    contents = new[]
                    {
                        new { role = "user", parts = new[] { new { text = "Ping" } } }
                    },
                    generationConfig = new { maxOutputTokens = 5 }
                };

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await _httpClient.PostAsync(url, content, cts.Token);
                string respBody = await resp.Content.ReadAsStringAsync(cts.Token);

                if (resp.IsSuccessStatusCode)
                {
                    return (true, "连接成功！Gemini 4 Hydrogen 服务正常响应就绪。");
                }

                string errMsg = $"HTTP {(int)resp.StatusCode}";
                try
                {
                    using var doc = JsonDocument.Parse(respBody);
                    if (doc.RootElement.TryGetProperty("error", out var errEl) && errEl.TryGetProperty("message", out var msgEl))
                    {
                        errMsg = msgEl.GetString() ?? errMsg;
                    }
                }
                catch { }

                if (resp.StatusCode == System.Net.HttpStatusCode.BadRequest || resp.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return (false, $"密钥无效或无访问权限 ({errMsg})");
                }

                return (false, $"服务返回异常 ({errMsg})");
            }
            catch (OperationCanceledException)
            {
                return (false, "请求超时（请检查本地网络连接或代理配置）");
            }
            catch (Exception ex)
            {
                return (false, $"网络连接失败: {ex.Message}");
            }
        }

        private static async Task<string> CallGeminiApiAsync(string apiKey, LocationInfo loc, string question, List<(string role, string text)> history)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-4-hydrogen:generateContent?key={apiKey}";

            var contents = new List<object>();

            // System instruction priming
            string sysPrompt = $"你是一名享誉全球的资深行星地质学家与卫星遥感科学家。" +
                               $"当前用户正在桌面欣赏 Google Earth 卫星地貌。\n" +
                               $"当前视界坐标：{loc.GetDisplayName()}（纬度: {loc.Lat}, 经度: {loc.Lng}）。\n" +
                               $"请用专业、生动、通俗且引人入胜的中文回答用户的地质/地理/地貌提问，剖析岩层走向、板块构造、外力侵蚀（水流/风蚀/冰川）及观测建议。格式清晰，善用加粗与要点分段。";

            contents.Add(new
            {
                role = "user",
                parts = new object[] { new { text = sysPrompt + "\n\n用户提问：" + question } }
            });

            var payload = new
            {
                contents = contents,
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 1200
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            string respJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"HTTP {(int)response.StatusCode}: {respJson}");
            }

            using var doc = JsonDocument.Parse(respJson);
            var candidates = doc.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() > 0)
            {
                var parts = candidates[0].GetProperty("content").GetProperty("parts");
                if (parts.GetArrayLength() > 0)
                {
                    return parts[0].GetProperty("text").GetString() ?? "Gemini 返回了空内容。";
                }
            }

            return "未能解析 Gemini 的响应。";
        }

        public static string GetHeuristicAnswer(LocationInfo loc, string question)
        {
            string locName = loc.GetDisplayName();
            string lat = loc.Lat;
            string lng = loc.Lng;

            if (question.Contains("构造") || question.Contains("形成") || question.Contains("成因") || question.Contains("火山"))
            {
                return $"### 🌋 地貌构造与动力学演化 ({locName})\n\n" +
                       $"该区域位于经纬度 ({lat}, {lng})。\n" +
                       $"**1. 板块动力基底**：其地表纹理主要受区域构造挤压与基底断裂控制。宏观线性走向反映了深层断层活动与古老地盾抬升过程。\n" +
                       $"**2. 剥蚀平衡**：卫星影像呈现出的明显起伏对比，记录了长达数百万年的重力崩塌与岩石抗风化差异。\n\n" +
                       $"*💡 观测小贴士：在 Google Earth 3D 模式下将视线逆光倾斜 45°，可清晰辨析岩层走向与倾角。*";
            }
            else if (question.Contains("水") || question.Contains("河流") || question.Contains("侵蚀") || question.Contains("沉积"))
            {
                return $"### 🌊 水系网络与外力下切雕琢 ({locName})\n\n" +
                       $"从光学卫星视角俯瞰 ({lat}, {lng})：\n" +
                       $"**1. 树枝状水系特征**：地表水流依据重力势能切割地层，形成了高度分形的冲沟与冲积扇构造。\n" +
                       $"**2. 搬运与沉降**：明暗相间的浅色区域多为细粒沉积物或干燥盐类结晶，深色凹槽则是季节性汇水干流通道。\n\n" +
                       $"*💡 观测小贴士：可通过历史遥感时间轴对比近 30 年降水丰水期与枯水期的水岸线变迁。*";
            }
            else if (question.Contains("颜色") || question.Contains("色彩") || question.Contains("为什么长这样") || question.Contains("遥感"))
            {
                return $"### 🛰️ 遥感多光谱与地表物性解析 ({locName})\n\n" +
                       $"Google Earth View 的绚丽色彩主要源自高分遥感光学与近红外反射波段的真彩色合成：\n" +
                       $"**1. 矿物成分响应**：赤红与赭石色调通常指示高含量的铁氧化物（如赤铁矿、褐铁矿）；黄白色多为碳酸盐或石膏结晶。\n" +
                       $"**2. 植被与微量水分**：青翠与碧蓝过渡带映射了极薄的潮汐水膜、富营养化藻类或耐旱荒漠植被覆盖。\n\n" +
                       $"*💡 这种超脱日常视角的抽象美，是大自然尺度放大后的宏观几何杰作。*";
            }
            else
            {
                return $"### ✨ 行星地貌综合视界研判 ({locName})\n\n" +
                       $"观测目标坐标：**{lat}° N, {lng}° E**\n\n" +
                       $"针对您关心的提问：“*{question}*”：\n" +
                       $"• **大地构造背景**：该地点处于典型陆表动态平衡演化带，既有内力地质作用形成的宏观骨架，又有持续的风力与温差剥蚀。\n" +
                       $"• **人类活动印记**：在当前分辨率下，人工道路、几何农田与自然流线形成强烈对比，体现了人类文明与地表生态的共生。\n\n" +
                       $"*(提示：在【设置】中绑定您的 Gemini API Key 可解锁针对任意复杂学科问题的万字实时研判！)*";
            }
        }
    }
}
