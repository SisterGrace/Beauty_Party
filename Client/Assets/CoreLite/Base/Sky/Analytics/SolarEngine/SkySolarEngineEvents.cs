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
        /// </summary>
        /// <summary>
        /// 广告收益事件上报（适配 SolarEngine 预置事件 _appImp）
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
            { "ad_format", format },
            { "value", revenue },
            { "currency", "USD" },
        };
                if (!string.IsNullOrEmpty(pos))
                    errorProps["position"] = pos;
                // 错误事件可以保留自定义，或者也改为预置 _appImp（收入为0），这里按原逻辑保留
                TrackEvent("ad_impression_error", errorProps);
                return;
            }

            // 重要：计算 eCPM（千次展示收益）
            // 假设 revenue 是单次展示收益（例如 0.002 USD），则 ecpm = 0.002 * 1000 = 2.0
            // 如果业务侧传入的 revenue 本身就是 eCPM，则不需要乘以 1000，请根据实际情况调整。
            double ecpm = revenue * 1000;

            // 构造 SolarEngine 预置事件 _appImp 的参数字典
            var revenueProps = new Dictionary<string, object>
    {
        // 必填：收益字段（必须是 ecpm，单位与 currency 对应）
        { "ecpm", ecpm },
        { "currency", "USD" },
        
        // 必填：广告平台标识（建议使用官方枚举值，如 admob, csj, pangle 等）
        { "ad_platform", platform },
        // 广告网络名称（如 admob, facebook 等）
        { "ad_source", network },
        // 广告位 ID（对应方法的 unit 参数）
        { "ad_unit_id", unit },
        // 广告形式（如 rewarded, interstitial, banner）
        { "ad_format", format },
    };

            // 可选：广告位置
            if (!string.IsNullOrEmpty(pos))
                revenueProps["position"] = pos;

            // 关键改动：改为调用 TrackPresetEvent，事件名固定为 _appImp
            // 这样 SolarEngine 后台才会将其识别为广告收益，归因到 ROI 报表中
            TrackPresetEvent("_appImp", revenueProps);
        }

    }
}
