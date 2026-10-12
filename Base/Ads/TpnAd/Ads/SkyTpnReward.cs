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

		// 新增：记录本次展示是否已完整播放
		private bool _isVideoPlayCompleted = false;

		public void ClearAd()
		{
			_placementIds.Clear();
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
			_isVideoPlayCompleted = false; // 重置完整播放标记
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
			// 【修正】注册视频播放结束回调（不是 onRewardedVideoAdPlayEndEvent）
			ATRewardedVideo.Instance.client.onAdVideoEndEvent += OnAdVideoEnd;
		}

		private void OnAdClick(object sender, ATAdEventArgs e)
		{
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
					var status = JsonUtility.FromJson<SkyTpnAdStatus>(statusJson);
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
				_retryAttempts[placementId] = 0;
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
				string currency = e?.callbackInfo?.currency ?? "USD";
				SkyLogUtils.Debug("tpn OnAdVideoStart " + JsonUtility.ToJson(e.callbackInfo) + " placementId: " + placementId);
				if (!_placementRevenue.ContainsKey(placementId)) _placementRevenue[placementId] = revenue;
				string pos = SkyAdsMgr.Instance._rvPos ?? "";
				string format = SkyAdsType.Rewarded.ToString();
				int formatNumber = SkyAdsPrefs.GetAndIncrementFormatPlayCount(format);
				int positionNumber = SkyAdsPrefs.GetAndIncrementPositionPlayCount(pos);
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
				TopOnRevenueReporter.SetRevenueContext(placementId, pos, formatNumber, positionNumber);
				SkyAdsMgr.Instance._rvShowCallback?.Invoke();
#if UNITY_ANDROID
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
				catch { }

				SkyLogUtils.Debug("[SkyTpn] Reward OnAdVideoPlayFail: " + placementId + " " + errorMsg);
				SkyAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>
				{
					{ "Position", SkyAdsMgr.Instance._rvPos ?? "" },
					{ "ad_platform", PlatformName},
					{ "ad_source", adSource },
					{ "ad_id", adId },
					{ "ad_ecpm", adRrevenue*1000 }
				});

				_cachedAdInfo.Remove(placementId);
				LoadReward(placementId);
			}, null);
		}

		private void OnRewardEvent(object sender, ATAdEventArgs e)
		{
			Loom.QueueOnMainThread(_ =>
			{
				var placementId = e?.placementId ?? "";
				// 只在本次展示尚未关闭时才记录 reward，避免脏数据
				if (_receivedRewardByPlacement.ContainsKey(placementId))
				{
					_receivedRewardByPlacement[placementId] = true;
				}
				SkyLogUtils.Debug("[SkyTpn] Reward OnRewardEvent: " + placementId);
			}, null);
		}

		// 【新增】视频播放结束回调（来自 onAdVideoEndEvent）
		private void OnAdVideoEnd(object sender, ATAdEventArgs e)
		{
			Loom.QueueOnMainThread(_ =>
			{
				var placementId = e?.placementId ?? "";
				_isVideoPlayCompleted = true;
				SkyLogUtils.Debug("[SkyTpn] Reward OnAdVideoEnd (完整播放): " + placementId);
			}, null);
		}

		// 【修改】签名从 ATAdEventArgs 改为 ATAdRewardEventArgs，拿到 isRewarded
		private void OnAdVideoClose(object sender, ATAdRewardEventArgs e)
		{
			Loom.QueueOnMainThread(_ =>
			{
				var placementId = e?.placementId ?? "";

				// 来源1：onRewardEvent 记录的值
				bool tracked = _receivedRewardByPlacement.TryGetValue(placementId, out var r) && r;
				// 来源2：SDK 在 close 事件里直接给的 isRewarded
				bool sdkRewarded = e != null && e.isRewarded;
				bool received = tracked || sdkRewarded;

				_receivedRewardByPlacement.Remove(placementId);
				_placementRevenue.Remove(placementId);
				_cachedAdInfo.Remove(placementId);

				// 只有“平台认为可发奖” && “视频完整播放” 才最终发奖
				bool finalReward = received && _isVideoPlayCompleted;

				SkyLogUtils.Debug($"[SkyTpn] Reward OnAdVideoClose: {placementId}, " +
								 $"tracked={tracked}, sdkRewarded={sdkRewarded}, " +
								 $"playCompleted={_isVideoPlayCompleted}, finalReward={finalReward}");

				string adSource = e?.callbackInfo?.network_firm_id != null ? "Topon_" + e.callbackInfo.network_firm_id : PlatformName;
				string adId = e?.callbackInfo?.network_placement_id ?? "";

				SkyAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object>
				{
					{"isended", finalReward }
				});

				_isVideoPlayCompleted = false; // 重置
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
