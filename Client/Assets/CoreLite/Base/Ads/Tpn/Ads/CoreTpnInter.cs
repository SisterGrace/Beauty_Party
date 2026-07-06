using System;
using System.Collections.Generic;
using AnyThinkAds.Api;
using UnityEngine;
using Up2SDK;

namespace CoreLite
{
    /// <summary>
    /// TPN(TopOn) 插屏广告封装，手动加载，支持多广告位。使用 ATInterstitialAd 而非 ATInterstitialAutoAd。
    /// </summary>
    public class CoreTpnInter : CoreSingleton<CoreTpnInter>
    {
        private List<string> _placementIds = new List<string>();
        private Dictionary<string, float> _startLoadTimes = new Dictionary<string, float>();
        private Dictionary<string, int> _retryAttempts = new Dictionary<string, int>();
        private Dictionary<string, double> _placementRevenue = new Dictionary<string, double>();
        // 缓存广告信息：adId, source, ecpm
        private Dictionary<string, (string adId, string source, double ecpm)> _cachedAdInfo = new Dictionary<string, (string adId, string source, double ecpm)>();
        private bool _callbacksRegistered;
        private const string PlatformName = "TPN";
        private const int MaxRetry = 6;

        public void SetPlacementIds(List<string> placementIds)
        {
            _placementIds = placementIds != null ? new List<string>(placementIds) : new List<string>();
        }

        public void LoadInter()
        {
            if (!_callbacksRegistered)
            {
                RegisterCallbacks();
                _callbacksRegistered = true;
            }
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                LoadInter(placementId);
            }
        }

        private void LoadInter(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            CoreAdsEvents.LogStartLoadEvent(placementId, PlatformName, AdLoadType.Interstitial);
            var jsonmap = new Dictionary<string, object>();
            ATInterstitialAd.Instance.loadInterstitialAd(placementId, jsonmap);
            _startLoadTimes[placementId] = Time.realtimeSinceStartup;
            CoreLogUtils.Debug("[CoreLiteTpn] Load inter placement: " + placementId);
        }

        public bool IsInterReady()
        {
            foreach (var placementId in _placementIds)
            {
                if (!string.IsNullOrEmpty(placementId) && ATInterstitialAd.Instance.hasInterstitialAdReady(placementId))
                    return true;
            }
            return false;
        }

        /// <summary>返回当前已就绪广告位中收益最高的 eCPM，无则返回 -1。已就绪但尚未写入 revenue 的按 0 参与比较，避免回调时序导致被排除竞价。</summary>
        public double GetInterRevenue()
        {
            double maxRevenue = -1;
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (!ATInterstitialAd.Instance.hasInterstitialAdReady(placementId)) continue;
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
                if (!ATInterstitialAd.Instance.hasInterstitialAdReady(placementId)) continue;
                double rev = _placementRevenue.TryGetValue(placementId, out var r) ? r : 0;
                if (rev > maxRevenue)
                {
                    maxRevenue = rev;
                    best = placementId;
                }
            }
            return best;
        }

        public void ShowInter(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            ATInterstitialAd.Instance.showInterstitialAd(placementId);
            CoreLogUtils.Debug("[CoreLiteTpn] Show inter: " + placementId);
        }

        private void RegisterCallbacks()
        {
            ATInterstitialAd.Instance.client.onAdLoadEvent += OnAdLoad;
            ATInterstitialAd.Instance.client.onAdLoadFailureEvent += OnAdLoadFail;
            ATInterstitialAd.Instance.client.onAdShowEvent += OnAdShow;
            ATInterstitialAd.Instance.client.onAdCloseEvent += OnAdClose;
            ATInterstitialAd.Instance.client.onAdShowFailureEvent += OnAdShowFail;
            ATInterstitialAd.Instance.client.onAdClickEvent += OnAdClick;
        }

        private void OnAdClick(object sender, ATAdEventArgs e)
        {
            // TopOn 各 Network 回调线程不一致，统一通过 Loom 转主线程，避免在子线程调用 Unity API。
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                if (!_placementIds.Contains(placementId)) return;
                int firmId = e?.callbackInfo?.network_firm_id ?? 0;
                string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + firmId : PlatformName;
                string adId = e?.callbackInfo?.network_placement_id ?? "";
                CoreLogUtils.Debug("[CoreLiteTpn] Inter OnAdClick: " + placementId);
                CoreAdsEvents.LogClickEvent(
                    AdLoadType.Interstitial,
                    GetInterRevenue(),
                    CoreAdsMgr.Instance._ivPos,
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
                    { "ads_type", "INTERSTITIAL" },
                    { "ad_id", placementId },
                    { "show_id", "" }
                });
                Up2SDKManager.Instance.OnToponAdClick(firmId, AdsType.Interstitial, placementId, "");
