using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// H5 广告格式枚举，对应上报的 ad_format（Interstitial 或 Rewarded）。
    /// </summary>
    public enum SpinH5AdType
    {
        Interstitial,
        Rewarded
    }

    /// <summary>
    /// H5 广告事件上报。与互动页面广告事件表对应：interactive_ad_fail / interactive_ad_click / interactive_ad_close / interactive_reward_reach / interactive_ad_show。
    /// ad_platform、ad_source 固定传 Okspin 或 SpinGO（由 SkyH5AdPlatform 枚举转换）。
    /// </summary>
    public static class SkyH5AdsEvents
    {
        private const string CurrencyUsd = "USD";
        private const string EventInit = "Init";
        private const string EventShowRewardedAd = "ShowRewardedAd";
        private const string EventShowInterstitialAd = "ShowInterstitialAd";

        private static void AddCommonParams(Dictionary<string, object> dict, SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position)
        {
            string platformStr = platform.ToString();
            dict["ad_platform"] = platformStr;
            dict["ad_source"] = platformStr;
            dict["ad_unit_name"] = adUnitNameUrl ?? string.Empty;
            dict["ad_format"] = adFormat.ToString();
            dict["currency"] = CurrencyUsd;
            dict["position"] = position ?? string.Empty;
        }

        /// <summary>
        /// 互动页面的广告展示失败。事件名：interactive_ad_fail。
        /// </summary>
        public static void InvokeAdFail(SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position, string reason)
        {
            var dict = new Dictionary<string, object>();
            AddCommonParams(dict, adFormat, platform, adUnitNameUrl, position);
            dict["reason"] = reason ?? string.Empty;
            SkySdkMgr.TrackEvent("interactive_ad_fail", dict);
        }

        /// <summary>
        /// 互动页面的广告点击。事件名：interactive_ad_click。
        /// </summary>
        public static void InvokeAdClick(SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position, int number)
        {
            var dict = new Dictionary<string, object>();
            AddCommonParams(dict, adFormat, platform, adUnitNameUrl, position);
            dict["number"] = number;
            SkySdkMgr.TrackEvent("interactive_ad_click", dict);
        }

        /// <summary>
        /// 互动页面的广告关闭。事件名：interactive_ad_close。
        /// </summary>
        public static void InvokeAdClose(SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position, int number)
        {
            var dict = new Dictionary<string, object>();
            AddCommonParams(dict, adFormat, platform, adUnitNameUrl, position);
            dict["number"] = number;
            SkySdkMgr.TrackEvent("interactive_ad_close", dict);
        }

        /// <summary>
        /// 互动页面触发激励生效。事件名：interactive_reward_reach。
        /// </summary>
        public static void InvokeRewardReach(SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position, int number, float passTime)
        {
            var dict = new Dictionary<string, object>();
            AddCommonParams(dict, adFormat, platform, adUnitNameUrl, position);
            dict["number"] = number;
            dict["pass_time"] = Mathf.CeilToInt(passTime);
            SkySdkMgr.TrackEvent("interactive_reward_reach", dict);
        }

        /// <summary>
        /// 互动页面广告展示成功。事件名：interactive_ad_show。
        /// </summary>
        public static void InvokeAdShow(SpinH5AdType adFormat, SkyH5AdPlatform platform, string adUnitNameUrl, string position, int number, double value)
        {
            var dict = new Dictionary<string, object>();
            AddCommonParams(dict, adFormat, platform, adUnitNameUrl, position);
            dict["number"] = number;
            dict["value"] = value;
            SkySdkMgr.TrackEvent("interactive_ad_show", dict);
        }

        /// <summary>
        /// 上报 H5 广告 Init 调用。对应 SpinH5Ad.Init(platform)。
        /// </summary>
        /// <param name="platform">聚合平台（Okspin 或 SpinGO），必传</param>
        public static void InvokeInit(SkyH5AdPlatform platform)
        {
            SkySdkMgr.TrackEvent(EventInit, new Dictionary<string, object>
            {
                { "SpinH5AdPlatform", platform.ToString() }
            });
        }

        /// <summary>
        /// 上报 H5 激励视频展示调用。对应 SpinH5Ad.ShowRewardedAd(url, rewardAfter, startOnPageLoaded, platform)。
        /// </summary>
        /// <param name="url">广告 URL</param>
        /// <param name="platform">聚合平台（Okspin 或 SpinGO），必传</param>
        /// <param name="rewardAfter">达到奖励所需秒数，默认 15</param>
        /// <param name="startOnPageLoaded">是否在页面加载完成后开始，默认 true</param>
        public static void InvokeShowRewardedAd(string url, SkyH5AdPlatform platform, int rewardAfter = 15, bool startOnPageLoaded = true)
        {
            SkySdkMgr.TrackEvent(EventShowRewardedAd, new Dictionary<string, object>
            {
                { "url", url ?? string.Empty },
                { "rewardAfter", rewardAfter },
                { "startOnPageLoaded", startOnPageLoaded },
                { "SpinH5AdPlatform", platform.ToString() }
            });
        }

        /// <summary>
        /// 上报 H5 插屏广告展示调用。对应 SpinH5Ad.ShowInterstitialAd(url, closeDelay, startOnPageLoaded, platform)。
        /// </summary>
        /// <param name="url">广告 URL</param>
        /// <param name="platform">聚合平台（Okspin 或 SpinGO），必传</param>
        /// <param name="closeDelay">关闭延迟秒数，默认 5</param>
        /// <param name="startOnPageLoaded">是否在页面加载完成后开始，默认 true</param>
        public static void InvokeShowInterstitialAd(string url, SkyH5AdPlatform platform, int closeDelay = 5, bool startOnPageLoaded = true)
        {
            SkySdkMgr.TrackEvent(EventShowInterstitialAd, new Dictionary<string, object>
            {
                { "url", url ?? string.Empty },
                { "closeDelay", closeDelay },
                { "startOnPageLoaded", startOnPageLoaded },
                { "SpinH5AdPlatform", platform.ToString() }
            });
        }
    }
}
