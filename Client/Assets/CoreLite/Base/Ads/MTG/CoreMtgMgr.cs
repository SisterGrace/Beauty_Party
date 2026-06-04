using System;
using System.Collections.Generic;
using MTGAds;


namespace CoreLite
{
    public class RewardAdSlot
    {
        public string PlacementId { get; set; }
        public string UnitId { get; set; }
        public string Token { get; set; }
        public double Price { get; set; }
        public int RetryCount { get; set; }
        public MBBidRewardVideoHandler Handler { get; set; }
        public BidManager BidManager { get; set; }
        public float AdStartLoadTime { get; set; }     
    }

    public class RewardAdConfig
    {
        public string PlacementId { get; set; }
        public string UnitId { get; set; }
    }

    public class InterstitialAdSlot
    {
        public string PlacementId { get; set; }
        public string UnitId { get; set; }
        public string Token { get; set; }
        public double Price { get; set; }
        public int RetryCount { get; set; }
        public MBBidNewInterstitialHandler Handler { get; set; }
        public BidManager BidManager { get; set; }
        public float AdStartLoadTime { get; set; }     
    }

    public class InterstitialAdConfig
    {
        public string PlacementId { get; set; }
        public string UnitId { get; set; }
    }
    
    public class CoreMtgMgr : CoreSingleton<CoreMtgMgr>, ICoreAdService
    {
        public string ClientName => PlatformType.ToString();
        public CorePlatformType PlatformType => CorePlatformType.Mintegral;
        public string _mtg_app_id;
        public string _mtg_app_key;
        public List<RewardAdConfig> rewardAdConfigs = new List<RewardAdConfig>();
        private Dictionary<string, RewardAdSlot> _rewardAdSlots = new Dictionary<string, RewardAdSlot>();
        public List<InterstitialAdConfig> interstitialAdConfigs = new List<InterstitialAdConfig>();
        private Dictionary<string, InterstitialAdSlot> _interstitialAdSlots = new Dictionary<string, InterstitialAdSlot>();
        private bool _isDestroyed;
        private bool _isInitialized;

        public void Destroy()
        {
            _isDestroyed = true;
            _rewardAdSlots.Clear();
            _interstitialAdSlots.Clear();
            CoreAnalyticsMgr.TrackEvent("MTG_Destroy", new Dictionary<string, object>() { { "ad_platform", CorePlatformType.Mintegral.ToString() } });
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            // 直接读取本地参数，配置插屏 / 激励广告位（广告 ID 不再走在线参数）
            _mtg_app_id = CoreAdsConfigParser.GetMTGAppId();
            _mtg_app_key = CoreAdsConfigParser.GetMTGAppKey();
            CoreLogUtils.Debug("[CoreLiteMTG] Initialize appId:" + _mtg_app_id + " appKey:" + _mtg_app_key);

            if (string.IsNullOrEmpty(_mtg_app_id) || string.IsNullOrEmpty(_mtg_app_key))
            {
                CoreLogUtils.Error("[CoreLiteMTG] Initialize aborted: appId or appKey is empty (appId:" + _mtg_app_id + ")");
                return;
            }

            interstitialAdConfigs.Clear();
            rewardAdConfigs.Clear();

            var interstitialUnits = CoreAdsConfigParser.GetMtgInterstitialAdUnits();
            if (interstitialUnits != null && interstitialUnits.Count > 0)
            {
                foreach (var unit in interstitialUnits)
                {
                    if (!string.IsNullOrEmpty(unit.placement_id) && !string.IsNullOrEmpty(unit.unit_id))
                    {
                        AddInterstitialAdConfig(unit.placement_id, unit.unit_id);
                        CoreLogUtils.Debug($"[CoreLiteMTG] Added interstitial ad config: {unit.placement_id}/{unit.unit_id}");
                    }
                }
            }
            else
            {
                CoreLogUtils.Debug("[CoreLiteMTG] No interstitial ad units configured");
            }

            var rewardedUnits = CoreAdsConfigParser.GetMtgRewardedAdUnits();
            if (rewardedUnits != null && rewardedUnits.Count > 0)
            {
                foreach (var unit in rewardedUnits)
                {
                    if (!string.IsNullOrEmpty(unit.placement_id) && !string.IsNullOrEmpty(unit.unit_id))
                    {
                        AddRewardAdConfig(unit.placement_id, unit.unit_id);
                        CoreLogUtils.Debug($"[CoreLiteMTG] Added rewarded ad config: {unit.placement_id}/{unit.unit_id}");
                    }
                }
            }
            else
            {
                CoreLogUtils.Debug("[CoreLiteMTG] No rewarded ad units configured");
            }

            _isDestroyed = false;
            _isInitialized = true;

            CoreLogUtils.Debug("[CoreLiteMTG] MBridgeSDKImpl.Init begin appId:" + _mtg_app_id);
            CoreMtgInitListener listener = new CoreMtgInitListener();
            MBridgeSDKImpl sdk = new MBridgeSDKImpl();
            sdk.Init(_mtg_app_id, _mtg_app_key, listener);
        }

