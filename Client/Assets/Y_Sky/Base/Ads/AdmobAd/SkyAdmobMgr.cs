#if ADMOB_AD
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob 直连广告管理类，实现 ISkyAdService，与 TopOn(TPN) 并行参与竞价。
    ///
    /// 竞价价格说明：
    /// AdMob 在 Load 阶段拿不到 eCPM（Google 只在广告展示后通过 OnAdPaid 下发真实收益），
    /// 所以 GetRewardedRevenue / GetInterstitialRevenue 返回的是「上一次实际展示」的 eCPM
    /// （来自 SkyAdmobRevenueCache，由 OnAdPaid 写入）。
    ///   - 有历史展示记录：用该价格参与竞价，价格随每次展示自动更新；
    ///   - 无历史记录：返回 0，由 SkyAdsMgr 走首次强制展示逻辑完成价格校准。
    /// </summary>
    public class SkyAdmobMgr : SkySingleton<SkyAdmobMgr>, ISkyAdService
    {
        private readonly SkyAdmobReward _reward = new SkyAdmobReward();
        private readonly SkyAdmobInterstitial _interstitial = new SkyAdmobInterstitial();
        private readonly SkyAdmobSplash _splash = new SkyAdmobSplash();
        private readonly SkyAdmobBanner _banner = new SkyAdmobBanner();

        private bool _isInitialized;
        private bool _sdkReady;
        private List<string> _rewardUnits = new List<string>();
        private List<string> _interUnits = new List<string>();
        private List<string> _splashUnits = new List<string>();
        private string _bannerUnit = "";

        public string ClientName => SkyPlatformType.Admob.ToString();
        public SkyPlatformType PlatformType => SkyPlatformType.Admob;

        #region 初始化

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _rewardUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Rewarded) ?? new List<string>();
            _interUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Interstitial) ?? new List<string>();
            _splashUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Splash) ?? new List<string>();
            var bannerUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Banner) ?? new List<string>();
            _bannerUnit = bannerUnits.Count > 0 ? bannerUnits[0] : "";

            if (_rewardUnits.Count == 0 && _interUnits.Count == 0 && _splashUnits.Count == 0 && string.IsNullOrEmpty(_bannerUnit))
            {
                SkyLogUtils.Debug("[Admob] 未配置广告位（rewarded/interstitial/splash/banner），跳过初始化");
                return;
            }

            SkyLogUtils.Debug($"[Admob] Initialize reward:{_rewardUnits.Count} inter:{_interUnits.Count} splash:{_splashUnits.Count} banner:{(_bannerUnit != "" ? "1" : "0")}");

            MobileAds.Initialize(status =>
            {
                _sdkReady = true;
                SkyLogUtils.Debug("[Admob] SDK 初始化完成，开始预加载");
                _reward.Initialize(_rewardUnits);
                _interstitial.Initialize(_interUnits);
                _splash.Initialize(_splashUnits);
                _banner.Initialize(_bannerUnit);
            });
        }

        /// <summary>在线配置变化时重新读取广告位并重载。</summary>
        public void ReloadAdUnits()
        {
            if (!_isInitialized) return;

            ClearAllAds();

            _rewardUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Rewarded) ?? new List<string>();
            _interUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Interstitial) ?? new List<string>();
            _splashUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Splash) ?? new List<string>();
            var bannerUnits = SkyAdsConfigParser.GetAdmobAdUnits(SkyAdsType.Banner) ?? new List<string>();
            _bannerUnit = bannerUnits.Count > 0 ? bannerUnits[0] : "";

            SkyLogUtils.Debug($"[Admob] 广告位已刷新 reward:{_rewardUnits.Count} inter:{_interUnits.Count} splash:{_splashUnits.Count} banner:{(_bannerUnit != "" ? "1" : "0")}");

            if (_sdkReady)
            {
                _reward.Initialize(_rewardUnits);
                _interstitial.Initialize(_interUnits);
                _splash.Initialize(_splashUnits);
                _banner.Initialize(_bannerUnit);
            }
        }

        public void ClearAllAds()
        {
            _reward.ClearAd();
            _interstitial.ClearAd();
            _splash.ClearAd();
            _banner.ClearAd();
        }

        #endregion

        #region 激励视频

        public double GetRewardedRevenue()
        {
            // 竞价价格 = 上一次实际展示的 Revenue（单次收益）；无记录返回 0
            return SkyAdmobRevenueCache.Instance.GetRewardedLastShowRevenue();
        }

        public bool IsRewardedAvailable() => _sdkReady && _reward.IsReady();

        public void LoadRewarded() => _reward.LoadAll();

        public void ShowRewarded(string position = null)
        {
            if (!IsRewardedAvailable())
            {
                SkyLogUtils.Debug("[Admob] Rewarded 未就绪，触发重载");
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", "not_ready" },
                    { "ad_platform", ClientName },
                    { "unitId", "" }
                });
                _reward.LoadAll();
                return;
            }

            SkyAnalyticsMgr.TrackEvent("RV_AchieveShow_Match", new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                { "ad_platform", ClientName }
            });

            _reward.Show();
        }

        #endregion

        #region 插屏

        public double GetInterstitialRevenue()
        {
            // 竞价价格 = 上一次实际展示的 Revenue（单次收益）；无记录返回 0
            return SkyAdmobRevenueCache.Instance.GetInterstitialLastShowRevenue();
        }

        public bool IsInterstitialAvailable() => _sdkReady && _interstitial.IsReady();

        public void LoadInterstitial() => _interstitial.LoadAll();

        public void ShowInterstitial(string position = null)
        {
            if (!IsInterstitialAvailable())
            {
                SkyLogUtils.Debug("[Admob] Interstitial 未就绪，触发重载");
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                _interstitial.LoadAll();
                return;
            }

            _interstitial.Show();
        }

        public (string adId, string adSource, double adEcpm) GetInterstitialAdInfo()
        {
            return _interstitial.GetAdInfo();
        }

        #endregion

        #region Banner

        public double GetBannerRevenue()
        {
            // 竞价价格 = 上一次实际展示的 eCPM；无记录返回 0
            return SkyAdmobRevenueCache.Instance.GetBannerLastShowRevenue();
        }

        public bool IsBannerReady() => _sdkReady && _banner.IsReady();

        public void LoadBanner() => _banner.Initialize(_bannerUnit);

        public void ShowBanner()
        {
            if (!IsBannerReady())
            {
                SkyLogUtils.Debug("[Admob] Banner 未就绪，触发重载");
                _banner.Initialize(_bannerUnit);
                return;
            }

            _banner.Show();
        }

        public void HideBanner()
        {
            _banner.Hide();
        }

        #endregion

        #region Splash（开屏）

        public double GetSplashRevenue()
        {
            // 竞价价格 = 上一次实际展示的 Revenue（单次收益）；无记录返回 0
            return SkyAdmobRevenueCache.Instance.GetSplashLastShowRevenue();
        }

        public bool IsSplashAvailable() => _sdkReady && _splash.IsReady();

        public void LoadSplash() => _splash.LoadAll();

        public void ShowSplash()
        {
            if (!IsSplashAvailable())
            {
                SkyLogUtils.Debug("[Admob] Splash 未就绪，触发重载");
                SkyAdsMgr.Instance._spCloseCallback?.Invoke();
                _splash.LoadAll();
                return;
            }

            _splash.Show();
        }

        #endregion

        #region Native（不支持）

        public void LoadNative() => SkyLogUtils.Debug("[Admob] Native not supported.");
        public bool IsNativeAvailable() => false;
        public void ShowNative() => SkyLogUtils.Debug("[Admob] Native not supported.");
        public void RemoveNative() => SkyLogUtils.Debug("[Admob] Native not supported.");
        public double GetNativeRevenue() => -1;

        public bool IsNativeFullscreenAvailable() => false;
        public void ShowNativeFullscreen() => SkyLogUtils.Debug("[Admob] NativeFullscreen not supported.");
        public void RemoveNativeFullscreen() => SkyLogUtils.Debug("[Admob] NativeFullscreen not supported.");
        public double GetNativeFullscreenRevenue() => -1;

        public bool IsNativeSmallAvailable() => false;
        public void ShowNativeSmall() => SkyLogUtils.Debug("[Admob] NativeSmall not supported.");
        public void RemoveNativeSmall() => SkyLogUtils.Debug("[Admob] NativeSmall not supported.");
        public double GetNativeSmallRevenue() => -1;

        #endregion
    }
}
#endif
