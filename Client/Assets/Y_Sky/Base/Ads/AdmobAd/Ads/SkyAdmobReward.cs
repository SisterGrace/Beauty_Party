#if ADMOB_AD
using System;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob 激励视频（直连，支持多广告位）。
    /// 展示后通过 OnAdPaid 回写「上一次展示价格」到 SkyAdmobEcpmCache，供下一次竞价使用。
    /// </summary>
    public class SkyAdmobReward
    {
        private const int MaxRetryCount = 6;
        private const string PlatformName = "Admob";

        private readonly List<string> _unitIds = new List<string>();
        private readonly Dictionary<string, RewardedAd> _ads = new Dictionary<string, RewardedAd>();
        private readonly Dictionary<string, int> _retryCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _loadStartTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, string> _adIds = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _adSources = new Dictionary<string, string>();

        private bool _destroyed;
        private bool _rewarded;

        #region 初始化 / 加载

        public void Initialize(List<string> unitIds)
        {
            if (unitIds == null || unitIds.Count == 0)
            {
                SkyLogUtils.Debug("[Admob] Rewarded 未配置广告位");
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
            SkyAdsEvents.LogStartLoadEvent(unitId, PlatformName, AdLoadType.Reward);

            RewardedAd.Load(unitId, new AdRequest(), (ad, error) =>
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
            float loadTime = GetLoadDuration(unitId);
            string msg = error != null ? error.GetMessage() : "unknown";
            SkyAdsEvents.LogFailToLoadEvent(unitId, PlatformName, msg, loadTime, AdLoadType.Reward);
            SkyLogUtils.Debug($"[Admob] Rewarded 加载失败 {unitId} : {msg}");

            int retryCount = _retryCounts.TryGetValue(unitId, out var count) ? count + 1 : 1;
            _retryCounts[unitId] = retryCount;

            if (retryCount >= MaxRetryCount)
            {
                SkyLogUtils.Debug($"[Admob] Rewarded {unitId} 已达最大重试次数 {MaxRetryCount}");
                return;
            }

            double delay = Math.Pow(2, Math.Min(MaxRetryCount, retryCount));
            SkyFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () =>
            {
                if (!_destroyed) LoadAd(unitId);
            });
        }

        private void OnLoadSucceeded(string unitId, RewardedAd ad)
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
                GetLoadDuration(unitId), AdLoadType.Reward, unitId, 0);
            SkyLogUtils.Debug($"[Admob] Rewarded 加载成功 {unitId} source:{sourceName}");

            RegisterCallbacks(unitId, ad, sourceInstanceId, sourceName);
        }

        private void RegisterCallbacks(string unitId, RewardedAd ad, string adId, string adSource)
        {
            ad.OnAdPaid += adValue => HandleAdPaid(unitId, adValue, adId, adSource);

            ad.OnAdImpressionRecorded += () =>
                SkyLogUtils.Debug($"[Admob] Rewarded 曝光记录 {unitId}");

            ad.OnAdClicked += () =>
            {
                SkyLogUtils.Debug($"[Admob] Rewarded 点击 {unitId}");
                SkyAdsEvents.LogClickEvent(AdLoadType.Reward,
                    SkyAdmobEcpmCache.Instance.GetRewardedLastShowEcpm(),
                    SkyAdsMgr.Instance._rvPos, adId, adSource, PlatformName, unitId);
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                if (_destroyed) return;
                SkyLogUtils.Debug($"[Admob] Rewarded 展示开始 {unitId}");

                string pos = SkyAdsMgr.Instance._rvPos ?? "";
                int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(SkyAdsType.Rewarded.ToString());
                SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);

                SkyAdsEvents.LogRvShowEvent(pos, adId, adSource,
                    SkyAdmobEcpmCache.Instance.GetRewardedLastShowEcpm(),
                    formatNumber, SkyAdsPrefs.GetFirstShowRvTime(), PlatformName, unitId);

                SkyAdsMgr.Instance._rvShowCallback?.Invoke();
            };

            ad.OnAdFullScreenContentClosed += () =>
            {
                if (_destroyed) return;
                SkyLogUtils.Debug($"[Admob] Rewarded 关闭 {unitId}");

                bool isEnded = _rewarded;
                _rewarded = false;

                SkyAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "ad_id", adId },
                    { "ad_source", adSource },
                    { "ad_platform", PlatformName },
                    { "unitId", unitId },
                    { "isended", isEnded }
                });

                // AdMob 广告一次性消耗，关闭后立即补载
                DestroyAd(unitId);
                LoadAd(unitId);
            };

            ad.OnAdFullScreenContentFailed += adError =>
            {
                if (_destroyed) return;
                string err = adError != null ? adError.GetMessage() : "unknown";
                SkyLogUtils.Debug($"[Admob] Rewarded 展示失败 {unitId} : {err}");

                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", err },
                    { "ad_platform", PlatformName },
                    { "unitId", unitId }
                });

                DestroyAd(unitId);
                LoadAd(unitId);
            };
        }

        /// <summary>
        /// OnAdPaid：广告产生展示后触发，拿到本次真实收益。
        /// 这是 AdMob 唯一能拿到 eCPM 的时机，写入缓存供下一次竞价使用。
        /// </summary>
        private void HandleAdPaid(string unitId, AdValue adValue, string adId, string adSource)
        {
            if (adValue == null) return;

            double ecpm = adValue.Value / 1_000_000.0;
            string currency = adValue.CurrencyCode;

            SkyLogUtils.Debug($"[Admob] Rewarded OnAdPaid {unitId} : ${ecpm:F4} {currency} precision:{adValue.Precision}");

            // 竞价价格来源：本次展示的真实 eCPM
            SkyAdmobEcpmCache.Instance.UpdateRewardedLastShowEcpm(ecpm, currency, unitId);

            string pos = SkyAdsMgr.Instance._rvPos ?? "";
            int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(SkyAdsType.Rewarded.ToString());
            int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);

            SkyAnalyticsMgr.TrackAdImpressionEvent(
                ecpm, adSource, adId, unitId,
                SkyAdsType.Rewarded.ToString(), PlatformName,
                pos, formatNumber, positionNumber, currency);
        }

        #endregion

        #region 展示

        public bool IsReady()
        {
            return !string.IsNullOrEmpty(GetAvailableUnit());
        }

        /// <summary>取一个当前可展示的广告位（同一格式共用一份展示价格，无需按价格挑选）。</summary>
        public string GetAvailableUnit()
        {
            foreach (var id in _unitIds)
            {
                if (_ads.TryGetValue(id, out var ad) && ad != null && ad.CanShowAd())
                    return id;
            }
            return null;
        }

        public void Show()
        {
            var unitId = GetAvailableUnit();
            if (string.IsNullOrEmpty(unitId))
            {
                SkyLogUtils.Debug("[Admob] Rewarded 无可用广告位");
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", "not_ready" },
                    { "ad_platform", PlatformName },
                    { "unitId", "" }
                });
                LoadAll();
                return;
            }

            _rewarded = false;
            var ad = _ads[unitId];
            ad.Show(reward =>
            {
                _rewarded = true;
                SkyLogUtils.Debug($"[Admob] Rewarded 发奖 amount:{reward?.Amount} type:{reward?.Type}");
            });

            SkyLogUtils.Debug($"[Admob] Rewarded 展示 {unitId}");
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
            SkyLogUtils.Debug("[Admob] Rewarded 已清理");
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
                SkyLogUtils.Debug($"[Admob] Rewarded Destroy 异常 {unitId}: {e.Message}");
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
