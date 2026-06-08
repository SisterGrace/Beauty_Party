/*using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sky
{
    public class SkyMaxInter : SkySingleton<SkyMaxInter>
    {
        private string _placementId;
        private float _startLoadTime;
        private int _retryAttempt;
        private double _lastRevenue;

        private const string PlatformName = "MAX";
        private const int MaxRetry = 6;

        public void SetPlacementId(string placementId)
        {
            _placementId = placementId;
        }

        public void LoadInter()
        {
            if (string.IsNullOrEmpty(_placementId)) return;
            RegisterCallbacks();
            _startLoadTime = Time.realtimeSinceStartup;
            MaxSdk.LoadInterstitialAd(_placementId);
            SkyLogUtils.Debug("[SkyMax] Load inter: " + _placementId);
        }

        public bool IsInterReady()
        {
            return !string.IsNullOrEmpty(_placementId) && MaxSdk.IsInterstitialAdReady(_placementId);
        }

        public void ShowInter()
        {
            if (!IsInterReady())
            {
                SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
                LoadInter();
                return;
            }
            MaxSdk.ShowInterstitialAd(_placementId);
            SkyLogUtils.Debug("[SkyMax] Show inter: " + _placementId);
        }

        private void RegisterCallbacks()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;

            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnAdLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnAdLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnAdDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnAdDisplayFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnAdHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        private void OnAdLoadedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - _startLoadTime);
            _lastRevenue = adInfo.Revenue;
            string adId = adInfo.CreativeId ?? "";
            string source = "MAX_" + adInfo.NetworkName;
            SkyAdsEvents.LogLoadedEvent(adId, source, PlatformName, loadTime, SkyAdsType.Interstitial, adUnitId, _lastRevenue);
            _retryAttempt = 0;
            SkyLogUtils.Debug("[SkyMax] Inter OnAdLoaded: " + adUnitId);
        }

        private void OnAdLoadFailedEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo)
        {
            if (adUnitId != _placementId) return;
            float loadTime = Mathf.Ceil(Time.realtimeSinceStartup - _startLoadTime);
            SkyAdsEvents.LogFailToLoadEvent(adUnitId, PlatformName, errorInfo.Message, loadTime, SkyAdsType.Interstitial);
            SkyLogUtils.Debug("[SkyMax] Inter OnAdLoadFailed: " + adUnitId);
            ScheduleRetry();
        }

        private void OnAdDisplayedEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            string adId = adInfo.CreativeId ?? "";
            string source = "MAX_" + adInfo.NetworkName;
            string pos = SkyAdsMgr.Instance._ivPos ?? "";
            SkyAdsMgr.Instance._ivShowCallback?.Invoke(new Dictionary<string, object>
            {
                { "Position", pos },
                { "ad_source", source },
                { "ad_id", adId },
                { "ad_platform", PlatformName },
                { "unitId", adUnitId },
                { "revenue", _lastRevenue }
            });
            SkyLogUtils.Debug("[SkyMax] Inter OnAdDisplayed: " + adUnitId);
        }

        private void OnAdDisplayFailedEvent(string adUnitId, MaxSdk.ErrorInfo errorInfo, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            SkyLogUtils.Debug("[SkyMax] Inter OnAdDisplayFailed: " + adUnitId);
            SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
            LoadInter();
        }

        private void OnAdHiddenEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            SkyAdsMgr.Instance._ivCloseCallback?.Invoke();
            LoadInter();
            SkyLogUtils.Debug("[SkyMax] Inter OnAdHidden: " + adUnitId);
        }

        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdk.AdInfo adInfo)
        {
            if (adUnitId != _placementId) return;
            _lastRevenue = adInfo.Revenue;
            SkyLogUtils.Debug("[SkyMax] Inter OnAdRevenuePaid: " + adUnitId);
        }

        private void ScheduleRetry()
        {
            if (_retryAttempt >= MaxRetry) return;
            _retryAttempt++;
            double delay = Math.Pow(2, Math.Min(MaxRetry, _retryAttempt));
            SkyFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, LoadInter);
        }
    }
}*/