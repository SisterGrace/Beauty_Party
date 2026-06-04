using System;
using System.Collections.Generic;
using Config.RemoteConfig;
using SRF;

/// <summary>
/// RemoteConfig.SpinH5 的解析结果（扁平结构）。RemoteKey 拉取 JSON 覆写各字段；无 Key 或解析失败时使用配置表原值。
/// 用于 OkSpin/SpinGo 平台配置的解析。
/// </summary>
public struct SpinH5ResolvedPlatformConfig
{
    public bool Enabled;
    public int BidFormat;
    /// <summary>H5 广告展示 CD（秒），展示后进入 CD，CD 内 H5 不参与竞价</summary>
    public int BidCdSecond;
    public float IvBidPrice;
    public float RvBidPrice;
    public string IvBidUrl;
    public string RvBidUrl;
    public int IvDailyLimit;
    public int RvDailyLimit;
    public int IvTotalLimit;
    public int RvTotalLimit;
    /// <summary>插屏关闭延迟（秒），默认 5</summary>
    public int IvCloseDelay;
    /// <summary>激励关闭延迟（秒），默认 15</summary>
    public int RvCloseDelay;
    /// <summary>平台标识，用于 H5 展示时上报。OkSpin 或 SpinGo</summary>
    public string Platform;

    /// <summary>
    /// 从 RemoteConfig.SpinH5 解析。RemoteKey 为在线参数 Key，返回 JSON 覆写各字段；无 Key 或解析失败时用原值。
    /// </summary>
    /// <param name="platform">平台标识，如 "OkSpin" 或 "SpinGo"</param>
    public static SpinH5ResolvedPlatformConfig Resolve(global::Config.RemoteConfig.SpinH5 src, Func<string, string, string> getStr, string platform = "OkSpin")
    {
        if (src == null) return default;

        var key = src.RemoteKey ?? "";
        var json = !string.IsNullOrEmpty(key) ? getStr(key, "") : "";
        if (string.IsNullOrEmpty(json)) return FromOriginal(src, platform);

        Dictionary<string, object> dict = null;
        try { dict = Json.Deserialize(json) as Dictionary<string, object>; } catch { }
        if (dict == null) return FromOriginal(src, platform);

        return new SpinH5ResolvedPlatformConfig
        {
            Enabled = TryGetBool(dict, "enabled", src.Enabled),
            BidFormat = TryGetInt(dict, "bid_format", src.BidFormat),
            BidCdSecond = TryGetInt(dict, "bid_cd_second", src.BidCdSecond),
            IvBidPrice = TryGetFloat(dict, "iv_bid_price", ParseFloat(src.IvBidPrice, 0.5f)),
            RvBidPrice = TryGetFloat(dict, "rv_bid_price", ParseFloat(src.RvBidPrice, 0.5f)),
            IvBidUrl = TryGetString(dict, "iv_bid_url", src.IvBidUrl ?? ""),
            RvBidUrl = TryGetString(dict, "rv_bid_url", src.RvBidUrl ?? ""),
            IvDailyLimit = TryGetInt(dict, "iv_daily_limit", src.IvDailyLimit),
            RvDailyLimit = TryGetInt(dict, "rv_daily_limit", src.RvDailyLimit),
            IvTotalLimit = TryGetInt(dict, "iv_total_limit", src.IvTotalLimit),
            RvTotalLimit = TryGetInt(dict, "rv_total_limit", src.RvTotalLimit),
            IvCloseDelay = TryGetInt(dict, "iv_close_delay", src.IvCloseDelay),
            RvCloseDelay = TryGetInt(dict, "rv_close_delay", src.RvCloseDelay),
            Platform = platform
        };
    }

    /// <summary>从配置表原值构建，不解析 RemoteKey</summary>
    public static SpinH5ResolvedPlatformConfig FromOriginal(global::Config.RemoteConfig.SpinH5 src, string platform = "OkSpin")
    {
        if (src == null) return default;
        return new SpinH5ResolvedPlatformConfig
        {
            Enabled = src.Enabled,
            BidFormat = src.BidFormat,
            BidCdSecond = src.BidCdSecond,
            IvBidPrice = ParseFloat(src.IvBidPrice, 0.5f),
            RvBidPrice = ParseFloat(src.RvBidPrice, 0.5f),
            IvBidUrl = src.IvBidUrl ?? "",
            RvBidUrl = src.RvBidUrl ?? "",
            IvDailyLimit = src.IvDailyLimit,
            RvDailyLimit = src.RvDailyLimit,
            IvTotalLimit = src.IvTotalLimit,
            RvTotalLimit = src.RvTotalLimit,
            IvCloseDelay = src.IvCloseDelay,
            RvCloseDelay = src.RvCloseDelay,
            Platform = platform
        };
    }

    private static float ParseFloat(string s, float fallback)
    {
        if (string.IsNullOrEmpty(s)) return fallback;
        return float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : fallback;
    }

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

    private static float TryGetFloat(Dictionary<string, object> dict, string key, float fallback)
    {
        if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
        if (v is float f) return f;
        if (v is double d) return (float)d;
        return float.TryParse(v.ToString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    private static bool TryGetBool(Dictionary<string, object> dict, string key, bool fallback)
    {
        if (!dict.TryGetValue(key, out var v) || v == null) return fallback;
        if (v is bool b) return b;
        return bool.TryParse(v.ToString(), out var parsed) ? parsed : fallback;
    }
}
