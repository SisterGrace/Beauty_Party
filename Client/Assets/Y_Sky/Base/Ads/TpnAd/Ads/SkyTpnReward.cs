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
    /// TPN(TopOn) 激励视频广告封装，手动加载，支持多广告位。使用 ATRewardedVideo 而非 ATRewardedAutoVideo。
    /// </summary>
    public class SkyTpnReward : SkySingleton<SkyTpnReward>
    {
        private List<string> _placementIds = new List<string>();
        private Dictionary<string, float> _startLoadTimes = new Dictionary<string, float>();
        private Dictionary<string, int> _retryAttempts = new Dictionary<string, int>();
        private Dictionary<string, double> _placementRevenue = new Dictionary<string, double>();
        private Dictionary<string, bool> _receivedRewardByPlacement = new Dictionary<string, bool>();
        // 缓存广告信息：adId, source, ecpm
        private Dictionary<string, (string adId, string source, double ecpm)> _cachedAdInfo = new Dictionary<string, (string adId, string source, double ecpm)>();
        private bool _callbacksRegistered;
        private const string PlatformName = "TPN";
        private const int MaxRetry = 6;
       
        public void ClearAd()
        {
            _placementIds.Clear();          // 清空列表
            _placementRevenue.Clear();
            _retryAttempts.Clear();
            _cachedAdInfo.Clear();
            SkyLogUtils.Debug("[SkyTpnReward]  ads cleared.");
        }
        public void SetPlacementIds(List<string> placementIds)
        {
            _placementIds = placementIds != null ? new List<string>(placementIds) : new List<string>();
            RegisterRevenueListeners();
        }

        public void LoadReward()
        {
            if (!_callbacksRegistered)
            {
                RegisterCallbacks();
                _callbacksRegistered = true;
            }
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                LoadReward(placementId);
            }
        }

        private void LoadReward(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            SkyAdsEvents.LogStartLoadEvent(placementId, PlatformName, AdLoadType.Reward);
            var jsonmap = new Dictionary<string, string>();
            ATRewardedVideo.Instance.loadVideoAd(placementId, jsonmap);
            _startLoadTimes[placementId] = Time.realtimeSinceStartup;
            SkyLogUtils.Debug("[SkyTpn] Load reward placement: " + placementId);
        }

        public bool IsRewardReady()
        {
            foreach (var placementId in _placementIds)
            {
                if (!string.IsNullOrEmpty(placementId) && ATRewardedVideo.Instance.hasAdReady(placementId))
                    return true;
            }
            return false;
        }

        /// <summary>返回当前已就绪广告位中收益最高的 eCPM，无则返回 -1。已就绪但尚未写入 revenue 的按 0 参与比较，避免回调时序导致被排除竞价。</summary>
        public double GetRewardRevenue()
        {
            double maxRevenue = -1;
            foreach (var placementId in _placementIds)
            {
                if (string.IsNullOrEmpty(placementId)) continue;
                if (!ATRewardedVideo.Instance.hasAdReady(placementId)) continue;
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
                if (!ATRewardedVideo.Instance.hasAdReady(placementId)) continue;
                double rev = _placementRevenue.TryGetValue(placementId, out var r) ? r : 0;
                if (rev > maxRevenue)
                {
                    maxRevenue = rev;
                    best = placementId;
                }
            }
            return best;
        }

        public void ShowReward(string placementId)
        {
            if (string.IsNullOrEmpty(placementId)) return;
            _receivedRewardByPlacement[placementId] = false;
            ATRewardedVideo.Instance.showAd(placementId);
            SkyLogUtils.Debug("[SkyTpn] Show reward: " + placementId);
        }
        private TpnRevenueListener _revenueListener;


        private void RegisterRevenueListeners()
        {
            _revenueListener ??= new TpnRevenueListener();
            foreach (var id in _placementIds)
            {
                if (string.IsNullOrEmpty(id)) continue;
                ATRewardedVideo.Instance.client.setAdRevenueListener(id, _revenueListener);
            }
        }
        private void RegisterCallbacks()
        {
            ATRewardedVideo.Instance.client.onAdLoadEvent += OnAdLoad;
            ATRewardedVideo.Instance.client.onAdLoadFailureEvent += OnAdLoadFail;
            ATRewardedVideo.Instance.client.onAdVideoStartEvent += OnAdVideoStart;
            ATRewardedVideo.Instance.client.onAdVideoFailureEvent += OnAdVideoPlayFail;
            ATRewardedVideo.Instance.client.onRewardEvent += OnRewardEvent;
            ATRewardedVideo.Instance.client.onAdVideoCloseEvent += OnAdVideoClose;
            ATRewardedVideo.Instance.client.onAdClickEvent += OnAdClick;
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
                SkyLogUtils.Debug("[SkyTpn] Reward OnAdClick: " + placementId);
                SkyAdsEvents.LogClickEvent(
                    AdLoadType.Reward,
                    GetRewardRevenue(),
                    SkyAdsMgr.Instance._rvPos,
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
                    { "ads_type", "REWARD_VIDEO" },
                    { "ad_id", placementId },
                    { "show_id", "" }
                });
