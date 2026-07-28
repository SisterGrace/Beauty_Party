using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using CoreLite;



    [Serializable]
    public class ServerConfig
    {
        [Header("服务器配置")]
        public string BaseUrl = "";
        public string AsnUrl = "";
    }

    [Serializable]
    public class AnalyticsConfig
    {
        [Header("统计配置")]
        public string SSAppID_Android = "";
        public string SSAppID_iOS = "";
        public string SSUrl = "";
        public string AdjustToken_Android = "";
        public string AdjustToken_iOS = "";
        public string AdmobAppID_Android = "";
        public string AdmobAppID_iOS = "";
    }

    /// <summary>
    /// TopOn（TPN）应用与广告位 ID，由宿主在 CoreGameConfigData 资源中填写；SDK 只从此读取，不使用远程 AdUnits JSON。
    /// </summary>
    [Serializable]
    public class CoreToponLocalConfig
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

    /// <summary>
    /// Mintegral（MTG）应用、版位与单元 ID，由宿主本地填写；SDK 只从此读取，不使用远程 AdUnits JSON。
    /// </summary>
    [Serializable]
    public class CoreMtgLocalConfig
    {
        [Header("应用 ID / App Key")]
        public string appId = "";
        public string appKey = "";
        [Header("插屏 IV — 版位 ID / 单元 ID")]
        public string interstitialPlacementId = "";
        public string interstitialUnitId = "";
        [Header("激励 RV — 版位 ID / 单元 ID")]
        public string rewardedPlacementId = "";
        public string rewardedUnitId = "";
    }

    /// <summary>
    /// UP2（FineRoas）SDK 的 Application 启动参数，由宿主在 CoreGameConfigData 资源中填写；
    /// 构建 Android 时会写入 AndroidManifest 的 meta-data，供 Up2Application 在 attachBaseContext 阶段读取。
    /// </summary>
    [Serializable]
    public class CoreUp2LocalConfig
    {
        [Header("UP2 开发者平台 AppId（必填）")]
        public string appId = "";
    }

    [CreateAssetMenu(fileName = "CoreGameConfigData", menuName = "Game Configuration/Game Config")]
    public class CoreGameConfigData : ScriptableObject
    {
        [Header("服务器配置")]
        public ServerConfig serverConfig;

        [Header("统计相关配置")]
        public AnalyticsConfig analyticsConfig;

        // [Header("广告配置")]
        // public string appIdAndroid = "";
        // public string appIdIOS = "";
        
        [Space(10)]
        [Header("TopOn（TPN）— 本地广告参数")]
        public CoreToponLocalConfig toponLocalConfig = new CoreToponLocalConfig();

        [Header("MTG（Mintegral）— 本地广告参数")]
        public CoreMtgLocalConfig mtgLocalConfig = new CoreMtgLocalConfig();

        [Header("UP2（FineRoas）— Application 启动参数")]
        public CoreUp2LocalConfig up2LocalConfig = new CoreUp2LocalConfig();

        public string GetLoginUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/user/login";
        }

        public string GetIAPReportUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/report/v2/iap";
        }
        
        public string GetProductListUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/iap/products/list";
        }
        
        public string GetGameConfigUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/user/get-game-config";
        }

        public string GetCreateOrderUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/order/v2/create";
        }

        public string GetReportOrderRewardUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/order/v2/awarded";
        }
        
        public string GetQueryOrderUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/order/v2/query";
        }

        public string GetQueryOrderListUrl()
        {
            return serverConfig.BaseUrl + "/hkoc/order/v2/list";
        }

        public string GetAsnUrl()
        {
            return serverConfig.AsnUrl;
        }
        
        public string GetSSAppId()
        {
#if UNITY_IOS
            return analyticsConfig.SSAppID_iOS;
#else
            return analyticsConfig.SSAppID_Android;
#endif
        }

        public string GetSSUrl()
        {
            return analyticsConfig.SSUrl;
        }

        public string GetAdjustId()
        {
#if UNITY_IOS            
            return analyticsConfig.AdjustToken_iOS;
#else
            return analyticsConfig.AdjustToken_Android;
#endif
        }

        public string GetAdmobAppId()
        {
#if UNITY_IOS            
            return analyticsConfig.AdmobAppID_iOS;
#else
            return analyticsConfig.AdmobAppID_Android;
#endif
        }
    }