using System;
using System.Collections.Generic;
using AnyThinkAds.Api;
using UnityEngine;
using Up2SDK;

namespace CoreLite
{
    /// <summary>
    /// TPN(TopOn) 横幅广告封装，手动加载，支持多广告位。使用 ATBannerAd。
    /// 默认基于 320x50 比例按屏幕宽度自适应；位置默认 bottom，可通过 SetPosition 切换。
    /// </summary>
    public class CoreTpnBanner : CoreSingleton<CoreTpnBanner>
    {
        private List<string> _placementIds = new List<string>();
        private Dictionary<string, float> _startLoadTimes = new Dictionary<string, float>();
        private Dictionary<string, int> _retryAttempts = new Dictionary<string, int>();
        private Dictionary<string, double> _placementRevenue = new Dictionary<string, double>();
        private bool _callbacksRegistered;
        private string _currentPlacement;
        private bool _isShown;
        private string _position = ATBannerAdLoadingExtra.kATBannerAdShowingPisitionBottom;
        private const string PlatformName = "TPN";
        private const int MaxRetry = 6;
        private const int BannerRefWidth = 320;
        private const int BannerRefHeight = 50;

        public void SetPlacementIds(List<string> placementIds)
        {
            _placementIds = placementIds != null ? new List<string>(placementIds) : new List<string>();
        }

        /// <summary>
        /// 设置 banner 展示位置：传入 ATBannerAdLoadingExtra.kATBannerAdShowingPisitionTop 或
        /// kATBannerAdShowingPisitionBottom。该值仅作用于下一次 ShowBanner。
        /// </summary>
        public void SetPosition(string position)
        {
            if (string.IsNullOrEmpty(position)) return;
            _position = position;
        }

        public string GetPosition() => _position;

        public void LoadBanner()
        {
            if (!_callbacksRegistered)
            {
                RegisterCallbacks();
                _callbacksRegistered = true;
            }
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                LoadBanner(placementId);
            }
        }

        private void LoadBanner(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            CoreAdsEvents.LogStartLoadEvent(placementId, PlatformName, AdLoadType.Banner);
            int screenWidth = Screen.width > 0 ? Screen.width : BannerRefWidth;
            int height = Mathf.Max(1, screenWidth * BannerRefHeight / BannerRefWidth);
            var size = new ATSize(screenWidth, height, true);
            var jsonmap = new Dictionary<string, object>
            {
                { ATBannerAdLoadingExtra.kATBannerAdLoadingExtraBannerAdSizeStruct, size },
                { ATBannerAdLoadingExtra.kATBannerAdLoadingExtraInlineAdaptiveWidth, screenWidth },
                { ATBannerAdLoadingExtra.kATBannerAdLoadingExtraInlineAdaptiveOrientation, ATBannerAdLoadingExtra.kATBannerAdLoadingExtraInlineAdaptiveOrientationCurrent }
            };
            ATBannerAd.Instance.loadBannerAd(placementId, jsonmap);
            _startLoadTimes[placementId] = Time.realtimeSinceStartup;
            CoreLogUtils.Debug("[CoreLiteTpn] Load banner placement: " + placementId + " size:" + screenWidth + "x" + height);
        }

