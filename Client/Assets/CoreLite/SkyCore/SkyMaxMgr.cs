using AnyThinkAds.Api;
using CoreLite;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// TopOn/TPN 广告状态回调结构，用于解析 checkAdStatus 返回的 JSON（手动加载 API）。
    /// </summary>
    [System.Serializable]
    public class SkyMaxAdInfo
    {
        public double publisher_revenue;
        public string network_placement_id;
        public int network_firm_id;
    }

    [System.Serializable]
    public class SkyMaxAdStatus
    {
        public bool isReady;
        public SkyMaxAdInfo adInfo;
    }

    /// <summary>
    /// Max 广告管理类，实现 ICoreAdService，对应 HMAnythinkMgr + Inter + Reward 逻辑。
    /// </summary>
    public class SkyMaxMgr : SkySingleton<SkyMaxMgr>, ICoreAdService
    {
        private string _appId;
        private string _appKey;
        private bool _isInitialized;
        public string ClientName => CorePlatformType.Max.ToString();
        public CorePlatformType PlatformType => CorePlatformType.TPN;

        public void Initialize()
        {
            if (_isInitialized) return;

            // 直接读取本地参数，配置各广告位（广告 ID 不再走在线参数）
            _appId = SkyAdsConfigParser.GetMaxAppId();
            _appKey = SkyAdsConfigParser.GetMaxAppKey();
            var interUnits = SkyAdsConfigParser.GetMaxAdUnits(SkyAdsType.Interstitial);
            var rewardUnits = SkyAdsConfigParser.GetMaxAdUnits(SkyAdsType.Rewarded);
            var splashUnits = SkyAdsConfigParser.GetMaxAdUnits(SkyAdsType.Splash);
            var bannerUnits = SkyAdsConfigParser.GetMaxAdUnits(SkyAdsType.Banner);
            SkyLogUtils.Debug("[SkyMax] Initialize appId:" + _appId
                + " interCount:" + (interUnits?.Count ?? 0)
                + " rewardCount:" + (rewardUnits?.Count ?? 0)
                + " splashCount:" + (splashUnits?.Count ?? 0)
                + " bannerCount:" + (bannerUnits?.Count ?? 0));

            if (string.IsNullOrEmpty(_appId) || string.IsNullOrEmpty(_appKey))
            {
                SkyLogUtils.Error("[SkyMax] Initialize aborted: appId or appKey is empty (appId:" + _appId + ")");
                return;
            }

           *//* SkyMaxInter.Instance.SetPlacementIds(interUnits ?? new List<string>());
            SkyMaxReward.Instance.SetPlacementIds(rewardUnits ?? new List<string>());
            SkyMaxSplash.Instance.SetPlacementIds(splashUnits ?? new List<string>());
            SkyMaxBanner.Instance.SetPlacementIds(bannerUnits ?? new List<string>());*//*

#if UNITY_ANDROID || UNITY_IOS
            AnyThinkAds.Api.ATSDKAPI.setLogDebug(SkyLogUtils.Enabled);
            SkyLogUtils.Debug("[SkyMax] ATSDKAPI.initSDK begin appId:" + _appId);
        }

        private void OnSdkInitSuccess()
        {
            _isInitialized = true;
            SkyLogUtils.Debug("[SkyMax] SDK initialized, start loading ads (Splash first)");
            // 开屏优先 load，其余并行
            LoadSplash();
            LoadRewarded();
            LoadInterstitial();
            LoadBanner();
        }

        private void OnSdkInitFail(string message)
        {
            _isInitialized = false;
            SkyLogUtils.Error("[SkyMax] SDK init failed appId:" + _appId + " message:" + (message ?? ""));
        }

        #region Rewarded
        public double GetRewardedRevenue() => SkyMaxReward.Instance.GetRewardRevenue();
        public bool IsRewardedAvailable() => SkyMaxReward.Instance.IsRewardReady();
        public void LoadRewarded() => SkyMaxReward.Instance.LoadReward();
        public void ShowRewarded()
        {
            if (!IsRewardedAvailable())
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", "not_ready" },
                    { "ad_platform", ClientName },
                    { "unitId", "" }
                });
                LoadRewarded();
                return;
            }
            var bestPlacement = SkyMaxReward.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", "highest_paying_ad_not_found" },
                    { "ad_platform", ClientName },
                    { "unitId", "" }
                });
                LoadRewarded();
                return;
            }
            SkyAnalyticsMgr.TrackEvent("RV_AchieveShow_Match", new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._rvPos },
                { "ad_platform", ClientName }
            });
            SkyMaxReward.Instance.ShowReward(bestPlacement);
        }
        #endregion

        #region Interstitial
        public double GetInterstitialRevenue() => SkyMaxInter.Instance.GetInterRevenue();
        public bool IsInterstitialAvailable() => SkyMaxInter.Instance.IsInterReady();
        public void LoadInterstitial() => SkyMaxInter.Instance.LoadInter();
        public void ShowInterstitial()
        {
            if (!IsInterstitialAvailable())
            {
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }
            var bestPlacement = SkyMaxInter.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                SkyAnalyticsMgr.TrackEvent("IV_AchieveShow_Fail", new Dictionary<string, object>()
                {
                    { "Position", SkyAdsMgr.Instance._ivPos },
                    { "ad_platform", SkyPlatformType.TPN.ToString() },
                    { "unitId", "" },
                    { "show_fail_reason", "highest_paying_placement_not_found" }
                });
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInterstitial();
                return;
            }
            SkyAnalyticsMgr.TrackEvent("IV_AchieveShow", new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._ivPos },
                { "ad_platform", ClientName },
                { "unitId", bestPlacement }
            });
            SkyMaxInter.Instance.ShowInter(bestPlacement);
        }
        #endregion

        #region Banner
        public double GetBannerRevenue() => SkyMaxBanner.Instance.GetBannerRevenue();
        public bool IsBannerReady() => SkyMaxBanner.Instance.IsBannerReady();
        public void LoadBanner() => SkyMaxBanner.Instance.LoadBanner();
        public void HideBanner() => SkyMaxBanner.Instance.HideBanner();
        public void ShowBanner()
        {
            if (!IsBannerReady())
            {
                LoadBanner();
                return;
            }
            var bestPlacement = SkyMaxBanner.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                LoadBanner();
                return;
            }
            SkyMaxBanner.Instance.ShowBanner(bestPlacement);
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
        public double GetSplashRevenue() => SkyMaxSplash.Instance.GetSplashRevenue();
        public bool IsSplashAvailable() => SkyMaxSplash.Instance.IsSplashReady();
        public void LoadSplash() => SkyMaxSplash.Instance.LoadSplash();
        public void ShowSplash()
        {
            if (!IsSplashAvailable())
            {
                LoadSplash();
                return;
            }
            var bestPlacement = SkyMaxSplash.Instance.GetHighestPayingPlacement();
            if (string.IsNullOrEmpty(bestPlacement))
            {
                LoadSplash();
                return;
            }
            SkyMaxSplash.Instance.ShowSplash(bestPlacement);
        }
        #endregion
    }
}