        #region reward

        // 加载所有bid 广告
        public void LoadAllBidRewardAds()
        {
            if (_isDestroyed || rewardAdConfigs.Count == 0) return;

            foreach (var config in rewardAdConfigs)
            {
                LoadBidRewardAd(config.PlacementId, config.UnitId);
            }
        }

        // 加载单个bid 广告
        public void LoadBidRewardAd(string placementId, string unitId)
        {
            if (_isDestroyed || string.IsNullOrEmpty(placementId) || string.IsNullOrEmpty(unitId)) 
                return;

            var key = GetAdsTypeKey(placementId, unitId);
            
            if (!_rewardAdSlots.ContainsKey(key))
            {
                _rewardAdSlots[key] = new RewardAdSlot
                {
                    PlacementId = placementId,
                    UnitId = unitId,
                    RetryCount = 0
                };
            }

            var slot = _rewardAdSlots[key];
            slot.AdStartLoadTime = UnityEngine.Time.realtimeSinceStartup;
            slot.BidManager = new BidManager(placementId, unitId);
            slot.BidManager.setBidListener(new CoreLiteMTGRewardBidListener(placementId, unitId));
            slot.BidManager.bid();
            
            CoreAdsEvents.LogStartLoadEvent(placementId+"_"+unitId, CorePlatformType.Mintegral.ToString(), AdLoadType.Reward);
            CoreLogUtils.Debug($" MTG RV start load bid token for {placementId}/{unitId}");
        }

        // bid 成功回调（不在此处重置 RetryCount，否则 Bid 成功但素材加载失败时重试次数会一直被清零）
        public void OnRewardBidSuccess(string placementId, string unitId, string token, string price)
        {
            if (_isDestroyed) return;
            var key = GetAdsTypeKey(placementId, unitId);
            if (_rewardAdSlots.TryGetValue(key, out var slot))
            {
                slot.Token = token;
                slot.Price = CoreDataUtils.StringToDouble(price) / 1000;
                LoadReward(placementId, unitId);
            }
        }

        // 激励广告加载成功时重置重试计数，便于下一轮失败重试从 0 开始
        public void OnRewardLoadSuccess(string placementId, string unitId)
        {
            if (_isDestroyed) return;
            var key = GetAdsTypeKey(placementId, unitId);
            if (_rewardAdSlots.TryGetValue(key, out var slot))
                slot.RetryCount = 0;
        }

        // bid 成功后加载rv
        public void LoadReward(string placementId, string unitId)
        {
            if (_isDestroyed) return;
            var key = GetAdsTypeKey(placementId, unitId);
            if (!_rewardAdSlots.TryGetValue(key, out var slot)) return;

            slot.Handler = new MBBidRewardVideoHandler(placementId, unitId);
            slot.Handler.setRewardVideoListener(new CoreMtgRewardListener(placementId, unitId));
            slot.Handler.loadFromBid(slot.Token);
            CoreLogUtils.Debug($" MTG RV start load bid rv for {placementId}/{unitId}: {slot.Token}");
        }

        public RewardAdSlot GetRewardAdData(string placementId, string unitId)
        {
            var key = GetAdsTypeKey(placementId, unitId);
            if (_rewardAdSlots.TryGetValue(key, out var slot))
            {
                return slot;
            }
            return null;
        }

        public void ClearRewardToken(string placementId, string unitId)
        {
            var key = GetAdsTypeKey(placementId, unitId);
            if (_rewardAdSlots.TryGetValue(key, out var slot))
            {
                slot.Token = "";
                slot.Price = -1;
            }
        }
        
        public double GetRewardedRevenue()
        {
            var highestSlot = GetHighestValueRewardAd();
            return highestSlot?.Price ?? -1;
        }

