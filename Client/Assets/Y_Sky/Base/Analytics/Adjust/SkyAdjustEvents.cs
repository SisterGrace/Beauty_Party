#if ADJUST
using System;
using System.Collections;
using System.Collections.Generic;

using AdjustSdk;

using Newtonsoft.Json;
using UnityEngine;

namespace Sky
{
    public static class SkyAdjustEvents
    {
        private static Dictionary<string, string> _tokenDict;
        private static string _deferredInitToken;
        private const string EVENT_CONFIG_KEY = "event_adjust_set";

        private struct CachedAdImpression
        {
            public double Revenue;
            public string Network;
            public string Unit;
            public string Placement;
        }
        private static List<CachedAdImpression> _cachedAdImpressions;
        private static string GetEventToken(string eventName)
        {
            if (_tokenDict == null || string.IsNullOrEmpty(eventName)) return null;
            // 先精准匹配，再尝试全小写匹配
            if (_tokenDict.TryGetValue(eventName, out var token)) return token;
            return _tokenDict.GetValueOrDefault(eventName.ToLowerInvariant());
        }

        /// <summary>
        /// 设置延迟初始化：当上报的事件 token 与此一致时再初始化 Adjust。
        /// </summary>
        public static void SetDeferredInitToken(string token) => _deferredInitToken = token;

        /// <summary>
        /// 在线参数 adj_event_token 是否在 event_adjust_set 中有对应事件配置。
        /// </summary>
        public static bool IsTokenConfigured(string token)
        {
            if (string.IsNullOrEmpty(token) || _tokenDict == null) return false;
            foreach (var v in _tokenDict.Values)
                if (v == token) return true;
            return false;
        }

        public static void UpdateEventToken(string configJson = null)
        {

            configJson ??= SkyFirebaseMgr.Instance.GetConfigStr(EVENT_CONFIG_KEY); 
            SkyLogUtils.Debug("Sky adjust event json:"+configJson);
            if (string.IsNullOrEmpty(configJson)) return;
            try
            {
                var deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(configJson);
                _tokenDict ??= new Dictionary<string, string>();
                foreach (var kvp in deserialized)
                    _tokenDict[kvp.Key] = kvp.Value;
                // 补充小写 key，便于全小写匹配（不覆盖已有 key，保证精准匹配优先）
                foreach (var kvp in deserialized)
                {
                    var lowerKey = kvp.Key.ToLowerInvariant();
                    if (!_tokenDict.ContainsKey(lowerKey))
                        _tokenDict[lowerKey] = kvp.Value;
                }
            }
            catch
            {
                
            }
        }

        public static void TrackEvent(string eventName)
        {
            var token = GetEventToken(eventName);
            if (!string.IsNullOrEmpty(_deferredInitToken) && token == _deferredInitToken)
            {
                SkyAdjustMgr.Instance.Init();
                _deferredInitToken = null;
            }
            SkyLogUtils.Debug("Sky adjust track event: "+eventName+" token:"+token);
            if (!string.IsNullOrEmpty(token)) Adjust.TrackEvent(new AdjustEvent(token));
        }

        public static void TrackAdImpressionEvent(double revenue, string network, string unit, string placement)
        {
            if (!SkyAdjustPrefs.HasInitialized)
            {
                _cachedAdImpressions ??= new List<CachedAdImpression>();
                _cachedAdImpressions.Add(new CachedAdImpression
                {
                    Revenue = revenue,
                    Network = network ?? "",
                    Unit = unit ?? "",
                    Placement = placement ?? ""
                });
                SkyLogUtils.Debug("Sky adjust ad impression cached (not inited): revenue=" + revenue + " network:" + network + " unit:" + unit + " placement:" + placement);
                return;
            }
            TrackAdImpressionInternal(revenue, network, unit, placement);
        }

        private static void TrackAdImpressionInternal(double revenue, string network, string unit, string placement)
        {
            var source = SkyAdImpressionSource.GetSource("revenue_adj", "applovin_max_sdk");
            AdjustAdRevenue adjustAdRevenue = new AdjustAdRevenue(source);
            adjustAdRevenue.SetRevenue(revenue, "USD");
            adjustAdRevenue.AdRevenueNetwork = network ?? "";
            adjustAdRevenue.AdRevenueUnit = unit ?? "";
            adjustAdRevenue.AdRevenuePlacement = placement ?? "";
            Adjust.TrackAdRevenue(adjustAdRevenue);
            SkyLogUtils.Debug("Sky adjust track event: " + revenue + " network:" + network + " unit:" + unit + " placement:" + placement + " source:" + source);
        }

        /// <summary>
        /// 在 Adjust 初始化完成后调用，上报缓存的广告展示事件。
        /// </summary>
        public static void FlushCachedAdImpressionEvents()
        {
            if (_cachedAdImpressions == null || _cachedAdImpressions.Count == 0) return;
            foreach (var item in _cachedAdImpressions)
                TrackAdImpressionInternal(item.Revenue, item.Network, item.Unit, item.Placement);
            _cachedAdImpressions.Clear();
            SkyLogUtils.Debug("Sky adjust flushed cached ad impression events");
        }
    }
}
#endif
