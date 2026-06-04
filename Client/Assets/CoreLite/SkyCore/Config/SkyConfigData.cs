using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CoreLite;




    [Serializable]
    public class SkyAnalyticsConfig
{
        [Header("统计配置")]
        public string fireBaseAppID_Android = "";
        public string fireBaseAppID_iOS = "";
        public string GA_ID = "";
        public string reyun_ID = "";
    }

    /// <summary>
    /// 广告位 ID，由宿主在 BlueSkyConfigData 资源中填写；SDK 只从此读取，不使用远程 AdUnits JSON。
    /// </summary>
    [Serializable]
    public class CoreMaxLocalConfig
    {
        [Header("应用 ID / App Key")]
        public string appId = "";
        public string appKey = "";
        [Header("广告位 Placement ID")]
        public string interstitialPlacementId = "";
        public string rewardedPlacementId = "";
        public string bannerPlacementId = "";
        public string splashPlacementId = "";
    }


   
    [CreateAssetMenu(fileName = "SkyConfigData", menuName = "Game Configuration/Sky Game Config")]
    public class SkyConfigData : ScriptableObject
    {
        [Header("统计相关配置")]
        public SkyAnalyticsConfig analyticsConfig;

        [Space(10)]
        [Header("Max — 本地广告参数")]
        public CoreMaxLocalConfig maxLocalConfig = new CoreMaxLocalConfig();

    }