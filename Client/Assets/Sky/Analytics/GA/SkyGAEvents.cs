
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using GameAnalyticsSDK.Wrapper;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Sky
{
    public static class SkyGAEvents
    {

        /// <summary>
        /// 通用事件上报（无参数）
        /// 映射为 GameAnalytics 的 Design 事件
        /// </summary>
        public static void TrackEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            GameAnalytics.NewDesignEvent(FormatEventName(eventName));
        }

        /// <summary>
        /// 通用事件上报（单参数）
        /// 映射为 Design 事件，参数值作为 eventValue（float）
        /// </summary>
        public static void TrackEvent(string eventName, string key, object value)
        {
            if (string.IsNullOrEmpty(eventName)) return;

            // 如果不能转成数字，就按无值上报，但可以通过自定义字段把原始值带过去
            var fields = new Dictionary<string, object>();
            if (key != null && value != null)
                fields[key] = value;
            GameAnalytics.NewDesignEvent(FormatEventName(eventName), fields);
        }

        /// <summary>
        /// 通用事件上报（多参数）
        /// 映射为 Design 事件 + 自定义字段（Custom Event Fields）
        /// </summary>
        public static void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if (string.IsNullOrEmpty(eventName) || properties == null)
            {
                if (!string.IsNullOrEmpty(eventName))
                    TrackEvent(eventName);
                return;
            }

            // 将 properties 作为自定义字段传入
            GameAnalytics.NewDesignEvent(FormatEventName(eventName), properties);
        }

        /// <summary>
        /// 广告收益事件上报
        /// 说明：GameAnalytics 原生 Ad 事件不支持 revenue，因此：
        /// - 仍调用 NewAdEvent 记录广告展示（不带金额）
        /// - 额外上报一个 Design 事件 ad_impression_with_revenue 携带金额信息
        /// </summary>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0)
        {
            if (revenue < 0)
            {
                LogTool.DebugLog($"[GA] ad_impression skipped: revenue={revenue} < 0, reporting ad_impression_error");
                var errorProps = new Dictionary<string, object>
                {
                    { "ad_platform", platform },
                    { "ad_source", network },
                    { "ad_unit_name", unit },
                    { "ad_format", format },
                    { "value", revenue },
                    { "currency", "USD" },
                };
                if (!string.IsNullOrEmpty(pos))
                    errorProps["position"] = pos;
                TrackEvent("ad_impression_error", errorProps);
                return;
            }

            // 1. 原生 GA Ad 事件（无 revenue）
            GAAdType adType = GAAdType.Banner;
            switch (format.ToLower())
            {
                case "rewarded": adType = GAAdType.RewardedVideo; break;
                case "interstitial": adType = GAAdType.Interstitial; break;
                case "banner": adType = GAAdType.Banner; break;
                default: adType = GAAdType.Banner; break;
            }
            // 使用 Show 动作（或可以区分 Clicked，这里只做展示）
            GameAnalytics.NewAdEvent(GAAdAction.Show, adType, network, unit);

            // 2. 自定义 Design 事件携带收入详情
            var revenueProps = new Dictionary<string, object>
            {
                { "ad_platform", platform },
                { "ad_source", network },
                { "ad_unit_name", unit },
                { "ad_format", format },
                { "value", revenue },
                { "currency", "USD" },
            };
            if (!string.IsNullOrEmpty(pos))
                revenueProps["position"] = pos;
            if (formatNumber > 0)
                revenueProps["format_number"] = formatNumber;
            if (positionNumber > 0)
                revenueProps["position_number"] = positionNumber;

            TrackEvent("ad_impression", revenueProps);
        }

        /// <summary>
        /// 事件名清洗：只保留字母、数字、中文，其余变下划线，合并连续下划线，去首尾，截断49字符。
        /// 完全复用 ThinkingData 的逻辑。
        /// </summary>
        public static string FormatEventName(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return eventName;

            string formatted = Regex.Replace(eventName, @"[^a-zA-Z0-9\u4e00-\u9fa5]", "_");
            formatted = Regex.Replace(formatted, @"_+", "_");
            formatted = formatted.Trim('_');
            if (formatted.Length > 49)
                formatted = formatted.Substring(0, 49);
            return formatted;
        }
    }
}