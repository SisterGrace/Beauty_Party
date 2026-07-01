//#define TOPON_TEST
//#define ADMOB_TEST
using System.Collections.Generic;
using AnyThinkAds.Api;
using UnityEngine;

namespace CoreLite
{
    /// <summary>
    /// TopOn/TPN 广告状态回调结构，用于解析 checkAdStatus 返回的 JSON（手动加载 API）。
    /// </summary>
    [System.Serializable]
    public class CoreLiteTpnAdInfo
    {
        public double publisher_revenue;
        public string network_placement_id;
        public int network_firm_id;
    }

    [System.Serializable]
    public class CoreLiteTpnAdStatus
    {
        public bool isReady;
        public CoreLiteTpnAdInfo adInfo;
    }

    /// <summary>
    /// TPN(TopOn/AnyThink) 广告管理类，实现 ICoreAdService，对应 HMAnythinkMgr + Inter + Reward 逻辑。
    /// </summary>
    public class CoreTpnMgr : CoreSingleton<CoreTpnMgr>, ICoreAdService
    {
        private string _appId;
        private string _appKey;
        private bool _isInitialized;
        public string ClientName => CorePlatformType.TPN.ToString();
        public CorePlatformType PlatformType => CorePlatformType.TPN;

        public void Initialize()
        {
            if (_isInitialized) return;

            // 直接读取本地参数，配置各广告位（广告 ID 不再走在线参数）
            _appId = CoreAdsConfigParser.GetTpnAppId();
            _appKey = CoreAdsConfigParser.GetTpnAppKey();
            var interUnits = CoreAdsConfigParser.GetTpnAdUnits(CoreLiteAdsType.Interstitial);
            var rewardUnits = CoreAdsConfigParser.GetTpnAdUnits(CoreLiteAdsType.Rewarded);
            var splashUnits = CoreAdsConfigParser.GetTpnAdUnits(CoreLiteAdsType.Splash);
            var bannerUnits = CoreAdsConfigParser.GetTpnAdUnits(CoreLiteAdsType.Banner);
            CoreLogUtils.Debug("[CoreLiteTpn] Initialize appId:" + _appId
                + " interCount:" + (interUnits?.Count ?? 0)
                + " rewardCount:" + (rewardUnits?.Count ?? 0)
                + " splashCount:" + (splashUnits?.Count ?? 0)
                + " bannerCount:" + (bannerUnits?.Count ?? 0));

            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appKey))
            {
                CoreLogUtils.Error("[CoreLiteTpn] Initialize aborted: appId or appKey is empty (appId:" + _appId + ")");
                return;
            }

            CoreTpnInter.Instance.SetPlacementIds(interUnits ?? new List<string>());
            CoreTpnReward.Instance.SetPlacementIds(rewardUnits ?? new List<string>());
            CoreTpnSplash.Instance.SetPlacementIds(splashUnits ?? new List<string>());
            CoreTpnBanner.Instance.SetPlacementIds(bannerUnits ?? new List<string>());

#if UNITY_ANDROID || UNITY_IOS
            AnyThinkAds.Api.ATSDKAPI.setLogDebug(CoreLogUtils.Enabled);
#if TOPON_TEST && UNITY_ANDROID
            // 海外版 SDK 使用 secmtp 混淆，原始路径 com.anythink.core.api.* 不可用
            try
            {
                using (var atsdkClass = new AndroidJavaClass("com.secmtp.sdk.core.api.ATSDK"))
                {
                    atsdkClass.CallStatic("setNetworkLogDebug", true);
                    CoreLogUtils.Debug("[CoreLiteTpn] setNetworkLogDebug ON (secmtp)");
                }
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Debug("[CoreLiteTpn] setNetworkLogDebug failed: " + e.Message);
            }

            // TopOn 调试模式：使用硬编码 GAID 开启 Admob 测试广告（避免主线程阻塞获取GAID失败）
            SetTopOnDebugMode("2d04bdd5-eb09-4df0-9293-250ab4990c5b");
#endif
#if ADMOB_TEST && UNITY_ANDROID
            SetAdmobTestDevice("CECC96A68B641E880BC8530704249FE9"); // AdMob 设备 hash（从日志获取，非 GAID）
            EnableAdmobDebug();
#endif


