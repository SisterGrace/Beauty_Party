
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
        /// <param name="pos">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="formatNumber">第几次展示该格式的广告（1、2、3…），每个广告格式独立统计</param>
        /// <param name="positionNumber">第几次展示该点位的广告（1、2、3…），每个广告触发点位独立统计</param>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string placement, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0, string currency = "USD")
        {
            SkyLogUtils.Debug("TrackAdImpressionEvent"+" revenue:"+revenue+" network:"+network+" unit:"+unit+" placement:"+placement+" format:"+format+" platform:"+platform+" pos:"+pos+" format_number:"+formatNumber+" position_number:"+positionNumber+" currency:"+currency);
            
            // 上报买量消耗成本事件（热云）
            SkyAnalyticsMgr.TrackEvent("track_ad_spend", new Dictionary<string, object>
            {
                { "ad_network", Sky.SkySolarEnginePrefs.HasAttribution ? Sky.SkySolarEnginePrefs.Network : "" }
            });
            
            SkyFirebaseEvents.TrackAdImpressionEvent(revenue, network, unit, format, platform, pos, formatNumber, positionNumber);
            Sky.SkyGAEvents.TrackAdImpressionEvent(revenue, network, unit, format, platform, pos, formatNumber, positionNumber);
            Sky.SkySolarEngineEvents.TrackAdImpressionEvent(revenue, network, unit, format, platform, pos, formatNumber, positionNumber, currency);
#if ADJUST
            SkyAdjustEvents.TrackAdImpressionEvent(revenue, network, unit, placement);
#endif
#if SSThinkingData
            SkyThinkingDataEvents.TrackAdImpressionEvent(revenue, network, unit, format, platform, pos, formatNumber, positionNumber);
#endif
#if USE_XMSDK
            SkyXmSdkMgr.Instance.TrackAdImpression(revenue, format, platform, unit, network, placement);
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