        public RewardAdSlot GetHighestValueRewardAd()
        {
            RewardAdSlot highestSlot = null;
            double highestPrice = 0;
            
            foreach (var slot in _rewardAdSlots.Values)
            {
                // 检查广告是否可用
                if (!string.IsNullOrEmpty(slot.Token) && 
                    slot.Handler != null && 
                    slot.Handler.isBidReady())
                {
                    // 比较价格，选择最高的
                    if (slot.Price > highestPrice)
                    {
                        highestPrice = slot.Price;
                        highestSlot = slot;
                    }
                }
            }
            
            if (highestSlot != null)
            {
                CoreLogUtils.Debug($"Highest value RV ad found: {highestSlot.PlacementId}/{highestSlot.UnitId}, price: {highestSlot.Price}");
            }
            else
            {
                CoreLogUtils.Debug("No available RV ads found");
            }
            
            return highestSlot;
        }


        public bool IsRewardedAvailable()
        {
            var availableSlots = new List<RewardAdSlot>();
            foreach (var slot in _rewardAdSlots.Values)
            {
                if (!string.IsNullOrEmpty(slot.Token) && 
                    slot.Handler != null && 
                    slot.Handler.isBidReady())
                {
                    availableSlots.Add(slot);
                }
            }
            return availableSlots.Count > 0;
        }
        public void LoadRewarded()
        {
            
        }


        public void ShowRewarded()
        {
            var highestSlot = GetHighestValueRewardAd();
            if (highestSlot != null && highestSlot.Handler != null)
            {
                CoreLogUtils.Debug($"mtg Showing highest value RV ad: {highestSlot.PlacementId}/{highestSlot.UnitId}, price: {highestSlot.Price}");
                CoreAnalyticsMgr.TrackEvent("RV_AchieveShow_Match", new Dictionary<string, object>()
                {
                    { "Position", CoreAdsMgr.Instance._rvPos },
                    { "ad_platform", CorePlatformType.Mintegral.ToString() }
                });
                highestSlot.Handler.showFromBid();
            }
            else
            {
                CoreLogUtils.Debug("Cannot show highest value RV ad: no available ads");
                CoreAdsMgr.Instance._rvShowFailCallback?.Invoke(new Dictionary<string, object>()
                { 
                    { "Position", CoreAdsMgr.Instance._rvPos ?? ""},
                    { "show_fail_reason","highest_paying_ad_not_found"},
                    { "ad_platform", CorePlatformType.Mintegral.ToString() },
                    { "unitId", ""}
                });
                if (rewardAdConfigs.Count > 0)
                {
                    var config = rewardAdConfigs[0];
                    LoadBidRewardAd(config.PlacementId, config.UnitId);
                    CoreLogUtils.Debug($" MTG RV retry load bid rv for {config.PlacementId}/{config.UnitId}");
                }
            }
        }
        #endregion

        #region interstitial
        // 加载所有bid 广告
        public void LoadAllBidInterstitialAds()
        {
            if (_isDestroyed || interstitialAdConfigs.Count == 0) return;

            foreach (var config in interstitialAdConfigs)
            {
                LoadBidInterstitialAd(config.PlacementId, config.UnitId);
            }
        }

        // 加载单个bid 广告
        public void LoadBidInterstitialAd(string placementId, string unitId)
        {
            if (_isDestroyed || string.IsNullOrEmpty(placementId) || string.IsNullOrEmpty(unitId)) 
                return;

            var key = GetAdsTypeKey(placementId, unitId);
            
            if (!_interstitialAdSlots.ContainsKey(key))
            {
                _interstitialAdSlots[key] = new InterstitialAdSlot
                {
                    PlacementId = placementId,
                    UnitId = unitId,
                    RetryCount = 0
                };
            }

            var slot = _interstitialAdSlots[key];
            slot.AdStartLoadTime = UnityEngine.Time.realtimeSinceStartup;
            slot.BidManager = new BidManager(placementId, unitId);
            slot.BidManager.setBidListener(new CoreLiteMTGInterstitialBidListener(placementId, unitId));
            slot.BidManager.bid();

            CoreAdsEvents.LogStartLoadEvent(placementId+"_"+unitId, CorePlatformType.Mintegral.ToString(), AdLoadType.Interstitial);
            CoreLogUtils.Debug($" MTG iv start load bid token for {placementId}/{unitId}");
        }

        // bid 成功回调（不在此处重置 RetryCount，否则 Bid 成功但素材加载失败时重试次数会一直被清零）
        public void OnInterstitialBidSuccess(string placementId, string unitId, string token, string price)
        {
            if (_isDestroyed) return;
            var key = GetAdsTypeKey(placementId, unitId);
            if (_interstitialAdSlots.TryGetValue(key, out var slot))
            {
                slot.Token = token;
                slot.Price = CoreDataUtils.StringToDouble(price) / 1000;
                LoadInterstitial(placementId, unitId);
            }
        }

