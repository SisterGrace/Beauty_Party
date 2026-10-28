#if ADMOB_AD
using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob 插屏（直连，支持多广告位）。
    /// 展示后通过 OnAdPaid 回写「上一次展示价格」到 SkyAdmobRevenueCache，供下一次竞价使用。
    /// </summary>
    public class SkyAdmobInterstitial
    {
        private const int MaxRetryCount = 6;
        private const string PlatformName = "Admob";

        private readonly List<string> _unitIds = new List<string>();
        private readonly Dictionary<string, InterstitialAd> _ads = new Dictionary<string, InterstitialAd>();
        private readonly Dictionary<string, int> _retryCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _loadStartTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, string> _adIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _adSources = new Dictionary<string, string>();

        private bool _destroyed;

        #region 初始化 / 加载

        public void Initialize(List<string> unitIds)
        {
            if (unitIds == null || unitIds.Count == 0)
            {
                SkyLogUtils.Debug("[Admob] Interstitial 未配置广告位");
                return;
            }

            _unitIds.Clear();
            foreach (var id in unitIds)
            {
                if (string.IsNullOrEmpty(id) || _unitIds.Contains(id)) continue;
                _unitIds.Add(id);
            }

            foreach (var id in _unitIds)
            {
                LoadAd(id);
            }
        }

        public void LoadAll()
        {
            foreach (var id in _unitIds)
            {
                if (_ads.ContainsKey(id)) continue;
                LoadAd(id);
            }
        }

        private void LoadAd(string unitId)
        {
            if (_destroyed || string.IsNullOrEmpty(unitId)) return;

            _loadStartTimes[unitId] = Time.realtimeSinceStartup;
            SkyAdsEvents.LogStartLoadEvent(unitId, PlatformName, AdLoadType.Interstitial);

            InterstitialAd.Load(unitId, new AdRequest(), (ad, error) =>
            {
                if (_destroyed) return;

                if (error != null || ad == null)
                {
                    OnLoadFailed(unitId, error);
                    return;
                }

                OnLoadSucceeded(unitId, ad);
            });
        }

        private void OnLoadFailed(string unitId, LoadAdError error)
        {
            string msg = error != null ? error.GetMessage() : "unknown";
            SkyAdsEvents.LogFailToLoadEvent(unitId, PlatformName, msg, GetLoadDuration(unitId), AdLoadType.Interstitial);
            SkyLogUtils.Debug($"[Admob] Interstitial 加载失败 {unitId} : {msg}");

            int retryCount = _retryCounts.TryGetValue(unitId, out var count) ? count + 1 : 1;
            _retryCounts[unitId] = retryCount;

            if (retryCount >= MaxRetryCount)
            {
                SkyLogUtils.Debug($"[Admob] Interstitial {unitId} 已达最大重试次数 {MaxRetryCount}");
                return;
            }

            double delay = Math.Pow(2, Math.Min(MaxRetryCount, retryCount));
            SkyFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () =>
            {
                if (!_destroyed) LoadAd(unitId);
            });
        }

        private void OnLoadSucceeded(string unitId, InterstitialAd ad)
        {
            _ads[unitId] = ad;
            _retryCounts[unitId] = 0;

            var adapterInfo = ad.GetResponseInfo()?.GetLoadedAdapterResponseInfo();
            string sourceInstanceId = adapterInfo?.AdSourceInstanceId ?? "";
            string sourceName = adapterInfo?.AdSourceName ?? "";
            _adIds[unitId] = sourceInstanceId;
            _adSources[unitId] = sourceName;

            // Load 阶段拿不到 eCPM，这里只上报 0，真实价格由 OnAdPaid 回写
            SkyAdsEvents.LogLoadedEvent(sourceInstanceId, sourceName, PlatformName,
                GetLoadDuration(unitId), AdLoadType.Interstitial, unitId, 0);
            SkyLogUtils.Debug($"[Admob] Interstitial 加载成功 {unitId} source:{sourceName}");

            RegisterCallbacks(unitId, ad, sourceInstanceId, sourceName);
        }

        private void RegisterCallbacks(string unitId, InterstitialAd ad, string adId, string adSource)
        {
            ad.OnAdPaid += adValue => HandleAdPaid(unitId, adValue, adId, adSource);

            ad.OnAdImpressionRecorded += () =>
                SkyLogUtils.Debug($"[Admob] Interstitial 曝光记录 {unitId}");

            ad.OnAdClicked += () =>
            {
                SkyLogUtils.Debug($"[Admob] Interstitial 点击 {unitId}");
                SkyAdsEvents.LogClickEvent(AdLoadType.Interstitial,
                    SkyAdmobRevenueCache.Instance.GetInterstitialLastShowRevenue(),
                    SkyAdsMgr.Instance._ivPos, adId, adSource, PlatformName, unitId);
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                if (_destroyed) return;
                SkyLogUtils.Debug($"[Admob] Interstitial 展示开始 {unitId}");

                // 兜底逻辑：如果没有历史记录，立即写入 0 作为基准价
                if (!SkyAdmobRevenueCache.Instance.HasInterstitialRecord)
                {
                    SkyAdmobRevenueCache.Instance.UpdateInterstitialLastShowRevenue(0, "USD", unitId);
                    SkyLogUtils.Debug("[Admob] Interstitial 首次展示且无历史记录，写入 0 基准价打破强制展示循环");
                }

                SkyAdsMgr.Instance._ivShowCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._ivPos ?? "" },
                    { "ad_platform", PlatformName },
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_ecpm", SkyAdmobRevenueCache.Instance.GetInterstitialLastShowRevenue() }
                });
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                if (_destroyed) return;
                SkyLogUtils.Debug($"[Admob] Interstitial 关闭 {unitId}");

                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();

                // AdMob 广告一次性消耗，关闭后立即补载
                DestroyAd(unitId);
                LoadAd(unitId);
            };

            ad.OnAdFullScreenContentFailed += adError =>
            {
                if (_destroyed) return;
                SkyLogUtils.Debug($"[Admob] Interstitial 展示失败 {unitId} : {(adError != null ? adError.GetMessage() : "unknown")}");

                // 插屏展示失败走关闭回调，避免调用方卡在等待里
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();

                DestroyAd(unitId);
                LoadAd(unitId);
            };
        }

        /// <summary>
        /// OnAdPaid：广告产生展示后触发，拿到本次真实收益，写入缓存供下一次竞价使用。
        /// </summary>
        private void HandleAdPaid(string unitId, AdValue adValue, string adId, string adSource)
        {
            if (adValue == null) return;

            double rawRevenue = adValue.Value / 1_000_000.0;
            string currency = adValue.CurrencyCode;

			SkyLogUtils.Debug($"[Admob] Interstitial {unitId} OnAdPaid | 原始值(微美元): {adValue.Value} | 原始收益: ${rawRevenue:F6} {currency} | 精度: {adValue.Precision}");

            // 货币统一转换：使用全局 SkyCurrencyConverter
            double revenueInUsd = SkyCurrencyConverter.ToUSD(rawRevenue, currency);

			SkyAdmobRevenueCache.Instance.UpdateInterstitialLastShowRevenue(revenueInUsd, "USD", unitId);

            string pos = SkyAdsMgr.Instance._ivPos ?? "";
            int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(SkyAdsType.Interstitial.ToString());
            int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);

            SkyAnalyticsMgr.TrackAdImpressionEvent(
				revenueInUsd, adSource, adId, unitId,
                SkyAdsType.Interstitial.ToString(), PlatformName,
                pos, formatNumber, positionNumber, "USD");
        }

        #endregion

        #region 展示

        public bool IsReady()
        {
            return !string.IsNullOrEmpty(GetAvailableUnit());
        }

        public string GetAvailableUnit()
        {
            foreach (var id in _unitIds)
            {
                if (_ads.TryGetValue(id, out var ad) && ad != null && ad.CanShowAd())
                    return id;
            }
            return null;
        }

        /// <summary>展示当前可用的插屏；无可用时回调关闭，保证调用链不中断。</summary>
        public void Show()
        {
            var unitId = GetAvailableUnit();
            if (string.IsNullOrEmpty(unitId))
            {
                SkyLogUtils.Debug("[Admob] Interstitial 无可用广告位");
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadAll();
                return;
            }

            _ads[unitId].Show();
            SkyLogUtils.Debug($"[Admob] Interstitial 展示 {unitId}");
        }

        #endregion

        #region 信息查询（打点用）

        public (string adId, string adSource, double adEcpm) GetAdInfo()
        {
            var unitId = GetAvailableUnit();
            if (string.IsNullOrEmpty(unitId))
                return ("", PlatformName, SkyAdmobRevenueCache.Instance.GetInterstitialLastShowRevenue());

            _adIds.TryGetValue(unitId, out var adId);
            _adSources.TryGetValue(unitId, out var adSource);
            return (adId ?? "", string.IsNullOrEmpty(adSource) ? PlatformName : adSource,
                SkyAdmobRevenueCache.Instance.GetInterstitialLastShowRevenue());
        }

        #endregion

        #region 清理

        public void ClearAd()
        {
            foreach (var id in new List<string>(_ads.Keys))
            {
                DestroyAd(id);
            }
            _ads.Clear();
            _unitIds.Clear();
            _retryCounts.Clear();
            _loadStartTimes.Clear();
            _adIds.Clear();
            _adSources.Clear();
            SkyLogUtils.Debug("[Admob] Interstitial 已清理");
        }

        public void Destroy()
        {
            _destroyed = true;
            ClearAd();
        }

        private void DestroyAd(string unitId)
        {
            if (!_ads.TryGetValue(unitId, out var ad)) return;
            try
            {
                ad?.Destroy();
            }
            catch (Exception e)
            {
                SkyLogUtils.Debug($"[Admob] Interstitial Destroy 异常 {unitId}: {e.Message}");
            }
            _ads.Remove(unitId);
        }

        private float GetLoadDuration(string unitId)
        {
            float start = _loadStartTimes.TryGetValue(unitId, out var t) ? t : 0f;
            return Mathf.Ceil(Time.realtimeSinceStartup - start);
        }

        #endregion
    }
}
#endif
