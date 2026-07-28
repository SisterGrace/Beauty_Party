using System.Collections;
using System.Collections.Generic;
#if FIRBASE
using Firebase.Analytics;
using Sky;

#endif
using UnityEngine;

namespace CoreLite
{
    public static class CoreFirebaseEvents
    {
        public static void TrackEvent(string eventName)
        {
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
#if FIRBASE
            var safeName = CoreStringUtils.SanitizeEventName(eventName, allowUnicode: true, maxLength: 40);
            FirebaseAnalytics.LogEvent(safeName);
#endif

        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
#if FIRBASE
            var safeName = CoreStringUtils.SanitizeEventName(eventName, allowUnicode: true, maxLength: 40);
            FirebaseAnalytics.LogEvent(safeName, key, value.ToString());
#endif
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> dict)
        {
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
#if FIRBASE
            List<Parameter> parmlist = new List<Parameter>();
            foreach (var item in dict)
            {
                if (item.Value == null)
                    continue;
                // 根据实际类型分别处理
                if (item.Value is int intVal)
                    parmlist.Add(new Parameter(item.Key, intVal));
                else if (item.Value is long longVal)
                    parmlist.Add(new Parameter(item.Key, longVal));
                else if (item.Value is double doubleVal)
                    parmlist.Add(new Parameter(item.Key, doubleVal));
                else if (item.Value is float floatVal)
                    parmlist.Add(new Parameter(item.Key, floatVal));
                else
                    parmlist.Add(new Parameter(item.Key, item.Value.ToString()));
            }
            var safeName = CoreStringUtils.SanitizeEventName(eventName, allowUnicode: true, maxLength: 40);
            FirebaseAnalytics.LogEvent(safeName, parmlist.ToArray());
#endif
        }

        /// <param name="pos">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="formatNumber">第几次展示该格式的广告（1、2、3…），每个广告格式独立统计</param>
        /// <param name="positionNumber">第几次展示该点位的广告（1、2、3…），每个广告触发点位独立统计</param>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0)
        {
            //var source = CoreAdImpressionSource.GetSource("revenue_fir","ad_impression");
            var source = "ad_impression";
            if (revenue < 0)
            {
                Sky.LogTool.DebugLog($"[Firebase] ad_impression skipped: revenue={revenue} < 0, reporting ad_impression_error");
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
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
#if FIRBASE
            var impressionParameters = new List<Parameter>
            {
                new Parameter("ad_platform", platform),
                new Parameter("ad_source", network),
                new Parameter("ad_unit_name", unit),
                new Parameter("ad_format", format),
                new Parameter("value", revenue),
                new Parameter("currency", "USD"),
            };
            if (!string.IsNullOrEmpty(pos))
                impressionParameters.Add(new Parameter("position", pos));
            if (formatNumber > 0)
                impressionParameters.Add(new Parameter("format_number", formatNumber));
            if (positionNumber > 0)
                impressionParameters.Add(new Parameter("position_number", positionNumber));

            FirebaseAnalytics.LogEvent(source, impressionParameters.ToArray());
#endif
            CoreLogUtils.Debug("CoreLite adjust track event: "+revenue+" network:"+network+" unit:"+unit+" source:"+source);

        }

        
    }
}