        public bool IsBannerReady()
        {
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (HasBannerCache(placementId)) return true;
            }
            return false;
        }

        /// <summary>返回当前已就绪广告位中收益最高的 eCPM，无则返回 -1。</summary>
        public double GetBannerRevenue()
        {
            double maxRevenue = -1;
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (!HasBannerCache(placementId)) continue;
                double rev = _placementRevenue.TryGetValue(placementId, out var r) ? r : 0;
                if (rev > maxRevenue)
                    maxRevenue = rev;
            }
            return maxRevenue;
        }

        /// <summary>获取当前收益最高的已就绪广告位 ID，无则返回 null。</summary>
        public string GetHighestPayingPlacement()
        {
            string best = null;
            double maxRevenue = -1;
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (!HasBannerCache(placementId)) continue;
                double rev = _placementRevenue.TryGetValue(placementId, out var r) ? r : 0;
                if (rev > maxRevenue)
                {
                    maxRevenue = rev;
                    best = placementId;
                }
            }
            return best;
        }

        public void ShowBanner(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            // 切换广告位时清理上一次的展示，避免不同 placement 的 banner 同时挂在屏幕上。
            // 切换 placement 后下一次必须按"首次展示"调用，所以这里把状态重置。
            if (!string.IsNullOrEmpty(_currentPlacement) && _currentPlacement != placementId)
            {
                ATBannerAd.Instance.cleanBannerAd(_currentPlacement);
                _currentPlacement = null;
            }

            // TopOn 的两个 showBannerAd 重载语义不同：
            //   - showBannerAd(id, position)：首次展示，按 position 创建并 attach BannerView
            //   - showBannerAd(id)         ：重新展示已被 hideBannerAd 隐藏的 BannerView
            // 如果同一 placement 已经 show 过，只是被 Hide 暂时隐藏，必须走不带 position 的重载，
            // 否则 SDK 不会把已有 BannerView 重新挂回根视图，表现就是"再次 show 不出来"。
            bool isResume = _currentPlacement == placementId;
            if (isResume)
            {
                ATBannerAd.Instance.showBannerAd(placementId);
            }
            else
            {
                ATBannerAd.Instance.showBannerAd(placementId, _position);
                _currentPlacement = placementId;
            }
            _isShown = true;
            CoreLogUtils.Debug("[CoreLiteTpn] Show banner: " + placementId + " pos:" + _position + " resume:" + isResume);
        }

        public void HideBanner()
        {
            if (string.IsNullOrEmpty(_currentPlacement)) return;
            // 注意：hideBannerAd 后只能用 showBannerAd(placementId)（不带 position）恢复显示，
            // 否则后续 ShowBanner 不会重新挂回 BannerView。_currentPlacement 必须保留，用于
            // 下次 ShowBanner 判断是恢复展示还是首次展示。
            ATBannerAd.Instance.hideBannerAd(_currentPlacement);
            _isShown = false;
            CoreLogUtils.Debug("[CoreLiteTpn] Hide banner: " + _currentPlacement);
        }

        public void RemoveBanner()
        {
            if (string.IsNullOrEmpty(_currentPlacement)) return;
            ATBannerAd.Instance.cleanBannerAd(_currentPlacement);
            _placementRevenue.Remove(_currentPlacement);
            _isShown = false;
            _currentPlacement = null;
        }

        /// <summary>
        /// banner 没有 hasBannerAdReady，借助 checkAdStatus 解析 isReady。
        /// </summary>
        private bool HasBannerCache(string placementId)
        {
            try
            {
                var statusJson = ATBannerAd.Instance.checkAdStatus(placementId);
                if (string.IsNullOrEmpty(statusJson)) return false;
                var status = JsonUtility.FromJson<CoreLiteTpnAdStatus>(statusJson);
                return status != null && status.isReady;
            }
            catch
            {
                return false;
            }
        }

        private void RegisterCallbacks()
        {
            ATBannerAd.Instance.client.onAdLoadEvent += OnAdLoad;
            ATBannerAd.Instance.client.onAdLoadFailureEvent += OnAdLoadFail;
            ATBannerAd.Instance.client.onAdImpressEvent += OnAdImpress;
            ATBannerAd.Instance.client.onAdAutoRefreshEvent += OnAdAutoRefresh;
            ATBannerAd.Instance.client.onAdAutoRefreshFailureEvent += OnAdAutoRefreshFail;
            ATBannerAd.Instance.client.onAdClickEvent += OnAdClick;
            ATBannerAd.Instance.client.onAdCloseEvent += OnAdClose;
            ATBannerAd.Instance.client.onAdCloseButtonTappedEvent += OnAdCloseButtonTapped;
        }

        private void OnAdLoad(object sender, ATAdEventArgs e)
        {
            // TopOn 各 Network 回调线程不一致，统一通过 Loom 转主线程，避免在子线程调用 Unity API。
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                if (!_placementIds.Contains(placementId)) return;
                float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - (_startLoadTimes.TryGetValue(placementId, out var t) ? t : 0));
                double revenue = 0;
                string adId = "";
                string source = "";
                try
                {
                    var statusJson = ATBannerAd.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<CoreLiteTpnAdStatus>(statusJson);
                    if (status?.adInfo != null)
                    {
                        revenue = status.adInfo.publisher_revenue;
                        _placementRevenue[placementId] = revenue;
                        adId = status.adInfo.network_placement_id;
                        source = "Topon_" + status.adInfo.network_firm_id.ToString();
                    }
                }
                catch { }
                CoreAdsEvents.LogLoadedEvent(adId, source, PlatformName, loadTime, AdLoadType.Banner, placementId, revenue);
                _retryAttempts[placementId] = 0;
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdLoad: " + placementId);
            }, null);
        }

        private void OnAdLoadFail(object sender, ATAdErrorEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                if (!_placementIds.Contains(placementId)) return;
                float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - (_startLoadTimes.TryGetValue(placementId, out var t) ? t : 0));
                string error = e?.errorMessage ?? "";
                CoreAdsEvents.LogFailToLoadEvent(placementId, PlatformName, error, loadTime, AdLoadType.Banner);
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdLoadFail: " + placementId + " " + error);
                ScheduleRetry(placementId, LoadBanner);
            }, null);
        }

        private void OnAdImpress(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                int firmId = e?.callbackInfo?.network_firm_id ?? 0;
                string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + firmId : PlatformName;
                string adId = e?.callbackInfo?.network_placement_id ?? "";
                double revenue = e?.callbackInfo?.publisher_revenue ?? 0;
                if (revenue > 0) _placementRevenue[placementId] = revenue;
                string format = CoreLiteAdsType.Banner.ToString();
                int formatNumber = CoreAdsPrefs.GetAndIncrementFormatPlayCount(format);
                int positionNumber = CoreAdsPrefs.GetAndIncrementPositionPlayCount(format);
                CoreAnalyticsMgr.TrackEvent("BN_Show", new Dictionary<string, object>
                {
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_platform", PlatformName },
                    { "unitId", placementId },
                    { "revenue", revenue }
                });
                if (revenue > 0)
                {
                    CoreAnalyticsMgr.TrackAdImpressionEvent(
                        revenue, adSource, adId, placementId,
                        format, PlatformName,
                        "",
                        formatNumber,
                        positionNumber);
                }
#if UNITY_ANDROID
                CoreAnalyticsMgr.TrackEvent("Up2SDK_ToponAdShow", new Dictionary<string, object>
                {
                    { "ad_platform", "Topon" },
                    { "topon_firm_id", firmId },
                    { "ads_type", "BANNER" },
                    { "ad_id", placementId },
                    { "ecpm", revenue * 1000 },
                    { "show_id", "" }
                });
                Up2SDKManager.Instance.OnToponAdShow(firmId, AdsType.Banner, placementId, revenue * 1000, "");
#endif
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdImpress: " + placementId);
            }, null);
        }

        private void OnAdAutoRefresh(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                double revenue = e?.callbackInfo?.publisher_revenue ?? 0;
                if (revenue > 0) _placementRevenue[placementId] = revenue;
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdAutoRefresh: " + placementId + " revenue:" + revenue);
            }, null);
        }

        private void OnAdAutoRefreshFail(object sender, ATAdErrorEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdAutoRefreshFail: " + placementId + " " + (e?.errorMessage ?? ""));
            }, null);
        }

        private void OnAdClick(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                if (!_placementIds.Contains(placementId)) return;
                int firmId = e?.callbackInfo?.network_firm_id ?? 0;
                string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + firmId : PlatformName;
                string adId = e?.callbackInfo?.network_placement_id ?? "";
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdClick: " + placementId);
                CoreAdsEvents.LogClickEvent(
                    AdLoadType.Banner,
                    GetBannerRevenue(),
                    "",
                    adId,
                    adSource,
                    PlatformName,
                    placementId
                );
#if UNITY_ANDROID
                CoreAnalyticsMgr.TrackEvent("Up2SDK_ToponAdClick", new Dictionary<string, object>
                {
                    { "ad_platform", "Topon" },
                    { "topon_firm_id", firmId },
                    { "ads_type", "BANNER" },
                    { "ad_id", placementId },
                    { "show_id", "" }
                });
                Up2SDKManager.Instance.OnToponAdClick(firmId, AdsType.Banner, placementId, "");
#endif
            }, null);
        }

        private void OnAdClose(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdClose: " + placementId);
                HandleBannerDismissed(placementId, dismissByUser: false);
            }, null);
        }

        private void OnAdCloseButtonTapped(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                CoreLogUtils.Debug("[CoreLiteTpn] Banner OnAdCloseButtonTapped: " + placementId);
                HandleBannerDismissed(placementId, dismissByUser: true);
            }, null);
        }

        // 仅清理状态，不再触发 LoadBanner：
        //   1) 调用 HideBanner 后，部分 Network 会触发 onAdClose 回调，如果这里再 load 会和
        //      banner 自身的 auto-refresh、以及 OnAdLoadFail 的 ScheduleRetry 叠加，撞到 TopOn
        //      "load too frequent" (errorCode 2008) 的限频。
        //   2) Banner 本身有 auto-refresh 机制（onAdAutoRefreshEvent），不需要我们手动重 load。
        //   3) 用户主动关闭（dismissByUser）时也保持一致，不在这里重 load；下次需要展示时由
        //      上层 CoreSdkMgr/外部业务在 ShowBanner 前自行决定是否需要 LoadBanner。
        private void HandleBannerDismissed(string placementId, bool dismissByUser)
        {
            _placementRevenue.Remove(placementId);
            if (_currentPlacement == placementId)
            {
                _isShown = false;
                if (dismissByUser)
                {
                    // 用户主动点关闭按钮，BannerView 已被销毁，下次必须按"首次展示"路径走，
                    // 所以清掉 _currentPlacement 让 ShowBanner 走 showBannerAd(id, position)。
                    _currentPlacement = null;
                }
            }
        }

        private void ScheduleRetry(string placementId, Action<string> loadAction)
        {
            if (!_retryAttempts.TryGetValue(placementId, out var count))
                _retryAttempts[placementId] = 0;
            _retryAttempts[placementId]++;
            count = _retryAttempts[placementId];
            if (count >= MaxRetry)
            {
                CoreLogUtils.Debug("[CoreLiteTpn] Banner placement " + placementId + " max retry " + MaxRetry + " reached");
                return;
            }
            double delay = Math.Pow(2, Math.Min(MaxRetry, count));
            CoreFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () => loadAction(placementId));
        }
    }
}
