
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if SSThinkingData
using ThinkingData.Analytics;
#endif
using UnityEngine;

namespace Sky
{
    public static class SkyThinkingDataEvents
    {
#if SSThinkingData
        public static void TrackEvent(string eventName)
        {
            if(SkyThinkingDataMgr.IsAppIdOrServerUrlEmpty()) return;
            TDAnalytics.Track(FormatEventName(eventName));
        }

        public static void TrackEvent(string eventName, string key1, object value1)
        {
            if(SkyThinkingDataMgr.IsAppIdOrServerUrlEmpty()) return;
            var extraInfo = new Dictionary<string, object>();
            if (key1 != null && value1 != null)
            {
                extraInfo[key1] = value1;
            }

            TrackEvent(FormatEventName(eventName), extraInfo);
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if(SkyThinkingDataMgr.IsAppIdOrServerUrlEmpty()) return;
            TDAnalytics.Track(FormatEventName(eventName), properties);
        }

        /// <param name="pos">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="formatNumber">第几次展示该格式的广告（1、2、3…），每个广告格式独立统计</param>
        /// <param name="positionNumber">第几次展示该点位的广告（1、2、3…），每个广告触发点位独立统计</param>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0)
        {
            
            if (SkyThinkingDataMgr.IsAppIdOrServerUrlEmpty()) return;
            if (revenue < 0)
            {
                SkyLogUtils.Debug($"[ThinkingData] ad_impression skipped: revenue={revenue} < 0, reporting ad_impression_error instead");
                var errorProperties = new Dictionary<string, object>
                {
                    { "ad_platform", platform },
                    { "ad_source", network },
                    { "ad_unit_name", unit },
                    { "ad_format", format },
                    { "value", revenue },
                    { "currency", "USD" },
                };
                if (!string.IsNullOrEmpty(pos))
                    errorProperties["position"] = pos;
                TrackEvent("ad_impression_error", errorProperties);
                return;
            }

            var properties = new Dictionary<string, object>
            {
                { "ad_platform", platform },
                { "ad_source", network },
                { "ad_unit_name", unit },
                { "ad_format", format },
                { "value", revenue },
                { "currency", "USD" },
            };
            if (!string.IsNullOrEmpty(pos))
                properties["position"] = pos;
            if (formatNumber > 0)
                properties["format_number"] = formatNumber;
            if (positionNumber > 0)
                properties["position_number"] = positionNumber;
            TrackEvent("ad_impression", properties);
        }
#endif
        public static string FormatEventName(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
                return eventName;
            
            // 替换所有非字母、数字、中文字符为下划线
            string formatted = Regex.Replace(eventName, @"[^a-zA-Z0-9\u4e00-\u9fa5]", "_");
            
            // 将连续多个下划线合并为一个
            formatted = Regex.Replace(formatted, @"_+", "_");
            
            // 去除首尾的下划线
            formatted = formatted.Trim('_');
            
            // 只保留49个字符
            if (formatted.Length > 49)
            {
                // 确保截断后最后一个字符不是下划线
                formatted = formatted.Substring(0, 49);
            }
            
            return formatted;
        }
    }
}

