using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Sky;



[Serializable]
public class ServerConfig
{
    [Header("服务器配置")]
    public string BaseUrl = "";
    public string AsnUrl = "";
}

[Serializable]
public class AnalyticsConfig
{
    [Header("统计配置")]
#if SSThinkingData
        public string SSAppID_Android = "";
        public string SSAppID_iOS = "";
        public string SSUrl = "";
#endif
#if ADJUST
        public string AdjustToken_Android = "";
        public string AdjustToken_iOS = "";
#endif
    public string AdmobAppID_Android = "";
    public string AdmobAppID_iOS = "";
    public string GA_Game_key = "";
    public string GA_Secret_key = "";
    public string reyun_ID = "";
}

/// <summary>
/// TopOn（TPN）应用与广告位 ID，由宿主在 SkyGameConfigData 资源中填写；SDK 只从此读取，不使用远程 AdUnits JSON。
/// </summary>
[Serializable]
public class SkyToponLocalConfig
{
    [Header("应用 ID / App Key")]
    public string appId = "";
    public string appKey = "";
    [Header("广告位 Placement ID")]
    public string interstitialPlacementId = "";
    public string rewardedPlacementId = "";
    public string bannerPlacementId = "";
    public string splashPlacementId = "";
}

/// <summary>
/// Mintegral（MTG）应用、版位与单元 ID，由宿主本地填写；SDK 只从此读取，不使用远程 AdUnits JSON。
/// </summary>
[Serializable]
public class SkyMtgLocalConfig
{
    [Header("应用 ID / App Key")]
    public string appId = "";
    public string appKey = "";
    [Header("插屏 IV — 版位 ID / 单元 ID")]
    public string interstitialPlacementId = "";
    public string interstitialUnitId = "";
    [Header("激励 RV — 版位 ID / 单元 ID")]
    public string rewardedPlacementId = "";
    public string rewardedUnitId = "";
}

/// <summary>
/// AdMob 广告位 ID 配置，由宿主在 SkyGameConfigData 资源中填写。
/// 支持多广告位（逗号或分号分隔），用于与 TopOn 并行竞价。
/// </summary>
[Serializable]
public class SkyAdmobLocalConfig
{
    [Header("激励视频广告位（多广告位用逗号分隔）")]
    public string rewardedUnitIds = "";
    [Header("插屏广告位（多广告位用逗号分隔）")]
    public string interstitialUnitIds = "";
    [Header("开屏广告位（多广告位用逗号分隔）")]
    public string splashUnitIds = "";
    [Header("Banner 广告位")]
    public string bannerUnitId = "";
}

/// <summary>
/// UP2（FineRoas）SDK 的 Application 启动参数，由宿主在 SkyGameConfigData 资源中填写；
/// 构建 Android 时会写入 AndroidManifest 的 meta-data，供 Up2Application 在 attachBaseContext 阶段读取。
/// </summary>
[Serializable]
public class SkyUp2LocalConfig
{
    [Header("UP2 开发者平台 AppId（必填）")]
    public string appId = "";
}

[CreateAssetMenu(fileName = "SkyGameConfigData", menuName = "Game Configuration/Game Config")]
public class SkyGameConfigData : ScriptableObject
{
    [Header("统计相关配置")]
    public AnalyticsConfig analyticsConfig;

    // [Header("广告配置")]
    // public string appIdAndroid = "";
    // public string appIdIOS = "";
    [Space(10)]
    [Header("TopOn（TPN）— 本地广告参数")]
    public SkyToponLocalConfig toponLocalConfig = new SkyToponLocalConfig();
    [Header("MTG（Mintegral）— 本地广告参数")]
    public SkyMtgLocalConfig mtgLocalConfig = new SkyMtgLocalConfig();
    [Header("AdMob — 本地广告参数")]
    public SkyAdmobLocalConfig admobLocalConfig = new SkyAdmobLocalConfig();
    [Space(10)]
    [Header("服务器配置")]
    public ServerConfig serverConfig;



