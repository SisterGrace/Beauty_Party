using System.Collections.Generic;
using Config.RemoteConfig;
using SRF;

namespace Config
{
    /// <summary>
    /// RemoteConfig.SpinH5 的在线参数覆写结果。RemoteKey 拉取 JSON，解析后覆写各字段；找不到 Key 或解析失败时使用配置表原值。
    /// </summary>
    public struct SpinH5ResolvedConfig
    {
        public string Url;
        public int CloseDelay;
        public bool StartOnPageLoaded;
        public string Platform;

        /// <summary>
        /// 从 RemoteConfig.SpinH5 解析（兼容新扁平结构）。RemoteKey 拉取 JSON 覆写；无 Key 或解析失败时用原值。
        /// </summary>
        /// <param name="isRv">true=解析 RV 配置（RvBidUrl），false=解析 IV 配置（IvBidUrl）</param>
        public static SpinH5ResolvedConfig Resolve(RemoteConfig.SpinH5 src, System.Func<string, string, string> getStr, bool isRv)
        {
            if (src == null) return default;

            var key = src.RemoteKey ?? "";
            var json = !string.IsNullOrEmpty(key) ? getStr(key, "") : "";
            if (string.IsNullOrEmpty(json)) return FromOriginal(src, isRv);

            Dictionary<string, object> dict = null;
            try { dict = Json.Deserialize(json) as Dictionary<string, object>; } catch { }
            if (dict == null) return FromOriginal(src, isRv);

            var urlKey = isRv ? "rv_bid_url" : "iv_bid_url";
            var delayKey = isRv ? "rv_close_delay" : "iv_close_delay";
            var urlFallback = isRv ? (src.RvBidUrl ?? "") : (src.IvBidUrl ?? "");
            var delayFallback = isRv ? src.RvCloseDelay : src.IvCloseDelay;

            return new SpinH5ResolvedConfig
            {
                Url = TryGetString(dict, urlKey, urlFallback),
                CloseDelay = TryGetInt(dict, delayKey, delayFallback),
                StartOnPageLoaded = true,
                Platform = TryGetString(dict, "platform", "OkSpin")
            };
        }

        private static SpinH5ResolvedConfig FromOriginal(RemoteConfig.SpinH5 src, bool isRv) => new SpinH5ResolvedConfig
        {
            Url = isRv ? (src.RvBidUrl ?? "") : (src.IvBidUrl ?? ""),
            CloseDelay = isRv ? src.RvCloseDelay : src.IvCloseDelay,
            StartOnPageLoaded = true,
            Platform = "OkSpin"
        };

        private static string TryGetString(Dictionary<string, object> dict, string key, string fallback)
        {
            if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
            return v.ToString();
        }

        private static int TryGetInt(Dictionary<string, object> dict, string key, int fallback)
        {
            if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
            if (v is long l) return (int)l;
            if (v is int i) return i;
            if (v is double d) return (int)d;
            return int.TryParse(v.ToString(), out var parsed) ? parsed : fallback;
        }

        private static bool TryGetBool(Dictionary<string, object> dict, string key, bool fallback)
        {
            if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
            if (v is bool b) return b;
            return bool.TryParse(v.ToString(), out var parsed) ? parsed : fallback;
        }
    }
}
