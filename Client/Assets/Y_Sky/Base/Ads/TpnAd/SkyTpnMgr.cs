#if TPN_AD
using System.Collections.Generic;
using AnyThinkAds.Api;
using Sky;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// TopOn/TPN 广告状态回调结构，用于解析 checkAdStatus 返回的 JSON（手动加载 API）。
    /// </summary>
    [System.Serializable]
    public class SkyTpnAdInfo
    {
        public double publisher_revenue;
        public string network_placement_id;
        public int network_firm_id;
    }

    [System.Serializable]
    public class SkyTpnAdStatus
    {
        public bool isReady;
        public SkyTpnAdInfo adInfo;
    }

    /// <summary>
    /// TPN(TopOn/AnyThink) 广告管理类，实现 ISkyAdService，对应 HMAnythinkMgr + Inter + Reward 逻辑。
    /// </summary>
    public class SkyTpnMgr : SkySingleton<SkyTpnMgr>, ISkyAdService
    {
        private string _appId;
        private string _appKey;
        private bool _isInitialized;
        public string ClientName => SkyPlatformType.TPN.ToString();
        public SkyPlatformType PlatformType => SkyPlatformType.TPN;

#if TOPON_TEST
        // ================== TopOn SDK 版本切换开关 ==================
        /// <summary>
        /// TopOn SDK 混淆版本枚举
        /// </summary>
        public enum TpnSdkApiVersion
        {
            Secmtp,   // 对应 com.secmtp.sdk.* (海外版混淆)
            Thinkup,  // 对应 com.thinkup.* (特定版本混淆)
            AnyThink  // 对应 com.anythink.* (标准未混淆版，国内通常用这个)
        }

        /// <summary>
        /// 【切换开关】在这里一键切换当前使用的 TopOn 版本
        /// 打包前修改此处即可，无需再去代码里搜索替换字符串
        /// </summary>
        public static TpnSdkApiVersion CURRENT_TPN_API_VERSION = TpnSdkApiVersion.Thinkup;

        // 获取核心类路径 (ATSDK / TUSDK)
        private static string GetTpnSdkSkyApiClass()
        {
            switch (CURRENT_TPN_API_VERSION)
            {
                case TpnSdkApiVersion.Thinkup: return "com.thinkup.core.api.TUSDK";
                case TpnSdkApiVersion.AnyThink: return "com.anythink.core.api.ATSDK";
                case TpnSdkApiVersion.Secmtp:
                default: return "com.secmtp.sdk.core.api.ATSDK";
            }
        }

        // 获取配置构建器类路径
        private static string GetTpnSdkDebuggerConfigBuilderClass()
        {
            switch (CURRENT_TPN_API_VERSION)
            {
                case TpnSdkApiVersion.Thinkup: return "com.thinkup.core.api.TUDebuggerConfig$Builder";
                case TpnSdkApiVersion.AnyThink: return "com.anythink.core.api.ATDebuggerConfig$Builder";
                case TpnSdkApiVersion.Secmtp:
                default: return "com.secmtp.sdk.core.api.ATDebuggerConfig$Builder";
            }
        }

        // 获取调试器 UI 类路径
        private static string GetTpnSdkDebuggerUiClass()
        {
            switch (CURRENT_TPN_API_VERSION)
            {
                case TpnSdkApiVersion.Thinkup: return "com.thinkup.debug.api.TUDebuggerUITest";
                case TpnSdkApiVersion.AnyThink: return "com.anythink.debug.api.ATDebuggerUITest";
                case TpnSdkApiVersion.Secmtp:
                default: return "com.secmtp.sdk.debug.api.ATDebuggerUITest";
            }
        }
        // ===========================================================
#endif

        public void Initialize()
        {
            if (_isInitialized) return;

            // 直接读取本地参数，配置各广告位（广告 ID 不再走在线参数）
            _appId = SkyAdsConfigParser.GetTpnAppId();
            _appKey = SkyAdsConfigParser.GetTpnAppKey();
            var interUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Interstitial);
            var rewardUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Rewarded);
            var splashUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Splash);
            var bannerUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Banner);
            SkyLogUtils.Debug("[SkyTpn] Initialize appId:" + _appId
                + " interCount:" + (interUnits?.Count ?? 0)
                + " rewardCount:" + (rewardUnits?.Count ?? 0)
                + " splashCount:" + (splashUnits?.Count ?? 0)
                + " bannerCount:" + (bannerUnits?.Count ?? 0));

            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appKey))
            {
                SkyLogUtils.Error("[SkyTpn] Initialize aborted: appId or appKey is empty (appId:" + _appId + ")");
                return;
            }

            SkyTpnInter.Instance.SetPlacementIds(interUnits ?? new List<string>());
            SkyTpnReward.Instance.SetPlacementIds(rewardUnits ?? new List<string>());
            SkyTpnSplash.Instance.SetPlacementIds(splashUnits ?? new List<string>());
            SkyTpnBanner.Instance.SetPlacementIds(bannerUnits ?? new List<string>());

