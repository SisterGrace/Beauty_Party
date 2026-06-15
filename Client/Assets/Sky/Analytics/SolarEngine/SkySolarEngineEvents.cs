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
    }
}
