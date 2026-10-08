using Sky;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public class SkySolarEngineMgr : SkySingleton<SkySolarEngineMgr>
    {
        /// <summary>
        /// 本地日志开关。上线前可关闭以减少 logcat 噪音。
        /// </summary>
        private const bool EnableLog = false;

        /// <summary>
        /// 后台 Debug 模式开关。开启后，只有在热力引擎后台 Debug 设备列表里的设备，
        /// 其数据才仅校验不入库；不在列表里的设备正常入库。
        /// 
        /// ⚠️ 上线前务必置为 false！
        /// </summary>
        private const bool EnableDebugModel = false;
        private const int ATTRIBUTION_TIMEOUT_SECONDS = 180;

        public bool IsInitialized { get; private set; }
        private float _appStartTime;
        private Coroutine _attributionCoroutine;

        //public static event Action<bool> OnAttributionCompleted; // 参数为是否买量用户

        public bool TrackingEnable() => IsInitialized;

        void Awake()
        {
            _appStartTime = Time.realtimeSinceStartup;
        }



        private static string GetAppKey()
        {
            var cfg = SkyConfigMgr.Instance != null ? SkyConfigMgr.Instance.gameConfig : null;
            if (cfg == null || cfg.analyticsConfig == null) return string.Empty;
            return cfg.analyticsConfig.reyun_ID ?? string.Empty;
        }

        public void PreInit()
        {
            var appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey)) return;
            SkySolarEngineBridge.PreInit(appKey);
        }

        public void Init()
        {
            if (IsInitialized) return;
            var appKey = GetAppKey();
            if (string.IsNullOrEmpty(appKey))
            {
                LogTool.Error("[SolarEngine] AppKey empty (SkyConfigData.reyun_ID), skip init");
                return;
            }

            SkySolarEngineBridge.SetUnityCallbackTarget(gameObject.name);
            SkySolarEngineBridge.PreInit(appKey);
            SkySolarEngineBridge.Initialize(appKey, EnableLog, EnableDebugModel);
            IsInitialized = true;
            LogTool.DebugLog("[SolarEngine] Init triggered, appKey=" + appKey + ", log=" + EnableLog,
                LogTool.Color.yellow);

            StartAttributionTimer();

            AppLifecycleListener.Instance.Init();
            LogTool.DebugLog("[SolarEngine] distinct_id = " + SkySolarEngineBridge.GetDistinctId(), LogTool.Color.yellow);
            Sky.SkyAnalyticsMgr.TrackEvent("track_init", new Dictionary<string, object>
            {
                { "ad_network", SkySolarEnginePrefs.HasAttribution ? SkySolarEnginePrefs.Network : "" }
            });
        }

        #region 归因计时器（3分钟轮询 + 跨启动累计）

        private void StartAttributionTimer()
        {
            if (SkySolarEnginePrefs.IsFinalized)
            {
                LogTool.DebugLog(
                    $"[SolarEngine] Attribution already finalized, skip timer. " +
                    $"IsPaidUser={SkySolarEnginePrefs.IsPaidUser}, " +
                    $"Network={SkySolarEnginePrefs.Network}, " +
                    $"Campaign={SkySolarEnginePrefs.Campaign}, " +
                    $"Adgroup={SkySolarEnginePrefs.Adgroup}, " +
                    $"Creative={SkySolarEnginePrefs.Creative}, " +
                    $"ClickLabel={SkySolarEnginePrefs.ClickLabel}");
                return;
            }

            long firstTicks = SkySolarEnginePrefs.FirstStartTicks;
            int elapsed = SkySolarEnginePrefs.ElapsedSeconds;
            DateTime now = DateTime.UtcNow;

            if (firstTicks == 0)
            {
                SkySolarEnginePrefs.FirstStartTicks = now.Ticks;
                elapsed = 0;
            }
            else
            {
                DateTime firstStart = new DateTime(firstTicks, DateTimeKind.Utc);
                int totalSeconds = (int)(now - firstStart).TotalSeconds;
                if (totalSeconds >= ATTRIBUTION_TIMEOUT_SECONDS)
                {
                    FinalizeAsOrganic();
                    return;
                }
                elapsed = totalSeconds;
                SkySolarEnginePrefs.ElapsedSeconds = elapsed;
            }

            int remaining = ATTRIBUTION_TIMEOUT_SECONDS - elapsed;
            if (remaining <= 0)
            {
                FinalizeAsOrganic();
                return;
            }

            LogTool.DebugLog($"[SolarEngine] Attribution timer started, remaining {remaining}s");
            if (_attributionCoroutine != null)
                StopCoroutine(_attributionCoroutine);
            _attributionCoroutine = StartCoroutine(AttributionTimerCoroutine(remaining));
        }

        private IEnumerator AttributionTimerCoroutine(int waitSeconds)
        {
            yield return new WaitForSeconds(waitSeconds);
            if (!SkySolarEnginePrefs.IsFinalized)
            {
                LogTool.DebugLog("[SolarEngine] Attribution timeout, finalize as organic");
                FinalizeAsOrganic();
            }
            _attributionCoroutine = null;
        }

        private void FinalizeAsPaid(string network, string campaign, string adgroup, string creative, string clickId)
        {
            if (SkySolarEnginePrefs.IsFinalized) return;

            string finalNetwork = network ?? "";
            if (finalNetwork.Length > 10)
                finalNetwork = finalNetwork.Substring(0, 10);

            SkySolarEnginePrefs.HasAttribution = true;
            SkySolarEnginePrefs.Network = finalNetwork;
            SkySolarEnginePrefs.Campaign = campaign ?? "";
            SkySolarEnginePrefs.Adgroup = adgroup ?? "";
            SkySolarEnginePrefs.Creative = creative ?? "";
            SkySolarEnginePrefs.ClickLabel = clickId ?? "";
            SkySolarEnginePrefs.IsPaidUser = true;
            SkySolarEnginePrefs.IsFinalized = true;

            if (_attributionCoroutine != null)
            {
                StopCoroutine(_attributionCoroutine);
                _attributionCoroutine = null;
            }
            SkySdkMgr.Instance.OnUserAttributionReceived?.Invoke(true, SkySolarEnginePrefs.Network, SkySolarEnginePrefs.ClickLabel);
            //Sky.SkyAnalyticsMgr.DesignEvent("SE", "Attribution", "Success", true, finalNetwork);

            float passTime = Time.realtimeSinceStartup - _appStartTime;
            int passTimeSeconds = Mathf.CeilToInt(passTime);
            Sky.SkyAnalyticsMgr.TrackEvent("track_get_success", new Dictionary<string, object>
            {
                { "Pass_Time", passTimeSeconds },
                { "ad_network", finalNetwork }
            });
            Sky.SkyAnalyticsMgr.TrackEvent("track_get_time", new Dictionary<string, object>
            {
                { "Pass_Time", passTimeSeconds },
                { "ad_network", finalNetwork }
            });
            ReportAdSpendOnce(finalNetwork);
            LogTool.DebugLog($"[SolarEngine] Finalized as PAID user: network={finalNetwork}");
        }

        private void FinalizeAsOrganic()
        {
            if (SkySolarEnginePrefs.IsFinalized) return;

            SkySolarEnginePrefs.HasAttribution = true;
            SkySolarEnginePrefs.Network = "Organic";
            SkySolarEnginePrefs.Campaign = "";
            SkySolarEnginePrefs.Adgroup = "";
            SkySolarEnginePrefs.Creative = "";
            SkySolarEnginePrefs.ClickLabel = "";
            SkySolarEnginePrefs.IsPaidUser = false;
            SkySolarEnginePrefs.IsFinalized = true;

            if (_attributionCoroutine != null)
            {
                StopCoroutine(_attributionCoroutine);
                _attributionCoroutine = null;
            }
            SkySdkMgr.Instance.OnUserAttributionReceived?.Invoke(false, SkySolarEnginePrefs.Network, SkySolarEnginePrefs.ClickLabel);
            //Sky.SkyAnalyticsMgr.DesignEvent("SE", "Attribution", "Organic", true, "Organic");

            float passTime = Time.realtimeSinceStartup - _appStartTime;
            int passTimeSeconds = Mathf.CeilToInt(passTime);
            Sky.SkyAnalyticsMgr.TrackEvent("track_get_time", new Dictionary<string, object>
            {
                { "Pass_Time", passTimeSeconds },
                { "ad_network", "Organic" }
            });
            ReportAdSpendOnce("Organic");
            LogTool.DebugLog("[SolarEngine] Finalized as ORGANIC user");
        }
        private void ReportAdSpendOnce(string network)
        {
            if (SkySolarEnginePrefs.AdSpendReported) return;   // 只报一次
            SkySolarEnginePrefs.AdSpendReported = true;

            SkySolarEngineEvents.TrackEvent("track_ad_spend", new Dictionary<string, object>
    {
        { "ad_network", network ?? "" }
    });

            LogTool.DebugLog("[SolarEngine] track_ad_spend reported once, ad_network=" + network);
        }
        #endregion

        #region 事件上报（不变）
        public void TrackEvent(string eventName)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackEvent(eventName, null);
        }

        public void TrackEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackEvent(eventName, ToJson(properties));
        }

        public void TrackPresetEvent(string eventName)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackPresetEvent(eventName, null);
        }

        public void TrackPresetEvent(string eventName, Dictionary<string, object> properties)
        {
            if (!IsInitialized || string.IsNullOrEmpty(eventName)) return;
            SkySolarEngineBridge.TrackPresetEvent(eventName, ToJson(properties));
        }

        /// <summary>
        /// 广告展示收益事件（_appImp）
        /// </summary>
        public void TrackAdImpression(
            string adNetworkPlatform,
            string mediationPlatform,
            int adType,
            string adNetworkAppID,
            string adNetworkADID,
            double ecpm,
            string currencyType,
            bool isRenderSuccess,
            Dictionary<string, object> customProperties = null)
        {
            if (!IsInitialized) return;

            float ecpmFloat = (float)ecpm;
            string customJson = customProperties != null ? ToJson(customProperties) : "{}";

            SkySolarEngineBridge.TrackAdImpression(
                adNetworkPlatform, mediationPlatform, adType,
                adNetworkAppID ?? "", adNetworkADID ?? "",
                ecpmFloat, currencyType, isRenderSuccess, customJson);

            LogTool.DebugLog($"[SolarEngine] TrackAdImpression: platform={adNetworkPlatform}, " +
                $"mediation={mediationPlatform}, type={adType}, adId={adNetworkADID}, ecpm={ecpm}");
        }

        /// <summary>
        /// 广告点击事件（_appClick）
        /// </summary>
        public void TrackAdClick(
            string adNetworkPlatform,
            string mediationPlatform,
            int adType,
            string adNetworkADID,
            Dictionary<string, object> customProperties = null)
        {
            if (!IsInitialized) return;

            string customJson = customProperties != null ? ToJson(customProperties) : "{}";

            SkySolarEngineBridge.TrackAdClick(
                adNetworkPlatform, mediationPlatform, adType,
                adNetworkADID ?? "", customJson);

            LogTool.DebugLog($"[SolarEngine] TrackAdClick: platform={adNetworkPlatform}, unit={adNetworkADID}");
        }

        /// <summary>
        /// 应用内购买事件（_appPur）
        /// </summary>
        public void TrackPurchase(
            double payAmount,
            string currencyType,
            int payStatus,
            string orderId = "",
            string payType = "",
            string productId = "",
            string productName = "",
            int productNum = 1,
            string failReason = "",
            Dictionary<string, object> customProperties = null)
        {
            if (!IsInitialized) return;

            string customJson = customProperties != null ? ToJson(customProperties) : "{}";

            SkySolarEngineBridge.TrackPurchase(
                orderId, payAmount, currencyType, payType,
                productId, productName, productNum, payStatus,
                failReason, customJson);

            LogTool.DebugLog($"[SolarEngine] TrackPurchase: amount={payAmount}, status={payStatus}");
        }
        #endregion

        #region 归因回调（Java → C#）
        public void OnAttributionSuccess(string json)
        {
            LogTool.DebugLog("[SolarEngine] OnAttributionSuccess: " + json);

            if (SkySolarEnginePrefs.IsFinalized)
            {
                LogTool.DebugLog(
                    $"[SolarEngine] Attribution already finalized, ignore callback. " +
                    $"Current: IsPaidUser={SkySolarEnginePrefs.IsPaidUser}, " +
                    $"Network={SkySolarEnginePrefs.Network}, " +
                    $"Campaign={SkySolarEnginePrefs.Campaign}, " +
                    $"Adgroup={SkySolarEnginePrefs.Adgroup}, " +
                    $"Creative={SkySolarEnginePrefs.Creative}, " +
                    $"ClickLabel={SkySolarEnginePrefs.ClickLabel}");
                return;
            }

            try
            {
                var network = ExtractString(json, "channel_name", "cid_name", "network");
                var campaign = ExtractString(json, "adplan_name", "turl_campaign_name", "campaign_name", "campaign");
                var adgroup = ExtractString(json, "adgroup_name", "aid_name", "adgroup");
                var creative = ExtractString(json, "adcreative_name", "creative_name", "creative");
                var clickId = ExtractString(json, "click_id", "clickId", "click_label");

                if (string.IsNullOrEmpty(network)) network = "";

                // ---- 特殊处理：防作弊 / Google Organic Search 直接判定为自然（立即固化） ----
                if (network.Equals("Untrusted Devices", StringComparison.OrdinalIgnoreCase) ||
                    network.Equals("Google Organic Search", StringComparison.OrdinalIgnoreCase))
                {
                    FinalizeAsOrganic();
                    return;
                }

                // ---- 判断是否为买量（非 Organic 等） ----
                if (IsPaidUser(network))
                {
                    FinalizeAsPaid(network, campaign, adgroup, creative, clickId);
                }
                else
                {
                    // Organic 或其他自然类型 → 不固化，等待超时或后续买量
                    LogTool.DebugLog($"[SolarEngine] Received organic attribution (network={network}), waiting for timeout or paid result");
                }
            }
            catch (Exception e)
            {
                LogTool.Error("[SolarEngine] parse attribution failed: " + e);
            }
        }

        public void OnAttributionFail(string code)
        {
            LogTool.DebugLog("[SolarEngine] OnAttributionFail, code=" + code, LogTool.Color.yellow);
            //Sky.SkyAnalyticsMgr.DesignEvent("SE", "Attribution", "Fail", true, code ?? "");
        }

        private static bool IsPaidUser(string network)
        {
            if (string.IsNullOrEmpty(network)) return false;
            string n = network.Trim();
            if (n.Equals("Organic", StringComparison.OrdinalIgnoreCase)) return false;
            if (n.Equals("Untrusted Devices", StringComparison.OrdinalIgnoreCase)) return false;
            if (n.Equals("Google Organic Search", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
        #endregion

        #region JSON 工具（不变）
        private static string ExtractString(string json, params string[] keys)
        {
            if (string.IsNullOrEmpty(json) || keys == null) return string.Empty;
            foreach (var key in keys)
            {
                var v = TryGetField(json, key);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return string.Empty;
        }

        private static string TryGetField(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;
            var pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern, StringComparison.Ordinal);
            if (idx < 0) return null;
            int colon = json.IndexOf(':', idx + pattern.Length);
            if (colon < 0) return null;
            int p = colon + 1;
            while (p < json.Length && (json[p] == ' ' || json[p] == '\t')) p++;
            if (p >= json.Length) return null;
            if (json[p] == '"')
            {
                int end = p + 1;
                while (end < json.Length && json[end] != '"')
                {
                    if (json[end] == '\\' && end + 1 < json.Length) end += 2;
                    else end++;
                }
                if (end >= json.Length) return null;
                return json.Substring(p + 1, end - p - 1);
            }
            int e = p;
            while (e < json.Length && json[e] != ',' && json[e] != '}' && json[e] != ']') e++;
            return json.Substring(p, e - p).Trim();
        }

        private static string ToJson(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0) return null;
            var sb = new System.Text.StringBuilder(64);
            sb.Append('{');
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(EscapeJson(kv.Key)).Append('"').Append(':');
                AppendJsonValue(sb, kv.Value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendJsonValue(System.Text.StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            switch (v)
            {
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int _:
                case long _:
                case float _:
                case double _:
                case decimal _:
                    sb.Append(System.Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture));
                    break;
                default:
                    sb.Append('"').Append(EscapeJson(v.ToString())).Append('"');
                    break;
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
        #endregion

        void OnDestroy()
        {
            Debug.LogWarning("SkySolarEngineMgr 被销毁了！");
        }
    }
}