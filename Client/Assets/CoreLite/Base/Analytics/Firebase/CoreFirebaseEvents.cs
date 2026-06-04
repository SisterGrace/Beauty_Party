using System.Collections;
using System.Collections.Generic;
//using Firebase.Analytics;
using UnityEngine;

namespace CoreLite
{
    public static class CoreFirebaseEvents
    {
        private const string REVENUE_CONFIG_KEY = "revenue_fir";
        private const string DEFAULT_SOURCE = "ad_impression";
        public static void TrackEvent(string eventName)
        {
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
            #if FIRBASE
            FirebaseAnalytics.LogEvent(eventName);
#endif
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
            #if FIRBASE
            FirebaseAnalytics.LogEvent(eventName, key, value.ToString());
#endif
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> dict)
        {
            if (!CoreFirebaseMgr.Instance.TrackingEnable()) return;
            #if FIRBASE
            List<Parameter> parmlist = new List<Parameter>();
            foreach (var item in dict)
            {
                parmlist.Add(new Parameter(item.Key, item.Value.ToString()));
            }
            FirebaseAnalytics.LogEvent(eventName, parmlist.ToArray());
#endif
        }

        /// <param name="pos">广告展示点位，客户端根据实际位置上报</param>
        /// <param name="formatNumber">第几次展示该格式的广告（1、2、3…），每个广告格式独立统计</param>
        /// <param name="positionNumber">第几次展示该点位的广告（1、2、3…），每个广告触发点位独立统计</param>
        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string format, string platform, string pos = "", int formatNumber = 0, int positionNumber = 0)
        {
            var source = CoreAdImpressionSource.GetSource("revenue_fir","ad_impression");
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

