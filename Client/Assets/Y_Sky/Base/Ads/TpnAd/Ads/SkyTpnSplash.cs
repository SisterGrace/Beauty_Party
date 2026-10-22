#if TPN_AD
using System;
using System.Collections.Generic;
using AnyThinkAds.Api;
using UnityEngine;
#if Up_2SDK
using Up2SDK;
#endif

namespace Sky
{
    /// <summary>
    /// TPN(TopOn) 开屏广告封装，手动加载，支持多广告位。使用 ATSplashAd。
    /// </summary>
    public class SkyTpnSplash : SkySingleton<SkyTpnSplash>
    {
        private List<string> _placementIds = new List<string>();
        private Dictionary<string, float> _startLoadTimes = new Dictionary<string, float>();
        private Dictionary<string, int> _retryAttempts = new Dictionary<string, int>();
        private Dictionary<string, double> _placementRevenue = new Dictionary<string, double>();
        /// <summary>
        /// 缓存每个广告位的广告信息：adId（第三方广告ID）, source（广告网络来源）, ecpm（千次展示收益）
        /// 用于在展示失败/关闭时上报精确的广告源信息
        /// </summary>
        private Dictionary<string, (string adId, string source, double ecpm)> _cachedAdInfo
            = new Dictionary<string, (string adId, string source, double ecpm)>();
        private bool _callbacksRegistered;
        private const string PlatformName = "TPN";
        private const int MaxRetry = 6;
        private const int FetchAdTimeoutMs = 5000;

        public void ClearAd()
        {
            _placementIds.Clear();          // 清空列表
            _placementRevenue.Clear();
            _retryAttempts.Clear();
            _cachedAdInfo.Clear();
            SkyLogUtils.Debug("[SkyTpnSplash]  ads cleared.");
        }
        public void SetPlacementIds(List<string> placementIds)
        {
            _placementIds = placementIds != null ? new List<string>(placementIds) : new List<string>();
            RegisterRevenueListeners();
        }

        public void LoadSplash()
        {
            if (!_callbacksRegistered)
            {
                RegisterCallbacks();
                _callbacksRegistered = true;
            }
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                LoadSplash(placementId);
            }
        }

        private void LoadSplash(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            
            // SP_Start_Load 打点
            SkySplashAdsMgr.TrackSplashEvent("SP_Start_Load", new Dictionary<string, object>
            {
                // 加载开始时还没有广告信息，所以这里只能传空值或省略
                // { "ad_platform", "" },
                // { "ad_id", "" },
                // { "ad_ecpm", 0 }
            });
            
            SkyAdsEvents.LogStartLoadEvent(placementId, PlatformName, AdLoadType.Splash);
            var jsonmap = new Dictionary<string, object>();
            ATSplashAd.Instance.loadSplashAd(placementId, jsonmap, FetchAdTimeoutMs);
            _startLoadTimes[placementId] = Time.realtimeSinceStartup;
            SkyLogUtils.Debug("[SkyTpn] Load splash placement: " + placementId);
        }

        public bool IsSplashReady()
        {
            foreach (var placementId in _placementIds)
            {
                if (!string.IsNullOrEmpty(placementId) && ATSplashAd.Instance.hasSplashAdReady(placementId))
                    return true;
            }
            return false;
        }