#if Up_2SDK
                Up2SDKManager.Instance.OnToponAdClick(firmId, AdsType.Rewarded, placementId, "");
#endif
#endif
            }, null);
        }

        private void OnAdLoad(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";

                SkyLogUtils.Debug("TPN OnAdLoad: " + JsonUtility.ToJson(e.callbackInfo) + " placementId: " + placementId + " s:" + ATRewardedVideo.Instance.checkAdStatus(placementId));

                if (!_placementIds.Contains(placementId)) return;
                float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - (_startLoadTimes.TryGetValue(placementId, out var t) ? t : 0));
                double revenue = 0;
                string adId = "";
                string source = "";

                try
                {
                    var statusJson = ATRewardedVideo.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);//获取广告状态，上报收益数据
                    if (status?.adInfo != null)
                    {
                        revenue = status.adInfo.publisher_revenue;
                        _placementRevenue[placementId] = revenue;
                        adId = status.adInfo.network_placement_id;
                        source = "Topon_" + status.adInfo.network_firm_id.ToString();
                    }
                    _cachedAdInfo[placementId] = (adId, source, revenue);
                }
                catch { }
                
                SkyLogUtils.Debug("tpn onadload " + source + " adid:" + adId + " firm:" + e.callbackInfo.network_firm_id + " revenue:" + revenue);
                SkyAdsEvents.LogLoadedEvent(adId, source, PlatformName, loadTime, AdLoadType.Reward, placementId, revenue);
                if (!_retryAttempts.ContainsKey(placementId)) _retryAttempts[placementId] = 0;
                else _retryAttempts[placementId] = 0;
                SkyLogUtils.Debug("[SkyTpn] Reward OnAdLoad: " + placementId);
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
                SkyAdsEvents.LogFailToLoadEvent(placementId, PlatformName, error, loadTime, AdLoadType.Reward);
                SkyLogUtils.Debug("[SkyTpn] Reward OnAdLoadFail: " + placementId + " " + error);
                ScheduleRetry(placementId, LoadReward);
            }, null);
        }

        private void OnAdVideoStart(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                int firmId = e?.callbackInfo?.network_firm_id ?? 0;
                string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + firmId : PlatformName;
                string adId = e?.callbackInfo?.network_placement_id ?? "";
                double revenue = e?.callbackInfo?.publisher_revenue ?? 0;
                string currency = e?.callbackInfo?.currency ?? "USD";  // 从 TopOn 回调中获取货币类型
                SkyLogUtils.Debug("tpn OnAdVideoStart " + JsonUtility.ToJson(e.callbackInfo) + " placementId: " + placementId);
                if (!_placementRevenue.ContainsKey(placementId)) _placementRevenue[placementId] = revenue;
                string pos = SkyAdsMgr.Instance._rvPos ?? "";
                string format = SkyAdsType.Rewarded.ToString();
                int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(format);
                int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);
                SkyLogUtils.Debug("tpn OnAdVideoStart " + adSource + " adid:" + adId + " reve:" + revenue);
                SkyAdsEvents.LogRvShowEvent(
                    pos,
                    adId,
                    adSource,
                    revenue,
                    formatNumber,
                    SkyAdsPrefs.GetFirstShowRvTime(),
                    PlatformName,
                    placementId
                );
                // 缓存业务上下文，供收入回调使用
                TopOnRevenueReporter.SetRevenueContext(placementId, pos, formatNumber, positionNumber);
                SkyAdsMgr.Instance._rvShowCallback?.Invoke();
                /* if (revenue > 0)
                 {
                     SkyAnalyticsMgr.TrackAdImpressionEvent(//删掉成功展示的收入上报
                         revenue, adSource, adId, placementId,
                         format, PlatformName,
                         pos,
                         formatNumber,
                         positionNumber,
                         currency);  // 传递货币类型
                 }*/
