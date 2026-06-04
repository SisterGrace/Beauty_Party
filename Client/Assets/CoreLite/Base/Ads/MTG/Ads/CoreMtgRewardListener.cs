using System.Collections;
using System.Collections.Generic;
using MTGAds;
using UnityEngine;
using Up2SDK;

namespace CoreLite
{
    public class CoreLiteMTGRewardBidListener : BidListennning
    {
        private string _placementId;
        private string _unitId;

        public CoreLiteMTGRewardBidListener(string placementId, string unitId)
        {
            _placementId = placementId;
            _unitId = unitId;
        }
        public void onFailed(string msg)
        {
            CoreLogUtils.Debug(" MTG RV Bid Failed: " + msg);
            var slot = CoreMtgMgr.Instance.GetRewardAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogMTGBiddingFailToLoadEvent
            (
                _placementId+"_"+_unitId,
                CorePlatformType.Mintegral.ToString(),
                msg,
                Mathf.Ceil(time),
                false
            );
            CoreMtgMgr.Instance.Retry(CoreLiteAdsType.Rewarded, _placementId, _unitId);
        }

        public void onSuccessed(BidResponsed responsed)
        {
            CoreLogUtils.Debug(" MTG RV Bid Successed: " + responsed.bidToken + " price: " + responsed.price+" placementId: "+_placementId+" unitId: "+_unitId);
            CoreMtgMgr.Instance.OnRewardBidSuccess(_placementId, _unitId, responsed.bidToken, responsed.price);
            var slot = CoreMtgMgr.Instance.GetRewardAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogMTGBiddingLoadedEvent
            (
                "Mintegral",
                CorePlatformType.Mintegral.ToString(),
                _placementId+"_"+_unitId,
                Mathf.Ceil(time) ,
                false,
                CoreDataUtils.StringToDouble(responsed.price) / 1000,
                _unitId
            );
        }
    }

    public class CoreMtgRewardListener : RewardVideoListener
    {
        private string _placementId;
        private string _unitId;
        public CoreMtgRewardListener(string placementId, string unitId)
        {
            _placementId = placementId;
            _unitId = unitId;
        }

        public void onAdClose(MBridgeIds var1, RewardInfo var2)
        {
            CoreLogUtils.Debug(" MTG RV Ad Close: " + var1);
            CoreAdsMgr.Instance._rvCloseCallback?.Invoke(new Dictionary<string, object>
            {
                { "Position", CoreAdsMgr.Instance._rvPos ?? "" },
                { "ad_id", _unitId},
                { "ad_source","Mintegral"},
                { "ad_platform", CorePlatformType.Mintegral.ToString() },
                { "unitId",_unitId}
            });

            CoreMtgMgr.Instance.ClearRewardToken(_placementId, _unitId);  
            CoreMtgMgr.Instance.LoadBidRewardAd(_placementId, _unitId);
        }

        public void onAdShow(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG RV Ad Show: " + var1);
            string pos = CoreAdsMgr.Instance._rvPos ?? "";
            string format = CoreLiteAdsType.Rewarded.ToString();
            int formatNumber = CoreAdsPrefs.GetAndIncrementFormatPlayCount(format);
            int positionNumber = CoreAdsPrefs.GetAndIncrementPositionPlayCount(pos);
            double revenue = CoreMtgMgr.Instance.GetRewardAdRevenueInfo(_placementId, _unitId);
            CoreAnalyticsMgr.TrackAdImpressionEvent(revenue,
                "Mintegral",
                _unitId,
                _unitId,
                format,
                CorePlatformType.Mintegral.ToString(),
                pos,
                formatNumber,
                positionNumber);
            
            CoreAdsEvents.LogRvShowEvent(
                pos,
                _unitId,
                "Mintegral",
                formatNumber,
                CoreAdsPrefs.GetFirstShowRvTime(),
                CorePlatformType.Mintegral.ToString(),
                _unitId
            );
#if UNITY_ANDROID
            CoreAnalyticsMgr.TrackEvent("Up2SDK_MTGAdShow", new Dictionary<string, object>
            {
                { "ad_platform", "MTG" },
                { "ads_type", "REWARD_VIDEO" },
                { "ad_id", _unitId },
                { "ecpm", revenue * 1000 },
                { "show_id", "" }
            });
            Up2SDKManager.Instance.OnMTGAdShow(AdsType.Rewarded, _unitId, revenue * 1000);
#endif
            CoreAdsMgr.Instance._rvShowCallback?.Invoke();
        }