#if UNITY_ANDROID || UNITY_IOS
            AnyThinkAds.Api.ATSDKAPI.setLogDebug(SkyLogUtils.Enabled);
#if TOPON_TEST && UNITY_ANDROID
            // 海外版/特定版本 SDK 使用混淆，动态获取类路径
            try
            {
                using (var atsdkClass = new AndroidJavaClass(GetTpnSdkSkyApiClass()))
                {
                    atsdkClass.CallStatic("setNetworkLogDebug", true);
                    SkyLogUtils.Debug("[SkyTpn] setNetworkLogDebug ON (" + CURRENT_TPN_API_VERSION + ")");
                }
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Debug("[SkyTpn] setNetworkLogDebug failed: " + e.Message);
            }

            //SetUMPAndAdDebugMode("CECC96A68B641E880BC8530704249FE9", "2d04bdd5-eb09-4df0-9293-250ab4990c5b");//不能两个一起测试，只能一个一个测试
            SetTopOnDebugMode("2d04bdd5-eb09-4df0-9293-250ab4990c5b");

#endif
#if ADMOB_TEST && UNITY_ANDROID
            SetAdmobTestDevice("CECC96A68B641E880BC8530704249FE9"); // AdMob 设备 hash（从日志获取，非 GAID）
            EnableAdmobDebug();
#endif


            SkyLogUtils.Debug("[SkyTpn] ConsentDismissListener:_appId:" + _appId);
            ATSDKAPI.showGDPRConsentDialog(new ConsentDismissListener(_appId, _appKey, OnSdkInitSuccess, OnSdkInitFail));
#else
            SkyLogUtils.Debug("[SkyTpn] Skip init on non-mobile platform");
#endif
        }
        public (string adId, string adSource, double adEcpm) GetInterstitialAdInfo()
        {
            string adId = "";
            string adSource = "";
            double adEcpm = 0;

            string bestPlacement = SkyTpnInter.Instance.GetHighestPayingPlacement();
            if (!string.IsNullOrEmpty(bestPlacement))
            {
                try
                {
                    var statusJson = ATInterstitialAd.Instance.checkAdStatus(bestPlacement);
                    var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);
                    if (status?.adInfo != null)
                    {
                        adId = status.adInfo.network_placement_id;
                        adSource = "Topon_" + status.adInfo.network_firm_id.ToString();
                        adEcpm = status.adInfo.publisher_revenue * 1000;
                    }
                }
                catch { }
            }

            return (adId, adSource, adEcpm);
        }

        private void OnSdkInitSuccess()
        {
            _isInitialized = true;
            LogTool.DebugLog("[SkyTpn] SDK initialized, start loading ads (Splash first)");


#if ADMOB_TEST && UNITY_ANDROID
            VerifyAdmobTestDevice();
           /* bool isTestDevice = IsCurrentDeviceTestDevice();
            SkyLogUtils.Debug("[SkyTpn] Is current device a test device? " + isTestDevice);*/
#endif
#if TOPON_TEST && UNITY_ANDROID
            // SDK 初始化成功后，调起 TopOn 测试工具 UI（链接包方式）
            ShowDebuggerUI();
#endif

            // 开屏优先 load，其余并行
            LoadSplash();
            LoadRewarded();
            LoadInterstitial();
            LoadBanner();
        }

