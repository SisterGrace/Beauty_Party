using Config;
using Config.config;
using Config.RemoteConfig;

/// <summary>
/// SpinH5 配置读取器。从 TIAASpinH5 获取当前参与竞价的平台（OkSpin 或 SpinGo），并解析为可用配置。
/// 按清单逻辑：先判断 OkSpin，再 SpinGo，有数据且 enabled==true 则采用，最多一个。
/// </summary>
public static class SpinH5ConfigReader
{

    /// <summary>缓存的 enabled 布尔值，默认使用配置表；获取在线参数（Resolve）时更新。</summary>
    private static bool? _cachedOkSpinEnabled;
    private static bool? _cachedSpinGoEnabled;

    /// <summary>清除缓存。在线参数刷新后调用。</summary>
    public static void InvalidateCache()
    {
        _cachedOkSpinEnabled = null;
        _cachedSpinGoEnabled = null;
    }

    /// <summary>
    /// 获取当前参与竞价的平台配置。OkSpin 优先，SpinGo 其次，第一个 enabled 且有效则返回。
    /// </summary>
    public static SpinH5ResolvedPlatformConfig? GetActivePlatformConfig(TbConstant cfg)
    {
        if (cfg == null) return null;

        if (IsPlatformValid(cfg.OkSpin, "OkSpin"))
        {
            var resolved = SpinH5ResolvedPlatformConfig.Resolve(cfg.OkSpin, SpinH5ConfigProvider.SafeGetStr, "OkSpin");
            _cachedOkSpinEnabled = resolved.Enabled;
            if (resolved.Enabled) return resolved;
        }

        if (IsPlatformValid(cfg.SpinGo, "SpinGo"))
        {
            var resolved = SpinH5ResolvedPlatformConfig.Resolve(cfg.SpinGo, SpinH5ConfigProvider.SafeGetStr, "SpinGo");
            _cachedSpinGoEnabled = resolved.Enabled;
            if (resolved.Enabled) return resolved;
        }

        return null;
    }

    /// <summary>平台有效：非空且 enabled。enabled 读取缓存布尔值，默认使用配置表；获取在线参数时更新。</summary>
    private static bool IsPlatformValid(Config.RemoteConfig.SpinH5 platform, string platformName)
    {
        if (platform == null) return false;
        var enabled = platformName == "OkSpin"
            ? (_cachedOkSpinEnabled ?? platform.Enabled)
            : (_cachedSpinGoEnabled ?? platform.Enabled);
        return enabled;
    }

    /// <summary>从平台配置转为 RV 展示用 Config（Url、CloseDelay、Platform）</summary>
    public static SpinH5ResolvedConfig ToRvDisplayConfig(SpinH5ResolvedPlatformConfig platform)
    {
        return new SpinH5ResolvedConfig
        {
            Url = platform.RvBidUrl ?? "",
            CloseDelay = platform.RvCloseDelay,
            StartOnPageLoaded = true,
            Platform = platform.Platform ?? "OkSpin"
        };
    }

    /// <summary>从平台配置转为 IV 展示用 Config（Url、CloseDelay、Platform）</summary>
    public static SpinH5ResolvedConfig ToIvDisplayConfig(SpinH5ResolvedPlatformConfig platform)
    {
        return new SpinH5ResolvedConfig
        {
            Url = platform.IvBidUrl ?? "",
            CloseDelay = platform.IvCloseDelay,
            StartOnPageLoaded = true,
            Platform = platform.Platform ?? "OkSpin"
        };
    }

    /// <summary>获取 H5 RV/IV 可用性（仅 URL）。基于当前参与的平台配置。</summary>
    public static (bool h5RvAvailable, bool h5IvAvailable) GetH5Availability(TbConstant cfg)
    {
        var platform = GetActivePlatformConfig(cfg);
        if (platform == null) return (false, false);
        var p = platform.Value;
        return (!string.IsNullOrEmpty(p.RvBidUrl), !string.IsNullOrEmpty(p.IvBidUrl));
    }

    /// <summary>获取经 bid_format、daily_limit、total_limit 筛选后的 H5 可用性。</summary>
    public static (bool h5RvAvailable, bool h5IvAvailable) GetFilteredH5Availability(TbConstant cfg)
    {
        var platform = GetActivePlatformConfig(cfg);
        if (platform == null) return (false, false);
        var p = platform.Value;

        var rvUrlOk = !string.IsNullOrEmpty(p.RvBidUrl);
        var ivUrlOk = !string.IsNullOrEmpty(p.IvBidUrl);
        var rvBidFormatOk = p.BidFormat == 2 || p.BidFormat == 3;
        var ivBidFormatOk = p.BidFormat == 1 || p.BidFormat == 3;
        var rvLimitOk = SpinH5LimitTracker.Instance.CanShowRv(p.RvDailyLimit, p.RvTotalLimit);
        var ivLimitOk = SpinH5LimitTracker.Instance.CanShowIv(p.IvDailyLimit, p.IvTotalLimit);

        return (rvUrlOk && rvBidFormatOk && rvLimitOk, ivUrlOk && ivBidFormatOk && ivLimitOk);
    }
}
