using Sky;
using TK_Engine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 【新增】热力引擎（SolarEngine / 热云）Unity 调 Android 桥接类。
    /// 数据区域：海外（com.reyun.solar.engine.oversea）。
    ///
    /// 用途：
    ///   - 从 SkyConfigData.asset 读取 AppKey（reyun_ID 字段）
    ///   - 通过 AndroidJavaClass 调用 com.unity3d.player.SolarEngineProxy 完成 preInit / initialize
    ///
    /// 当前为最小验证版本：仅实现 PreInit + Initialize，不做事件上报、不做归因回调。
    /// 后续若需要事件上报，再在此类追加方法并扩展 SolarEngineProxy.java。
    /// </summary>
    public static class SolarEngineBridge
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Java 桥接类全限定名，对应 Plugins/Android/SolarEngineProxy.java
        private const string ProxyClass = "com.unity3d.player.SolarEngineProxy";
#endif

        /// <summary>
        /// 从 SkyConfigData（Resources/SkyConfigData.asset）读取热云 AppKey。
        /// </summary>
        public static string GetAppKey()
        {
            var cfg = SkyConfigMgr.Instance != null ? SkyConfigMgr.Instance.gameConfig : null;
            if (cfg == null || cfg.analyticsConfig == null) return string.Empty;
            return cfg.analyticsConfig.reyun_ID ?? string.Empty;
        }

        /// <summary>
        /// 预初始化：应用启动后立即调用，SDK 此阶段不会上报任何数据。
        /// </summary>
        public static void PreInit(string appKey)
        {
            if (string.IsNullOrEmpty(appKey))
            {
                Log.Warning("[SolarEngine] AppKey 为空，跳过 PreInit。");
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var cls = new AndroidJavaClass(ProxyClass))
                {
                    cls.CallStatic("preInit", appKey);
                }
                Log.Info($"[SolarEngine] PreInit 已调用，key={appKey}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[SolarEngine] PreInit 失败：{e}");
            }
#else
            // 非 Android 设备（Editor / iOS）不做任何事，防止崩溃
            Log.Info($"[SolarEngine] 非 Android 设备，跳过 PreInit，key={appKey}");
#endif
        }

        /// <summary>
        /// 正式初始化：用户同意《隐私政策》后调用，开始正式上报。
        /// </summary>
        /// <param name="enableLog">是否打开 SDK 本地调试日志，灰度阶段建议 true</param>
        public static void Initialize(string appKey, bool enableLog = true)
        {
            if (string.IsNullOrEmpty(appKey))
            {
                Log.Warning("[SolarEngine] AppKey 为空，跳过 Initialize。");
                return;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var cls = new AndroidJavaClass(ProxyClass))
                {
                    cls.CallStatic("initialize", appKey, enableLog);
                }
                Log.Info($"[SolarEngine] Initialize 已调用，key={appKey}, enableLog={enableLog}");
            }
            catch (System.Exception e)
            {
                Log.Error($"[SolarEngine] Initialize 失败：{e}");
            }
#else
            Log.Info($"[SolarEngine] 非 Android 设备，跳过 Initialize，key={appKey}");
#endif
        }
    }
}