#if ADMOB_TEST
        private static void SetAdmobTestDevice(string testDeviceId)
        {
            try
            {
                using (var mobileAdsClass = new AndroidJavaClass("com.google.android.gms.ads.MobileAds"))
                using (var builder = new AndroidJavaObject("com.google.android.gms.ads.RequestConfiguration$Builder"))
                using (var deviceIds = new AndroidJavaObject("java.util.ArrayList"))
                {
                    deviceIds.Call<bool>("add", testDeviceId);
                    builder.Call<AndroidJavaObject>("setTestDeviceIds", deviceIds);
                    var config = builder.Call<AndroidJavaObject>("build");
                    mobileAdsClass.CallStatic("setRequestConfiguration", config);

                    SkyLogUtils.Debug("[SkyTpn] Admob test device registered: " + testDeviceId);
                // 不自己循环论证，等广告加载完用 isTestDevice() 验证
                }
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Error("[SkyTpn] Failed to set Admob test device: " + e.Message);
            }
        }

        private static void VerifyAdmobTestDevice()
        {
            try
            {
                using (var mobileAdsClass = new AndroidJavaClass("com.google.android.gms.ads.MobileAds"))
                {
                    var config = mobileAdsClass.CallStatic<AndroidJavaObject>("getRequestConfiguration");
                    var testDeviceIds = config.Call<AndroidJavaObject>("getTestDeviceIds");
                    int count = testDeviceIds.Call<int>("size");
                    SkyLogUtils.Debug("[SkyTpn] AdMob test device count: " + count);
                    for (int i = 0; i < count; i++)
                    {
                        var id = testDeviceIds.Call<string>("get", i);
                        SkyLogUtils.Debug("[SkyTpn] AdMob test device[" + i + "]: " + id);
                    }
                }
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Debug("[SkyTpn] VerifyAdmobTestDevice failed: " + e.Message);
            }
        }

        private static void EnableAdmobDebug()
        {
            try
            {
                using (var mobileAdsClass = new AndroidJavaClass("com.google.android.gms.ads.MobileAds"))
                {
                    // 先试新版 API
                    try
                    {
                        mobileAdsClass.CallStatic("setAdInspectorEnabled", true);
                        SkyLogUtils.Debug("[SkyTpn] Admob setAdInspectorEnabled(true) OK");
                    }
                    catch (System.Exception e1)
                    {
                        SkyLogUtils.Debug("[SkyTpn] setAdInspectorEnabled failed: " + e1.Message);
                        // 回退旧版 API
                        mobileAdsClass.CallStatic("enableDebugErrorIndicator", true);
                        SkyLogUtils.Debug("[SkyTpn] Admob enableDebugErrorIndicator(true) OK");
                    }
                }
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Debug("[SkyTpn] EnableAdmobDebug all failed: " + e.Message);
            }
        }
        /// <summary>
        /// 判断当前设备是否为 AdMob 测试设备
        /// 参考 React Native AdMob 的实现，调用原生 SDK 的 isTestDevice 方法//问题，拿不到istestdevice这个方法
        /// </summary>
        private static bool IsCurrentDeviceTestDevice()
        {
            try
            {
                // 方法1：通过 AdRequest.isTestDevice(Context) 判断（推荐）
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var adRequestClass = new AndroidJavaClass("com.google.android.gms.ads.AdRequest"))
                {
                    // 注意：这里是静态方法调用 AdRequest.isTestDevice(Context)
                    bool isTest = adRequestClass.CallStatic<bool>("isTestDevice", activity);
                    return isTest;
                }
            }
            catch (System.Exception e1)
            {
                SkyLogUtils.Debug("[SkyTpn] AdRequest.isTestDevice failed: " + e1.Message);

                // 方法2：如果上面失败，尝试从 RequestConfiguration 读取
                try
                {
                    using (var mobileAdsClass = new AndroidJavaClass("com.google.android.gms.ads.MobileAds"))
                    {
                        var config = mobileAdsClass.CallStatic<AndroidJavaObject>("getRequestConfiguration");
                        var testDeviceIds = config.Call<AndroidJavaObject>("getTestDeviceIds");
                        int count = testDeviceIds.Call<int>("size");
                        return count > 0;
                    }
                }
                catch (System.Exception e2)
                {
                    SkyLogUtils.Debug("[SkyTpn] RequestConfiguration fallback failed: " + e2.Message);
                    return false;
                }
            }
        }
#endif

