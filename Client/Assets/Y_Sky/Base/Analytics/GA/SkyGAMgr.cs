#if GA
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using GameAnalyticsSDK.Wrapper;
using Sky;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Sky
{
    public class SkyGAMgr : SkySingleton<SkyGAMgr>
    {
        public void Init()
        {
            LogTool.DebugLog("GA init", LogTool.Color.yellow);
            if (UnityEngine.Object.FindObjectOfType(typeof(GameAnalytics)) == null)
            {
                GameObject ga = new GameObject();
                ga.name = "GameAnalytics";
                ga.transform.parent = transform;
                ga.AddComponent<GA_SpecialEvents>();
                ga.AddComponent<GameAnalytics>();
            }
            else
            {
                Debug.LogWarning("A GameAnalytics object already exists in this scene - you should never have more than one per scene!");
            }
			var gamekey = SkyConfigMgr.Instance.GetGA_gameKey();
			var secretkey = SkyConfigMgr.Instance.GetGA_secretKey();
			LogTool.DebugLog("GA_gameKey=" + gamekey + "GA_secretKey=" + secretkey + " Application.version=" + Application.version, LogTool.Color.yellow);
			//GameAnalytics.Initialize();
			GA_Wrapper.SetBuild(Application.version);
			try { GA_Wrapper.Initialize(gamekey, secretkey); GameAnalytics._hasInitializeBeenCalled = true; }
			catch (Exception e) { LogTool.DebugLog("GA_Wrapper.Initialize 异常，可能是缺少Setting.assets文件: " + e.ToString()); }
			//LogTool.DebugLog("GA_gameKey=" + gamekey + "GA_secretKey=" + secretkey + " Application.version=" + Application.version, LogTool.Color.yellow);
			_isInitialized = true;
        }
        static bool _isInitialized = false;
        public bool TrackingEnable()
        {
            return _isInitialized;
        }
        void OnDestroy()
        {
            Debug.LogWarning("SkyGAMgr 被销毁了！");
        }
    }
}
#endif
