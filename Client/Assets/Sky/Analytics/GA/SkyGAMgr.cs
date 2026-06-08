using CoreLite;
using GameAnalyticsSDK;
using GameAnalyticsSDK.Events;
using GameAnalyticsSDK.Wrapper;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using Sky;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Reporter;
namespace Sky
{
    public class SkyGAMgr : SkySingleton<SkyGAMgr>
    {
        public void Init()
        {
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
            GameAnalytics.Initialize();
            GA_Wrapper.SetBuild(Application.version);
            var gamekey = SkyConfigMgr.Instance.GetGA_gameKey();
            var secretkey = SkyConfigMgr.Instance.GetGA_secretKey();
            GA_Wrapper.Initialize(gamekey, secretkey);
            LogTool.DebugLog("GA: init :GA_gameKey=" + gamekey + "GA_secretKey=" + secretkey + " Application.version=" + Application.version);
            _isInitialized = true;
        }
        static bool _isInitialized = false;
        public bool TrackingEnable()
        {
            return _isInitialized;
        }
        public void GAEvent(string str)
        {
            GameAnalytics.NewDesignEvent(str);
        }
    }
}