            CoreLogUtils.Debug("[CoreLiteTpn] ATSDKAPI.initSDK begin appId:" + _appId);
            var listener = new CoreTpnInitListener(
                _appId,
                onSuccess: OnSdkInitSuccess,
                onFail: OnSdkInitFail);
            AnyThinkAds.Api.ATSDKAPI.initSDK(_appId, _appKey, listener);
#else
            CoreLogUtils.Debug("[CoreLiteTpn] Skip init on non-mobile platform");
#endif
        }

        private void OnSdkInitSuccess()
        {
            _isInitialized = true;
            CoreLogUtils.Debug("[CoreLiteTpn] SDK initialized, start loading ads (Splash first)");


#if ADMOB_TEST && UNITY_ANDROID
            VerifyAdmobTestDevice();
           /* bool isTestDevice = IsCurrentDeviceTestDevice();
            CoreLogUtils.Debug("[CoreLiteTpn] Is current device a test device? " + isTestDevice);*/
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

                    CoreLogUtils.Debug("[CoreLiteTpn] Admob test device registered: " + testDeviceId);
                // 不自己循环论证，等广告加载完用 isTestDevice() 验证
                }
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Error("[CoreLiteTpn] Failed to set Admob test device: " + e.Message);
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
                    CoreLogUtils.Debug("[CoreLiteTpn] AdMob test device count: " + count);
                    for (int i = 0; i < count; i++)
                    {
                        var id = testDeviceIds.Call<string>("get", i);
                        CoreLogUtils.Debug("[CoreLiteTpn] AdMob test device[" + i + "]: " + id);
                    }
                }
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Debug("[CoreLiteTpn] VerifyAdmobTestDevice failed: " + e.Message);
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
                        CoreLogUtils.Debug("[CoreLiteTpn] Admob setAdInspectorEnabled(true) OK");
                    }
                    catch (System.Exception e1)
                    {
                        CoreLogUtils.Debug("[CoreLiteTpn] setAdInspectorEnabled failed: " + e1.Message);
                        // 回退旧版 API
                        mobileAdsClass.CallStatic("enableDebugErrorIndicator", true);
                        CoreLogUtils.Debug("[CoreLiteTpn] Admob enableDebugErrorIndicator(true) OK");
                    }
                }
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Debug("[CoreLiteTpn] EnableAdmobDebug all failed: " + e.Message);
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
                CoreLogUtils.Debug("[CoreLiteTpn] AdRequest.isTestDevice failed: " + e1.Message);

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
                    CoreLogUtils.Debug("[CoreLiteTpn] RequestConfiguration fallback failed: " + e2.Message);
                    return false;
                }
            }
        }
#endif

#if TOPON_TEST
        private static void ShowDebuggerUI()
        {
            try
            {
                // 使用 TopOn Unity 插件提供的 API 调起测试工具（需链接包 debugger-ui-tpn）
                ATSDKAPI.showDebuggerUI();
                CoreLogUtils.Debug("[CoreLiteTpn] Debugger UI launched via ATSDKAPI.showDebuggerUI()");
            }
            catch (System.Exception e)
            {
                // 回退：直接通过 AndroidJavaClass 调用
                CoreLogUtils.Debug("[CoreLiteTpn] ATSDKAPI.showDebuggerUI() failed, trying direct JNI: " + e.Message);
                try
                {
                    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var debuggerClass = new AndroidJavaClass("com.secmtp.sdk.debug.api.ATDebuggerUITest"))
                    {
                        debuggerClass.CallStatic("showDebuggerUI", activity);
                    }
                    CoreLogUtils.Debug("[CoreLiteTpn] Debugger UI launched via direct JNI");
                }
                catch (System.Exception e2)
                {
                    CoreLogUtils.Error("[CoreLiteTpn] Failed to launch debugger UI: " + e2.Message);
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
                CoreLogUtils.Debug("[CoreLiteTpn] SetTopOnDebugMode skipped: GAID is empty");
                return;
            }
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var atsdkClass = new AndroidJavaClass("com.secmtp.sdk.core.api.ATSDK"))
                {
                    // 读取 Admob 网络常量，兜底用 2
                    int admobNetwork;
                    try { admobNetwork = atsdkClass.GetStatic<int>("Admob_NETWORK"); }
                    catch { admobNetwork = 2; }

                    using (var builder = new AndroidJavaObject("com.secmtp.sdk.core.api.ATDebuggerConfig$Builder", admobNetwork))
                    {
                        var config = builder.Call<AndroidJavaObject>("build");
                        atsdkClass.CallStatic("setDebuggerConfig", activity, gaid, config);
                        CoreLogUtils.Debug("[CoreLiteTpn] TopOn debug mode ON: Admob(network=" + admobNetwork + ") GAID=" + gaid);
                    }
                }
            }
            catch (System.Exception e)
            {
                CoreLogUtils.Debug("[CoreLiteTpn] SetTopOnDebugMode failed: " + e.Message);
            }
        }