    /* [Header("UP2（FineRoas）— Application 启动参数")]
     public SkyUp2LocalConfig up2LocalConfig = new SkyUp2LocalConfig();
*/
    public string GetLoginUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/user/login";
    }

    public string GetIAPReportUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/report/v2/iap";
    }

    public string GetProductListUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/iap/products/list";
    }

    public string GetGameConfigUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/user/get-game-config";
    }

    public string GetCreateOrderUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/order/v2/create";
    }

    public string GetReportOrderRewardUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/order/v2/awarded";
    }

    public string GetQueryOrderUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/order/v2/query";
    }

    public string GetQueryOrderListUrl()
    {
        return serverConfig.BaseUrl + "/hkoc/order/v2/list";
    }

    public string GetAsnUrl()
    {
        return serverConfig.AsnUrl;
    }

    public string GetSSAppId()
    {
#if SSThinkingData
#if UNITY_IOS
            return analyticsConfig.SSAppID_iOS;
#else
            return analyticsConfig.SSAppID_Android;
#endif
#else
        return "";
#endif
    }

    public string GetSSUrl()
    {
#if SSThinkingData
            return analyticsConfig.SSUrl;
#else
        return "";
#endif
    }

    public string GetAdjustId()
    {
#if ADJUST
#if UNITY_IOS
            return analyticsConfig.AdjustToken_iOS;
#else
        return analyticsConfig.AdjustToken_Android;
#endif
#else
        return "";
#endif
    }

    public string GetAdmobAppId()
    {
#if UNITY_IOS
            return analyticsConfig.AdmobAppID_iOS;
#else
        return analyticsConfig.AdmobAppID_Android;
#endif
    }
    public string GetGA_gameKey()
    {
        return analyticsConfig.GA_Game_key;
    }
    public string GetGA_secretKey()
    {
        return analyticsConfig.GA_Secret_key;
    }

    /// <summary>
    /// 获取 AdMob 激励视频广告位列表（解析逗号/分号分隔的字符串）
    /// </summary>
    public List<string> GetAdmobRewardedUnits()
    {
        if (string.IsNullOrEmpty(admobLocalConfig.rewardedUnitIds))
            return new List<string>();
        
        var separators = new char[] { ',', ';', '，', '；' }; // 支持中英文分隔符
        var units = new List<string>();
        foreach (var id in admobLocalConfig.rewardedUnitIds.Split(separators))
        {
            var trimmed = id.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                units.Add(trimmed);
        }
        return units;
    }

    /// <summary>
    /// 获取 AdMob 插屏广告位列表（解析逗号/分号分隔的字符串）
    /// </summary>
    public List<string> GetAdmobInterstitialUnits()
    {
        if (string.IsNullOrEmpty(admobLocalConfig.interstitialUnitIds))
            return new List<string>();
        
        var separators = new char[] { ',', ';', '，', '；' };
        var units = new List<string>();
        foreach (var id in admobLocalConfig.interstitialUnitIds.Split(separators))
        {
            var trimmed = id.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                units.Add(trimmed);
        }
        return units;
    }

    /// <summary>
    /// 获取 AdMob 开屏广告位列表（解析逗号/分号分隔的字符串）
    /// </summary>
    public List<string> GetAdmobSplashUnits()
    {
        if (string.IsNullOrEmpty(admobLocalConfig.splashUnitIds))
            return new List<string>();
        
        var separators = new char[] { ',', ';', '，', '；' };
        var units = new List<string>();
        foreach (var id in admobLocalConfig.splashUnitIds.Split(separators))
        {
            var trimmed = id.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                units.Add(trimmed);
        }
        return units;
    }

    /// <summary>
    /// 获取 AdMob Banner 广告位（单个）
    /// </summary>
    public string GetAdmobBannerUnit()
    {
        return admobLocalConfig.bannerUnitId?.Trim() ?? "";
    }
}