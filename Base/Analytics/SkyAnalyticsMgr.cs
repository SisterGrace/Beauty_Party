
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sky
{
    public static class SkyAnalyticsMgr
    {
        //static bool AnalyticsOver = false;
        public static void Init()
        {
            SkyFirebaseMgr.Instance.Init();
            #if SSThinkingData
            SkyThinkingDataMgr.Instance.Init();
#endif
#if USE_XMSDK
            SkyXmSdkMgr.Instance.OnAnalyticsReady();
#endif
#if GA
            Sky.SkyGAMgr.Instance.Init();
#endif
            // 【新增】SolarEngine（热云）最小接入：与 GA 并列初始化
            Sky.SkySolarEngineMgr.Instance.Init();
        }
        
        #region  在线参数
        public static string GetConfigStr(string key, string defaultValue = "")
        {
#if LAUNCHER_AD
            return SkyLauncherMgr.Instance.GetAdGameConfig();
#endif
            return SkyFirebaseMgr.Instance.GetConfigStr(key, defaultValue);   
        }

        public static int GetConfigInt(string key, int defaultValue = 0)
        {
            return SkyFirebaseMgr.Instance.GetConfigInt(key, defaultValue);
        }

        public static bool GetConfigBool(string key, bool defaultValue = false)
        {
            return SkyFirebaseMgr.Instance.GetConfigBool(key, defaultValue);
        }
#endregion

        #region  事件上报
        public static void TrackEvent(string eventName)
        {
            //if (!AnalyticsOver) return;
            if (!BoolUp(eventName)) return;
            SkyLogUtils.Debug("[SkyAnalyticsMgr] eventName:" + eventName);
            SkyFirebaseEvents.TrackEvent(eventName);
            Sky.SkyGAEvents.TrackEvent(eventName);
            Sky.SkySolarEngineEvents.TrackEvent(eventName);
#if LAUNCHER_AD
            Sky.LauncherEvents.TrackEvent(eventName,null);
#endif
#if ADJUST
            SkyAdjustEvents.TrackEvent(eventName);
#endif
#if SSThinkingData
            SkyThinkingDataEvents.TrackEvent(eventName);
#endif
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            //if (!AnalyticsOver) return;
            if (!BoolUp(eventName)) return;
            SkyLogUtils.Debug("[SkyAnalyticsMgr] eventName:" + eventName);
            SkyFirebaseEvents.TrackEvent(eventName, key, value);
            Sky.SkyGAEvents.TrackEvent(eventName, key, value);
            Sky.SkySolarEngineEvents.TrackEvent(eventName, key, value);
#if LAUNCHER_AD
            Sky.LauncherEvents.TrackEvent(eventName,null);
#endif
#if ADJUST
            SkyAdjustEvents.TrackEvent(eventName);
#endif
#if SSThinkingData
            SkyThinkingDataEvents.TrackEvent(eventName, key, value);
#endif
        }
        static bool BoolUp(string eventName)
        {
            if (eventName.Contains("Up2SDK") || eventName.Contains("IAP") || eventName.Contains("RV_AchieveShow_Match") || eventName.Contains("Parameter_Connect")
               || eventName.Contains("User_") || eventName.Contains("BN_") || eventName.Contains("RV_Button_Show") || eventName.Contains("SP_Winner")|| eventName.Contains("SP_ShowSkip") 
               ) return false;
#if LAUNCHER_AD
            if (eventName.Contains("RV_") || eventName.Contains("IV_") || eventName.Contains("BN_") || eventName.Contains("SP_"))
            {
                return false;
            }
#endif
            return true;
        }
        public static void TrackEvent(string eventName, Dictionary<string, object> dict)
        {
            //if (!AnalyticsOver) return;
            if (!BoolUp(eventName)) return;
            SkyLogUtils.Debug("[SkyAnalyticsMgr] eventName:" + eventName);
            if (dict == null)
            {
                dict = new Dictionary<string, object>();
            }

            var keys = dict.Keys.ToList();
            foreach (var key in keys)
            {
                if (dict[key] == null)
                {
                    dict[key] = "";
                }
            }
            Sky.SkyGAEvents.TrackEvent(eventName, dict);
            Sky.SkySolarEngineEvents.TrackEvent(eventName, dict);
            SkyFirebaseEvents.TrackEvent(eventName, dict);
#if LAUNCHER_AD
            Sky.LauncherEvents.TrackEvent(eventName, dict);
#endif
#if ADJUST
            SkyAdjustEvents.TrackEvent(eventName);
#endif
#if SSThinkingData
            SkyThinkingDataEvents.TrackEvent(eventName, dict);
#endif
        }

        #endregion
        #region  广告事件上报
        /// <summary>
        /// 广告展示收益上报（统一入口）。
        /// 由 TopOn 专用收入回调 TopOnRevenueReporter 调用，分发给所有平台。
        /// </summary>
        /// <param name="revenue">单次展示收益</param>
        /// <param name="network">广告平台名（如 "admob" / "applovin"），已由 TopOnRevenueReporter 映射好</param>
        /// <param name="unit">三方广告位 ID（network_placement_id）</param>
        /// <param name="placement">TopOn 广告位 ID</param>
        /// <param name="format">广告类型（banner / inter / reward / splash）</param>
        /// <param name="platform">聚合平台（"tpn"）</param>
        /// <param name="pos">展示点位，默认空</param>
        /// <param name="formatNumber">第几次展示该格式的广告</param>
        /// <param name="positionNumber">第几次展示该点位的广告</param>
        /// <param name="currency">货币单位，从 TopOn 回调取，取不到由调用方决定是否跳过</param>
        /// <param name="customMediationPlatform">热力引擎自定义归类（"APL" / "IS"），null 时由 SkySolarEngineEvents 内部回退</param>
        public static void TrackAdImpressionEvent(
            double revenue,
            string network,
            string unit,
            string placement,
            string format,
            string platform,
            string pos = "",
            int formatNumber = 0,
            int positionNumber = 0,
            string currency = "USD",
            string customMediationPlatform = null)
        {
            SkyLogUtils.Debug("TrackAdImpressionEvent"
                + " revenue:" + revenue
                + " network:" + network
                + " unit:" + unit
                + " placement:" + placement
                + " format:" + format
                + " platform:" + platform
                + " pos:" + pos
                + " format_number:" + formatNumber
                + " position_number:" + positionNumber
                + " currency:" + currency
                + " customMediationPlatform:" + customMediationPlatform);

            // Firebase
            SkyFirebaseEvents.TrackAdImpressionEvent(
                revenue, network, unit, format, platform, pos, formatNumber, positionNumber);

            // GA
            Sky.SkyGAEvents.TrackAdImpressionEvent(
                revenue, network, unit, format, platform, pos, formatNumber, positionNumber);

            // 热力引擎：传 customMediationPlatform，让 APL / IS 归类生效
            Sky.SkySolarEngineEvents.TrackAdImpressionEvent(
                revenue, network, unit, format, platform, pos, formatNumber, positionNumber,
                currency, customMediationPlatform);

#if ADJUST
    // Adjust
    SkyAdjustEvents.TrackAdImpressionEvent(revenue, network, unit, placement);
#endif

#if SSThinkingData
    // ThinkingData
    SkyThinkingDataEvents.TrackAdImpressionEvent(
        revenue, network, unit, format, platform, pos, formatNumber, positionNumber);
#endif

#if USE_XMSDK
    // 小米 SDK
    SkyXmSdkMgr.Instance.TrackAdImpression(
        revenue, format, platform, unit, network, placement);
#endif
        }

        public static void TrackAdClickEvent(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId)
        {
#if USE_XMSDK
            SkyXmSdkMgr.Instance.TrackAdClick(revenue, adType, platformName, adUnitId, networkName, networkPlacementId);
#endif
        }

#endregion

        #region  公共事件属性/用户属性
        public static void UserSet(Dictionary<string, object> dic,bool setOnce = false)
        {
#if SSThinkingData
            SkyThinkingDataMgr.Instance.UserSet(dic, setOnce);
#endif
        }

        public static void SetSuperProperties(Dictionary<string, object> superProperties)
        {
#if SSThinkingData
            SkyThinkingDataMgr.Instance.SetSuperProperties(superProperties);
#endif
        }
#endregion

        #region  登录/登出
        public static void SetLoginAccount(string account)
        {
#if SSThinkingData
            SkyThinkingDataMgr.Instance.SetLoginAccount(account);
#endif
        }

        public static void Logout()
        {
#if SSThinkingData
            SkyThinkingDataMgr.Instance.Logout();
#endif
        }
#endregion

    }
}

