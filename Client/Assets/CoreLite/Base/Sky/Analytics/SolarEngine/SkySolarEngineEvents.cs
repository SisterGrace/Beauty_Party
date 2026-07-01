using System.Collections.Generic;

namespace Sky
{
    // 热云 SolarEngine 事件常量与上报入口。
    // 业务侧应通过 SkyAnalyticsMgr.DesignEvent 走统一分发，热云自定义事件极少需要直接调用本类。
    // 仅在需要上报「热云预置事件」（_appStart 等）时使用 TrackPresetEvent。
    public static class SkySolarEngineEvents
    {
        // 热云预置事件名（参考官方文档）
        public const string AppStart = "_appStart";
        public const string AppLogin = "_appLogin";
        public const string AppPur   = "_appPur";
        public const string AppReg   = "_appReg";

        #region 自定义事件
        public static void TrackEvent(string eventName)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackEvent(eventName);
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            if (string.IsNullOrEmpty(key) || value == null)
            {
                SkySolarEngineMgr.Instance.TrackEvent(eventName);
                return;
            }
            SkySolarEngineMgr.Instance.TrackEvent(eventName,
                new Dictionary<string, object> { [key] = value });
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackEvent(eventName, properties);
        }
        #endregion

        #region 预置事件
        public static void TrackPresetEvent(string eventName)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackPresetEvent(eventName);
        }

        public static void TrackPresetEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackPresetEvent(eventName, properties);
        }
        #endregion

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
                LogTool.DebugLog($"[SolarEngine] ad_impression skipped: revenue={revenue} < 0, reporting ad_impression_error");
                var errorProps = new Dictionary<string, object>
                {
                    { "ad_platform", platform },
                    { "ad_source", network },
                    //{ "ad_unit_name", unit },
                    { "ad_format", format },
                    { "value", revenue },
                    { "currency", "USD" },
                };
                if (!string.IsNullOrEmpty(pos))
                    errorProps["position"] = pos;
                TrackEvent("ad_impression_error", errorProps);
                return;
            }

            /*// 1. 原生 GA Ad 事件（无 revenue）
            GAAdType adType = GAAdType.Banner;
            switch (format.ToLower())
            {
                case "rewarded": adType = GAAdType.RewardedVideo; break;
                case "interstitial": adType = GAAdType.Interstitial; break;
                case "banner": adType = GAAdType.Banner; break;
                default: adType = GAAdType.Banner; break;
            }
            // 使用 Show 动作（或可以区分 Clicked，这里只做展示）
            GameAnalytics.NewAdEvent(GAAdAction.Show, adType, network, unit);*/

            // 2. 自定义 Design 事件携带收入详情
            var revenueProps = new Dictionary<string, object>
            {
                { "ad_platform", platform },
                { "ad_source", network },
                //{ "ad_unit_name", unit },
                { "ad_format", format },
                { "value", revenue },
                { "currency", "USD" },
            };
            if (!string.IsNullOrEmpty(pos))
                revenueProps["position"] = pos;
            /* if (formatNumber > 0)
                 revenueProps["format_number"] = formatNumber;
             if (positionNumber > 0)
                 revenueProps["position_number"] = positionNumber;*/

            TrackEvent("ad_impression", revenueProps);
        }

    }
}