        // 插屏广告资源加载成功时重置重试计数，便于下一轮失败重试从 0 开始
        public void OnInterstitialLoadSuccess(string placementId, string unitId)
        {
            if (_isDestroyed) return;
            var key = GetAdsTypeKey(placementId, unitId);
            if (_interstitialAdSlots.TryGetValue(key, out var slot))
                slot.RetryCount = 0;
        }

        // bid 成功后加载rv
        public void LoadInterstitial(string placementId, string unitId)
        {
            if (_isDestroyed) return;
            var key = GetAdsTypeKey(placementId, unitId);
            if (!_interstitialAdSlots.TryGetValue(key, out var slot)) return;

            slot.Handler = new MBBidNewInterstitialHandler(placementId, unitId);
            slot.Handler.setInterstitialVideoListener(new CoreMtgInterstitialListener(placementId, unitId));
            slot.Handler.loadFromBid(slot.Token);
            CoreLogUtils.Debug($" MTG IV start load bid rv for {placementId}/{unitId}: {slot.Token}");
        }

        public InterstitialAdSlot GetInterstitialAdData(string placementId, string unitId)
        {
            var key = GetAdsTypeKey(placementId, unitId);
            if (_interstitialAdSlots.TryGetValue(key, out var slot))
            {
                return slot;
            }
            return null;
        }

        public void ClearInterstitialToken(string placementId, string unitId)
        {
            var key = GetAdsTypeKey(placementId, unitId);
            if (_interstitialAdSlots.TryGetValue(key, out var slot))
            {
                slot.Token = "";
                slot.Price = -1;
            }
        }

        public double GetInterstitialRevenue()
        {
            var highestSlot = GetHighestValueInterstitialAd();
            return highestSlot?.Price ?? -1;
        }

        public InterstitialAdSlot GetHighestValueInterstitialAd()
        {
            InterstitialAdSlot highestSlot = null;
            double highestPrice = 0;
            
            foreach (var slot in _interstitialAdSlots.Values)
            {
                // 检查广告是否可用
                if (!string.IsNullOrEmpty(slot.Token) && 
                    slot.Handler != null && 
                    slot.Handler.isBidReady())
                {
                    // 比较价格，选择最高的
                    if (slot.Price > highestPrice)
                    {
                        highestPrice = slot.Price;
                        highestSlot = slot;
                    }
                }
            }
            
            if (highestSlot != null)
            {
                CoreLogUtils.Debug($"Highest value IV ad found: {highestSlot.PlacementId}/{highestSlot.UnitId}, price: {highestSlot.Price}");
            }
            else
            {
                CoreLogUtils.Debug("No available IV ads found");
            }
            
            return highestSlot;
        }
        public void LoadInterstitial()
        {
            
        }

        public bool IsInterstitialAvailable()
        {
            var availableSlots = new List<InterstitialAdSlot>();
            foreach (var slot in _interstitialAdSlots.Values)
            {
                if (!string.IsNullOrEmpty(slot.Token) && 
                    slot.Handler != null && 
                    slot.Handler.isBidReady())
                {
                    availableSlots.Add(slot);
                }
            }
            return availableSlots.Count > 0;
        }

        public void ShowInterstitial()
        {
            var highestSlot = GetHighestValueInterstitialAd();
            if (highestSlot != null && highestSlot.Handler != null)
            {
                CoreLogUtils.Debug($"mtg Showing highest value IV ad: {highestSlot.PlacementId}/{highestSlot.UnitId}, price: {highestSlot.Price}");
                highestSlot.Handler.showFromBid();
                CoreAnalyticsMgr.TrackEvent("IV_AchieveShow",new Dictionary<string, object>() 
                { 
                    { "Position", CoreAdsMgr.Instance._ivPos},
                    { "ad_platform", CorePlatformType.Mintegral.ToString() },
                    { "unitId",highestSlot.UnitId}
                });
            }
            else
            {
                CoreLogUtils.Debug("Cannot show highest value IV ad: no available ads");
                CoreAnalyticsMgr.TrackEvent("IV_AchieveShow_Fail", new Dictionary<string, object>()
                {
                    { "Position", CoreAdsMgr.Instance._ivPos },
                    { "ad_platform", CorePlatformType.Mintegral.ToString() },
                    { "unitId", "" }
                });
                CoreAdsMgr.Instance._ivCloseCallback?.Invoke();
                if (interstitialAdConfigs.Count > 0)
                {
                    var config = interstitialAdConfigs[0];
                    LoadBidInterstitialAd(config.PlacementId, config.UnitId);
                    CoreLogUtils.Debug($" MTG IV retry load bid iv for {config.PlacementId}/{config.UnitId}");
                }
            }
        }
        #endregion



