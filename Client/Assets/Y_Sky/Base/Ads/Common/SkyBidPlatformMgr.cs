using System;
using System.Collections;
using System.Collections.Generic;
using Sky;
using UnityEngine;

namespace Sky
{
    [Serializable]
    public class AdTypeBidState
    {
        public SkyAdsType AdType;
        public DateTime LastBidSuccessTime;
        
        public AdTypeBidState(SkyAdsType adType)
        {
            AdType = adType;
            LastBidSuccessTime = DateTime.MinValue;
        }
        
        public bool IsBidExpired(int expireInSec)
        {
            // 如果功能关闭或未配置有效过期时间（-1 或 0），则不视为过期
            if (expireInSec <= 0) return false;
            
            // 如果从未成功竞价过（广告还没加载好），不视为过期，避免误触发刷新
            if (LastBidSuccessTime == DateTime.MinValue) return false;
            
            // 检查是否超过过期时间
            TimeSpan timeSinceLastBid = DateTime.Now - LastBidSuccessTime;
            return timeSinceLastBid.TotalSeconds > expireInSec;
        }
    }
    
    [Serializable]
    public class BidPlatformState
    {
        public SkyPlatformType PlatformName;
        public bool IsEnabled = true;
        
        // 每种广告类型的竞价状态
        public Dictionary<SkyAdsType, AdTypeBidState> AdTypeStates = new Dictionary<SkyAdsType, AdTypeBidState>();
        
        public BidPlatformState(SkyPlatformType platformName)
        {
            PlatformName = platformName;
            
            // 初始化所有广告类型的状态
            foreach (SkyAdsType adType in Enum.GetValues(typeof(SkyAdsType)))
            {
                AdTypeStates.Add(adType, new AdTypeBidState(adType));
            }
        }
        
        // 记录特定广告类型的竞价成功
        public void RecordBidSuccess(SkyAdsType adType)
        {
            if (AdTypeStates.TryGetValue(adType, out var state))
            {
                state.LastBidSuccessTime = DateTime.Now;
                Debug.Log($"{PlatformName} 平台 {adType} 广告竞价成功，记录时间: {state.LastBidSuccessTime}");
            }
        }
        
        // 检查特定广告类型的竞价是否过期
        public bool IsBidExpired(SkyAdsType adType, int expireInSec)
        {
            if (!IsEnabled || expireInSec <= 0) return false;
            
            if (AdTypeStates.TryGetValue(adType, out var state))
            {
                return state.IsBidExpired(expireInSec);
            }
            
            return false;
        }
        
        // 获取所有过期的广告类型
        public List<SkyAdsType> GetExpiredAdTypes(int expireInSec)
        {
            List<SkyAdsType> expiredAdTypes = new List<SkyAdsType>();
            
            if (!IsEnabled || expireInSec <= 0) return expiredAdTypes;
            
            foreach (var kvp in AdTypeStates)
            {
                if (kvp.Value.IsBidExpired(expireInSec))
                {
                    expiredAdTypes.Add(kvp.Key);
                }
            }
            
            return expiredAdTypes;
        }
    }
    public class SkyBidPlatformMgr : SkySingleton<SkyBidPlatformMgr>
    {
        private Dictionary<SkyPlatformType, BidPlatformState> _platformStates = new Dictionary<SkyPlatformType, BidPlatformState>();
        private int _bidExpireInSec = -1;

        public void InitializePlatformStates(int expireInSec)
        {
            _bidExpireInSec = expireInSec;
            _platformStates.Clear();
            _platformStates.Add(SkyPlatformType.Admob, new BidPlatformState(SkyPlatformType.Admob));
            _platformStates.Add(SkyPlatformType.LevelPlay, new BidPlatformState(SkyPlatformType.LevelPlay));
            _platformStates.Add(SkyPlatformType.AppLovin, new BidPlatformState(SkyPlatformType.AppLovin));
            _platformStates.Add(SkyPlatformType.Pangle, new BidPlatformState(SkyPlatformType.Pangle));
            _platformStates.Add(SkyPlatformType.TPN, new BidPlatformState(SkyPlatformType.TPN));
            #if UNITY_ANDROID
            _platformStates.Add(SkyPlatformType.Mintegral, new BidPlatformState(SkyPlatformType.Mintegral));
            _platformStates.Add(SkyPlatformType.Kwai, new BidPlatformState(SkyPlatformType.Kwai));
            #endif
        }

        // 记录平台竞价成功
        public void RecordBidSuccess(SkyPlatformType platformName, SkyAdsType adType)
        {
            if (_platformStates.TryGetValue(platformName, out var state))
            {
                state.RecordBidSuccess(adType);
            }
        }
        // 获取所有过期平台
        public Dictionary<SkyPlatformType, List<SkyAdsType>> GetExpiredBids()
        {
            SkyLogUtils.Debug("GetExpiredBids: "+_bidExpireInSec);
            Dictionary<SkyPlatformType, List<SkyAdsType>> expiredBids = new Dictionary<SkyPlatformType, List<SkyAdsType>>();

            if (_bidExpireInSec <= 0) return expiredBids;

            foreach (var kvp in _platformStates)
            {
                List<SkyAdsType> expiredAdTypes = kvp.Value.GetExpiredAdTypes(_bidExpireInSec);
                if (expiredAdTypes.Count > 0)
                {
                    expiredBids.Add(kvp.Key, expiredAdTypes);
                }
            }

            return expiredBids;
        }
    }
}
