using System.Collections.Generic;

namespace Sky
{
    // 【新增】SolarEngine 事件常量与转发占位。
    // 当前最小版本只放最常用的三个预置事件名，避免一次性引入完整事件体系。
    public static class SkySolarEngineEvents
    {
        // 热云预置事件名（参考官方文档 _appStart / _appLogin / _appPur），后续按需扩展
        public const string AppStart = "_appStart";
        public const string AppLogin = "_appLogin";
        public const string AppPur   = "_appPur";

        public static void TrackEvent(string eventName)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            SkySolarEngineMgr.Instance.TrackEvent(eventName);
        }

        public static void TrackEvent(string eventName, string key, object value)
        {
            if (!SkySolarEngineMgr.Instance.TrackingEnable()) return;
            if (key == null || value == null)
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
    }
}