#endif
            }, null);
        }

        private void OnAdLoad(object sender, ATAdEventArgs e)
        {
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
                    var statusJson = ATInterstitialAd.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<CoreLiteTpnAdStatus>(statusJson);
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
                CoreAdsEvents.LogLoadedEvent(adId, source, PlatformName, loadTime, AdLoadType.Interstitial, placementId, revenue);
                if (!_retryAttempts.ContainsKey(placementId)) _retryAttempts[placementId] = 0;
                else _retryAttempts[placementId] = 0;
                CoreLogUtils.Debug("[CoreLiteTpn] Inter OnAdLoad: " + placementId);
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
                CoreAdsEvents.LogFailToLoadEvent(placementId, PlatformName, error, loadTime, AdLoadType.Interstitial);
                CoreLogUtils.Debug("[CoreLiteTpn] Inter OnAdLoadFail: " + placementId + " " + error);
                ScheduleRetry(placementId, LoadInter);
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
                    var statusJson = ATInterstitialAd.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<CoreLiteTpnAdStatus>(statusJson);
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
                
                CoreLogUtils.Debug("[CoreLiteTpn] Inter OnAdShowFail: " + placementId);
                
                // IV_ShowFail 通用打点
                CoreAnalyticsMgr.TrackEvent("IV_ShowFail", new Dictionary<string, object>
                {
                    { "Position", CoreAdsMgr.Instance._ivPos ?? "" },
                    { "ad_platform", PlatformName },
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_ecpm", adRevenue*1000 }
                });
                
                _cachedAdInfo.Remove(placementId);  // 清空缓存
                CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInter(placementId);
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
                if (!_placementRevenue.ContainsKey(placementId)) _placementRevenue[placementId] = revenue;
                string pos = CoreAdsMgr.Instance._ivPos ?? "";
                string format = CoreLiteAdsType.Interstitial.ToString();
                int formatNumber = CoreAdsPrefs.GetAndIncrementFormatPlayCount(format);
                int positionNumber = CoreAdsPrefs.GetAndIncrementPositionPlayCount(pos);
                CoreAdsMgr.Instance._ivShowCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", pos },
                    { "ad_platform",PlatformName },
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_ecpm", revenue*1000 }
                });
                if (revenue > 0)
                {
                    CoreAnalyticsMgr.TrackAdImpressionEvent(
                        revenue, adSource, adId, placementId,
                        format, PlatformName,
                        pos,
                        formatNumber,
                        positionNumber);
                }
#if UNITY_ANDROID
                CoreAnalyticsMgr.TrackEvent("Up2SDK_ToponAdShow", new Dictionary<string, object>
                {
                    { "ad_platform", "Topon" },
                    { "topon_firm_id", firmId },
                    { "ads_type", "INTERSTITIAL" },
                    { "ad_id", placementId },
                    { "ecpm", revenue * 1000 },
                    { "show_id", "" }
                });
                Up2SDKManager.Instance.OnToponAdShow(firmId, AdsType.Interstitial, placementId, revenue * 1000, "");
#endif
                CoreLogUtils.Debug("[CoreLiteTpn] Inter OnAdShow: " + placementId);
            }, null);
        }

        private void OnAdClose(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                CoreLogUtils.Debug("[CoreLiteTpn] Inter OnAdClose: " + placementId);
                _placementRevenue.Remove(placementId);
                _cachedAdInfo.Remove(placementId);  // 清空缓存
                CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInter(placementId);
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
                CoreLogUtils.Debug("[CoreLiteTpn] Inter placement " + placementId + " max retry " + MaxRetry + " reached");
                return;
            }
            double delay = Math.Pow(2, Math.Min(MaxRetry, count));
            CoreFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () => loadAction(placementId));
        }
    }
}