#if TOPON_TEST
        /// <summary>
        /// TopOn 测试广告平台枚举（根据 TopOn 官方文档的 network_firm_id）
        /// 文档来源：https://docs.toponad.com/#/zh-cn/android/mediation/mediation_overview
        /// </summary>
        public enum TopOnTestNetwork
        {
            Facebook = 1,      // Meta（Bidding S2S）- 美国填充率高，需安装Facebook应用并登录
            AdMob = 2,         // Google AdMob（Bidding S2S）- 美国填充率高，需Google Play和GAID
            InMobi = 3,        // InMobi（Bidding S2S/C2S）- 印度填充率高
            AppLovin = 5,      // AppLovin - 欧美、中国填充率高
            Mintegral = 6,     // Mintegral（Bidding S2S）- 欧美地区填充率高
            Chartboost = 9,    // Chartboost（Bidding C2S，需接入Helium）- 中国、美国填充率高
            IronSource = 11,   // IronSource（Bidding S2S）- 中国填充率高
            UnityAds = 12,     // Unity Ads（Bidding S2S）- 美国填充率高
            Vungle = 13,       // Liftoff(Vungle)（Bidding S2S）- 美国填充率高
            StartApp = 25,     // Start.io（Bidding S2S）
            Sigmob = 29,       // Sigmob（Bidding C2S）- 中国
            MyTarget = 32,     // VK(myTarget)（Bidding S2S）
            GoogleAdManager = 33, // Google Ad Manager
            Yandex = 34,       // Yandex（Bidding S2S）
            Fyber = 37,        // Digital Turbine(Fyber)
            Helium = 40,       // Helium（Bidding C2S）
            A4G = 48,          // A4G(Admob)
            Pangle = 50,       // Pangle（Bidding S2S）- 字节跳动，韩国、日本填充率高
            ReklamUp = 57,     // ReklamUp
            VerveGroup = 58,   // Verve Group（Bidding C2S）
            BigoAds = 59,      // Bigo Ads（Bidding S2S）- 新加坡填充率高
            Bidmachine = 65,   // Bidmachine（Bidding S2S）
            ADX = 66,          // TopOn ADX（Bidding S2S）- 无需单独SDK
            PremiumAds = 71,   // PremiumAds
            APS = 75,          // Amazon Publisher Services（Bidding C2S）
            Kwai = 77,         // Kwai Network（Bidding C2S）- 快手
            XiaomiColumbus = 81, // Xiaomi Columbus（Bidding S2S）
            TaurusX = 83,      // TaurusX(Webeye)（Bidding S2S）
            Smaato = 84,       // Smaato（Bidding S2S）
            Moloco = 91,       // Moloco（Bidding S2S）
            Opera = 100        // Opera（Bidding S2S）
        }

        // 【配置项】选择要测试的广告平台，修改这里即可切换测试平台
        private static TopOnTestNetwork TEST_NETWORK = TopOnTestNetwork.Facebook;

        private static void ShowDebuggerUI()
        {
            try
            {
                // 使用 TopOn Unity 插件提供的 API 调起测试工具（需链接包 debugger-ui-tpn）
                ATSDKAPI.showDebuggerUI("06f402c6cc6e648864b017edebda4fce07fc89a8");//debug key
                SkyLogUtils.Debug("[SkyTpn] Debugger UI launched via ATSDKAPI.showDebuggerUI()");
            }
            catch (System.Exception e)
            {
                // 回退：直接通过 AndroidJavaClass 调用
                SkyLogUtils.Debug("[SkyTpn] ATSDKAPI.showDebuggerUI() failed, trying direct JNI: " + e.Message);
                try
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var debuggerClass = new AndroidJavaClass(GetTpnSdkDebuggerUiClass()))
                    {
                        debuggerClass.CallStatic("showDebuggerUI", activity);
                    }
                    SkyLogUtils.Debug("[SkyTpn] Debugger UI launched via direct JNI");
                }
                catch (System.Exception e2)
                {
                    SkyLogUtils.Error("[SkyTpn] Failed to launch debugger UI: " + e2.Message);
                }
            }
        }
        
        /// <summary>
        /// TopOn 调试模式：锁定当前设备仅加载指定广告平台的测试广告。
        /// 必须在 ATSDK.initSDK 之前调用。
        /// 使用硬编码 GAID 避免主线程阻塞获取失败的问题（测试阶段专用，上线前删除）。
        /// </summary>
        private static void SetTopOnDebugMode(string gaid)
        {
            if (string.IsNullOrEmpty(gaid))
            {
                SkyLogUtils.Debug("[SkyTpn] SetTopOnDebugMode skipped: GAID is empty");
                return;
            }
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var atsdkClass = new AndroidJavaClass(GetTpnSdkSkyApiClass()))
                {
                    // 使用配置的测试网络 ID
                    int adNetwork = (int)TEST_NETWORK;

                    using (var builder = new AndroidJavaObject(GetTpnSdkDebuggerConfigBuilderClass(), adNetwork))
                    {
                        var config = builder.Call<AndroidJavaObject>("build");
                        atsdkClass.CallStatic("setDebuggerConfig", activity, gaid, config);
                        SkyLogUtils.Debug("[SkyTpn] TopOn debug mode ON: " + TEST_NETWORK.ToString() + " (network=" + adNetwork + ") GAID=" + gaid);
                    }
                }
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Debug("[SkyTpn] SetTopOnDebugMode failed: " + e.Message);
            }
        }
 
        /// <summary>
        /// 通过 Android 桥接强制开启 UMP 测试模式（欧盟地区 + 测试设备）含指定广告
        /// 必须在 showGDPRConsentDialog 之前调用
        /// </summary>
        /// <param name="testDeviceId">从日志中过滤 "addTestDeviceHashedId" 获取的设备ID</param>
        private static void SetUMPAndAdDebugMode(string testDeviceId, string gaid)
        {
            if (string.IsNullOrEmpty(testDeviceId))
            {
                SkyLogUtils.Debug("[SkyTpn] SetUMPAndAdDebugMode skipped: testDeviceId is empty");
                return;
            }

            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var atsdkClass = new AndroidJavaClass(GetTpnSdkSkyApiClass()))
                {
                    // 使用配置的测试网络 ID
                    int adNetwork = (int)TEST_NETWORK;

                    using (var builder = new AndroidJavaObject(GetTpnSdkDebuggerConfigBuilderClass(), adNetwork))
                    {
                        var config = builder.Call<AndroidJavaObject>("build");
                        atsdkClass.CallStatic("setDebuggerConfig", activity, gaid, config);
                        SkyLogUtils.Debug("[SkyTpn] TopOn debug mode ON: " + TEST_NETWORK.ToString() + " (network=" + adNetwork + ") GAID=" + gaid);
                    }
                }
            }
            catch (System.Exception e)
            {
                SkyLogUtils.Debug("[SkyTpn] SetUMPAndAdDebugMode failed: " + e.Message);
            }
        }
