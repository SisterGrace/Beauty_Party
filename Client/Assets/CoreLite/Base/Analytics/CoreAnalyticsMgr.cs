using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AdjustSdk;
using ThinkingData.Analytics;
using UnityEngine;

namespace CoreLite
{
    public static class CoreAnalyticsMgr
    {
        public static void Init()
        {
            CoreFirebaseMgr.Instance.Init();
            CoreThinkingDataMgr.Instance.Init();
#if USE_XMSDK
            CoreXmSdkMgr.Instance.OnAnalyticsReady();
#endif
        }
        
        #region  在线参数
        public static string GetConfigStr(string key, string defaultValue = "")
        {
            return CoreFirebaseMgr.Instance.GetConfigStr(key, defaultValue);   
        }

        public static int GetConfigInt(string key, int defaultValue = 0)
        {
            return CoreFirebaseMgr.Instance.GetConfigInt(key, defaultValue);
        }

        public static bool GetConfigBool(string key, bool defaultValue = false)
        {
            return CoreFirebaseMgr.Instance.GetConfigBool(key, defaultValue);
        }
        #endregion

        #region  事件上报
        public static void TrackEvent(string eventName)
        {
            CoreFirebaseEvents.TrackEvent(eventName);
            CoreAdjustEvents.TrackEvent(eventName);
            CoreThinkingDataEvents.TrackEvent(eventName);
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            CoreFirebaseEvents.TrackEvent(eventName, key, value);
            CoreAdjustEvents.TrackEvent(eventName);
            CoreThinkingDataEvents.TrackEvent(eventName, key, value);
            
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> dict)
        {
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

            CoreFirebaseEvents.TrackEvent(eventName, dict);
            CoreAdjustEvents.TrackEvent(eventName);
            CoreThinkingDataEvents.TrackEvent(eventName, dict);
        }

        #endregion

        #region  广告事件上报
        /// <param name="pos">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="formatNumber">第几次展示该格式的广告（1、2、3…），每个广告格式独立统计</param>
        /// <param name="positionNumber">第几次展示该点位的广告（1、2、3…），每个广告触发点位独立统计</param>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string placement, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0)
        {
            CoreLogUtils.Debug("TrackAdImpressionEvent"+" revenue:"+revenue+" network:"+network+" unit:"+unit+" placement:"+placement+" format:"+format+" platform:"+platform+" pos:"+pos+" format_number:"+formatNumber+" position_number:"+positionNumber);
            CoreFirebaseEvents.TrackAdImpressionEvent(revenue, network, unit, format, platform, pos, formatNumber, positionNumber);
            CoreAdjustEvents.TrackAdImpressionEvent(revenue, network, unit, placement);
            CoreThinkingDataEvents.TrackAdImpressionEvent(revenue, network, unit, format, platform, pos, formatNumber, positionNumber);
#if USE_XMSDK
            CoreXmSdkMgr.Instance.TrackAdImpression(revenue, format, platform, unit, network, placement);
#endif
        }

        public static void TrackAdClickEvent(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId)
        {
#if USE_XMSDK
            CoreXmSdkMgr.Instance.TrackAdClick(revenue, adType, platformName, adUnitId, networkName, networkPlacementId);
#endif
        }

        #endregion

        #region  公共事件属性/用户属性
        public static void UserSet(Dictionary<string, object> dic,bool setOnce = false)
        {
            CoreThinkingDataMgr.Instance.UserSet(dic, setOnce);
        }

        public static void SetSuperProperties(Dictionary<string, object> superProperties)
        {
            CoreThinkingDataMgr.Instance.SetSuperProperties(superProperties);
        }
        #endregion

        #region  登录/登出
        public static void SetLoginAccount(string account)
        {
            CoreThinkingDataMgr.Instance.SetLoginAccount(account);
        }

        public static void Logout()
        {
            CoreThinkingDataMgr.Instance.Logout();
        }
        #endregion

    }
}