        #region  Nv
        public double GetNativeRevenue(){return -1;}

        public bool IsNativeAvailable(){return false;}

        public void LoadNative(){}

        public void RemoveNative(){}

        public void ShowNative(){}

        public void LoadNativeFullscreen() { }
        public bool IsNativeFullscreenAvailable() => false;
        public void ShowNativeFullscreen() { }
        public void RemoveNativeFullscreen() { }
        public double GetNativeFullscreenRevenue() => -1;

        public void LoadNativeSmall() { }
        public bool IsNativeSmallAvailable() => false;
        public void ShowNativeSmall() { }
        public void RemoveNativeSmall() { }
        public double GetNativeSmallRevenue() => -1;
        #endregion  

        #region  Bv
        public double GetBannerRevenue(){return -1;}

        public void HideBanner(){}

        public void LoadBanner(){}

        public bool IsBannerReady(){return false;}

        public void ShowBanner(){}

        #endregion

        #region  Sp
        public double GetSplashRevenue(){return -1;}

        public bool IsSplashAvailable(){ return false;}

        public void LoadSplash(){}

        public void ShowSplash(){}

        #endregion

        #region 

        private string GetAdsTypeKey(string placementId, string unitId)
        {
            return $"{placementId}_{unitId}";
        }

        public void AddInterstitialAdConfig(string placementId, string unitId)
        {
            interstitialAdConfigs.Add(new InterstitialAdConfig
            {
                PlacementId = placementId,
                UnitId = unitId,
            });
        }

        public void AddRewardAdConfig(string placementId, string unitId)
        {
            rewardAdConfigs.Add(new RewardAdConfig
            {
                PlacementId = placementId,
                UnitId = unitId,
            });
        }

        private const int MAX_RETRY = 6;
        public void Retry(CoreLiteAdsType adsType, string placementId = null, string unitId = null)
        {
            if (_isDestroyed) return;
            if (adsType == CoreLiteAdsType.Rewarded)
            {
                var key = GetAdsTypeKey(placementId, unitId);
                if (_rewardAdSlots.TryGetValue(key, out var slot))
                {
                    slot.RetryCount++;
                    if (slot.RetryCount >= 6) return;
                    double delay = Math.Pow(2, Math.Min(MAX_RETRY, slot.RetryCount));
                    CoreFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () => { if (!_isDestroyed) LoadBidRewardAd(placementId, unitId); });
                    CoreLogUtils.Debug($" MTG RV Retry for {placementId}/{unitId}, retryCount: {slot.RetryCount}");
                }
            }
            else
            {
                var key = GetAdsTypeKey(placementId, unitId);
                if (_interstitialAdSlots.TryGetValue(key, out var slot))
                {
                    slot.RetryCount++;
                    if (slot.RetryCount >= 6) return;
                    double delay = Math.Pow(2, Math.Min(MAX_RETRY, slot.RetryCount));
                    CoreFuncDelayExecuteUtils.Instance.DelayExecute((float)delay, () => { if (!_isDestroyed) LoadBidInterstitialAd(placementId, unitId); });
                    CoreLogUtils.Debug($" MTG IV Retry for {placementId}/{unitId}, retryCount: {slot.RetryCount}");
                }
            }
        }

        public double GetInterstitialAdRevenueInfo(string placementId = null, string unitId = null)
        {
            if (string.IsNullOrEmpty(placementId) || string.IsNullOrEmpty(unitId))
            {
                return -1;
            }

            var key = GetAdsTypeKey(placementId, unitId);
            if (_interstitialAdSlots.TryGetValue(key, out var slot))
            {
                return slot.Price;
            }
            return -1;
        }

        public double GetRewardAdRevenueInfo(string placementId = null, string unitId = null)
        {
            if (string.IsNullOrEmpty(placementId) || string.IsNullOrEmpty(unitId))
            {
                return -1;
            }

            var key = GetAdsTypeKey(placementId, unitId);
            if (_rewardAdSlots.TryGetValue(key, out var slot))
            {
                return slot.Price;
            }
            return -1;
        }

        #endregion
    }

}