        public void onEndcardShow(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG RV Endcard Show: " + var1);
        }

        public void onLoadSuccess(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG RV Load Success: " + var1);
            CoreMtgMgr.Instance.OnRewardLoadSuccess(_placementId, _unitId);
            var slot = CoreMtgMgr.Instance.GetRewardAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogLoadedEvent(
                _placementId+"_"+_unitId,
                CorePlatformType.Mintegral.ToString(),
                "Mintegral",
                Mathf.Ceil(time),
                AdLoadType.Reward,
                _unitId,
                CoreMtgMgr.Instance.GetRewardAdRevenueInfo(_placementId, _unitId)
            );
        }

        public void onShowFail(MBridgeIds var1, string var2)
        {
            CoreLogUtils.Debug(" MTG RV Show Fail: " + var1 + " " + var2);
            CoreAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>()
            { 
                { "Position", CoreAdsMgr.Instance._rvPos ?? ""},
                { "show_fail_reason",CoreThinkingDataEvents.FormatEventName(var2.ToString())},
                { "ad_platform", CorePlatformType.Mintegral.ToString() },
                { "unitId", _unitId}
            });

            CoreMtgMgr.Instance.ClearRewardToken(_placementId, _unitId);
            CoreMtgMgr.Instance.Retry(CoreLiteAdsType.Rewarded, _placementId, _unitId);

        }

        public void onVideoAdClicked(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG RV Ad Clicked: " + var1);
            CoreAdsEvents.LogClickEvent(
                AdLoadType.Reward,
                CoreMtgMgr.Instance.GetRewardAdRevenueInfo(_placementId, _unitId),
                CoreAdsMgr.Instance._rvPos,
                _unitId,
                "Mintegral",
                CorePlatformType.Mintegral.ToString(),
                _unitId
            );
#if UNITY_ANDROID
            CoreAnalyticsMgr.TrackEvent("Up2SDK_MTGAdClick", new Dictionary<string, object>
            {
                { "ad_platform", "MTG" },
                { "ads_type", "REWARD_VIDEO" },
                { "ad_id", _unitId }
            });
            Up2SDKManager.Instance.OnMTGAdClick(AdsType.Rewarded, _unitId);
#endif
        }

        public void onVideoComplete(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG RV Video Complete: " + var1);
        }

        public void onVideoLoadFail(MBridgeIds var1, string var2)
        {
            CoreLogUtils.Debug(" MTG RV Video Load Fail: " + var1 + " " + var2);
            CoreMtgMgr.Instance.Retry(CoreLiteAdsType.Rewarded, _placementId, _unitId);
             var slot = CoreMtgMgr.Instance.GetRewardAdData(_placementId, _unitId);
            var time = 0.0f;
            if (slot != null) time = Time.realtimeSinceStartup - slot.AdStartLoadTime;

            CoreAdsEvents.LogFailToLoadEvent(
                _placementId+"_"+_unitId,
                CorePlatformType.Mintegral.ToString(),
                "onVideoLoadFail",
                Mathf.Ceil(time),
                AdLoadType.Reward
            );

        }

        public void onVideoLoadSuccess(MBridgeIds var1)
        {
            CoreLogUtils.Debug(" MTG RV Video Load Success: " + var1);
        }
    }
}

