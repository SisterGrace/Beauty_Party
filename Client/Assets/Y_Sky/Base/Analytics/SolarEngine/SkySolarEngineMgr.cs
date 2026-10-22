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
#if TestML
			StartCoroutine(Test());
#endif
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

			// 【调整】先初始化公共事件属性（is_newuser、device_num、pay_times）
			InitCommonProperties();

			// 【调整】后上报 track_init 事件（此时事件会自动带上刚设置的公共属性）
			Sky.SkyAnalyticsMgr.TrackEvent("track_init", new Dictionary<string, object>
			{
				{ "ad_network", SkySolarEnginePrefs.HasAttribution ? SkySolarEnginePrefs.Network : "" }
			});
		}
#if TestML
		IEnumerator Test()
		{
			yield return new WaitForSeconds(4f);
			FinalizeAsPaid("test", "test", "test", "test", "test");
		}

#endif

		#region 公共事件属性初始化
		/// <summary>
		/// 初始化公共事件属性（is_newuser, device_num, pay_times）
		/// 归因相关的属性在归因回调中设置
		/// </summary>
		private void InitCommonProperties()
		{
			long firstTicks = SkySolarEnginePrefs.FirstStartTicks;
			string isNewUser = "Y";
			if (firstTicks > 0)
			{
				DateTime firstStartLocal = new DateTime(firstTicks, DateTimeKind.Utc).ToLocalTime();
				isNewUser = (firstStartLocal.Date == DateTime.Now.Date) ? "Y" : "N";
			}

			var commonProps = new Dictionary<string, object>
			{
				{ "is_newuser", isNewUser },
                // 建议使用热力引擎自己的 distinct_id 以保持设备体系一致，如果你坚持用 Unity 的，可以换回 SystemInfo.deviceUniqueIdentifier
                { "device_num", SkySolarEngineBridge.GetDistinctId() },
				{ "pay_times", SkySolarEnginePrefs.PayTimes }
                // 注意：此时未获取到归因，source_network、source_campaign、source_creative 不设置，即传空
                // user_ad_network 属于用户属性，此时不设置
            };

			SkySolarEngineBridge.SetSuperProperties(ToJson(commonProps));
			LogTool.DebugLog("[SolarEngine] Common properties initialized.");
		}
		/// <summary>
		/// 记录支付次数（接收外部传入的最新总次数），更新热力引擎公共属性。
		/// 由 SkyIapEvents.IAPSuccess 在支付成功时调用。
		/// </summary>
		public void RecordPayTimes(int payTimes)
		{
			if (!IsInitialized) return;

			SkySolarEnginePrefs.PayTimes = payTimes;

			var superProps = new Dictionary<string, object>
			{
				{ "pay_times", payTimes }
			};
			LogTool.DebugLog($"[SolarEngine] PayTimes synced: {payTimes}");
		}

		/// <summary>
		/// 设置公共事件属性（供 SkyAnalyticsMgr 转发）
		/// </summary>
		public void SetSuperProperties(Dictionary<string, object> properties)
		{
			if (!IsInitialized || properties == null || properties.Count == 0) return;
			SkySolarEngineBridge.SetSuperProperties(ToJson(properties));
		}

		/// <summary>
		/// 设置用户属性（覆盖式）
		/// </summary>
		public void UserSet(Dictionary<string, object> properties, bool setOnce)
		{
			if (!IsInitialized || properties == null || properties.Count == 0) return;
			if (setOnce)
				SkySolarEngineBridge.UserInit(ToJson(properties));    // setOnce → userInit
			else
				SkySolarEngineBridge.UserUpdate(ToJson(properties));  // 覆盖式 → userUpdate
		}
		#endregion

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

			string finalCampaign = campaign ?? "";
			if (finalCampaign.Length > 20)
				finalCampaign = finalCampaign.Substring(0, 20);

			string finalCreative = creative ?? "";
			if (finalCreative.Length > 20)
				finalCreative = finalCreative.Substring(0, 20);

			SkySolarEnginePrefs.HasAttribution = true;
			SkySolarEnginePrefs.Network = finalNetwork;
			SkySolarEnginePrefs.Campaign = finalCampaign;
			SkySolarEnginePrefs.Adgroup = adgroup ?? "";
			SkySolarEnginePrefs.Creative = finalCreative;
			SkySolarEnginePrefs.ClickLabel = clickId ?? "";
			SkySolarEnginePrefs.IsPaidUser = true;
			SkySolarEnginePrefs.IsFinalized = true;

			if (_attributionCoroutine != null)
			{
				StopCoroutine(_attributionCoroutine);
				_attributionCoroutine = null;
			}
			SkySdkMgr.Instance.OnUserAttributionReceived?.Invoke(true, SkySolarEnginePrefs.Network, SkySolarEnginePrefs.ClickLabel);

			// 【新增】归因成功，设置公共属性 source_network, source_campaign, source_creative
			var superProps = new Dictionary<string, object>
			{
				{ "source_network", finalNetwork },
				{ "source_campaign", finalCampaign },
				{ "source_creative", finalCreative }
			};
			SkySolarEngineBridge.SetSuperProperties(ToJson(superProps));

			// 【新增】设置用户属性 user_ad_network（setOnce 机制，使用 userInit）
			var userProps = new Dictionary<string, object>
			{
				{ "user_ad_network", finalNetwork }
			};
			SkySolarEngineBridge.UserInit(ToJson(userProps));

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

			// 【新增】超时固化为自然量，公共属性 source_network 设为 Organic，其余为空
			var superProps = new Dictionary<string, object>
			{
				{ "source_network", "Organic" },
				{ "source_campaign", "" },
				{ "source_creative", "" }
			};
			SkySolarEngineBridge.SetSuperProperties(ToJson(superProps));

			// 【新增】设置用户属性 user_ad_network，必须明确传 "Organic"
			var userProps = new Dictionary<string, object>
			{
				{ "user_ad_network", "Organic" }
			};
			SkySolarEngineBridge.UserInit(ToJson(userProps));

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
			return;
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
		/// 注意：支付次数 pay_times 的公共属性更新，已由外层调用 RecordPayTimes() 处理，此处不再重复累加。
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
