using System;
using System.Collections;
using System.Collections.Generic;
using CoreLite;
using UnityEngine;

namespace CoreLite
{
    [Serializable]
    public class AdTypeBidState
    {
        public CoreLiteAdsType AdType;
        public DateTime LastBidSuccessTime;
        
        public AdTypeBidState(CoreLiteAdsType adType)
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
        public CorePlatformType PlatformName;
        public bool IsEnabled = true;
        
        // 每种广告类型的竞价状态
        public Dictionary<CoreLiteAdsType, AdTypeBidState> AdTypeStates = new Dictionary<CoreLiteAdsType, AdTypeBidState>();
        
        public BidPlatformState(CorePlatformType platformName)
        {
            PlatformName = platformName;
            
            // 初始化所有广告类型的状态
            foreach (CoreLiteAdsType adType in Enum.GetValues(typeof(CoreLiteAdsType)))
            {
                AdTypeStates.Add(adType, new AdTypeBidState(adType));
            }
        }
        
        // 记录特定广告类型的竞价成功
        public void RecordBidSuccess(CoreLiteAdsType adType)
        {
            if (AdTypeStates.TryGetValue(adType, out var state))
            {
                state.LastBidSuccessTime = DateTime.Now;
                Debug.Log($"{PlatformName} 平台 {adType} 广告竞价成功，记录时间: {state.LastBidSuccessTime}");
            }
        }
        
        // 检查特定广告类型的竞价是否过期
        public bool IsBidExpired(CoreLiteAdsType adType, int expireInSec)
        {
            if (!IsEnabled || expireInSec <= 0) return false;
            
            if (AdTypeStates.TryGetValue(adType, out var state))
            {
                return state.IsBidExpired(expireInSec);
            }
            
            return false;
        }
        
        // 获取所有过期的广告类型
        public List<CoreLiteAdsType> GetExpiredAdTypes(int expireInSec)
        {
            List<CoreLiteAdsType> expiredAdTypes = new List<CoreLiteAdsType>();
            
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
    public class CoreBidPlatformMgr : CoreSingleton<CoreBidPlatformMgr>
    {
        private Dictionary<CorePlatformType, BidPlatformState> _platformStates = new Dictionary<CorePlatformType, BidPlatformState>();
        private int _bidExpireInSec = -1;

        public void InitializePlatformStates(int expireInSec)
        {
            _bidExpireInSec = expireInSec;
            _platformStates.Clear();
            _platformStates.Add(CorePlatformType.Admob, new BidPlatformState(CorePlatformType.Admob));
            _platformStates.Add(CorePlatformType.LevelPlay, new BidPlatformState(CorePlatformType.LevelPlay));
            _platformStates.Add(CorePlatformType.AppLovin, new BidPlatformState(CorePlatformType.AppLovin));
            _platformStates.Add(CorePlatformType.Pangle, new BidPlatformState(CorePlatformType.Pangle));
            _platformStates.Add(CorePlatformType.TPN, new BidPlatformState(CorePlatformType.TPN));
            #if UNITY_ANDROID
            _platformStates.Add(CorePlatformType.Mintegral, new BidPlatformState(CorePlatformType.Mintegral));
            _platformStates.Add(CorePlatformType.Kwai, new BidPlatformState(CorePlatformType.Kwai));
            #endif
        }

        // 记录平台竞价成功
        public void RecordBidSuccess(CorePlatformType platformName, CoreLiteAdsType adType)
        {
            if (_platformStates.TryGetValue(platformName, out var state))
            {
                state.RecordBidSuccess(adType);
            }
        }
        // 获取所有过期平台
        public Dictionary<CorePlatformType, List<CoreLiteAdsType>> GetExpiredBids()
        {
            CoreLogUtils.Debug("GetExpiredBids: "+_bidExpireInSec);
            Dictionary<CorePlatformType, List<CoreLiteAdsType>> expiredBids = new Dictionary<CorePlatformType, List<CoreLiteAdsType>>();

            if (_bidExpireInSec <= 0) return expiredBids;

            foreach (var kvp in _platformStates)
            {
                List<CoreLiteAdsType> expiredAdTypes = kvp.Value.GetExpiredAdTypes(_bidExpireInSec);
                if (expiredAdTypes.Count > 0)
                {
                    expiredBids.Add(kvp.Key, expiredAdTypes);
                }
            }

            return expiredBids;
        }
    }
}
