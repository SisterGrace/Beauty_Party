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
            CoreLogUtils.Debug("[CoreLiteTpn] ATSDKAPI.initSDK begin appId:" + _appId);
            var listener = new CoreTpnInitListener(
                _appId,
                onSuccess: OnSdkInitSuccess,
                onFail: OnSdkInitFail);
            AnyThinkAds.Api.ATSDKAPI.initSDK(_appId, _appKey, listener);
#if UNITY_IOS
            //ATSDKAPI.showDebuggerUI("d5b84cf7902a69c69a7d55eb82efe9307d100df6");
#endif
#else
            CoreLogUtils.Debug("[CoreLiteTpn] Skip init on non-mobile platform");
#endif
        }

        private void OnSdkInitSuccess()
        {
            _isInitialized = true;
            CoreLogUtils.Debug("[CoreLiteTpn] SDK initialized, start loading ads (Splash first)");
            // 开屏优先 load，其余并行
            LoadSplash();
            LoadRewarded();
            LoadInterstitial();
            LoadBanner();
        }

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
                    { "show_fail_reason", "not_ready" },
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
                    { "show_fail_reason", "highest_paying_ad_not_found" },
                    { "ad_platform", ClientName },
                    { "unitId", "" }
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
                CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Fail", new Dictionary<string, object>()
                {
                    { "Position", CoreAdsMgr.Instance._ivPos },
                    { "ad_platform", CorePlatformType.TPN.ToString() },
                    { "unitId", "" },
                    { "show_fail_reason", "highest_paying_placement_not_found" }
                });
                CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }
            CoreAnalyticsMgr.TrackEvent("IV_AchieveShow", new Dictionary<string, object>
            {
                { "Position", CoreAdsMgr.Instance._ivPos },
                { "ad_platform", ClientName },
                { "unitId", bestPlacement }
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
