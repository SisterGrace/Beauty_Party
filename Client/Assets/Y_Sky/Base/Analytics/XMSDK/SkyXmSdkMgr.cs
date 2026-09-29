using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public class SkyXmSdkMgr : SkySingleton<SkyXmSdkMgr>
    {
        private string _baseUrl;
        private string _xmDeviceId;
        private bool _isRiskDetectionObtained;
        private bool _isPaidUser;
        private bool _isAnalyticsReady;
        private bool _isInitCompleted;
        private List<Action<bool>> _pendingCheckCallbacks;

        public bool UseXMSDK => !string.IsNullOrEmpty(_baseUrl);
        public bool IsPaidUser => _isPaidUser;
        public string XMDeviceId => _xmDeviceId;
        public bool IsRiskDetectionObtained => _isRiskDetectionObtained;
        public bool IsNormalDevice => !string.IsNullOrEmpty(_xmDeviceId);

#if USE_XMSDK
        private List<KeyValuePair<string, Dictionary<string, object>>> _pendingEvents;
        private float _riskSdkInitRequestRealtime;
        private float _riskInitCompletedRealtime;
        private string _lastRiskErrorMessage;

        public void InitRiskDetection(string baseUrl)
        {
            _baseUrl = baseUrl;
            if (!UseXMSDK) return;

            _riskSdkInitRequestRealtime = Time.realtimeSinceStartup;
            XM.XMSDK.InitRiskDetection(baseUrl, OnXMSDKEvent, OnInitRiskDetectionCompleted);
            SkyLogUtils.Debug("SkyXmSdkMgr: InitRiskDetection called with baseUrl=" + baseUrl);
            TrackRiskAnalytics("XMSDK_Risk_Init_Start", new Dictionary<string, object>
            {
                { "base_url", baseUrl ?? "" }
            });
        }

        private void OnInitRiskDetectionCompleted(string fingerprint)
        {
            _isInitCompleted = true;
            _riskInitCompletedRealtime = Time.realtimeSinceStartup;
            SkyLogUtils.Debug("SkyXmSdkMgr: InitRiskDetection completed, fingerprint=" + fingerprint);

            float initCompleteMs = (_riskInitCompletedRealtime - _riskSdkInitRequestRealtime) * 1000f;
            TrackRiskAnalytics("XMSDK_Risk_Init_Complete", new Dictionary<string, object>
            {
                { "fingerprint", fingerprint ?? "" },
                { "init_complete_elapsed_ms", Mathf.RoundToInt(initCompleteMs) },
                { "callback_time_ms", Mathf.RoundToInt(initCompleteMs) }
            });

            RequestRiskResultEarly();
        }

        /// <summary>
        /// 风控初始化完成后立即拉取检测结果，尽早写入 xmdeviceid，供 Adjust / 队列回调使用。
        /// </summary>
        private void RequestRiskResultEarly()
        {
            float requestStart = Time.realtimeSinceStartup;
            XM.XMSDK.GetResultRiskDetection((deviceId, errorMessage, rawResponse) =>
            {
                ApplyRiskDetectionResult(deviceId, errorMessage, requestStart);
            });
        }

        private void ApplyRiskDetectionResult(string deviceId, string errorMessage, float getResultRequestRealtime)
        {
            _xmDeviceId = deviceId;
            _isRiskDetectionObtained = true;
            _lastRiskErrorMessage = errorMessage ?? "";
            bool isNormal = !string.IsNullOrEmpty(deviceId);

            float now = Time.realtimeSinceStartup;
            float resultCallbackMs = (now - getResultRequestRealtime) * 1000f;
            float fromInitCompleteMs = (_riskInitCompletedRealtime > 0f)
                ? (now - _riskInitCompletedRealtime) * 1000f
                : 0f;
            float fromInitRequestMs = (_riskSdkInitRequestRealtime > 0f)
                ? (now - _riskSdkInitRequestRealtime) * 1000f
                : 0f;

            SkyLogUtils.Debug($"SkyXmSdkMgr: RiskDetection result deviceId={deviceId} error={errorMessage} normal={isNormal} result_cb_ms={resultCallbackMs:F0}");

            TrackRiskAnalytics("XMSDK_Risk_Result", new Dictionary<string, object>
            {
                { "is_normal", isNormal },
                { "has_device_id", !string.IsNullOrEmpty(deviceId) },
                { "error_message", _lastRiskErrorMessage },
                { "result_callback_elapsed_ms", Mathf.RoundToInt(resultCallbackMs) },
                { "callback_time_ms", Mathf.RoundToInt(resultCallbackMs) },
                { "from_init_complete_elapsed_ms", Mathf.RoundToInt(fromInitCompleteMs) },
                { "from_init_request_elapsed_ms", Mathf.RoundToInt(fromInitRequestMs) }
            });

            FlushPendingCheckCallbacks();
        }

        private void TrackRiskAnalytics(string eventName, Dictionary<string, object> props)
        {
            if (props == null) props = new Dictionary<string, object>();
            if (!_isAnalyticsReady)
            {
                _pendingEvents ??= new List<KeyValuePair<string, Dictionary<string, object>>>();
                _pendingEvents.Add(new KeyValuePair<string, Dictionary<string, object>>(eventName, props));
                SkyLogUtils.Debug("SkyXmSdkMgr: Analytics not ready, risk event cached: " + eventName);
                return;
            }
            SkyAnalyticsMgr.TrackEvent(eventName, props);
        }

        /// <summary>
        /// 由 SkyAnalyticsMgr.Init 调用，标记数数SDK已就绪，同时回放缓存的风控事件。
        /// </summary>
        public void OnAnalyticsReady()
        {
            _isAnalyticsReady = true;
            if (_pendingEvents == null || _pendingEvents.Count == 0) return;

            SkyLogUtils.Debug($"SkyXmSdkMgr: Flushing {_pendingEvents.Count} cached XMSDK events");
            foreach (var evt in _pendingEvents)
            {
                SkyAnalyticsMgr.TrackEvent(evt.Key, evt.Value);
            }
            _pendingEvents.Clear();
            _pendingEvents = null;
        }

        private void OnXMSDKEvent(string eventName, Dictionary<string, object> eventData)
        {
            if (string.IsNullOrEmpty(eventName)) return;
            SkyLogUtils.Debug("SkyXmSdkMgr: XMSDK event received: " + eventName);

            if (!_isAnalyticsReady)
            {
                _pendingEvents ??= new List<KeyValuePair<string, Dictionary<string, object>>>();
                _pendingEvents.Add(new KeyValuePair<string, Dictionary<string, object>>(eventName, eventData));
                SkyLogUtils.Debug("SkyXmSdkMgr: Analytics not ready, event cached: " + eventName);
                return;
            }

            SkyAnalyticsMgr.TrackEvent(eventName, eventData);
        }

        private void FlushPendingCheckCallbacks()
        {
            if (_pendingCheckCallbacks == null || _pendingCheckCallbacks.Count == 0) return;

            SkyLogUtils.Debug($"SkyXmSdkMgr: Risk result ready, flushing {_pendingCheckCallbacks.Count} pending CheckRiskDetection callbacks");
            var callbacks = _pendingCheckCallbacks;
            _pendingCheckCallbacks = null;
            foreach (var cb in callbacks)
            {
                bool isNormal = !string.IsNullOrEmpty(_xmDeviceId);
                cb?.Invoke(isNormal);
            }
        }

        /// <summary>
        /// 获取风控检测结果，通过回调返回设备是否正常（与尽早拉取的 GetResult 结果一致）。
        /// 若初始化或 GetResultRiskDetection 结果尚未就绪，回调会排队，在结果回调后统一执行。
        /// </summary>
        /// <param name="callback">true = 正常设备，false = 非正常设备</param>
        public void CheckRiskDetection(Action<bool> callback)
        {
            if (!UseXMSDK)
            {
                callback?.Invoke(true);
                return;
            }

            if (!_isInitCompleted)
            {
                _pendingCheckCallbacks ??= new List<Action<bool>>();
                _pendingCheckCallbacks.Add(callback);
                SkyLogUtils.Debug("SkyXmSdkMgr: Init not completed, CheckRiskDetection callback queued");
                return;
            }

            if (_isRiskDetectionObtained)
            {
                bool isNormal = !string.IsNullOrEmpty(_xmDeviceId);
                callback?.Invoke(isNormal);
                return;
            }

            _pendingCheckCallbacks ??= new List<Action<bool>>();
            _pendingCheckCallbacks.Add(callback);
            SkyLogUtils.Debug("SkyXmSdkMgr: Risk result pending, CheckRiskDetection callback queued");
        }

        public void SetPaidUser(bool isPaid)
        {
            _isPaidUser = isPaid;
            if (isPaid && UseXMSDK)
            {
                XM.XMSDK.Initialize();
                SkyLogUtils.Debug("SkyXmSdkMgr: Paid user detected, XMSDK.Initialize called");
            }
        }

        public void TrackAdImpression(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId)
        {
            if (!UseXMSDK || !_isPaidUser) return;

            var adInfo = new XM.AdInfo(revenue, adType, platformName, adUnitId, networkName, networkPlacementId);
            XM.XMSDK.AdImpression(adInfo);
            SkyLogUtils.Debug($"SkyXmSdkMgr: AdImpression tracked adType={adType} revenue={revenue}");
        }

        public void TrackAdClick(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId)
        {
            if (!UseXMSDK || !_isPaidUser) return;

            var adInfo = new XM.AdInfo(revenue, adType, platformName, adUnitId, networkName, networkPlacementId);
            XM.XMSDK.AdClick(adInfo);
            SkyLogUtils.Debug($"SkyXmSdkMgr: AdClick tracked adType={adType}");
        }
#else
        public void InitRiskDetection(string baseUrl)
        {
            _baseUrl = null;
        }

        public void OnAnalyticsReady()
        {
            _isAnalyticsReady = true;
        }

        public void CheckRiskDetection(Action<bool> callback)
        {
            callback?.Invoke(true);
        }

        public void SetPaidUser(bool isPaid)
        {
            _isPaidUser = isPaid;
        }

        public void TrackAdImpression(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId) { }

        public void TrackAdClick(double revenue, string adType, string platformName, string adUnitId, string networkName, string networkPlacementId) { }
#endif
    }

    public struct XMDeviceInfo
    {
        public string XMDeviceId;
        public bool IsXMSDKIntegrated;
        public bool IsRiskDetectionObtained;
    }
}
