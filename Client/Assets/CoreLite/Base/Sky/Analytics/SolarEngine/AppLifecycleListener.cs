using Sky;
using System.Collections.Generic;
using UnityEngine;

public class AppLifecycleListener : SkySingleton <AppLifecycleListener>
{
    private float _foregroundStartTime;
    public void Init()
    {
        LogTool.DebugLog("AppLifecycleListener:init", LogTool.Color.lightblue);
    }
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        _foregroundStartTime = Time.realtimeSinceStartup;
    }

    void OnApplicationPause(bool isPause)
    {
        if (isPause)
        {
            // 进入后台 -> 上报 ta_app_end
            float duration = Time.realtimeSinceStartup - _foregroundStartTime;
            string adNetwork = GetCurrentAdNetwork();
            CoreLite.CoreAnalyticsMgr.TrackEvent("ta_app_end", new Dictionary<string, object>
            {
                { "duration", Mathf.CeilToInt(duration) }, // 向上取整（需求）
                { "ad_network", adNetwork }
            });
        }
        else
        {
            // 从后台返回或首次启动 -> 上报 ta_app_start
            _foregroundStartTime = Time.realtimeSinceStartup;
            string adNetwork = GetCurrentAdNetwork();
            CoreLite.CoreAnalyticsMgr.TrackEvent("ta_app_start", new Dictionary<string, object>
            {
                { "ad_network", adNetwork }
            });
        }
    }

    private string GetCurrentAdNetwork()
    {
        // 从热云归因获取渠道，未获取到时传空
        if (Sky.SkySolarEnginePrefs.HasAttribution)
            return Sky.SkySolarEnginePrefs.Network ?? "";
        return "";
    }
}