#endif
        private void OnSdkInitFail(string message)
        {
            _isInitialized = false;
            CoreLogUtils.Error("[CoreLiteTpn] SDK init failed appId:" + _appId + " message:" + (message ?? ""));
        }

        #region Rewarded
        public double GetRewardedRevenue() => CoreTpnReward.Instance.GetRewardRevenue();
        public bool IsRewardedAvailable() => CoreTpnReward.Instance.IsRewardReady();
        public void LoadRewarded() => CoreTpnReward.Instance.LoadReward();
        public void ShowRewarded()
        {
            if (!IsRewardedAvailable())
            {
                CoreAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", CoreAdsMgr.Instance._rvPos ?? "" },
                    //{ "show_fail_reason", "not_ready" },
                    { "ad_platform", ClientName },
                    { "unitId", "" }
                });
                LoadRewarded();
                return;
            }
            var bestPlacement = CoreTpnReward.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                CoreAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", CoreAdsMgr.Instance._rvPos ?? "" },
                    //{ "show_fail_reason", "highest_paying_ad_not_found" },
                    { "ad_platform", ClientName },
                    //{ "unitId", "" }
                });
                LoadRewarded();
                return;
            }
            CoreAnalyticsMgr.TrackEvent("RV_AchieveShow_Match", new Dictionary<string, object>
            {
                { "Position", CoreAdsMgr.Instance._rvPos },
                { "ad_platform", ClientName }
            });
            CoreTpnReward.Instance.ShowReward(bestPlacement);
        }
        #endregion

        #region Interstitial
        public double GetInterstitialRevenue() => CoreTpnInter.Instance.GetInterRevenue();
        public bool IsInterstitialAvailable() => CoreTpnInter.Instance.IsInterReady();
        public void LoadInterstitial() => CoreTpnInter.Instance.LoadInter();
        public void ShowInterstitial()
        {
            if (!IsInterstitialAvailable())
            {
                CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }
            var bestPlacement = CoreTpnInter.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
               /* CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Fail", new Dictionary<string, object>()
                {
                    { "Position", CoreAdsMgr.Instance._ivPos },
                    { "ad_platform", CorePlatformType.TPN.ToString() },
                    { "unitId", "" },
                    { "show_fail_reason", "highest_paying_placement_not_found" }
                });*/
                CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }
            
            // 获取广告信息用于 IV_AchieveShow 打点
            string adId = "";
            string adSource = "";
            double adEcpm = 0;
            try
            {
                var statusJson = ATInterstitialAd.Instance.checkAdStatus(bestPlacement);
                var status = JsonUtility.FromJson<CoreLiteTpnAdStatus>(statusJson);
                if (status?.adInfo != null)
                {
                    adId = status.adInfo.network_placement_id;
                    adSource = "Topon_" + status.adInfo.network_firm_id.ToString();
                    adEcpm = status.adInfo.publisher_revenue;
                }
            }
            catch { }
            
            CoreAnalyticsMgr.TrackEvent("IV_AchieveShow", new Dictionary<string, object>
            {
                { "Position", CoreAdsMgr.Instance._ivPos },
                { "ad_platform", "TPN" },
                { "ad_source", adSource },
                { "ad_id", adId },
                { "ad_ecpm", adEcpm }
            });
            CoreTpnInter.Instance.ShowInter(bestPlacement);
        }
        #endregion

        #region Banner
        public double GetBannerRevenue() => CoreTpnBanner.Instance.GetBannerRevenue();
        public bool IsBannerReady() => CoreTpnBanner.Instance.IsBannerReady();
        public void LoadBanner() => CoreTpnBanner.Instance.LoadBanner();
        public void HideBanner() => CoreTpnBanner.Instance.HideBanner();
        public void ShowBanner()
        {
            if (!IsBannerReady())
            {
                LoadBanner();
                return;
            }
            var bestPlacement = CoreTpnBanner.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                LoadBanner();
                return;
            }
            CoreTpnBanner.Instance.ShowBanner(bestPlacement);
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
        public double GetSplashRevenue() => CoreTpnSplash.Instance.GetSplashRevenue();
        public bool IsSplashAvailable() => CoreTpnSplash.Instance.IsSplashReady();
        public void LoadSplash() => CoreTpnSplash.Instance.LoadSplash();
        public void ShowSplash()
        {
            if (!IsSplashAvailable())
            {
                LoadSplash();
                return;
            }
            var bestPlacement = CoreTpnSplash.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                LoadSplash();
                return;
            }
            CoreTpnSplash.Instance.ShowSplash(bestPlacement);
        }
        #endregion
    }
}