        /// <summary>返回当前已就绪广告位中收益最高的 eCPM，无则返回 -1。已就绪但尚未写入 revenue 的按 0 参与比较。</summary>
        public double GetSplashRevenue()
        {
            double maxRevenue = -1;
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (!ATSplashAd.Instance.hasSplashAdReady(placementId)) continue;
                double rev = _placementRevenue.TryGetValue(placementId, out var r) ? r : 0;
                if (rev > maxRevenue)
                    maxRevenue = rev;
            }
            return maxRevenue;
        }

        /// <summary>获取当前收益最高的已就绪广告位 ID，无则返回 null。已就绪但尚未写入 revenue 的按 0 参与比较。</summary>
        public string GetHighestPayingPlacement()
        {
            string best = null;
            double maxRevenue = -1;
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (!ATSplashAd.Instance.hasSplashAdReady(placementId)) continue;
                double rev = _placementRevenue.TryGetValue(placementId, out var r) ? r : 0;
                if (rev > maxRevenue)
                {
                    maxRevenue = rev;
                    best = placementId;
                }
            }
            return best;
        }

        public void ShowSplash(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            ATSplashAd.Instance.showSplashAd(placementId, new Dictionary<string, string>());
            SkyLogUtils.Debug("[SkyTpn] Show splash: " + placementId);
        }
        private TpnRevenueListener _revenueListener;

        private void RegisterRevenueListeners()
        {
            _revenueListener ??= new TpnRevenueListener();
            foreach (var id in _placementIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                ATSplashAd.Instance.client.setAdRevenueListener(id, _revenueListener);
            }
        }
        private void RegisterCallbacks()
        {
            ATSplashAd.Instance.client.onAdLoadEvent += OnAdLoad;
            ATSplashAd.Instance.client.onAdLoadFailureEvent += OnAdLoadFail;
            ATSplashAd.Instance.client.onAdLoadTimeoutEvent += OnAdLoadTimeout;
            ATSplashAd.Instance.client.onAdShowEvent += OnAdShow;
            ATSplashAd.Instance.client.onAdShowFailureEvent += OnAdShowFail;
            ATSplashAd.Instance.client.onAdCloseEvent += OnAdClose;
            ATSplashAd.Instance.client.onAdClickEvent += OnAdClick;
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
                    var statusJson = ATSplashAd.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);
                    if (status?.adInfo != null)
                    {
                        revenue = status.adInfo.publisher_revenue;
                        _placementRevenue[placementId] = revenue;
                        adId = status.adInfo.network_placement_id;
                        source = "Topon_" + status.adInfo.network_firm_id.ToString();
                    }
                    // 缓存广告信息，供展示失败等回调使用
                    _cachedAdInfo[placementId] = (adId, source, revenue);
                }
                catch { }
                SkyAdsEvents.LogLoadedEvent(adId, source, PlatformName, loadTime, AdLoadType.Splash, placementId, revenue);
                _retryAttempts[placementId] = 0;
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdLoad: " + placementId + " timeout:" + (e?.isTimeout ?? false));
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
                SkyAdsEvents.LogFailToLoadEvent(placementId, PlatformName, error, loadTime, AdLoadType.Splash);
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdLoadFail: " + placementId + " " + error);
                ScheduleRetry(placementId, LoadSplash);
            }, null);
        }

        private void OnAdLoadTimeout(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdLoadTimeout: " + placementId);
            }, null);
        }

        private void OnAdShow(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                int firmId = e?.callbackInfo?.network_firm_id ?? 0;
                string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + firmId : PlatformName;
                string adId = e?.callbackInfo?.network_placement_id ?? "";
                double revenue = e?.callbackInfo?.publisher_revenue ?? 0;
                string currency = e?.callbackInfo?.currency ?? "USD";  // 从 TopOn 回调中获取货币类型
                if (!_placementRevenue.ContainsKey(placementId)) _placementRevenue[placementId] = revenue;
                string format = SkyAdsType.Splash.ToString();
                int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(format);
                int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(format);
                string launchType = SkySplashAdsMgr.Instance != null ? SkySplashAdsMgr.Instance.CurrentLaunchType : "unknown";
                // 缓存业务上下文，供收入回调使用（Splash 的 pos 是启动类型）
                TopOnRevenueReporter.SetRevenueContext(placementId, launchType, formatNumber, positionNumber);
               /* SkySplashAdsMgr.TrackSplashEvent("SP_Show", new Dictionary<string, object>
                {
                    //{ "launch_type", launchType },  // 多余参数，已注释
                    { "ad_platform", PlatformName },
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_ecpm", revenue*1000 }
                    //{ "unitId", placementId },  // 多余参数，已注释
                    //{ "revenue", revenue }  // 多余参数，已注释，统一用 ad_ecpm
                });*/
                /* if (revenue > 0)
                 {
                     SkyAnalyticsMgr.TrackAdImpressionEvent(//删掉成功展示的收入上报
                         revenue, adSource, adId, placementId,
                         format, PlatformName,
                         launchType,
                         formatNumber,
                         positionNumber,
                         currency);  // 传递货币类型
                 }*/
#if UNITY_ANDROID
                /*   SkyAnalyticsMgr.TrackEvent("Up2SDK_ToponAdShow", new Dictionary<string, object>
                   {
                       { "ad_platform", "Topon" },
                       { "topon_firm_id", firmId },
                       { "ads_type", "SPLASH" },
                       { "ad_id", placementId },
                       { "ecpm", revenue * 1000 },
                       { "show_id", "" }
                   });*/
#if Up_2SDK
                Up2SDKManager.Instance.OnToponAdShow(firmId, AdsType.Splash, placementId, revenue * 1000, "");
#endif
#endif
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdShow: " + placementId);
            }, null);
        }

        private void OnAdShowFail(object sender, ATAdErrorEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                string adId = "";
                string adSource = "";
                double adRevenue = 0;
                
                // 尝试实时获取广告信息（最准确）
                try
                {
                    var statusJson = ATSplashAd.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);
                    if (status?.adInfo != null)
                    {
                        adId = status.adInfo.network_placement_id;
                        adSource = "Topon_" + status.adInfo.network_firm_id.ToString();
                        adRevenue = status.adInfo.publisher_revenue;
                    }
                }
                catch
                {
                    // checkAdStatus 失败，说明广告已经不可用，无法获取信息
                }
                
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdShowFail: " + placementId + " " + (e?.errorMessage ?? ""));
                
                // SP_ShowFail 通用打点
                SkySplashAdsMgr.TrackSplashEvent("SP_ShowFail", new Dictionary<string, object>
                {
                    { "ad_platform",PlatformName },
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_ecpm", adRevenue*1000 }
                });
                
                _cachedAdInfo.Remove(placementId);  // 清空缓存
                SkySplashAdsMgr.Instance?.NotifyAppOpenAdDismissedFromSdk();
                LoadSplash(placementId);
            }, null);
        }

        private void OnAdClose(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdClose: " + placementId);
                _placementRevenue.Remove(placementId);
                _cachedAdInfo.Remove(placementId);  // 清空缓存
                SkySplashAdsMgr.Instance?.NotifyAppOpenAdDismissedFromSdk();
                LoadSplash(placementId);
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
                SkyLogUtils.Debug("[SkyTpn] Splash OnAdClick: " + placementId);
                SkyAdsEvents.LogClickEvent(
                    AdLoadType.Splash,
                    GetSplashRevenue(),
                    SkySplashAdsMgr.Instance != null ? SkySplashAdsMgr.Instance.CurrentLaunchType : "",
                    adId,
                    adSource,
                    PlatformName,
                    placementId
                );
#if UNITY_ANDROID
                SkyAnalyticsMgr.TrackEvent("Up2SDK_ToponAdClick", new Dictionary<string, object>
                {
                    { "ad_platform", "Topon" },
                    { "topon_firm_id", firmId },
                    { "ads_type", "SPLASH" },
                    { "ad_id", placementId },
                    { "show_id", "" }
                });
#if Up_2SDK
                Up2SDKManager.Instance.OnToponAdClick(firmId, AdsType.Splash, placementId, "");
#endif
#endif
            }, null);
        }

        private void ScheduleRetry(string placementId, Action<string> loadAction)
        {
            if (!_retryAttempts.TryGetValue(placementId, out var count))
                _retryAttempts[placementId] = 0;
            _retryAttempts[placementId]++;
            count = _retryAttempts[placementId];
            if (count >= MaxRetry)
            {
                SkyLogUtils.Debug("[SkyTpn] Splash placement " + placementId + " max retry " + MaxRetry + " reached");
                return;
            }
            double delay = Math.Pow(2, Math.Min(MaxRetry, count));
            SkyFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () => loadAction(placementId));
        }
    }
}
#endif
