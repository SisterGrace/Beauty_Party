using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sky
{
    public class SkyMaxReward : SkySingleton<SkyMaxReward>
    {
        private string _placementId;
        private float _startLoadTime;
        private int _retryAttempt;
        private double _lastRevenue;
        private bool _rewardReceived;

        private const string PlatformName = "MAX";
        private const int MaxRetry = 6;

        public void SetPlacementId(string placementId)
        {
            _placementId = placementId;
        }

        public void LoadReward()
        {
            if (string.IsNullOrEmpty(_placementId))
            {
                SkyLogUtils.Error("[SkyMax] Reward placementId is empty");
                return;
            }
            RegisterCallbacks();
            _startLoadTime = Time.realtimeSinceStartup;
            MaxSdk.LoadRewardedAd(_placementId);
            SkyLogUtils.Debug("[SkyMax] Load reward: " + _placementId);
        }

        public bool IsRewardReady()
        {
            return !string.IsNullOrEmpty(_placementId) && MaxSdk.IsRewardedAdReady(_placementId);
        }

        public void ShowReward()
        {
            if (!IsRewardReady())
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", "not_ready" },
                    { "ad_platform", PlatformName },
                    { "unitId", _placementId ?? "" }
                });
                LoadReward();
                return;
            }
            _rewardReceived = false;
            MaxSdk.ShowRewardedAd(_placementId);
            SkyLogUtils.Debug("[SkyMax] Show reward: " + _placementId);
        }

        private void RegisterCallbacks()
        {
            // 避免重复注册
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;

            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnAdReceivedRewardEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - _startLoadTime);
            _lastRevenue = adInfo.Revenue;
            string adId = adInfo.CreativeId ?? "";
            string source = "MAX_" + adInfo.NetworkName;
            SkyAdsEvents.LogLoadedEvent(adId, source, PlatformName, loadTime, SkyAdsType.Rewarded, adUnitId, _lastRevenue);
            _retryAttempt = 0;
            SkyLogUtils.Debug("[SkyMax] Reward OnAdLoaded: " + adUnitId);
        }

        private void OnAdLoadFailedEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo)
        {
            if (adUnitId != _placementId) return;
            float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - _startLoadTime);
            SkyAdsEvents.LogFailToLoadEvent(adUnitId, PlatformName, errorInfo.Message, loadTime, SkyAdsType.Rewarded);
            SkyLogUtils.Debug("[SkyMax] Reward OnAdLoadFailed: " + adUnitId + " " + errorInfo.Message);
            ScheduleRetry();
        }

        private void OnAdDisplayedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            string adId = adInfo.CreativeId ?? "";
            string source = "MAX_" + adInfo.NetworkName;
            string pos = SkyAdsMgr.Instance._rvPos ?? "";
            int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(SkyAdsType.Rewarded.ToString());
            int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);
            SkyAdsEvents.LogRvShowEvent(pos, adId, source, formatNumber, SkyAdsPrefs.GetFirstShowRvTime(), PlatformName, adUnitId);
            SkyAdsMgr.Instance._rvShowCallback?.Invoke();

            if (_lastRevenue > 0)
            {
                SkyAnalyticsMgr.TrackAdImpressionEvent(_lastRevenue, source, adId, adUnitId,
                    SkyAdsType.Rewarded.ToString(), PlatformName, pos, formatNumber, positionNumber);
            }
            SkyLogUtils.Debug("[SkyMax] Reward OnAdDisplayed: " + adUnitId);
        }

        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            SkyLogUtils.Debug("[SkyMax] Reward OnAdDisplayFailed: " + adUnitId + " " + errorInfo.Message);
            SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
            {
                { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                { "show_fail_reason", errorInfo.Message },
                { "ad_platform", PlatformName },
                { "unitId", adUnitId }
            });
            LoadReward(); // 展示失败后重新加载
        }

        private void OnAdHiddenEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            SkyLogUtils.Debug("[SkyMax] Reward OnAdHidden: " + adUnitId);
            if (_rewardReceived)
            {
                string adId = adInfo.CreativeId ?? "";
                string source = "MAX_" + adInfo.NetworkName;
                SkyAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "ad_source", source },
                    { "ad_id", adId },
                    { "ad_platform", PlatformName },
                    { "unitId", adUnitId }
                });
            }
            else
            {
                SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
                {
                    { "Position", SkyAdsMgr.Instance._rvPos ?? "" },
                    { "show_fail_reason", "not_received_reward" },
                    { "ad_platform", PlatformName },
                    { "unitId", adUnitId }
                });
            }
            LoadReward(); // 关闭后预加载下一则
        }

        private void OnAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            _rewardReceived = true;
            SkyLogUtils.Debug("[SkyMax] Reward OnAdReceivedReward: " + adUnitId + " amount=" + reward.Amount);
        }

        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            _lastRevenue = adInfo.Revenue;
            SkyLogUtils.Debug("[SkyMax] Reward OnAdRevenuePaid: " + adUnitId + " revenue=" + adInfo.Revenue);
        }

        private void ScheduleRetry()
        {
            if (_retryAttempt >= MaxRetry) return;
            _retryAttempt++;
            double delay = Math.Pow(2, Math.Min(MaxRetry, _retryAttempt));
            SkyFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, LoadReward);
        }
    }
}