#endif
        private void OnSdkInitFail(string message)
        {
            _isInitialized = false;
            SkyLogUtils.Error("[SkyTpn] SDK init failed appId:" + _appId + " message:" + (message ?? ""));
        }
        public void ClearAllAds()
        {
            SkyTpnInter.Instance.ClearAd();
            SkyTpnReward.Instance.ClearAd();
            SkyTpnSplash.Instance.ClearAd();
            SkyTpnBanner.Instance.ClearAd();
        }
        public void ReloadAdUnits()
        {
            if (!_isInitialized)
            {
                SkyLogUtils.Debug("[SkyTpn] ReloadAdUnits called but SDK not initialized, skip");
                return;
            }
            // 兜底：如果开关未开启，直接跳过（防御性）
            if (!SkyAdsConfigParser.IsOnlineAdUnitsEnabled)
            {
                SkyLogUtils.Debug("[SkyTpn] Online ad units disabled, skip reload.");
                return;
            }
            // 1. 先清空所有旧的广告状态（内部列表）
            ClearAllAds();

            // 2. 重新读取配置
            var interUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Interstitial);
            var rewardUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Rewarded);
            var splashUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Splash);
            var bannerUnits = SkyAdsConfigParser.GetTpnAdUnits(SkyAdsType.Banner);

            SkyTpnInter.Instance.SetPlacementIds(interUnits ?? new List<string>());
            SkyTpnReward.Instance.SetPlacementIds(rewardUnits ?? new List<string>());
            SkyTpnSplash.Instance.SetPlacementIds(splashUnits ?? new List<string>());
            SkyTpnBanner.Instance.SetPlacementIds(bannerUnits ?? new List<string>());

            SkyLogUtils.Debug("[SkyTpn] Ad units reloaded, inter:" + (interUnits?.Count ?? 0)
                + " reward:" + (rewardUnits?.Count ?? 0)
                + " splash:" + (splashUnits?.Count ?? 0)
                + " banner:" + (bannerUnits?.Count ?? 0));

            // 3. 重新加载广告（如果列表为空，各个 Load 方法内部会跳过）
            LoadInterstitial();
            LoadRewarded();
            LoadSplash();
            LoadBanner();
        }
        #region Rewarded
        public double GetRewardedRevenue() => SkyTpnReward.Instance.GetRewardRevenue();
        public bool IsRewardedAvailable() => SkyTpnReward.Instance.IsRewardReady();
        public void LoadRewarded() => SkyTpnReward.Instance.LoadReward();
        public void ShowRewarded(string position = null)
        {
            if (!IsRewardedAvailable())
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    //{ "show_fail_reason", "not_ready" },
                    { "ad_platform", ClientName },
                    { "unitId", "" }
                });
                LoadRewarded();
                return;
            }
            var bestPlacement = SkyTpnReward.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    //{ "show_fail_reason", "highest_paying_ad_not_found" },
                    { "ad_platform", ClientName },
                    //{ "unitId", "" }
                });
                LoadRewarded();
                return;
            }
            SkyAnalyticsMgr.TrackEvent("RV_AchieveShow_Match", new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._rvPos },
                { "ad_platform", ClientName }
            });
            SkyTpnReward.Instance.ShowReward(bestPlacement);
        }
        #endregion

        #region Interstitial
        public double GetInterstitialRevenue() => SkyTpnInter.Instance.GetInterRevenue();
        public bool IsInterstitialAvailable() => SkyTpnInter.Instance.IsInterReady();
        public void LoadInterstitial() => SkyTpnInter.Instance.LoadInter();
        public void ShowInterstitial(string position = null)
        {
            if (!IsInterstitialAvailable())
            {
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }
            var bestPlacement = SkyTpnInter.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                /* SkyAnalyticsMgr.TrackEvent("IV_AchieveShow_Fail", new Dictionary<string, object>()
                 {
                     { "Position", SkyAdsMgr.Instance._ivPos },
                     { "ad_platform", SkyPlatformType.TPN.ToString() },
                     { "unitId", "" },
                     { "show_fail_reason", "highest_paying_placement_not_found" }
                 });*/
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }

            // 获取广告信息用于 IV_AchieveShow 打点
            string adId = "";
            string adSource = "";
            double adRevenue = 0;
            try
            {
                var statusJson = ATInterstitialAd.Instance.checkAdStatus(bestPlacement);
                var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);
                if (status?.adInfo != null)
                {
                    adId = status.adInfo.network_placement_id;
                    adSource = "Topon_" + status.adInfo.network_firm_id.ToString();
                    adRevenue = status.adInfo.publisher_revenue;
                }
            }
            catch { }

            SkyAnalyticsMgr.TrackEvent("IV_AchieveShow", new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._ivPos },
                { "ad_platform", "TPN" },
                { "ad_source", adSource },
                { "ad_id", adId },
                { "ad_ecpm", adRevenue*1000 }
            });
            SkyTpnInter.Instance.ShowInter(bestPlacement);
        }
        #endregion

        #region Banner
        public double GetBannerRevenue() => SkyTpnBanner.Instance.GetBannerRevenue();
        public bool IsBannerReady() => SkyTpnBanner.Instance.IsBannerReady();
        public void LoadBanner() => SkyTpnBanner.Instance.LoadBanner();
        public void HideBanner() => SkyTpnBanner.Instance.HideBanner();
        public void ShowBanner()
        {
            if (!IsBannerReady())
            {
                LoadBanner();
                return;
            }
            var bestPlacement = SkyTpnBanner.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                LoadBanner();
                return;
            }
            SkyTpnBanner.Instance.ShowBanner(bestPlacement);
        }
        #endregion

        #region Native
        public double GetNativeRevenue() => -1;
        public bool IsNativeAvailable() => false;
        public void LoadNative() { }
        public void RemoveNative() { }
        public void ShowNative() { }

        public void LoadNativeFullscreen() { }
        public bool IsNativeFullscreenAvailable() => false;
        public void ShowNativeFullscreen() { }
        public void RemoveNativeFullscreen() { }
        public double GetNativeFullscreenRevenue() => -1;

        public void LoadNativeSmall() { }
        public bool IsNativeSmallAvailable() => false;
        public void ShowNativeSmall() { }
        public void RemoveNativeSmall() { }
        public double GetNativeSmallRevenue() => -1;
        #endregion

        #region Splash
        public double GetSplashRevenue() => SkyTpnSplash.Instance.GetSplashRevenue();
        public bool IsSplashAvailable() => SkyTpnSplash.Instance.IsSplashReady();
        public void LoadSplash() => SkyTpnSplash.Instance.LoadSplash();
        public void ShowSplash()
        {
            if (!IsSplashAvailable())
            {
                LoadSplash();
                return;
            }
            var bestPlacement = SkyTpnSplash.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                LoadSplash();
                return;
            }
            SkyTpnSplash.Instance.ShowSplash(bestPlacement);
        }
        #endregion
    }
}
#endif