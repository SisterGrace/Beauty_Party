using System.Collections;
using System.Collections.Generic;
using MTGAds;
using UnityEngine;
using Up2SDK;

namespace CoreLite
{
    public class CoreLiteMTGInterstitialBidListener : BidListennning
    {
        private string _placementId;
        private string _unitId;

        public CoreLiteMTGInterstitialBidListener(string placementId, string unitId)
        {
            _placementId = placementId;
            _unitId = unitId;
        }

        public void onFailed(string msg)
        {
            CoreLogUtils.Debug(" MTG IV Bid Failed: " + msg);
            var slot = CoreMtgMgr.Instance.GetInterstitialAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;
            
            CoreAdsEvents.LogMTGBiddingFailToLoadEvent
            (
                _placementId+"_"+_unitId,
                CorePlatformType.Mintegral.ToString(),
                msg,
                Mathf.Ceil(time),
                true
            );
            CoreMtgMgr.Instance.Retry(CoreLiteAdsType.Interstitial, _placementId, _unitId);
        }

        public void onSuccessed(BidResponsed responsed)
        {
            CoreMtgMgr.Instance.OnInterstitialBidSuccess(_placementId, _unitId, responsed.bidToken, responsed.price);
            var slot = CoreMtgMgr.Instance.GetInterstitialAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogMTGBiddingLoadedEvent
            ( 
                "Mintegral",
                CorePlatformType.Mintegral.ToString(),
                _unitId,
                Mathf.Ceil(time),
                true,
                CoreDataUtils.StringToDouble(responsed.price) / 1000,
                _unitId
            );
        }
    }

    public class CoreMtgInterstitialListener : NewInterstitialListener
    {
        private string _placementId;
        private string _unitId;

        public CoreMtgInterstitialListener(string placementId, string unitId)
        {
            _placementId = placementId;
            _unitId = unitId;
        }

        public void onAdClicked(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG IV Ad Clicked: " + var1);
            CoreAdsEvents.LogClickEvent(
                AdLoadType.Interstitial,
                CoreMtgMgr.Instance.GetInterstitialAdRevenueInfo(_placementId, _unitId),
                CoreAdsMgr.Instance._ivPos,
                _unitId,
                "Mintegral",
                CorePlatformType.Mintegral.ToString(),
                _unitId
            );
#if UNITY_ANDROID
            CoreAnalyticsMgr.TrackEvent("Up2SDK_MTGAdClick", new Dictionary<string, object>
            {
                { "ad_platform", "MTG" },
                { "ads_type", "INTERSTITIAL" },
                { "ad_id", _unitId }
            });
            Up2SDKManager.Instance.OnMTGAdClick(AdsType.Interstitial, _unitId);
#endif
        }

        public void onAdClose(MBridgeIds var1, RewardInfo var2)
        {
            CoreLogUtils.Debug(" MTG IV Ad Close: " + var1);
            CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
            CoreMtgMgr.Instance.ClearInterstitialToken(_placementId, _unitId);   
            CoreMtgMgr.Instance.LoadBidInterstitialAd(_placementId, _unitId);  
        }

        public void onAdCloseWithNIReward(MBridgeIds var1, RewardInfo var2)
        {
            CoreLogUtils.Debug(" MTG IV Ad Close with NI Reward: " + var1);
        }

        public void onAdShow(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG IV Ad Show: " + var1);
            string pos = CoreAdsMgr.Instance._ivPos ?? "";
            string format = CoreLiteAdsType.Interstitial.ToString();
            int formatNumber = CoreAdsPrefs.GetAndIncrementFormatPlayCount(format);
            int positionNumber = CoreAdsPrefs.GetAndIncrementPositionPlayCount(pos);
            double revenue = CoreMtgMgr.Instance.GetInterstitialAdRevenueInfo(_placementId, _unitId);
            CoreAnalyticsMgr.TrackAdImpressionEvent(revenue,
                "Mintegral",
                _unitId,
                _unitId,
                format,
                CorePlatformType.Mintegral.ToString(),
                pos,
                formatNumber,
                positionNumber);
            
            CoreAdsMgr.Instance._ivShowCallback?.Invoke(new Dictionary<string, object>()
            {
                { "Position", pos },
                { "ad_id", _unitId},
                { "ad_source","Mintegral"},
                { "ad_platform", CorePlatformType.Mintegral.ToString() },
                { "unitId", _unitId },
                { "revenue", revenue}
            });
#if UNITY_ANDROID
            CoreAnalyticsMgr.TrackEvent("Up2SDK_MTGAdShow", new Dictionary<string, object>
            {
                { "ad_platform", "MTG" },
                { "ads_type", "INTERSTITIAL" },
                { "ad_id", _unitId },
                { "ecpm", revenue * 1000 },
                { "show_id", "" }
            });
            Up2SDKManager.Instance.OnMTGAdShow(AdsType.Interstitial, _unitId, revenue * 1000);
#endif
        }

        public void onEndcardShow(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG IV Endcard Show: " + var1);
        }

        public void onLoadCampaignSuccess(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG IV Load Campaign Success: " + var1);
        }

        public void onResourceLoadFail(MBridgeIds var1, string var2)
        {
            CoreLogUtils.Debug(" MTG IV Resource Load Fail: " + var1 + " " + var2);
            var slot = CoreMtgMgr.Instance.GetInterstitialAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogFailToLoadEvent(
                _placementId+"_"+_unitId,
                CorePlatformType.Mintegral.ToString(),
                "onResourceLoadFail",
                Mathf.Ceil(time),
                AdLoadType.Interstitial
            );

            CoreMtgMgr.Instance.Retry(CoreLiteAdsType.Interstitial, _placementId, _unitId);
        }

        public void onResourceLoadSuccess(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG IV Resource Load Success: " + var1);
            CoreMtgMgr.Instance.OnInterstitialLoadSuccess(_placementId, _unitId);
            var slot = CoreMtgMgr.Instance.GetInterstitialAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogLoadedEvent(
                _unitId,
                CorePlatformType.Mintegral.ToString(),
                "Mintegral",
                Mathf.Ceil(time),
                AdLoadType.Interstitial,
                _unitId,
                CoreMtgMgr.Instance.GetInterstitialAdRevenueInfo(_placementId, _unitId)
            );
        }

        public void onShowFail(MBridgeIds var1, string var2)
        {
            CoreLogUtils.Debug(" MTG IV Show Fail: " + var1 + " " + var2);
            CoreAnalyticsMgr.TrackEvent("IV_ShowFail",new Dictionary<string, object>()
            { 
                { "Position", CoreAdsMgr.Instance._ivPos ?? ""},
                { "show_fail_reason ",CoreThinkingDataEvents.FormatEventName(var2.ToString())},
                { "ad_platform", CorePlatformType.Mintegral.ToString()},
                { "unitId", _unitId}
            });
            CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
            CoreMtgMgr.Instance.ClearInterstitialToken(_placementId, _unitId);
            CoreMtgMgr.Instance.Retry(CoreLiteAdsType.Interstitial, _placementId, _unitId);
        }

        public void onVideoComplete(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG IV Video Complete: " + var1);
        }
    }
}

