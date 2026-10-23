#if ADMOB_AD
using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;

namespace Sky
{
    /// <summary>
    /// AdMob 开屏广告管理类，支持多广告位。
    /// 
    /// 使用场景：冷启动或场景切换时展示全屏开屏广告。
    /// 
    /// 关键逻辑：
    /// - 加载成功后在 OnAdPaid 中将 eCPM 写入 SkyAdmobEcpmCache（用于后续竞价）
    /// - 加载失败时进行指数退避重试，最多 6 次
    /// - 展示成功后自动重载
    /// - 展示关闭后（无论是否加载成功）触发回调并重载
    /// </summary>
    public class SkyAdmobSplash
    {
        private readonly List<string> _unitIds = new List<string>();
        private readonly Dictionary<string, AppOpenAd> _loadedAds = new Dictionary<string, AppOpenAd>();
        private readonly Dictionary<string, int> _retryCount = new Dictionary<string, int>();

        private const int MaxRetryCount = 6;

        public void Initialize(List<string> unitIds)
        {
            _unitIds.Clear();
            _unitIds.AddRange(unitIds ?? new List<string>());

            SkyLogUtils.Debug($"[Admob Splash] Initialize units: {string.Join(", ", _unitIds)}");
            LoadAll();
        }

        public void LoadAll()
        {
            if (_unitIds.Count == 0)
            {
                SkyLogUtils.Debug("[Admob Splash] 无可用广告位，跳过加载");
                return;
            }

            foreach (var unitId in _unitIds)
            {
                LoadAd(unitId);
            }
        }

        private void LoadAd(string unitId, int retryAttempt = 0)
        {
            if (retryAttempt > MaxRetryCount)
            {
                SkyLogUtils.Debug($"[Admob Splash] {unitId} 已达最大重试次数，放弃");
                return;
            }

            if (_loadedAds.ContainsKey(unitId))
            {
                SkyLogUtils.Debug($"[Admob Splash] {unitId} 已存在实例，销毁后重新加载");
                DestroyAd(unitId);
            }

            SkyLogUtils.Debug($"[Admob Splash] 开始加载 {unitId} (retry={retryAttempt})");

            var request = new AdRequest();
            AppOpenAd.Load(unitId, request, (ad, error) =>
            {
                if (error != null)
                {
                    SkyLogUtils.Debug($"[Admob Splash] {unitId} 加载失败: {error.GetMessage()}");

                    int nextRetry = retryAttempt + 1;
                    _retryCount[unitId] = nextRetry;

                    if (nextRetry <= MaxRetryCount)
                    {
                        float delay = Mathf.Pow(2, nextRetry); // 指数退避: 2^n 秒
                        SkyLogUtils.Debug($"[Admob Splash] {unitId} 将在 {delay}s 后重试 (retry={nextRetry})");
                        SkyFuncDelayExecuteUtils.Instance.DelayExecute(delay, () => LoadAd(unitId, nextRetry));
                    }
                    return;
                }

                if (ad == null)
                {
                    SkyLogUtils.Debug($"[Admob Splash] {unitId} 加载成功但 ad 为 null");
                    return;
                }

                SkyLogUtils.Debug($"[Admob Splash] {unitId} 加载成功");
                _loadedAds[unitId] = ad;
                _retryCount[unitId] = 0; // 重置重试计数

                RegisterCallbacks(unitId, ad);
            });
        }


        private void RegisterCallbacks(string unitId, AppOpenAd ad)
        {
            // 展示后收益回调（写入 eCPM 到缓存）
            ad.OnAdPaid += adValue =>
            {
                HandleAdPaid(unitId, adValue, ad.GetResponseInfo()?.GetResponseId() ?? "", "admob");
            };

            // 展示成功
            ad.OnAdFullScreenContentOpened += () =>
            {
                SkyLogUtils.Debug($"[Admob Splash] {unitId} 展示成功");
            };

            // 展示关闭（无论用户主动关闭或自动关闭）
            ad.OnAdFullScreenContentClosed += () =>
            {
                SkyLogUtils.Debug($"[Admob Splash] {unitId} 展示关闭");
                DestroyAd(unitId);
                LoadAd(unitId); // 自动重载
                SkyAdsMgr.Instance._spCloseCallback?.Invoke();
            };

            // 展示失败（已加载但展示时出错）
            ad.OnAdFullScreenContentFailed += adError =>
            {
                SkyLogUtils.Debug($"[Admob Splash] {unitId} 展示失败: {adError.GetMessage()}");
                SkyAdsMgr.Instance._spCloseCallback?.Invoke(); // 确保调用方不被阻塞
                DestroyAd(unitId);
                LoadAd(unitId);
            };
        }

        private void HandleAdPaid(string unitId, AdValue adValue, string adId, string adSource)
        {
            double ecpm = adValue.Value / 1_000_000.0; // micro USD → USD
            string currency = adValue.CurrencyCode;

            SkyLogUtils.Debug($"[Admob Splash] {unitId} OnAdPaid eCPM={ecpm:F4} {currency}");

            // 更新缓存（只保存 USD，其他货币丢弃）
            SkyAdmobEcpmCache.Instance.UpdateSplashLastShowEcpm(ecpm, currency, unitId);

            // 上报广告展示事件
            SkyAnalyticsMgr.TrackAdImpressionEvent(
                ecpm, adSource, adId, unitId,
                SkyAdsType.Splash.ToString(), SkyPlatformType.Admob.ToString(),
                SkyAdsMgr.Instance._spPos ?? ""
            );
        }

        public bool IsReady()
        {
            foreach (var ad in _loadedAds.Values)
            {
                if (ad != null) return true;
            }
            return false;
        }

        public void Show()
        {
            foreach (var kvp in _loadedAds)
            {
                if (kvp.Value != null)
                {
                    SkyLogUtils.Debug($"[Admob Splash] 展示开屏 {kvp.Key}");
                    kvp.Value.Show();
                    return; // 只展示第一个可用的广告
                }
            }

            SkyLogUtils.Debug("[Admob Splash] 无可用广告可展示");
        }

        public void ClearAd()
        {
            foreach (var unitId in _loadedAds.Keys)
            {
                DestroyAd(unitId);
            }
            _loadedAds.Clear();
            _retryCount.Clear();
            SkyLogUtils.Debug("[Admob Splash] 所有广告已清空");
        }

        private void DestroyAd(string unitId)
        {
            if (_loadedAds.TryGetValue(unitId, out var ad))
            {
                ad?.Destroy();
                _loadedAds.Remove(unitId);
                SkyLogUtils.Debug($"[Admob Splash] {unitId} 已销毁");
            }
        }
    }
}
#endif
