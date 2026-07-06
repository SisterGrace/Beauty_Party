using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{
    /// <summary>
    /// 广告加载类型，用于区分事件名：Native / 插屏(IV) / 激励视频(RV)
    /// </summary>
    public enum AdLoadType
    {
        Native,
        Interstitial,
        Reward,
        Splash,
        NativeFullscreen,
        NativeSmall,
        Banner
    }

    public static class CoreAdsEvents
    {
        private static readonly string[] StartLoadEventNames =
        {
            "Native_Start_Load", "IV_Start_Load", "RV_Start_Load", "SP_Start_Load",
            "NativeFullscreen_Start_Load", "NativeSmall_Start_Load", "BN_Start_Load"
        };
        private static readonly string[] FailLoadedEventNames =
        {
            "Native_Fail_Loaded", "IV_Fail_Loaded", "RV_Fail_Loaded", "SP_Fail_Loaded",
            "NativeFullscreen_Fail_Loaded", "NativeSmall_Fail_Loaded", "BN_Fail_Loaded"
        };
        private static readonly string[] LoadedEventNames =
        {
            "Native_Loaded", "IV_Loaded", "RV_Loaded", "SP_Loaded",
            "NativeFullscreen_Loaded", "NativeSmall_Loaded", "BN_Loaded"
        };
        private static readonly string[] ClickEventNames =
        {
            "Native_Click", "IV_Click", "RV_Click", "SP_Click",
            "NativeFullscreen_Click", "NativeSmall_Click", "BN_Click"
        };

        /// <summary>
        /// 统一上报广告开始加载（通过 loadType 区分 Native_Start_Load / IV_Start_Load / RV_Start_Load）
        /// </summary>
        public static void LogStartLoadEvent(string unit, string platform, AdLoadType loadType)
        {
            CoreAnalyticsMgr.TrackEvent(StartLoadEventNames[(int)loadType]);
           /* CoreAnalyticsMgr.TrackEvent(StartLoadEventNames[(int)loadType], new Dictionary<string, object>
            {
                {"unitId", unit},
                {"ad_platform", platform}
            });*/
        }

        /// <summary>
        /// 统一上报广告加载失败（通过 loadType 区分事件名，isMTGBidding 仅对插屏加后缀 _MTGBidding）
        /// </summary>
        public static void LogFailToLoadEvent(string unit, string platform, string error, float time, AdLoadType loadType, bool isMTGBidding = false)
        {
            string eventName = FailLoadedEventNames[(int)loadType];
            if (loadType == AdLoadType.Interstitial && isMTGBidding)
                eventName = "IV_Fail_Loaded_MTGBidding";
            CoreAnalyticsMgr.TrackEvent(eventName, new Dictionary<string, object>
            {
                //{"unitId", unit},
                //{"ad_platform", platform},
                //{"load_fail_reason", CoreThinkingDataEvents.FormatEventName(error)},
                {"Time", Mathf.CeilToInt(time)}
            });
        }

        /// <summary>
        /// 统一上报广告加载成功（通过 loadType 区分事件名，isMTGBidding 仅对插屏加后缀 _MTGBidding）
        /// </summary>
        public static void LogLoadedEvent(string adId, string source, string adPlatform, float time, AdLoadType loadType, string unit, double revenue,bool isMTGBidding = false)
        {
            string eventName = LoadedEventNames[(int)loadType];
            if (loadType == AdLoadType.Interstitial && isMTGBidding)
                eventName = "IV_Loaded_MTGBidding";
            CoreAnalyticsMgr.TrackEvent(eventName, new Dictionary<string, object>
            {
                {"ad_id", adId},//第三方广告ID
                //{"unitId", unit},//topon的广告位ID
                {"ad_ecpm", revenue*1000},//ecpm
                {"ad_platform", adPlatform},//聚合平台
                {"ad_source", source},
                {"Time", Mathf.CeilToInt(time)}//耗时
            });
        }

        public static void LogMTGBiddingFailToLoadEvent(string unit, string platform, string error, float time, bool isIv)
        {
            LogFailToLoadEvent(unit, platform, error, time, isIv ? AdLoadType.Interstitial : AdLoadType.Reward, true);
        }

        public static void LogMTGBiddingLoadedEvent(string adPlatform, string platform, string adId, float time, bool isIv, double revenue, string unit)
        {
            LogLoadedEvent(adId, platform, adPlatform, time, isIv ? AdLoadType.Interstitial : AdLoadType.Reward, unit,revenue,true);
        }

        /// <summary>
        /// 统一上报激励视频展示成功事件 RV_Show。各广告聚合在 RV 展示成功回调中调用，集中维护字段。
        /// </summary>
        public static void LogRvShowEvent(string position, string adId, string adSource, double revenue, int adRewardNum, int firstAdRewardTime, string adPlatform, string unitId)
        {
            CoreAnalyticsMgr.TrackEvent("RV_Show", new Dictionary<string, object>
            {
                {"Position", position ?? ""},
                {"ad_platform", "TPN"},
                {"ad_source", adSource ?? ""},
                {"ad_id", adId ?? ""},
                {"ad_ecpm",revenue*1000 },
                //{"ad_reward_num", adRewardNum},
                //{"first_ad_reward_time", firstAdRewardTime},
                //{"unitId", unitId ?? ""}
            });
        }

        /// <summary>
        /// 统一上报广告点击事件（按 loadType 区分 Native_Click / IV_Click / RV_Click / SP_Click 等）。
        /// 各广告聚合（Admob、AppLovin、LevelPlay、Pangle、TPN、Mintegral、Kwai）在点击回调中调用。
        /// 参数命名与 CoreAnalyticsMgr.TrackAdImpressionEvent 对齐：revenue/network/unit/placement/platform/pos。
        /// </summary>
        public static void LogClickEvent(AdLoadType loadType, double revenue, string pos, string placement, string network, string platform, string unit)
        {
            return;
            CoreAnalyticsMgr.TrackEvent(ClickEventNames[(int)loadType], new Dictionary<string, object>
            {
                {"Position", pos ?? ""},
                {"ad_id", placement ?? ""},
                {"ad_platform", network ?? ""},
                //{"ad_platform", platform ?? ""},
                //{"ad_unit_name", unit ?? ""},
                {"ecpm", revenue}
            });
        }
    }
}
