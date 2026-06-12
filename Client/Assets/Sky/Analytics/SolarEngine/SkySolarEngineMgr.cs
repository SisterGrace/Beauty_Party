using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    // 【新增】SolarEngine（热云）SDK 管理器。
    // 最小可验证版本：preInit + initialize(logEnabled=true)，不接事件体系/归因/GDPR。
    // 数据区域：海外 oversea。AppKey 来源：SkyConfigData.analyticsConfig.reyun_ID
    public class SkySolarEngineMgr : SkySingleton<SkySolarEngineMgr>
    {
        // 灰度阶段先打开 SDK 本地日志，方便 Logcat 自检；上线前应改为 false
        private const bool EnableLog = true;

        public bool IsInitialized { get; private set; }

        public bool TrackingEnable() => IsInitialized;

        // 从 SkyConfigData（Resources/SkyConfigData.asset）读取热云 AppKey。
        private static string GetAppKey()
        {
            var cfg = SkyConfigMgr.Instance != null ? SkyConfigMgr.Instance.gameConfig : null;
            if (cfg == null || cfg.analyticsConfig == null) return string.Empty;
            return cfg.analyticsConfig.reyun_ID ?? string.Empty;
        }

        // SDK 初始化：preInit 后立即正式 initialize（最小版本，不做隐私门禁）
        public void Init()
        {
            if (IsInitialized) return;
            var appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey))
            {
                LogTool.Error("[SolarEngine] AppKey empty (SkyConfigData.reyun_ID), skip init");
                return;
            }

            SkySolarEngineBridge.PreInit(appKey);
            SkySolarEngineBridge.Initialize(appKey, EnableLog);

            IsInitialized = true;
            LogTool.DebugLog("[SolarEngine] Init triggered, appKey=" + appKey + ", log=" + EnableLog,
                LogTool.Color.yellow);
        }

        // 单独暴露 PreInit 占位，便于后续接「同意隐私政策后再 Init」流程
        public void PreInit()
        {
            var appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey)) return;
            SkySolarEngineBridge.PreInit(appKey);
        }

        // 事件上报占位，最小版本不实际打事件，等接入归因/自定义事件再实现
        public void TrackEvent(string eventName)
        {
            if (!IsInitialized) return;
            // TODO: 接入 SolarEngine track API（需根据项目实际桥接结构调整）
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!IsInitialized) return;
            // TODO: 同上
        }

        void OnDestroy()
        {
            Debug.LogWarning("SkySolarEngineMgr 被销毁了！");
        }
    }
}