#if UNITY_ANDROID
                /*   SkyAnalyticsMgr.TrackEvent("Up2SDK_ToponAdShow", new Dictionary<string, object>
                   {
                       { "ad_platform", "Topon" },
                       { "topon_firm_id", firmId },
                       { "ads_type", "REWARD_VIDEO" },
                       { "ad_id", placementId },
                       { "ecpm", revenue * 1000 },
                       { "show_id", "" }
                   });*/
#if Up_2SDK
                Up2SDKManager.Instance.OnToponAdShow(firmId, AdsType.Rewarded, placementId, revenue * 1000, "");
#endif
#endif
                SkyLogUtils.Debug("[SkyTpn] Reward OnAdVideoStart: " + placementId);
            }, null);
        }

        private void OnAdVideoPlayFail(object sender, ATAdErrorEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                string errorMsg = e?.errorMessage ?? "";
                string adId = "";
                string adSource = "";
                double adRrevenue = 0;
                
                // 尝试实时获取广告信息（最准确）
                try
                {
                    var statusJson = ATRewardedVideo.Instance.checkAdStatus(placementId);
                    var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);
                    if (status?.adInfo != null)
                    {
                        adId = status.adInfo.network_placement_id;
                        adSource = "Topon_" + status.adInfo.network_firm_id.ToString();
                        adRrevenue = status.adInfo.publisher_revenue;
                    }
                }
                catch
                {
                    // checkAdStatus 失败，说明广告已经不可用，无法获取信息
                }
                
                SkyLogUtils.Debug("[SkyTpn] Reward OnAdVideoPlayFail: " + placementId + " " + errorMsg);
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    //{ "show_fail_reason", SkyThinkingDataEvents.FormatEventName(errorMsg) },
                    //{ "unitId", placementId }
                    { "ad_platform", PlatformName},
                    { "ad_source", adSource },
                    { "ad_id", adId },
                    { "ad_ecpm", adRrevenue*1000 }
                });

                _cachedAdInfo.Remove(placementId);  // 清空缓存
                LoadReward(placementId);
            }, null);
        }

        private void OnRewardEvent(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                _receivedRewardByPlacement[placementId] = true;
                SkyLogUtils.Debug("[SkyTpn] Reward OnRewardEvent: " + placementId);
            }, null);
        }

        private void OnAdVideoClose(object sender, ATAdEventArgs e)
        {
            Loom.QueueOnMainThread(_ =>
            {
                var placementId = e?.placementId ?? "";
                bool received = _receivedRewardByPlacement.TryGetValue(placementId, out var r) && r;
                _receivedRewardByPlacement.Remove(placementId);
                _placementRevenue.Remove(placementId);
                _cachedAdInfo.Remove(placementId);  // 清空缓存
                SkyLogUtils.Debug("[SkyTpn] Reward OnAdVideoClose: " + placementId);
                /*if (received)
                {*/
                    string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + e.callbackInfo.network_firm_id : PlatformName;
                    string adId = e?.callbackInfo?.network_placement_id ?? "";
                    SkyAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object>
                    {
                        {"isended",received }
                        //{ "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                        //{ "ad_source", adSource },
                        //{ "ad_id", adId },
                        //{ "ad_platform", PlatformName },
                        //{ "unitId", placementId }
                    });
               /* }
                else
                {
                    SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                    {
                        { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                        //{ "show_fail_reason", "not_received_reward" },
                        //{ "ad_platform", PlatformName },
                        //{ "unitId", placementId }
                    });
                }*/
                LoadReward(placementId);
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
                SkyLogUtils.Debug("[SkyTpn] Reward placement " + placementId + " max retry " + MaxRetry + " reached");
                return;
            }
            double delay = Math.Pow(2, Math.Min(MaxRetry, count));
            SkyFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () => loadAction(placementId));
        }
    }
}
#endif