
using CoreLite;
using GameAnalyticsSDK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public enum ADLimitType
{
    //Daily_Limit,//若为每日频次限制触发频次导致限制，传Daily_Limit//当前规则无此参数，不上报
    First_Time,//若为首次时间间隔限制触发频次导致限制，传First_Time
    Skip_Level,//若为首次频次间隔限制触发频次导致限制，传Skip_Level
    Interval_Time,//若为非首次时间间隔限制触发频次导致限制，传Interval_Time
    Over_Level,//若为非首次频次间隔限制触发频次导致限制，传Over_Level
    Not
}
namespace Sky
{
    public static class LauncherEvents
    {
        static string Loading_Page_Show = "Loading_Page_Show";
        static string Trigger_AD = "Trigger_AD";
        static string AD_Check_Limit = "AD_Check_Limit";
        /*    static string Limit_Reached = "Limit_Reached";
            static string Limit_Reason = "Limit_Reason";*/
        static string AD_Check_Ready = "AD_Check_Ready";
        static string ad_impression = "ad_impression";
        /*    static string AD_Type = "AD_Type";
            static string Position = "Position";
            static string AD_Ready = "AD_Ready";*/

        // 1. 定义 Launcher 包支持的 5 个事件名（建议加上 StringComparer.Ordinal 提升比对性能）
        private static readonly HashSet<string> LauncherAllowedEvents = new HashSet<string>(StringComparer.Ordinal)
    {
        Loading_Page_Show,
        Trigger_AD,
        AD_Check_Limit,
        AD_Check_Ready,
        ad_impression
        // 请替换为实际的 5 个事件名称
    };
        #region 打点上报
        public static void TrackEvent(string eventName, Dictionary<string, object> parameters)
        {
            // 2. 核心过滤逻辑：如果不在白名单，直接 return，不做任何处理
            if (!LauncherAllowedEvents.Contains(eventName))
            {
                return;
            }

            // 只有通过过滤的 5 个事件，才执行昂贵的序列化和 JNI 调用
#if UNITY_ANDROID && !UNITY_EDITOR
        string json = null;
            if (parameters != null)
            {
                json = JsonUtility.ToJson(parameters); // 按需序列化
            }
            try
            {
            if (CoreLauncherMgr.Bridge != null)
            {
                CoreLauncherMgr.Bridge.CallStatic("TrackEvent", eventName, json);
            }
        }
            catch (Exception e)
            {
                CoreLogUtils.Error("[CoreLauncherMgr] TrackEvent failed: " + e.Message);
            }
#endif
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            // 如果不能转成数字，就按无值上报，但可以通过自定义字段把原始值带过去
            var fields = new Dictionary<string, object>();
            if (key != null && value != null)
                fields[key] = value;
            TrackEvent(eventName, fields);
        }
        #endregion
        public static void LoadingShowEvent()
        {
            TrackEvent(Loading_Page_Show, null);
        }
        public static void TriggerADEvent(string position, string ad_type)
        {

            TrackEvent(Trigger_AD, new Dictionary<string, object>()
                    {
                        {"Position", position },
                        {"AD_Type",ad_type }
                    });
        }
        /// <summary>
        /// 广告检查是否达到频次限制或间隔时间限制（跟随展示,native和开屏无需上报此埋点）
        /// </summary>
        /// <param name="position"></param>
        /// <param name="ad_type"></param>
        /// <param name="_limit_Reached">本次广告检查的结果，是否达到了‘禁止展示’的触发条件</param>
        /// <param name="type">达到限制的原因</param>
        public static void ADCheckLimitEvent(string position, string ad_type, bool _limit_Reached, ADLimitType type)
        {
            TrackEvent(AD_Check_Limit, new Dictionary<string, object>()
                    {
                        {"Position", position },
                        {"AD_Type",ad_type },
                        {"Limit_Reached",_limit_Reached },
                        {"Limit_Reason",type.ToString()}
                    });
        }
        public static void ADCheckReadyEvent(string position, string ad_type, bool isReady)
        {
            TrackEvent(AD_Check_Ready, new Dictionary<string, object>()
                    {
                        {"Position", position },
                        {"AD_Type",ad_type },
                        {"AD_Ready",isReady }
                    });
        }

        public static void AdImpressionEvent(string position, string ad_type)
        {
            string format = position;
            int formatNumber = CoreAdsPrefs.GetAndIncrementFormatPlayCount(format);
            int positionNumber = CoreAdsPrefs.GetAndIncrementPositionPlayCount(format);
            TrackEvent(ad_impression, new Dictionary<string, object>()
                    {
                        {"Position", position },
                        {"AD_Type",ad_type },
                        {"position_number",positionNumber},
                        {"format_number",formatNumber}
                    });
        }

    }
}
